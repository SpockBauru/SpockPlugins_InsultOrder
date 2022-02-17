#define USE_ARMIK

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using nnPlugin;
using static BepInEx.IO_ExSlider.Plugin.IO_ExSlider;
using RootMotion;
using RootMotion.FinalIK;
using System.Reflection;
using System.Text.RegularExpressions;

namespace BepInEx.IO_ExSlider.Plugin
{
    public class PlgIK
    {
        public const bool DONOT_IN_ADV = true;
        bool isIkAlredy = false;
        bool isSetIk = false;
        bool flagNext = false;
        GirlCtrl girlCtrl;

        //FullBodyBipedIK bipedIK;
        //LookAtIK lookAtIK;
        public BipedIK bipedIK;
        public PluginBipedIkHelper helperIK;
        MyEyeLookAt myEye;

        public const float DefWeightHead = 0.85f;
        public const float DefWeightBody = 0.0f;
        public static readonly Vector3 DefHeadAxis = new Vector3(-15f, 90f, 0);
        public static Vector3 DefBodyAxis;// = new Vector3(-15f, 90f, 0);

        // 設定値
        bool _enabled = false;
        float _lookAtWeightHead = DefWeightHead;
        float _lookAtWeightBody = DefWeightBody;
        LookAtTarget _lookAtTarget = LookAtTarget.Camera;
        Vector3 _offsetRotHead = Vector3.zero;
        public List<string> disapplyMotionNames = new List<string>() {
            "UC1101A01_M", "UC1101B01_M",  //ネコ泥酔姦　無効にしないと頭がおかしくなる
        };
        public IKConfig ikConfig;

        public bool enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                if (bipedIK)
                    bipedIK.enabled = value;

                if (helperIK)
                    helperIK.enabled = value;

                if (myEye)
                    myEye.enabled = value;
            }
        }

        public enum LookAtTarget
        {
            Camera,
            AntiCamera,
            AntiCameraSoft,
            PlayerFace,
            PlayerBigOne,
            FriendFace,
        }

        public LookAtTarget lookAtTarget 
        {
            get => _lookAtTarget;
            set
            {
                _lookAtTarget = value;
                if (helperIK)
                {
                    helperIK.lookAtTarget = value;
                }
            }
        }


        public bool ready => (enabled && bipedIK && helperIK);

        public void ResetOnNewScene()
        {
            isIkAlredy = false;
            isSetIk = false;
            flagNext = false;

            prevAnime = null;
        }

        public void OnLateUpdate(float ikWeight = 1)
        {
            if (this.ready)
            {
                //if (disapplyMotionNames.Contains(girlCtrl.charaCtrl.getIOAnimeStateString()))
                if (PlgCharaCtrl.CheckAnimeFilteringList(disapplyMotionNames, girlCtrl.charaCtrl.getIOAnimeStateString()))
                    ikWeight = 0f;
                helperIK.ExtLateUpdate(ikWeight);
            }
        }

        public float lookAtHeadWeight
        {
            get => _lookAtWeightHead;

            set
            {
                if (value < 0f)
                    return;

                _lookAtWeightHead = value;

                if (bipedIK)
                    bipedIK.solvers.lookAt.headWeight = value;
            }
        }

        public float lookAtBodyWeight
        {
            get => _lookAtWeightBody;

            set
            {
                if (value < 0f)
                    return;

                _lookAtWeightBody = value;

                if (bipedIK)
                    bipedIK.solvers.lookAt.bodyWeight = value;
            }
        }

        public Vector3 helperOffsetHeadRot
        {
            get => _offsetRotHead;

            set
            {
                _offsetRotHead = value;

                if (helperIK)
                    helperIK.offsetRotHead = value;
            }
        }


        string prevAnime; 
        public void OnUpdate(GirlCtrl girlCtrl)
        {
            // プリセットロード
            if (dicIkPresetByMotions.Count > 0)
            {
                var anime = girlCtrl.charaCtrl.getIOAnimeStateString();
                if (anime != prevAnime)
                {
                    prevAnime = anime;

                    if (dicIkPresetByMotions.ContainsKey(anime))
                    {
                        Debug.Log($"{girlCtrl.ini.id}  {anime}用IKプリセットをロード");
                        this.PresetLoadFrom(dicIkPresetByMotions[anime]);
                    }
                }
            }

            // 設定
            SetupIk(girlCtrl);
        }

        public void SetupIk(GirlCtrl girlCtrl)
        {
            if (!enabled)
                return;

            if (DONOT_IN_ADV && actScene.name == "ADV")
            {
                // ADVシーンはロードしない
                return;
            }

            this.girlCtrl = girlCtrl;

            if (flagNext)
            {
                flagNext = false;
                bipedIK.solvers.lookAt.head.axis = DefHeadAxis;
                DefBodyAxis = bipedIK.solvers.lookAt.spine[0].axis;
            }

            if (isSetIk || isIkAlredy)
                return;

            if (!girlCtrl.isActive())
                return;

            bool isNeko = girlCtrl.ini.id == "Neko";

            var root = girlCtrl.FindBone().transform.Find(isNeko || FlagUsaFH ? "bip01" : "bip01_02");//girlCtrl.FindModel();
            if (!root || !girlCtrl.FindModel().GetComponent<Animator>())
                return;

            var ikroot = girlCtrl.FindModel();
            //var ikroot = root.gameObject; 

            if (ikroot.GetComponent<BipedIK>() || ikroot.GetComponent<LookAtIK>())
                return;

            // IKリファレンス作成
            BipedReferences.AutoDetectParams autoDetectParams = new BipedReferences.AutoDetectParams(false, false);

            BipedReferences references = new BipedReferences();
            BipedReferences.DetectReferencesByNaming(ref references, girlCtrl.FindBone().transform, autoDetectParams);

            // ゲーム用割り当て調整

            // 階層になってないと怒られ
            //references.rightForearm = girlCtrl.FindBone().transform.FindDeep(isNeko ? "bip01 R ForeTwist" : "bip01 R ForeTwist_02", true).transform;
            //references.leftForearm = girlCtrl.FindBone().transform.FindDeep(isNeko ? "bip01 L ForeTwist" : "bip01 L ForeTwist_02").transform;

            //// UpperArmはひねりの制御が難しそうなので肩で代替の場合
            //references.leftUpperArm = references.leftUpperArm.transform.parent;
            //references.rightUpperArm = references.rightUpperArm.transform.parent;

            references.root = ikroot.transform;///girlCtrl.FindBone().transform.Find(girlCtrl.ini.id == "Neko" ? "bip01" : "bip01_02");

            // ボーン回転初期化
            root.localEulerAngles = new Vector3(0, 0, 90);
            references.pelvis.localEulerAngles = new Vector3(-90, 90, 0);

//            references.pelvis.localEulerAngles += new Vector3(0, 0, 85); // 補正

            for (int i = 0; i < references.spine.Length; i++)
                references.spine[i].localEulerAngles = Vector3.zero;

            references.head.localEulerAngles = Vector3.zero;

            //references.rightHand.localEulerAngles = Vector3.zero;
            //references.rightForearm.localEulerAngles = Vector3.zero;
            //references.rightUpperArm.localEulerAngles = Vector3.zero;

            //references.head.localEulerAngles = new Vector3(0, 0, 0);

#if !FULLBODY
            var ikh = helperIK = ikroot.AddComponent<PluginBipedIkHelper>();
            ikh.enabled = false;
            ikh.offsetRotHead = this._offsetRotHead;

            bipedIK = ikroot.AddComponent<BipedIK>();
            bipedIK.references = references;

            // referenceセットしたらヘルパーのOnEnable呼ぶ
            ikh.enabled = true;

            bipedIK.InitiateBipedIK();
            bipedIK.SetToDefaults();
            //bipedIK.solvers.lookAt.target = Camera.main.transform;
            bipedIK.solvers.lookAt.IKPositionWeight = 1f;

            bipedIK.solvers.lookAt.bodyWeight = _lookAtWeightBody;
            bipedIK.solvers.lookAt.headWeight = _lookAtWeightHead;

            // 手足のボーン構成を上手く適用できないので保留…
            //RotationLimitAngle newLimit()
            //{
            //    var limit = new RotationLimitAngle();
            //    limit.limit = 0;
            //    limit.twistLimit = 0;
            //    return limit;
            //}
            //bipedIK.solvers.leftHand.bone2.rotationLimit = newLimit();
            //bipedIK.solvers.rightHand.bone2.rotationLimit = newLimit();
            //bipedIK.solvers.leftHand.bone3.rotationLimit = newLimit();
            //bipedIK.solvers.rightHand.bone3.rotationLimit = newLimit();
            ////bipedIK.solvers.rightHand.bone2.weight = 0;
            ////bipedIK.solvers.leftHand.bone2.weight = 0;
            ////bipedIK.solvers.rightHand.bone3.weight = 0;
            ////bipedIK.solvers.leftHand.bone3.weight = 0;
#if USE_ARMIK

            // 腕のIKセットアップ
            helperIK.SetupArmIk(girlCtrl);

            // ヘルパー内に移動
            //bipedIK.solvers.leftHand.IKPositionWeight = 1f;
            //bipedIK.solvers.leftHand.IKRotationWeight = 1f;
            //bipedIK.solvers.rightHand.IKPositionWeight = 1f;
            //bipedIK.solvers.rightHand.IKRotationWeight = 1f;
#endif

            var rigRoot = new GameObject("plg_IK_Rig_Root_" + girlCtrl.ini.id).transform;
            for (int i = 0; i < bipedIK.solvers.limbs.Length; i++)
            {
                var obj = createRig($"plg_IK_Rig_{i}");
                obj.transform.SetParent(rigRoot, false);
                bipedIK.solvers.limbs[i].target = obj.transform;
            }
            for (int i = 0; i < bipedIK.solvers.limbs.Length; i++)
            {
                var obj = createRig($"plg_Bend_Goal_{i}", true);
                obj.transform.SetParent(rigRoot, false);
                bipedIK.solvers.limbs[i].bendGoal = obj.transform;

                bipedIK.solvers.limbs[i].bendModifier = IKSolverLimb.BendModifier.Goal;
                bipedIK.solvers.limbs[i].bendModifierWeight = 1f;
            }

            GameObject createRig(string name, bool bendGoal = false)
            {
                GameObject obj;

#if TEST
                if (!bendGoal)
                    obj = GameObject.CreatePrimitive(PrimitiveType.Cube);//new GameObject($"plg_IK_Rig_{i}");
                else
                    obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);//new GameObject($"plg_Bend_Goal_{i}");

                obj.name = name;
                obj.SetMaterial(new Material(Shader.Find("Diffuse")));
#else
                obj = new GameObject(name);
#endif                

                return obj;
            }

#endif
#if FULLBODY
            bipedIK = root.gameObject.AddComponent<FullBodyBipedIK>();
            bipedIK.SetReferences(references, root);

            var rigRoot = new GameObject("plg_IK_Rig_Root").transform;
            for(int i = 0; i < bipedIK.solver.effectors.Length; i++)
            {
                var obj = new GameObject($"plg_IK_Rig_{i}");
                obj.transform.SetParent(rigRoot, false);
                bipedIK.solver.effectors[i].target = obj.transform;
            }
            for (int i = 0; i < bipedIK.solver.chain.Length; i++)
            {
                var obj = new GameObject($"plg_Bend_Goal_{i}");
                obj.transform.SetParent(rigRoot, false);
                bipedIK.solver.chain[i].bendConstraint.bendGoal = obj.transform;
            }
            RigUtil.updateRig(root.gameObject);

            
            lookAtIK = root.gameObject.AddComponent<LookAtIK>();
            lookAtIK.solver.SetChain(bipedIK.references.spine, bipedIK.references.head, null, bipedIK.references.root);
            lookAtIK.solver.target = Camera.main.transform;
#endif

            // 視線追従
            myEye = ikroot.AddComponent<MyEyeLookAt>();

            // セッティング
            lookAtBodyWeight = lookAtBodyWeight;
            lookAtHeadWeight = lookAtHeadWeight;
            lookAtTarget = lookAtTarget;
            
            isSetIk = true;
            flagNext = true;
        }


        [Serializable]
        public class SavePlgIK
        {
            public IKConfig ikConfig;
            public SaveLookAt lookAt;

            public SavePlgIK()
            {
                this.ikConfig = new IKConfig();
                this.lookAt = new SaveLookAt();
            }

            public SavePlgIK(PlgIK plgIK)
            {
                Save(plgIK);
            }

            public void Save(PlgIK plgIK)
            {
                this.ikConfig = Clone.Data(plgIK.ikConfig);
                this.lookAt = new SaveLookAt(plgIK);
            }

            public void Load(PlgIK plgIK)
            {
                plgIK.ikConfig = this.ikConfig;
                this.lookAt.Load(plgIK);
            }
        }

        public Dictionary<string, SavePlgIK> dicIkPresetByMotions = new Dictionary<string, SavePlgIK>();
        public KeyValuePair<string, SavePlgIK>[] saveListIkPresetByMotions
        {
            get
            {
                return dicIkPresetByMotions.ToArray();
            }
            set
            {
                dicIkPresetByMotions = value.ToDictionary(x => x.Key, x => x.Value);
            }
        }

        [Serializable]
        public class IKConfig
        {
            public bool ikTargetingDefSclBody = false;
            public bool ikTargetingXtMaster = true;
            public bool enableLegIK = false;

            public Vector3 PosOffsetHandR = Vector3.zero;
            //public Vector3 PosOffsetHandL = Vector3.zero;
            public float PosWeightHand = 1f;
            public float PosWeightFoot = 1f;
            public float RotWeightHand = 1f;
            public float RotWeightFoot = 0.5f;
            public bool limitRigDistance = true;
            public bool useFinger1Offset;

            public IKConfig() { }
        }

        internal void PresetLoadFrom(SavePlgIK o)
        {
            var dislist = this.disapplyMotionNames;

            o.Load(this);

            // ここのプリセットでは不適合モーションリストはロードしない（ごちゃごちゃになるため）
            this.disapplyMotionNames = dislist;
        }

        [Serializable]
        public class SaveLookAt
        {
            public bool lookAtEnabled = false;
            public Vector3 lookAtOffsetHeadRot = Vector3.zero;
            public PlgIK.LookAtTarget lookAtTarget = PlgIK.LookAtTarget.Camera;
            public float lookAtHeadWeight = PlgIK.DefWeightHead;
            public float lookAtBodyWeight = PlgIK.DefWeightBody;
            public string[] disapplyMotionNames = new string[0];

            public SaveLookAt()
            {
                lookAtHeadWeight = PlgIK.DefWeightHead;
                lookAtBodyWeight = PlgIK.DefWeightBody;
            }

            public SaveLookAt(PlgIK ctrl)
            {
                Save(ctrl);
            }

            public void Save(PlgIK plgIK)
            {
                this.lookAtEnabled = plgIK.enabled;
                this.lookAtOffsetHeadRot = plgIK.helperOffsetHeadRot;
                this.lookAtTarget = plgIK.lookAtTarget;
                this.lookAtHeadWeight = plgIK.lookAtHeadWeight;
                this.lookAtBodyWeight = plgIK.lookAtBodyWeight;
                this.disapplyMotionNames = plgIK.disapplyMotionNames.ToArray();
            }

            public void Load(PlgIK plgIK)
            {
                plgIK.enabled = this.lookAtEnabled;
                plgIK.helperOffsetHeadRot = this.lookAtOffsetHeadRot;
                plgIK.lookAtTarget = this.lookAtTarget;
                plgIK.lookAtHeadWeight = this.lookAtHeadWeight;
                plgIK.lookAtBodyWeight = this.lookAtBodyWeight;
                plgIK.disapplyMotionNames = this.disapplyMotionNames.ToList();
            }
        }


        [DisallowMultipleComponent]
        public class PluginBipedIkHelper : MonoBehaviour
        {
            BipedIK bipedIK;
            public Vector3 offsetRotHead = Vector3.zero;
            public float mesen = 0f;
            public LookAtTarget lookAtTarget = LookAtTarget.Camera;
            Transform target = null;
            Transform head = null;
            FieldInfo fiSkip = typeof(SolverManager).GetField("skipSolverUpdate", BindingFlags.NonPublic | BindingFlags.Instance);

            BipedReferences twistReferences;
            public RigUtil.LimbsIKTargets limbsIKTargets = new RigUtil.LimbsIKTargets();

            RotLR upperArmTwist = new RotLR();
            RotLR foreArmTwist = new RotLR();
            TwistBones addTwists = new TwistBones();
            PlgIK plgIK;
            RotLR calfTwist = new RotLR();
            RotLR thighTwist = new RotLR();
            //TrLR finger2 = new TrLR();
            TransformTR[] finger1Poss = new TransformTR[(int)AvatarIKGoal.RightHand + 1]; 

            bool skipRigUpdate = false;

            class RotLR
            {
                public Quaternion L;
                public Quaternion R;
            }

            class TwistBones
            {
                public TrLR HS_hiji = new TrLR();
                public TrLR HS_hiza = new TrLR();
            }

            class TrLR
            {
                public Transform L;
                public Transform R;
            }


            public void SetIkEnable(bool enable)
            {
                bipedIK.enabled = enable;
                this.enabled = enable;
            }

            void OnEnable()
            {
                bipedIK = gameObject.GetComponent<BipedIK>();
                if (!bipedIK)
                    return;

                //Debug.Log("OnEnable " + bipedIK.gameObject.name);
                if (!target)
                {
                    target = new GameObject("plgLookAtTarget_"+bipedIK.gameObject.transform.root.name).transform;
                }
                bipedIK.solvers.lookAt.target = target;

                //head = bipedIK.solvers.lookAt.head.transform;
                head = bipedIK.references.head;
                if (!head)
                    Debug.LogWarning("lookAt ヘッドがありません " + bipedIK.gameObject.name);
            }

            public void SetupArmIk(GirlCtrl girlCtrl)
            {
                plgIK = girlCtrl.plgIK;

                bool isNeko = girlCtrl.ini.id == "Neko" || IO_ExSlider.FlagUsaFH;

                twistReferences = new BipedReferences();

                twistReferences.rightForearm = girlCtrl.FindBone().transform.FindDeep(isNeko ? "bip01 R ForeTwist" : "bip01 R ForeTwist_02", true).transform;
                twistReferences.leftForearm = girlCtrl.FindBone().transform.FindDeep(isNeko ? "bip01 L ForeTwist" : "bip01 L ForeTwist_02", true).transform;

                twistReferences.rightUpperArm = girlCtrl.FindBone().transform.FindDeep(isNeko ? "bip01 RUpArmTwist" : "bip01 RUpArmTwist_02", true).transform;
                twistReferences.leftUpperArm = girlCtrl.FindBone().transform.FindDeep(isNeko ? "bip01 LUpArmTwist" : "bip01 LUpArmTwist_02", true).transform;
            
                addTwists.HS_hiji.L = girlCtrl.FindBone().transform.FindDeep(isNeko ? "HS_hijiL" : "HS_hijiL_02", true).transform;
                addTwists.HS_hiji.R = girlCtrl.FindBone().transform.FindDeep(isNeko ? "HS_hijiR" : "HS_hijiR_02", true).transform;

                // 脚
                twistReferences.rightCalf = girlCtrl.FindBone().transform.FindDeep(isNeko ? "bip01 RCalfTwist" : "bip01 RCalfTwist_02", true).transform;
                twistReferences.leftCalf = girlCtrl.FindBone().transform.FindDeep(isNeko ? "bip01 LCalfTwist" : "bip01 LCalfTwist_02", true).transform;

                twistReferences.rightThigh = girlCtrl.FindBone().transform.FindDeep(isNeko ? "bip01 RThighTwist" : "bip01 RThighTwist_02", true).transform;
                twistReferences.leftThigh = girlCtrl.FindBone().transform.FindDeep(isNeko ? "bip01 LThighTwist" : "bip01 LThighTwist_02", true).transform;

                addTwists.HS_hiza.L = girlCtrl.FindBone().transform.FindDeep(isNeko ? "HS_hizaL" : "HS_hizaL_02", true).transform;
                addTwists.HS_hiza.R = girlCtrl.FindBone().transform.FindDeep(isNeko ? "HS_hizaR" : "HS_hizaR_02", true).transform;

                // 指
                //finger2.L = FindFinger2(this.bipedIK.references.leftHand.transform);
                //finger2.R = FindFinger2(this.bipedIK.references.rightHand.transform);
            }

            Transform FindFinger1(Transform tr)
            {
                for(int i=0; i<tr.childCount; i++)
                {
                    var c = tr.GetChild(i);
                    if (c.name.Contains("Finger1") || c.name.Contains("Toe1"))
                    {
                        return c;
                    }
                }
                return null;
            }

            void Update()
            {
                if (!bipedIK)
                    return;

                // SolverのLateUpdateを無効化
                //((SolverManager)bipedIK).SetNonPublicField<bool>("skipSolverUpdate", true);
                fiSkip.SetValue(bipedIK, true);

                // LookAt
                //bipedIK.SetLookAtPosition(bipedIK.solvers.lookAt.target.position);
            }

            // 実行順を確実にするため外部から呼ぶ
            public void ExtLateUpdate(float ikWeight = 1)
            {
                if (!bipedIK || !Camera.main)
                    return;

                Transform tr = null;
                switch (lookAtTarget)
                {
                    case LookAtTarget.Camera:
                        target.position = Camera.main.transform.position;
                        tr = target;
                        break;

                    case LookAtTarget.AntiCameraSoft:
                        //がちすぎるので target.position = head.position + (head.position - Camera.main.transform.position);

                        target.position = Camera.main.transform.position;
                        //target.RotateAround(bipedIK.solvers.lookAt.head.transform.position, bipedIK.solvers.lookAt.head.transform.right, 180);
                        target.RotateAround(bipedIK.solvers.lookAt.spine[2].transform.position, bipedIK.solvers.lookAt.spine[2].transform.right, 180);
                        tr = target;
                        break;

                    case LookAtTarget.AntiCamera:
                        if (!head)
                            head = bipedIK.solvers.lookAt.head.transform;
                        target.position = head.position + (head.position - Camera.main.transform.position);
                        tr = target;
                        break;

                    case LookAtTarget.PlayerFace:
                        {
                            var go = Extentions.FindSp("PC00/PC0000/HS_kiten_PC/PC00Bip/PC00Bip Pelvis/PC00Bip Spine/PC00Bip Spine1/PC00Bip Spine2/PC00Bip Neck/PC00Bip Head");
                            if (go)
                            {
                                target.position = go.transform.position;
                                tr = target;
                            }
                        }
                        break;

                    case LookAtTarget.PlayerBigOne:
                        {
                            var go = Extentions.FindSp("PC00/PC0000/HS_kiten_PC/PC00Bip/PC00Bip Pelvis/HS00_penis");
                            if (go)
                            {
                                target.position = go.transform.position;
                                tr = target;
                            }
                        }
                        break;

                    case LookAtTarget.FriendFace:
                        {
                            if (!head) 
                                head = bipedIK.solvers.lookAt.head.transform;
                            var tgtid = head.root.name == "CH01" ? 1 : 0;
                            var go = SysVoiceMgr.GetGirlHead(tgtid);
                            if (go)
                            {
                                target.position = go.transform.position;
                                tr = target;
                            }
                        }
                        break;
                }
                //bipedIK.solvers.lookAt.target = tr;
                if (!tr)
                    bipedIK.solvers.lookAt.IKPositionWeight = 0;
                else
                    bipedIK.solvers.lookAt.IKPositionWeight = ikWeight;

                // 腕のIK
                bipedIK.solvers.rightHand.IKPositionWeight = 0;
                bipedIK.solvers.leftHand.IKPositionWeight = 0;
                bipedIK.solvers.rightFoot.IKPositionWeight = 0f;
                bipedIK.solvers.leftFoot.IKPositionWeight = 0f;
#if USE_ARMIK
                bool armIk = false;
                if (bipedIK.solvers.lookAt.bodyWeight > 0 || limbsIKTargets.ikWeight > 0 || this.skipRigUpdate)
                {
                    if (limbsIKTargets.ikWeight > 0)
                    {
                        bipedIK.solvers.rightHand.IKPositionWeight = limbsIKTargets.ikWeight;
                        bipedIK.solvers.leftHand.IKPositionWeight = limbsIKTargets.ikWeight;
                    }
                    else
                    {
                        //bipedIK.solvers.rightHand.IKPositionWeight = bipedIK.solvers.lookAt.bodyWeight;
                        //bipedIK.solvers.leftHand.IKPositionWeight = bipedIK.solvers.lookAt.bodyWeight;
                        //bipedIK.solvers.rightHand.IKRotationWeight = bipedIK.solvers.rightHand.IKPositionWeight;
                        //bipedIK.solvers.leftHand.IKRotationWeight = bipedIK.solvers.leftHand.IKPositionWeight;
                        bipedIK.solvers.rightHand.IKPositionWeight = 1f;
                        bipedIK.solvers.leftHand.IKPositionWeight = 1f;
                    }

                    if (bipedIK.enabled &&
                        (bipedIK.solvers.rightHand.IKPositionWeight > 0 && bipedIK.solvers.leftHand.IKPositionWeight > 0)
                      )
                    {
                        // IKターゲット等のセット
                        if (!this.skipRigUpdate)
                        {
                            RigUtil.updateBipedRig(bipedIK, limbsIKTargets, this.plgIK.ikConfig.limitRigDistance);

                            // 指のオフセット
                            this.StoreFingerPos();
                        }
                        else
                        {
                            this.skipRigUpdate = false;
                        }

                        // リグオフセット
                        ApplyRigOffset();

                        // 腕IK実行フラグ
                        armIk = true;
                        
                        // IKターゲットは毎フレームクリア
                        limbsIKTargets.ikWeight = 0f;

                        // ソルバーをボーンと一致させる
                        //bipedIK.solvers.leftHand.bone1.UpdateSolverState();
                        //bipedIK.solvers.leftHand.bone2.UpdateSolverState();
                        //bipedIK.solvers.leftHand.bone3.UpdateSolverState();
                        //bipedIK.solvers.rightHand.bone1.UpdateSolverState();
                        //bipedIK.solvers.rightHand.bone2.UpdateSolverState();
                        //bipedIK.solvers.rightHand.bone3.UpdateSolverState();
                    }

                    // ツイスト量を記録
                    upperArmTwist.L = Quaternion.Inverse(bipedIK.references.leftUpperArm.localRotation) * twistReferences.leftUpperArm.localRotation;
                    upperArmTwist.R = Quaternion.Inverse(bipedIK.references.rightUpperArm.localRotation) * twistReferences.rightUpperArm.localRotation;
                    foreArmTwist.L = Quaternion.Inverse(bipedIK.references.leftForearm.localRotation) * twistReferences.leftForearm.localRotation;
                    foreArmTwist.R = Quaternion.Inverse(bipedIK.references.rightForearm.localRotation) * twistReferences.rightForearm.localRotation;

                    if (plgIK.ikConfig.enableLegIK)
                    {
                        bipedIK.solvers.rightFoot.IKPositionWeight = bipedIK.solvers.rightHand.IKPositionWeight;
                        bipedIK.solvers.leftFoot.IKPositionWeight = bipedIK.solvers.leftHand.IKPositionWeight;

                        thighTwist.L = Quaternion.Inverse(bipedIK.references.leftThigh.localRotation) * twistReferences.leftThigh.localRotation;
                        thighTwist.R = Quaternion.Inverse(bipedIK.references.rightThigh.localRotation) * twistReferences.rightThigh.localRotation;
                        calfTwist.L = Quaternion.Inverse(bipedIK.references.leftCalf.localRotation) * twistReferences.leftCalf.localRotation;
                        calfTwist.R = Quaternion.Inverse(bipedIK.references.rightCalf.localRotation) * twistReferences.rightCalf.localRotation;
                    }
                }

                // 回転ウェイト
                bipedIK.solvers.rightHand.IKRotationWeight = bipedIK.solvers.rightHand.IKPositionWeight;
                bipedIK.solvers.leftHand.IKRotationWeight = bipedIK.solvers.leftHand.IKPositionWeight;

                bipedIK.solvers.rightFoot.IKRotationWeight = bipedIK.solvers.rightFoot.IKPositionWeight;
                bipedIK.solvers.leftFoot.IKRotationWeight = bipedIK.solvers.leftFoot.IKPositionWeight;

                if (armIk)
                {
                    // ユーザーウェイト補正
                    bipedIK.solvers.rightHand.IKPositionWeight *= this.plgIK.ikConfig.PosWeightHand;
                    bipedIK.solvers.leftHand.IKPositionWeight *= this.plgIK.ikConfig.PosWeightHand;
                    bipedIK.solvers.rightFoot.IKPositionWeight *= this.plgIK.ikConfig.PosWeightFoot;
                    bipedIK.solvers.leftFoot.IKPositionWeight *= this.plgIK.ikConfig.PosWeightFoot;

                    bipedIK.solvers.rightHand.IKRotationWeight *= this.plgIK.ikConfig.RotWeightHand;
                    bipedIK.solvers.leftHand.IKRotationWeight *= this.plgIK.ikConfig.RotWeightHand;
                    bipedIK.solvers.rightFoot.IKRotationWeight *= this.plgIK.ikConfig.RotWeightFoot;
                    bipedIK.solvers.leftFoot.IKRotationWeight *= this.plgIK.ikConfig.RotWeightFoot;
                }
#endif

                // IK解決
                bipedIK.UpdateSolverExternal();
                // オフセット
                bipedIK.references.head.localRotation *= Quaternion.Euler(offsetRotHead);


#if USE_ARMIK
                if (armIk && twistReferences != null)
                {
                    // UpperArmは良い方法がわからん…
                    twistReferences.leftUpperArm.localRotation = bipedIK.references.leftUpperArm.localRotation * upperArmTwist.L;
                    twistReferences.rightUpperArm.localRotation = bipedIK.references.rightUpperArm.localRotation * upperArmTwist.R;
                    // x, y はForearmと同じ0の方が簡易的だけど安定？
                    twistReferences.leftForearm.localRotation = bipedIK.references.leftForearm.localRotation * foreArmTwist.L;
                    twistReferences.rightForearm.localRotation = bipedIK.references.rightForearm.localRotation * foreArmTwist.R;

                    // 肘はzにForearmの子なので角度の0.5を突っ込むだけ,IKならz以外曲げないはず?
                    addTwists.HS_hiji.L.localEulerAngles = new Vector3(0f, 0f, -bipedIK.references.leftForearm.localEulerAngles.z / 2f);
                    addTwists.HS_hiji.R.localEulerAngles = new Vector3(0f, 180f, bipedIK.references.rightForearm.localEulerAngles.z / 2f);


                    if (plgIK.ikConfig.enableLegIK)
                    {
                        // 脚
                        twistReferences.leftThigh.localRotation = bipedIK.references.leftThigh.localRotation * thighTwist.L;
                        twistReferences.rightThigh.localRotation = bipedIK.references.rightThigh.localRotation * thighTwist.R;
                        twistReferences.leftCalf.localRotation = bipedIK.references.leftCalf.localRotation * calfTwist.L;
                        twistReferences.rightCalf.localRotation = bipedIK.references.rightCalf.localRotation * calfTwist.R;

                        // 膝
                        addTwists.HS_hiza.L.localEulerAngles = new Vector3(0f, 0f, -bipedIK.references.leftCalf.localEulerAngles.z / 2f);
                        addTwists.HS_hiza.R.localEulerAngles = new Vector3(0f, 0f, -bipedIK.references.rightCalf.localEulerAngles.z / 2f);
                    }

                    // 手抜き過ぎて駄目だったやつ
                    //else
                    //{
                    //    var v3 = twistReferences.leftForearm.localEulerAngles;
                    //    twistReferences.leftForearm.localEulerAngles = new Vector3(v3.x, v3.y, bipedIK.references.leftForearm.localEulerAngles.z);

                    //    v3 = twistReferences.rightForearm.localEulerAngles;
                    //    twistReferences.rightForearm.localEulerAngles = new Vector3(v3.x, v3.y, bipedIK.references.rightForearm.localEulerAngles.z);
                    //}
                }
#endif
            }

            public void ApplyRigOffset()
            {
                if (this.plgIK.ikConfig.useFinger1Offset)
                {
                    // 指の付け根の位置補正用
                    var limbs = this.bipedIK.solvers.limbs;

                    for (int i = 0; i < limbs.Length; i++)
                    {
                        var e = limbs[i];
                        if (finger1Poss[i] != null)
                        {
                            //Debug.Log("f1k2 " + e.bone3.transform.name);
                            var bf = FindFinger1(e.bone3.transform);
                            e.target.transform.position += (finger1Poss[i].position - bf.position) - (e.target.transform.position - e.bone3.transform.position);
                        }
                    }
                }

                if (this.plgIK.ikConfig.PosOffsetHandR != Vector3.zero)
                {
                    var ofsR = this.plgIK.ikConfig.PosOffsetHandR;
                    bipedIK.solvers.rightHand.target.localPosition += bipedIK.solvers.rightHand.target.localRotation * ofsR;
                    var ofsL = new Vector3(ofsR.x, ofsR.y, -ofsR.z);
                    bipedIK.solvers.leftHand.target.localPosition += bipedIK.solvers.leftHand.target.localRotation * ofsL;
                }
            }

            public void StoreFingerPos()
            {
                if (this.plgIK.ikConfig.useFinger1Offset)
                {
                    //Debug.Log("f1k0-2");

                    // 指の付け根の位置補正用
                    var limbs = this.bipedIK.solvers.limbs;
                    var tgts = this.limbsIKTargets;
                    //Debug.Log("f1k0-1");

                    for (int i = 0; i < limbs.Length; i++)
                    {
                        finger1Poss[i] = null;

                        var e = limbs[i];
                        Transform tr;

                        if (tgts.ikWeight > 0 && tgts.limbs[i] != null && tgts.limbs[i].ikTarget)
                        {
                            //Debug.Log("f1k0");
                            tr = tgts.limbs[i].ikTarget;
                        }
                        else
                        {
                            tr = e.bone3.transform;
                        }

                        if (tr && e.target
                            && e.target.transform.position == tr.position
                        )
                        {
                            var tf = FindFinger1(tr);
                            if (tf)
                            {
                                //Debug.Log("f1k1");
                                finger1Poss[i] = new TransformTR(tf);
                            }
                            //if (tf)
                            //{
                            //    Debug.Log("f1k2");

                            //    var bf = FindFinger2(e.bone3.transform);
                            //    e.target.transform.position += (tf.position - bf.position);
                            //}
                        }
                    }
                }
            }

            public void ExSetLimbsRigPosition()
            {
                if (this.limbsIKTargets.ikWeight <= 0f)
                {
                    // 外部にターゲッティングがされてなければ
                    RigUtil.updateBipedRig(bipedIK, limbsIKTargets, this.plgIK.ikConfig.limitRigDistance);

                    // 指のオフセット
                    this.StoreFingerPos();

                    // リグ位置更新一回休み
                    this.skipRigUpdate = true;
                }

                //this.limbsIKTargets.Init();
                //this.limbsIKTargets.limbs[(int)AvatarIKGoal.LeftFoot].ikTarget = bipedIK.references.leftFoot;
                //this.limbsIKTargets.limbs[(int)AvatarIKGoal.RightHand].ikTarget = bipedIK.references.rightHand;
                //this.limbsIKTargets.limbs[(int)AvatarIKGoal.LeftFoot].ikTarget = bipedIK.references.leftFoot;
                //this.limbsIKTargets.limbs[(int)AvatarIKGoal.RightFoot].ikTarget = bipedIK.references.rightFoot;
            }
        }

    }


    public class RigUtil
    {
        public class IKTarget
        {
            public Transform ikTarget;
            public Transform bendGoel;
        }
        public class LimbsIKTargets
        {
            public float ikWeight = 0f;

            // limbsの並び順はAvatarIKGoalと共通
            public IKTarget[] limbs = new IKTarget[(int)AvatarIKGoal.RightHand + 1];

            public void Reset()
            {
                ikWeight = 0f;
                //limbs = new IKTarget[(int)AvatarIKGoal.RightHand + 1];
                for (int i = 0; i < limbs.Length; i++)
                {
                    limbs[i] = null;
                }
            }

            public void Init()
            {
                for (int i = 0; i < limbs.Length; i++)
                {
                    if (limbs[i] == null)
                        limbs[i] = new IKTarget();

                    limbs[i].ikTarget = null;
                    limbs[i].bendGoel = null;
                }
            }
        }

        public static void updateBipedRig(BipedIK ik, LimbsIKTargets tgts, bool limitDistance)
        {
            if (ik)
            {
                // FinalIKリグ処理
                var limbs = ik.solvers.limbs;
                // IKポイント
                //foreach (var e in limbs)
                for (int i = 0; i < limbs.Length; i++)
                {
                    var e = limbs[i];
                    Transform tr;

                    if (tgts.ikWeight > 0 && tgts.limbs[i] != null && tgts.limbs[i].ikTarget)
                    {
                        tr = tgts.limbs[i].ikTarget;
                    }
                    else
                    {
                        tr = e.bone3.transform;
                    }

                    if (e.target && tr)
                    {
                        //e.ResetOffset(solver);
                        e.target.transform.rotation = tr.rotation;
                        e.target.transform.position = tr.position;

                        if (limitDistance && tr != e.bone3.transform)
                        {
                            var dis0 = Vector3.Magnitude(e.bone3.transform.position - e.bone2.transform.position) + Vector3.Magnitude(e.bone2.transform.position - e.bone1.transform.position);
                            var dis1 = Vector3.Magnitude(tr.position - e.bone1.transform.position);
                            var f = dis1 / dis0;
                            if (f > 0.995f)
                            {
                                e.target.transform.position = (tr.position - e.bone1.transform.position) * (0.995f / f) + e.bone1.transform.position;
                            }
                        }
                    }
                    else
                    {
                        e.IKPositionWeight = 0f;
                        e.IKRotationWeight = 0f;
                    }
                }

                // 関節部の方向
                //foreach (var c in limbs)
                for (int i = 0; i < limbs.Length; i++)
                {
                    var c = limbs[i];
                    var b = c.bendGoal;
                    Transform tgt;

                    if (tgts.ikWeight > 0 && tgts.limbs[i] != null && tgts.limbs[i].bendGoel)
                    {
                        tgt = tgts.limbs[i].bendGoel;
                    }
                    else
                    {
                        tgt = c.bone2.transform;
                    }

                    //if (!b || !c.bone3.transform || !tgt)
                    //    continue;

                    if (tgt && b && c.bone1.transform && c.bone3.transform)
                    {
                        b.transform.rotation = tgt.rotation; //c.bone2.transform.rotation;
                        b.transform.position = tgt.position; //c.bone2.transform.position; //多少割り増しした方が安定するけど再現性重視なら省略
                        
                        c.bendModifier = IKSolverLimb.BendModifier.Goal;
                        //c.bendModifierWeight = 1f;

                        if (c.bone1.transform && c.bone3.transform)
                        {
                            var bone1 = c.bone1.transform;
                            var bone2 = tgt; // c.bone2.transform;
                            var bone3 = c.bone3.transform;
                            // 割り増しもやってみる
                            // ベクトルbone1→3上にbone2から降ろした垂線の交点（最も近い点）
                            var vec = Vector3.Project(bone2.position - bone1.position,
                                bone3.position - bone1.position)
                                + bone1.position;

                            // 交点からbone2までの距離を上乗せ（とりあえず2倍）
                            var vec2 = (bone2.position - vec) * 2f;

                            var mag = vec2.magnitude;
                            if (mag < 1f) // 大きさが1以下なら
                            {
                                vec2 = vec2 * (1f / mag);  // 大きさを1に
                            }

                            // 割り増し完了のはず
                            b.transform.position = vec + vec2;
                        }
                    }
                    else
                    {
                        c.bendModifier = IKSolverLimb.BendModifier.Animation;
                    }
                }
            }
        }

        public static void updateRig(GameObject biped)
        {
            var ik = biped.GetComponent<FullBodyBipedIK>();
            if (ik)
            {
                // FinalIKリグ処理
                var solver = ik.solver;
                // IKポイント
                foreach (var e in solver.effectors)
                {
                    if (e.target && e.bone)
                    {
                        //e.ResetOffset(solver);
                        e.target.transform.rotation = e.bone.rotation;
                        e.target.transform.position = e.bone.position;
                    }
                }

                // 関節部の方向
                foreach (var c in solver.chain)
                {
                    var b = c.bendConstraint.bendGoal;
                    if (!b || !c.bendConstraint.bone3)
                        continue;

                    if (c.bendConstraint.bone2)
                    {
                        b.transform.rotation = c.bendConstraint.bone2.rotation;
                        b.transform.position = c.bendConstraint.bone2.position; //多少割り増しした方が安定するけど再現性重視なら省略

                        if (c.bendConstraint.bone1 && c.bendConstraint.bone3)
                        {
                            // 割り増しもやってみる
                            // ベクトルbone1→3上にbone2から降ろした垂線の交点（最も近い点）
                            var vec = Vector3.Project(c.bendConstraint.bone2.position - c.bendConstraint.bone1.position,
                                c.bendConstraint.bone3.position - c.bendConstraint.bone1.position)
                                + c.bendConstraint.bone1.position;

                            // 交点からbone2までの距離を上乗せ（とりあえず1.5倍）
                            var vec2 = (c.bendConstraint.bone2.position - vec) * 1.5f;

                            if (vec2.magnitude < 0.2f) // 大きさが0.2以下なら
                            {
                                vec2 = vec2 * (0.2f / vec2.magnitude);  // 大きさを0.2に
                            }

                            // 割り増し完了のはず
                            b.transform.position = vec + vec2;
                        }
                    }
                }
            }
        }
    }

    [DisallowMultipleComponent]
    public class MyEyeLookAt : MonoBehaviour
    {
        Transform dummyEye;
        Vector3 eyeEuler;
        Vector3 eyeEuler2;
        Dictionary<string, int> dicShapes = new Dictionary<string, int>();
        Transform dummyHead;
        Transform dummyEyeTgt;
        Transform head;

        // シェイプキーを使う場合
        //BlendUtil blendUtil;

        public const float SPEED_DEF = 10f;
        public const float MAXSPEED_DEF = 15f;
        public const float WEIGHT_DEF = 2.8f;
        public static float Speed = SPEED_DEF;
        public static float MaxSpeed = MAXSPEED_DEF;
        public static float Weight = WEIGHT_DEF;
        public readonly static Vector3 DummyOffsetDef = new Vector3(-90, 90, 0);
        public static Vector3 DummyOffset = DummyOffsetDef;

        public static bool NoCheckIkWeight = false;
        public static bool InhibitHitomiAnime = true;
        public static bool Enabled = true;

        // ゲーム固有オブジェクト
        GameObject mesenL, mesenR;
        GameObject siromeL, siromeR;
        static readonly string[] hitomiNames = { "HS_Hitomi[LR]00", "HS_Hitomi[LR]01", "HS_Hitomi[LR]02", "HS_Hitomi[LR]03" };
        Transform[] hitomisL, hitomisR;


        bool initend = false;
        RootMotion.FinalIK.BipedIK lookAtIK;
        private void Awake()
        {
            Init(gameObject.transform);
        }

        public void Init(Transform tgt)
        {
            if (initend)
                return;

            //blendUtil = new BlendUtil(gameObject);

            initend = true;
            var lookik = tgt.GetComponent<BipedIK>();
            if (!lookik)
                return;

            lookAtIK = lookik;
            //eyeDummy = tgt.Find(HEAD_PATH).gameObject;

            //head = lookAtIK.references.head;
            head = lookAtIK.solvers.lookAt.head.transform;
            dummyHead = new GameObject("headDummyPlg").transform;
            dummyHead.transform.SetParent(tgt, false);

            dummyEye = new GameObject("eyeDummyPlg").transform;
            //eyeDummy.transform.SetParent(tgt.Find(HEAD_PATH), false);
            dummyEye.SetParent(dummyHead, false);

            dummyEye.localPosition = Vector3.zero;
            dummyEye.localRotation = Quaternion.identity;

            dummyEyeTgt = new GameObject("eyeDummyTgt").transform;
            //eyeDummy.transform.SetParent(tgt.Find(HEAD_PATH), false);
            dummyEyeTgt.SetParent(dummyHead, false);

            //mesenR = Extentions.FindSp("CH01/CH0001/HS_kiten/bip01/bip01 Pelvis/bip01 Spine/bip01 Spine1/bip01 Spine2/bip01 Neck/bip01 Head/HS_HeadScale/HS_Head/HS_eyePosi/HS_mesenR");
            //mesenL = Extentions.FindSp("CH01/CH0001/HS_kiten/bip01/bip01 Pelvis/bip01 Spine/bip01 Spine1/bip01 Spine2/bip01 Neck/bip01 Head/HS_HeadScale/HS_Head/HS_eyePosi/HS_mesenL");

            if (tgt.name != "CH0002" || FlagUsaFH)
            {
                mesenR = tgt.FindDeep("HS_mesenR", true);
                mesenL = tgt.FindDeep("HS_mesenL", true);

                siromeR = tgt.FindDeep("HS_WhiteEyeR", true);
                siromeL = tgt.FindDeep("HS_WhiteEyeL", true);
            }
            else
            {
                mesenR = tgt.FindDeep("HS_mesenR_02", true);
                mesenL = tgt.FindDeep("HS_mesenL_02", true);

                siromeR = tgt.FindDeep("HS_WhiteEyeR_02", true);
                siromeL = tgt.FindDeep("HS_WhiteEyeL_02", true);
            }

            if (mesenL && mesenR)
            {
                hitomisL = null;
                getHitomis(ref hitomisL, mesenL.transform);
                hitomisR = null;
                getHitomis(ref hitomisR, mesenR.transform);
            }
        }

        void getHitomis(ref Transform[] hitomis, Transform mesen)
        {
            bool root = false;
            if (hitomis == null)
            {
                hitomis = new Transform[hitomiNames.Length];
                root = true;
            }

            //遅い foreach (Transform t in mesen)
            for (int j = 0; j < mesen.childCount; j++)
            {
                var t = mesen.GetChild(j);

                for (int i = 0; i < hitomiNames.Length; i++)
                    if (Regex.IsMatch(t.name, hitomiNames[i]))
                        hitomis[i] = t;

                getHitomis(ref hitomis, t);
            }

            if (root)
            {
                if (!hitomis.All(x => x))
                    hitomis = null;
            }
        }

        bool check()
        {
            if (!lookAtIK || !lookAtIK.enabled)
                return false;

            if (!mesenL || !mesenR)
                return false;

            if (!NoCheckIkWeight && (lookAtIK.solvers.lookAt.IKPositionWeight == 0 || lookAtIK.solvers.lookAt.headWeight == 0))
            {
                if (prevVal != 0)
                    SwingEye(0);
                return false;
            }

            return true;
        }

        //public void Update()
        int frameCountOnRenderObject = 0;
        private void OnRenderObject()
        {
            if (frameCountOnRenderObject == Time.frameCount) //カメラ数回呼ばれてしまうので安定化
                return;
            frameCountOnRenderObject = Time.frameCount;
            //Debug.Log(frameCountOnRenderObject);

            if (!Enabled)
                return;

            if (!check())
                return;

            // IKの回転状態
            dummyHead.position = head.position;
            dummyHead.rotation = head.rotation * Quaternion.Euler(DummyOffset);

            dummyEye.localPosition = Vector3.zero;

            var target = lookAtIK.solvers.lookAt.target;
            dummyEyeTgt.position = target.position;
            dummyEyeTgt.rotation = target.rotation;

            // Y軸の回転抑制のためY軸を合わせる、上下の視点追尾もしたいなら不要
            var v = dummyEyeTgt.localPosition;
            v.y = 0;
            dummyEyeTgt.localPosition = v;

            for (int i = 0; i < 5; i++) //5回くらいが滑らかになるっぽい
            {
                Vector3 vector = (dummyEyeTgt.position - dummyEye.position); //.normalized;
                Quaternion rot = Quaternion.LookRotation(vector, dummyEye.up);
                var angle = Quaternion.Angle(dummyEye.rotation, rot);
                if (angle > 0)
                {
                    // angleで割らないと加減速が付きすぎ、angleで割ると等速直線運動になりすぎるので弄る
                    var acc = Mathf.Sqrt(angle);
                    dummyEye.rotation = Quaternion.Slerp(dummyEye.rotation, rot, Mathf.Clamp01(Time.deltaTime * Speed / acc));
                }
            }

            eyeEuler = dummyEye.transform.localEulerAngles.angle180();
        }

        public void userLateUpdate(bool skipLateUpdate = true)
        {
            if (!this.enabled)
                return;

            LateUpdate();
            _skipLateUpdate = skipLateUpdate;
        }

        bool _skipLateUpdate = false;
        float prevVal = 0;

        private void LateUpdate()
        {
            if (_skipLateUpdate)
            {
                _skipLateUpdate = false;
                return;
            }

            if (!Enabled)
                return;

            if (!check())
                return;

            // アニメーターで回転がリセットされている（IK処理と前後した場合）
            eyeEuler2 = dummyEye.transform.localEulerAngles.angle180();

            var val = Mathf.Clamp((eyeEuler.y) / (90f) * Weight, -1f, 1f);
            SwingEye(val);
        }

        void SwingEye(float val)
        { 
            var r = val - prevVal;
            var max = MaxSpeed * Time.deltaTime;
            if (Mathf.Abs(r) > max)
            {
                if (r > 0)
                    val = prevVal + max;
                else
                    val = prevVal - max;
            }
            prevVal = val;

            val = Mathf.Sin((0.5f * val + 2f) * Mathf.PI);

            var left = (val < 0 ? -val : 0); //* 100f; 今回はブレンドシェイプじゃないので100倍不要
            var right = (val > 0 ? val : 0); //* 100f;

            float rz = -3.451f - 0.45f * right + 0.45f * left;
            float lz = 3.451f - 0.45f * right + 0.45f * left;

            // ゲーム別反映処理
            //Func<Vector3, float, Vector3> setX = (v, x) => { return new Vector3(x, v.y, v.z); };
            //Func<Vector3, float, Vector3> setY = (v, y) => { return new Vector3(v.x, y, v.z); };
            //Func<Vector3, float, Vector3> setZ = (v, z) => { return new Vector3(v.x, v.y, z); };
            // v0.92 こっちの方が多少早いらしいので
            Vector3 setX(Vector3 v, float x) => new Vector3(x, v.y, v.z);
            Vector3 setY(Vector3 v, float y) => new Vector3(v.x, y, v.z);

            if (actScene.name != "Custom")
            {
                // 目を閉じたときのめり込みを防ぐ
                float ry = 5.041f - 0.24f * right;
                float ly = 5.041f - 0.24f * left;

                mesenR.transform.localPosition = new Vector3(mesenR.transform.localPosition.x, ry, rz);
                mesenL.transform.localPosition = new Vector3(mesenL.transform.localPosition.x, ly, lz);

                // フェイスモーションによって白目の位置に変動があるので保険
                if (siromeR.transform.localPosition.y > 5.81f)
                    siromeR.transform.localPosition = setY(siromeR.transform.localPosition, 5.81f);
                if (siromeL.transform.localPosition.y > 5.81f)
                    siromeL.transform.localPosition = setY(siromeL.transform.localPosition, 5.81f);
            
                if (InhibitHitomiAnime)
                {
                    if (hitomisL != null)
                    {
                        hitomisL[0].localPosition = setX(hitomisL[0].localPosition, -0.27f);
                        hitomisL[3].localPosition = setX(hitomisL[3].localPosition, 0.167f);
                    }
                    if (hitomisR != null)
                    {
                        hitomisR[0].localPosition = setX(hitomisR[0].localPosition, 0.27f);
                        hitomisR[3].localPosition = setX(hitomisR[3].localPosition, -0.167f);
                    }
                }
            }
            else
            {
                mesenR.transform.localPosition = new Vector3(mesenR.transform.localPosition.x, mesenR.transform.localPosition.y, rz);
                mesenL.transform.localPosition = new Vector3(mesenL.transform.localPosition.x, mesenL.transform.localPosition.y, lz);

                // カスタム中は八方睨みモデルなので、瞳の位置が深い＆フェイスモーションにより白目の位置が浅くなるので埋まり回避
                // 白目を奥に移動してもあまり違和感なさげなので埋まらない程度に奥まらせる
                siromeR.transform.localPosition = setY(siromeR.transform.localPosition, 3f);
                siromeL.transform.localPosition = setY(siromeL.transform.localPosition, 3f);
            }
        }

    }
}
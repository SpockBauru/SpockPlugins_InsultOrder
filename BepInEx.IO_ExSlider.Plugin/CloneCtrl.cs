using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using nnPlugin;
using static BepInEx.IO_ExSlider.Plugin.IO_ExSlider;
using BepInEx.IO_ExSlider.Plugin;
using Hook.nnPlugin.Managed;

namespace BepInEx.IO_ExSlider.Plugin
{
    public class CloneCtrlMgr
    {
        public static Dictionary<string, CloneCtrlMgr> dicCloneMgr => _dicCloneMgr;
        private static Dictionary<string, CloneCtrlMgr> _dicCloneMgr = new Dictionary<string, CloneCtrlMgr>();
        public List<CloneCtrl> dicClone = new List<CloneCtrl>();

        public void Clear()
        {
            foreach (var v in dicClone.ToArray())
            {
                v.Destroy();
            }
            dicClone.Clear();
        }


        public CloneCtrl Add(GirlCtrl girlCtrl)
        {
            return CloneCtrl.Add(this, girlCtrl);
        }


        [Serializable]
        public class SaveClone
        {
            public List<CloneCtrl> dicClone;

            public SaveClone()
            {
            }

            public SaveClone(CloneCtrlMgr ctrl)
            {
                if (ctrl != null)
                    Save(ctrl);
            }

            public void Save(CloneCtrlMgr ctrl)
            {
                this.dicClone = ctrl.dicClone;
            }

            public void Load(CloneCtrlMgr ctrl)
            {
                //if (ctrl.dicClone.Count > 0)
                //    ctrl.Clear();

                //ctrl.dicClone = this.dicClone;
                //foreach (var c in ctrl.dicClone.ToArray())
                foreach (var c in this.dicClone.ToArray())
                {
                    var tgtchar = CtrlGirlsInst.FirstOrDefault(x => x.ini.root == c.cloneObjName);
                    if (tgtchar == null || !tgtchar.isActive())
                    {
                        Debug.LogWarning("クローンターゲットが見つかりません :" + c.cloneObjName);
                        continue;
                    }

                    c.setManager(ctrl);
                    c.Instantiate(tgtchar.charaCtrl);
                }
            }
        }

    }

    [Serializable]
    public class CloneCtrl
    {
        static public bool AnimCopy_ = true;
        static public bool AnimSync_ = false;
        static public bool LookAtCopy_ = false;

        static public bool UpdateEdits = true;
        static public bool UpdateShape = true;
        static public bool UpdateAnimState = false;
        //static public List<CloneCtrl> _dicClone = new List<CloneCtrl>();

        private CloneCtrlMgr _cloneMgr;
        private GameObject _obj;
        private PlgCharaCtrl _ctrl;

        public List<CloneCtrl> getDicCloneInst() { return _cloneMgr.dicClone; }

        public string cloneObjName;
        public bool animCopy = true;
        public bool animSync = true;
        public bool lookAtCopy = false;

        // エディット情報
        public SaveShapeKeys shapeData;
        public BoneScales.Edits editData;
        //public bool smahoOn = false;

        // シーン情報
        public SaveDicStrV3 boneCapData;
        public ScenePos scnPos;
        //public MiscData misc;

        // アニメ情報
        public string animeCtrlName;
        public int animeHash = -1;
        public float animeTimeOffset;

        //public GameObject getGameObject()
        //{
        //    return _obj;
        //}

        public CloneCtrl()
        {
        }

        public CloneCtrl(CloneCtrlMgr mgr)
        {
            setManager(mgr);
        }

        public void setManager(CloneCtrlMgr mgr)
        {
            this._cloneMgr = mgr;
        }

        public GameObject getScnPosRoot()
        {
            return _ctrl.girlCtrl.ScnPosRoot();
        }

        public PlgCharaCtrl getCharaCtrl()
        {
            return _ctrl;
        }

        public bool isNull()
        {
            return !_obj;
        }

        public bool isActive()
        {
            return _obj.activeSelf;
        }

        public void setActive(bool active)
        {
            if (!active)
                _ctrl.backupAnimState();

            _obj.SetActive(active);
        }

        public static void LateUpdate()
        {
#if DEBUG
            // クローン作製
            if (Input.GetKeyDown(KeyCode.C))
            {
                Add();
            }
            if (Input.GetKeyDown(KeyCode.V))
            {
                Clear();
            }
#endif
        }

        public void Instantiate(PlgCharaCtrl orgChar)
        {
            if (_obj)
                return;

            GameObject tgtgo = orgChar.girlCtrl.ini.rootObj;
            if (tgtgo)
            {
                cloneObjName = tgtgo.name;

                // IO Shota用
                var ccsy = tgtgo.GetComponent<CostumeSetUp_SY>();
                if (ccsy)
                    ccsy.enabled = false;
                else
                    tgtgo.name = "Nigeteeeee";  // クローンのAwakeでのFindサーチに巻き込まれないように

                try
                {
                    //GameObject tmpJoint = null;
                    //if (cloneObjName == "PC00")
                    //{
                    //    tmpJoint = new GameObject("tmp");
                    //    tmpJoint.transform.SetParent(tgtgo.transform, false);
                    //    //tgtgo.transform.Find("PC0000");
                    //    foreach (Transform t in tgtgo.transform)
                    //    {
                    //        if (!t || t == tmpJoint)
                    //            continue;

                    //        t.SetParent(tmpJoint.transform, false);
                    //    }
                    //    var pc = tgtgo.transform.Find("PC0000");
                    //    if (pc) pc.SetParent(tmpJoint.transform, false);
                    //    _obj = GameObject.Instantiate(tmpJoint);

                    //    foreach (Transform t in tmpJoint.transform)
                    //    {
                    //        if (!t || t == tmpJoint)
                    //            continue;

                    //        t.SetParent(tgtgo.transform, false);
                    //    }
                    //    GameObject.DestroyImmediate(tmpJoint);
                    //}

                    MyHook.DISABLE_CLONE_AWAKE = true;

                    //_obj = UnityEngine.Object.Instantiate(tgtgo);
                    _obj = GameObject.Instantiate(tgtgo);

                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                }
                finally { 
                    MyHook.DISABLE_CLONE_AWAKE = false;
                }
                

                //var n = _obj;
                _obj.name = cloneObjName; // オブジェクト名で反応するアタッチスクリプト向け（ただしAwakeは防げない）
                var parentName = $"Clone-{cloneObjName}-" + getDicCloneInst().Count;

                for (int i = getDicCloneInst().Count; i < 1000; i++)
                {
                    parentName = $"Clone-{cloneObjName}-" + i;
                    if (!GameObject.Find(parentName))
                        break;
                }

                var root = new GameObject(parentName);
                _obj.transform.SetParent(root.transform, true);

                if (ccsy)
                {
                    ccsy.enabled = true; // 戻す

                    // Awake回避後
                    var ccsy_clone = _obj.GetComponent<CostumeSetUp_SY>();

                    ccsy_clone.Path = PlgUtil.GetFullPath(_obj.transform);
                    //ccsy_clone.enabled = true;
                    //ccsy_clone.SendMessage("Awake"); ショタはコンフィグでの表示状態変更関係だけなのでなくても実害ない（元々呼ばれないし）
                }
                else
                {
                    // 戻す
                    tgtgo.name = cloneObjName;

                    if (cloneObjName == "PC00")
                    {
                        var lc = _obj.GetComponent<LiquidCounter>();
                        if (lc) lc.enabled = false;
                    }
                }

                // リストに追加
                if (!getDicCloneInst().Contains(this))
                    getDicCloneInst().Add(this);
                
                //追加エディット
                _ctrl = _obj.AddComponent<PlgCharaCtrl>();
                _ctrl.enabled = true;
                var n = _ctrl.girlCtrl.FindModel();

                //var eb = n.AddComponent<MyEyeBlink.Comp>();
                //eb.enabled = true;
                //_ctrl.girlCtrl.eyeBlink = eb.blink;

                // シェイプキー
                if (_ctrl.girlCtrl.shapeKeys != null)
                {
                    if (this.shapeData == null)
                    {
                        if (orgChar.girlCtrl.shapeKeys != null)
                            this.shapeData = new SaveShapeKeys(orgChar.girlCtrl.shapeKeys.edits);
                        else
                            this.shapeData = new SaveShapeKeys();
                    }
                    this.shapeData.Load(_ctrl.girlCtrl.shapeKeys);
                }
                else
                {
                    this.shapeData = new SaveShapeKeys();
                }

                // 体形
                if (this.editData == null)
                {
                    this.editData = Clone.Data(orgChar.edits);
                }
                _ctrl.edits = this.editData;
                _ctrl.girlCtrl.boneScales.LoadEdits(_ctrl.edits);

                //if (this.misc == null)
                //{
                //    this.misc = Clone.Data(orgChar.girlCtrl.misc);
                //}
                //_ctrl.girlCtrl.misc = this.misc;

                // シーン位置
                if (this.scnPos == null)
                {
                    this.scnPos = Clone.Data(orgChar.girlCtrl.scnPos);
                    //if (!this.scnPos.enable)
                    {
                        this.scnPos.enable = true;
                        var tr = orgChar.girlCtrl.ScnPosRoot().transform;
                        if (tr)
                        {
                            this.scnPos.pos = tr.localPosition;
                            this.scnPos.rot = tr.localEulerAngles.angle180();
                            this.scnPos.scale = tr.localScale.x;

                            // クローンにはオフセット済みの位置が設定されるはずなのでオフセットは削除
                            _ctrl.girlCtrl.boneScales.ClearOffsetData();
                        }

                        if (tgtgo.name == "PC00")
                        {
                            // プレイヤーの場合基点をルートに変更
                            _ctrl.girlCtrl.pathScnPosRoot = PlgUtil.GetFullPath(_obj.transform);

                            this.scnPos.pos = tr.root.localPosition;
                            this.scnPos.rot = tr.root.localEulerAngles.angle180();
                            this.scnPos.scale = tr.localScale.x / 0.1f;
                        }
                    }
                }
                else if (tgtgo.name == "PC00")
                {
                    // プレイヤーの場合基点をルートに変更
                    _ctrl.girlCtrl.pathScnPosRoot = PlgUtil.GetFullPath(_obj.transform);
                    // クローンにはオフセット済みの位置が設定されるはずなのでオフセットは削除
                    _ctrl.girlCtrl.boneScales.ClearOffsetData();
                }
                _ctrl.girlCtrl.scnPos = this.scnPos;


                if (!animCopy)
                {
                    if (_ctrl.girlCtrl.rcbFullbody == null)
                    {
                        _ctrl.girlCtrl.ctrlBone = new CtrlBone();
                        _ctrl.girlCtrl.rcbFullbody = _ctrl.girlCtrl.ctrlBone.addCtrl<CtrlBone.RotctrlBone>();
                    }

                    if (this.boneCapData != null)
                    {
                        //ポーズ状態の復元
                        this.boneCapData.Load(ref _ctrl.girlCtrl.rcbFullbody.dicEulers);
                    }
                    else
                    {
                        //ポーズ状態でコピー
                        //CtrlBone.RotctrlBone.CapAll(_ctrl.girlCtrl.rcbFullbody, tgtgo.transform, "Bone_Root");
                        _ctrl.girlCtrl.rcbFullbody.capAll(orgChar.girlCtrl.FindBone().transform);
                        this.boneCapData = new SaveDicStrV3(_ctrl.girlCtrl.rcbFullbody.dicEulers);
                    }

                    var aik = n.GetComponent<RootMotion.FinalIK.LookAtIK>();
                    if (aik && aik.enabled)
                        aik.enabled = false;

                    var ikh = n.GetComponent<PlgIK.PluginBipedIkHelper>();
                    if (ikh)
                        ikh.SetIkEnable(false);

                    /* 瞬きも止まるので
                    var ani = n.GetComponent<Animator>();
                    if (ani)
                        ani.enabled = false;
                        */
                }
                else
                {
                    //アニメもコピー
                    var ani = n.GetComponent<Animator>();
                    var ani0 = orgChar.girlCtrl.FindModel().GetComponent<Animator>();
                    var asi0 = ani0.GetCurrentAnimatorStateInfo(0);

                    if (animeHash == -1)
                    {
                        animeCtrlName = ani0.runtimeAnimatorController.name;
                        animeHash = asi0.shortNameHash;
                        if (animSync)
                            animeTimeOffset = 0;
                        else
                            animeTimeOffset = 0; // 責め側のクローンでは使いにくいので無し　-asi0.normalizedTime;

                        if (animSync)
                        {
                            var comp = n.GetComponent<cloneAnimeSync>();
                            if (!comp)
                                comp = n.AddComponent<cloneAnimeSync>();

                            comp.setTarget(orgChar.girlCtrl.FindModel());
                        }

                        /*
                        if (animSync)
                            ani.Play(asi0.shortNameHash, 0, asi0.normalizedTime);
                        else
                            ani.Play(asi0.shortNameHash, 0);*/
                    }
                    else if (!string.IsNullOrEmpty(animeCtrlName) && ani.runtimeAnimatorController.name != animeCtrlName)
                    {
                        // クリップセットごと復元
                        var c = PlgUtil.GetResourceByName<RuntimeAnimatorController>(animeCtrlName);
                        if (c)
                            ani.runtimeAnimatorController = c;
                    }
                    ani.Play(animeHash, 0, Mathf.Repeat(asi0.normalizedTime + animeTimeOffset, 1f));
                    ani.Update(0);
                }

                if (animCopy || lookAtCopy)
                {
                    // 今回はLookat自力実装なので不要
                    ////視点
                    //var aik = n.GetComponent<RootMotion.FinalIK.LookAtIK>();
                    //if (aik)
                    //{
                    //    var aik0 = go.GetComponent<RootMotion.FinalIK.LookAtIK>();
                    //    if (aik0)
                    //    {
                    //        aik.enabled = aik0.enabled; //v1.2
                    //        aik.solver.Initiate(n.transform);
                    //        aik.solver.head.axis = aik0.solver.head.axis;
                    //        aik.solver.head.rotationLimit = aik0.solver.head.rotationLimit;
                    //        aik.solver.head.solverPosition = aik0.solver.head.solverPosition;
                    //        aik.solver.head.solverRotation = aik0.solver.head.solverRotation;
                    //        aik.solver.head.defaultLocalPosition = aik0.solver.head.defaultLocalPosition;
                    //        aik.solver.head.defaultLocalRotation = aik0.solver.head.defaultLocalRotation;
                    //        aik.solver.target = aik0.solver.target;

                    //        if (Camera.main.transform == aik0.solver.target)
                    //        {
                    //            PlgLookAtTarget.Init();
                    //            if (PlgLookAtTarget.target)
                    //            {
                    //                aik.solver.target = PlgLookAtTarget.target;
                    //            }
                    //        }
                    //    }
                    //}
                }

                //// ikは未対応
                //var oik = go.GetComponent<RootMotion.FinalIK.FullBodyBipedIK>();
                //var nik = n.GetComponent<RootMotion.FinalIK.FullBodyBipedIK>();
                //if (oik && nik)
                //{
                //    if (oik.solver.IKPositionWeight != 0)
                //    {
                //        nik.solver.IKPositionWeight = 0;
                //        Debug.LogWarning("クローンはIK未対応");
                //    }
                //}
            }
        }

        public void UpdateCloneInst()
        {
            if (_obj && _obj.transform.parent && _obj.transform.parent.name.StartsWith("Clone"))
                GameObject.DestroyImmediate(_obj.transform.parent.gameObject);

            if (_obj)
                GameObject.DestroyImmediate(_obj);
            _obj = null;

            // エディット情報を破棄
            if (UpdateEdits)
            {
                this.editData = null;
                //this.misc = null;
            }
            if (UpdateShape)
                this.shapeData = null;
            //this.smahoOn = false;

            if (UpdateAnimState)
            {
                this.animCopy = AnimCopy_;
                this.animSync = AnimSync_;
                this.lookAtCopy = LookAtCopy_;
            }

            // 再生成
            var tgtchar = CtrlGirlsInst.FirstOrDefault(x => x.ini.root == this.cloneObjName);
            if (tgtchar == null || !tgtchar.isActive())
            {
                Debug.LogWarning("クローンターゲットが見つかりません :" + this.cloneObjName);
            }
            Instantiate(tgtchar.charaCtrl);
        }

#if REPLACE_ENABELED
        // クローンと本体を置換、今回はオミット
        public void Replace(GameObject tgt, PlgCharaCtrl orgChar)
        {
            if (!_obj)
                return;

            if (tgt != null)
            {
                var bks = this.shapeData;
                this.shapeData = new SaveShapeKeys(orgChar.girlCtrl.shapeKeys.edits);
                this.shapeData.Load(_ctrl.girlCtrl.shapeKeys);
                bks.Load(orgChar.girlCtrl.shapeKeys);

                var bke = this.editData;
                this.editData = Clone.Data(orgChar.edits);
                _ctrl.edits = this.editData;
                _ctrl.girlCtrl.boneScales.LoadEdits(_ctrl.edits);
                orgChar.girlCtrl.boneScales.LoadEdits(bke);

                //var bkp = this.misc;
                //this.misc = Clone.Data(Ctrl.misc);
                //_ctrl.Ctrl.misc = this.misc;
                //Ctrl.misc = bkp;

                var bksp = this.scnPos;
                this.scnPos = Clone.Data(orgChar.girlCtrl.scnPos);
                if (!this.scnPos.enable)
                {
                    this.scnPos.enable = true;
                    var tr = tgt.transform.FindSp("_Root");
                    if (tr)
                    {
                        this.scnPos.pos = tr.localPosition - _ctrl.edits.girl_dpos;
                        this.scnPos.rot = tr.localEulerAngles.angle180();
                    }
                }
                _ctrl.girlCtrl.scnPos = this.scnPos;
                orgChar.girlCtrl.scnPos = bksp;

                if (!animCopy)
                {
                    var bkrf = new SaveDicStrV3(_ctrl.girlCtrl.rcbFullbody.dicEulers);
                    //CtrlBone.RotctrlBone.CapAll(_ctrl.girlCtrl.rcbFullbody, tgt.transform, "_Root");
                    _ctrl.girlCtrl.rcbFullbody.capAll(orgChar.girlCtrl.FindBone().transform);

                    this.boneCapData = new SaveDicStrV3(_ctrl.girlCtrl.rcbFullbody.dicEulers);
                    bkrf.Load(ref orgChar.girlCtrl.rcbFullbody.dicEulers);

                    var aik = _obj.GetComponent<RootMotion.FinalIK.LookAtIK>();
                    if (aik && aik.enabled)
                        aik.enabled = false;
                    aik = tgt.GetComponent<RootMotion.FinalIK.LookAtIK>();
                    if (aik && aik.enabled)
                        aik.enabled = false;

                    /* 瞬きも止まるので
                    var ani = _obj.GetComponent<Animator>();
                    if (ani)
                        ani.enabled = false;
                    */
                }
                else
                {
                    //メインのアニメーターを弄ると色々問題起きそうなので省略?
                    var ani = _obj.GetComponent<Animator>();
                    var ani0 = tgt.GetComponent<Animator>();
                    var asi = ani.GetCurrentAnimatorStateInfo(0);
                    var asi0 = ani0.GetCurrentAnimatorStateInfo(0);

                    animeHash = asi0.shortNameHash;
                    if (animSync)
                        animeTimeOffset = 0;
                    else
                        animeTimeOffset = asi.normalizedTime - asi0.normalizedTime;

                    var a0 = asi0.shortNameHash;
                    var t0 = asi0.normalizedTime;

                    var a = asi.shortNameHash;
                    var t = asi.normalizedTime;

                    // クリップごとコントローラー交換
                    animeCtrlName = ani0.runtimeAnimatorController.name;
                    var ac0 = ani0.runtimeAnimatorController;
                    ani0.runtimeAnimatorController = ani.runtimeAnimatorController;
                    ani.runtimeAnimatorController = ac0;

                    ani.Play(a0, 0, t0);
                    ani.Update(0);

                    ani0.Play(a, 0, t);
                    ani0.Update(0);
                }
            }

        }
#endif

        public void Destroy()
        {
            if (ScenePos.gizmoTgt == _obj)
            {
                ScenePos.gizmoTgt = null;
            }

            if (_obj)
                _obj.SetActive(false);

            if (_obj && _obj.transform.parent && _obj.transform.parent.name.StartsWith("Clone"))
                GameObject.DestroyImmediate(_obj.transform.parent.gameObject);

            if (_obj)
                GameObject.DestroyImmediate(_obj);

            getDicCloneInst().Remove(this);
        }

        public static CloneCtrl Add(CloneCtrlMgr mgr, GirlCtrl girlCtrl)
        {
            var c = new CloneCtrl(mgr);
            c.animCopy = AnimCopy_;
            c.animSync = AnimSync_;
            c.lookAtCopy = LookAtCopy_;
            c.Instantiate(girlCtrl.charaCtrl);

            return c;
        }


        public static bool CloneFilter_OnAnimatorHook(Animator animator)
        {
            // クローンのステートは処理させない
            if (animator.transform.root.name.StartsWith("Clone", StringComparison.Ordinal))
                return true;

            return false;
        }
    }

    public class cloneAnimeSync : MonoBehaviour
    {
        //public static Transform target;
        Animator anmSelf;
        Animator anmTarget;

        private void Awake()
        {
            anmSelf = gameObject.GetComponent<Animator>();
        }

        public void setTarget(GameObject go)
        {
            anmTarget = go.GetComponent<Animator>();
        }

        private void Update()
        {
            if (!(anmTarget && anmSelf))
                return;

            if (anmSelf.runtimeAnimatorController && anmTarget.runtimeAnimatorController
                && anmSelf.runtimeAnimatorController.name != anmTarget.runtimeAnimatorController.name)
            {
                var c = GameObject.Instantiate(anmTarget.runtimeAnimatorController);
                if (c)
                    anmSelf.runtimeAnimatorController = c;
            }

            for (int i=0; i<anmTarget.layerCount; i++)
            {
                var asi0 = anmTarget.GetCurrentAnimatorStateInfo(i);
                var asi = anmSelf.GetCurrentAnimatorStateInfo(i);

                if (asi.shortNameHash != asi0.shortNameHash)
                {
                    anmSelf.Play(asi0.shortNameHash, i, asi0.normalizedTime);
                }
            }

            if (anmTarget.speed != anmTarget.speed)
                anmTarget.speed = anmTarget.speed;
        }
    }

    /*
    public class PlgLookAtTarget : MonoBehaviour
    {
        static bool init_end = false;
        public static Transform target;
        static Camera cam;

        public static void Init()
        {
            if (init_end)
                return;

            init_end = true;
            GameObject gameObject = new GameObject("PlgLookAtTarget_Main");
            var o = gameObject.AddComponent<PlgLookAtTarget>();
            gameObject.SetActive(true);
            o.name = "PlgLookAtTargetComp";

            target = new GameObject("PlgLookAtTarget").transform;
            o.Update();
        }

        private void Awake()
        {
        }

        public void Update()
        {
            if (!cam || !cam.isActiveAndEnabled || Camera.main != cam)
            {
                cam = Camera.main;
                if (cam)
                {
                    target.SetParent(cam.transform, false);
                    target.localPosition = Vector3.zero;
                    target.localRotation = Quaternion.identity;
                }
            }
        }
    }*/
}

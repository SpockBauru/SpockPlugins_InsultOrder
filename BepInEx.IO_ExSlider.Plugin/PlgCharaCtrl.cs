using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
//using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;
using static BepInEx.IO_ExSlider.Plugin.IO_ExSlider;
using nnPlugin;
//using static BepInEx.IO_ExSlider.Plugin.IO_ExSlider;

namespace BepInEx.IO_ExSlider.Plugin
{
    [DisallowMultipleComponent]
    public partial class PlgCharaCtrl : MonoBehaviour//UnityEngine.Component
    {
        static readonly HashSet<string> NoCustomObjnames = new HashSet<string> { };

        // ボーン系
        public BoneScales.Edits edits = new BoneScales.Edits();
        public ScenePos scenePos = new ScenePos();

        bool _isClone = true;
        public bool isClone { get { return _isClone; } }

        GameObject ggo;
        GameObject ggoModel;
        public GameObject boneObject { get { return ggo; } }
        public GameObject modelObject { get { return ggoModel; } }

        Vector3 prevGpos;
        Vector3 prevGposBase;
        Vector3 prevGdrot;
        Quaternion prevGrot;
        Quaternion prevGrotBase;

        // クローン用
        Animator _animator;
        bool _resumeAnime = false;
        int _animeHash;


        public Vector3 OriginalLocPos
        {
            get {
                if (edits.offsetData.girlOffset.coord != BoneScales.OffsetCoord.None
                    || edits.offsetData.girlOffset.dpos != Vector3.zero)
                    return prevGposBase;
                else
                    return ggo.transform.localPosition;
            }
        }
        public Vector3 OriginalLocRot
        {
            get
            {
                if (edits.offsetData.girlOffset.coord != BoneScales.OffsetCoord.None
                    || edits.offsetData.girlOffset.drot != Vector3.zero)
                    return prevGrotBase.eulerAngles;
                else
                    return ggo.transform.localEulerAngles;
            }
        }

        public IO_ExSlider.GirlCtrl girlCtrl;

        public PlgCharaCtrl()
        {
        }

        private void Awake()
        {
            bool clone = base.gameObject.name.Contains("(Clone)");
            var name = base.gameObject.name.Replace(" (Clone)", "").Replace("(Clone)", "");

            if (!clone)
            {
                var _obj = base.gameObject;
                if (_obj && _obj.transform.parent && _obj.transform.parent.name.StartsWith("Clone"))
                    clone = true;
            }
            var tgtini = TargetIni.targetIni.girls.FirstOrDefault(x => x.root == name);
            
            if (string.IsNullOrEmpty(tgtini.id))
            {
                Debug.LogError("ターゲット情報の取得に失敗 :"+name);
                GameObject.Destroy(this);
                return;
            }

            if (clone)
            {
                // ボーン構成を反映したini情報をクローン用に作成
                tgtini = Clone.Data(tgtini);
                tgtini.root = PlgUtil.GetFullPath(base.transform);

                Debug.Log("Clone: RootPath生成: " + tgtini.root);
                var mdl = Extentions.FindSp(tgtini.getFullPath(tgtini.modelRoot), NeedRetry: true);
                Debug.Log("Clone: CheckBoneRoot " + PlgUtil.GetFullPath(mdl.transform));
            }

            this.girlCtrl = new GirlCtrl(tgtini);
            Init(this.girlCtrl, clone);
        }

        public void Init(IO_ExSlider.GirlCtrl gc, bool clone = false)
        {
            girlCtrl = gc;
            ggo = girlCtrl.FindBone();
            ggoModel = girlCtrl.FindModel();

            // クローン用
            _isClone = clone;
            _animator = ggoModel.GetComponent<Animator>();
        }

        private void OnEnable()
        {
            //Init(gameObject, true);

            // 非アクティブ対応
            if (_resumeAnime && _animator && _animator.isActiveAndEnabled)
            {
                _resumeAnime = false;

                _animator.Play(_animeHash, 0, 0);
                _animator.Update(0);
            }
        }

        public void backupAnimState()
        {
            // 非アクティブになる前に復元できるようにする
            if (_animator && _animator.isActiveAndEnabled)
            {
                _animeHash = _animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
                _resumeAnime = true;
            }
        }

        void Update()
        {
            girlCtrl.OnUpdate();

            Invoke("PostUpdate", 0);
        }

        private void PostUpdate()
        {
            girlCtrl.OnPostUpdate();
        }

        public void ExtUpdate()
        {
            if (!ggo || !ggo.activeInHierarchy)
                return;

            // プレ更新
            if (edits.girl_bScales != null &&
                !NoCustomObjnames.Contains(ggo.name))
            {
                edits.getControl().OnUpdate();
            }

        }

        void LateUpdate()
        {
            girlCtrl.OnPreLateUpdate();

            Invoke("LateLateUpdate", 0);
        }

        private void LateLateUpdate()
        {
            girlCtrl.OnLateUpdate();
        }

        /// <summary>
        ///     揺れ処理などのゲーム側LateUpdate前
        /// </summary>
        public void ExtPreLateUpdate()
        {
            // ボーンコントロール適用
            if (girlCtrl.ctrlBone != null)
                girlCtrl.ctrlBone.WriteEuler2Bone(ggo.transform);
        }

        /// <summary>
        ///     揺れ処理などのゲーム側LateUpdate完了後
        /// </summary>
        public void ExtLateUpdate()
        {
            if (!ggo || !ggo.activeInHierarchy)
                return;

            // シーン位置(固定化処理なのでここ)
            ScenePos.WritePosRot(girlCtrl);

            // 変形前の基準点を保持
            Transform trCoord = null;
            TransformTR saveCoord = null;
            if (edits.offsetData.girlOffset.coord != BoneScales.OffsetCoord.None)
            {
                // ゲーム側の変更がなかったら位置を一度戻す
                var tr = ggo.transform;
                if (prevGpos == tr.localPosition)
                {
                    //tr.position = prevGposBase;
                    tr.localPosition = prevGposBase;
                }
                if (prevGrot == tr.localRotation)
                {
                    tr.localRotation = prevGrotBase;
                }

                var cs = BoneScales.offsetCoordsData.First(x => x.coord == edits.offsetData.girlOffset.coord);

                switch (girlCtrl.ini.id)
                {
                    case "Neko":
                        trCoord = ggo.FindDeepSp(cs.boneName);
                        break;

                    case "Usa":
                        if (FlagUsaFH)
                            trCoord = ggo.FindDeepSp(cs.boneName);
                        else
                            trCoord = ggo.FindDeepSp(cs.boneName + "_02");
                        break;

                    case TargetIni.PlayerCharID:
                        break;

                    default:
                        break;
                }
                if (trCoord)
                    saveCoord = new TransformTR(trCoord);
                else
                {
                    MyDebug.Log($"警告: {girlCtrl.ini.id}のtrCoordがnullです");
                }
            }

            // 体形操作前のIKの手足の位置をセットするか
            if (this.girlCtrl.plgIK != null && this.girlCtrl.plgIK.ready && this.girlCtrl.plgIK.ikConfig.ikTargetingDefSclBody)
            {
                this.girlCtrl.plgIK.helperIK.ExSetLimbsRigPosition();
            }

            // シェイプキー
            if (girlCtrl.shapeKeys != null)
                girlCtrl.shapeKeys.OnLateUpdate();

            // ボーンコントロール適用
            //girlCtrl.ctrlBone.WriteEuler2Bone(ggo.transform);

            // ボーンスケール
            if (edits.girl_bScales != null &&
                !NoCustomObjnames.Contains(ggo.name))
            {
                edits.getControl().OnLateUpdate();
                //PlgCharaCtrl.ProcBoneScale(ggo, edits);
            }

            // 位置調整ギズモ
            bool needGposUpdate = false;
            BoneScales.gizmoGirlPos.gizmoOffsetPos = Vector3.zero;
            if (trCoord)
            {
                var gizmoOffsetPos = trCoord.position - ggo.transform.position;

                if (BoneScales.gizmoGirlPos.visible && BoneScales.gizmoGirlPos.isActiveAndEnabled
                    && BoneScales.gizmoGirlPos.target == ggo.transform)
                {
                    var t = ggo.transform;

                    BoneScales.gizmoGirlPos.Update();
                    if (BoneScales.gizmoGirlPos.isDrag)
                    {
                        if (!BoneScales.gizmoGirlPos.trs[1].isDrag && BoneScales.gizmoGirlPos.trs[0].isDrag)
                        {
                            //edits.offsetData.girlOffset.dpos = Quaternion.Inverse(BoneScales.gizmoGirlPos.rotationPos) * (BoneScales.gizmoGirlPos.position - trCoord.position);
                            edits.offsetData.girlOffset.dpos += Quaternion.Inverse(BoneScales.gizmoGirlPos.rotationPos) * BoneScales.gizmoGirlPos.draggingDPos;
                        }
                        else
                        {
                            //edits.offsetData.girlOffset.drot = (Quaternion.Inverse(trCoord.rotation) * BoneScales.gizmoGirlPos.rotation).eulerAngles.angle180();
                            edits.offsetData.girlOffset.drot = (edits.offsetData.girlOffset.drot + BoneScales.gizmoGirlPos.draggingDRot.eulerAngles).angle180();
                        }
                    }
                    else
                    {
                        BoneScales.gizmoGirlPos.rotation = trCoord.rotation;
                        BoneScales.gizmoGirlPos.position = trCoord.position;

                        needGposUpdate = true;
                    }
                }

                // 角度設定
                if (edits.offsetData.girlOffset.drot != Vector3.zero
                    || prevGdrot != Vector3.zero
                    || trCoord)
                {
                    var tr = ggo.transform;
                    if (tr)
                    {
                        if (prevGrot != tr.localRotation)
                        {
                            prevGrotBase = tr.localRotation;
                        }
                        else
                        {
                            tr.localRotation = prevGrotBase;
                        }

                        //tr.rotation *= Quaternion.Inverse(trCoord.rotation) * (trCoord.rotation * Quaternion.Euler(edits.offsetData.girlOffset.drot));
                        //tr.localRotation = prevGrotBase * Quaternion.Euler(edits.offsetData.girlOffset.drot);

                        MyRotateAround.Proc2(tr, trCoord, edits.offsetData.girlOffset.drot);

                        if (needGposUpdate)
                        {
                            BoneScales.gizmoGirlPos.position = trCoord.position;
                            BoneScales.gizmoGirlPos.rotation = trCoord.rotation;
                            BoneScales.gizmoGirlPos.rotationPos = trCoord.rotation * Quaternion.Inverse(Quaternion.Euler(edits.offsetData.girlOffset.drot));
                        }
                    }
                    prevGrot = tr.localRotation;

                    prevGdrot = edits.offsetData.girlOffset.drot;
                    if (prevGdrot == Vector3.zero)
                    {
                        tr.localRotation = prevGrotBase;
                    }
                }

                // 位置設定
                if (edits.offsetData.girlOffset.dpos != Vector3.zero
                    || trCoord)
                {
                    var tr = ggo.transform;
                    if (tr)
                    {
                        if (prevGpos != tr.localPosition)
                        {
                            prevGposBase = tr.localPosition;
                        }
                        else
                        {
                            //tr.position = prevGposBase;
                            tr.localPosition = prevGposBase;
                        }

                        tr.position += (saveCoord.position - trCoord.position)
                            + (trCoord.rotation * Quaternion.Inverse(Quaternion.Euler(edits.offsetData.girlOffset.drot)) * edits.offsetData.girlOffset.dpos);
                        prevGpos = tr.localPosition;

                        if (needGposUpdate)
                        {
                            BoneScales.gizmoGirlPos.position = trCoord.position;
                        }
                    }
                }

            }
            else
            {
                if (BoneScales.gizmoGirlPos.visible && BoneScales.gizmoGirlPos.isActiveAndEnabled
                    && BoneScales.gizmoGirlPos.target == ggo.transform)
                {
                    var t = ggo.transform;

                    BoneScales.gizmoGirlPos.Update();
                    /*if (gizmoGirlPos.isDragEnd)
                    {
                        edits.girlOffset.dpos = Quaternion.Inverse(t.localRotation) * (gizmoGirlPos.transform.localPosition - t.localPosition);
                        edits.girlOffset.drot = (gizmoGirlPos.rotation * Quaternion.Inverse(t.localRotation)).eulerAngles;
                    }
                    else*/
                    if (BoneScales.gizmoGirlPos.isDrag)
                    {
                        if (!BoneScales.gizmoGirlPos.trs[1].isDrag && BoneScales.gizmoGirlPos.trs[0].isDrag)
                        {
                            edits.offsetData.girlOffset.dpos = Quaternion.Inverse(t.parent.rotation) * (BoneScales.gizmoGirlPos.position - t.position);
                        }
                        else
                        {
                            edits.offsetData.girlOffset.drot = (edits.offsetData.girlOffset.drot + BoneScales.gizmoGirlPos.draggingDRot.eulerAngles).angle180();
                            //edits.offsetData.girlOffset.drot = (Quaternion.Inverse(t.rotation) * BoneScales.gizmoGirlPos.rotation).eulerAngles.angle180();
                        }
                    }
                    else
                    {
                        BoneScales.gizmoGirlPos.rotation = t.rotation;
                        BoneScales.gizmoGirlPos.position = t.position;
                        BoneScales.gizmoGirlPos.rotationPos = t.parent.rotation;

                        needGposUpdate = true;
                    }
                }
                // 位置設定
                if (edits.offsetData.girlOffset.dpos != Vector3.zero)
                {
                    var tr = ggo.transform;
                    if (tr)
                    {
                        if (prevGpos != tr.localPosition)
                        {
                            prevGposBase = tr.localPosition;
                        }

                        //tr.localPosition = prevGposBase + tr.localRotation * edits.offsetData.girlOffset.dpos;
                        tr.localPosition = prevGposBase + edits.offsetData.girlOffset.dpos;
                        prevGpos = tr.localPosition;

                        if (needGposUpdate)
                        {
                            BoneScales.gizmoGirlPos.position = tr.position;
                        }
                    }
                }

                // 角度設定
                if (edits.offsetData.girlOffset.drot != Vector3.zero
                    || prevGdrot != Vector3.zero)
                {
                    var tr = ggo.transform;
                    if (tr)
                    {
                        if (prevGrot != tr.localRotation)
                        {
                            prevGrotBase = tr.localRotation;
                        }
                        tr.localRotation = prevGrotBase * Quaternion.Euler(edits.offsetData.girlOffset.drot);

                        if (needGposUpdate)
                        {
                            BoneScales.gizmoGirlPos.position = tr.position;
                            BoneScales.gizmoGirlPos.rotation = tr.rotation;
                            BoneScales.gizmoGirlPos.rotationPos = tr.parent.rotation;
                        }
                    }
                    prevGrot = tr.localRotation;

                    prevGdrot = edits.offsetData.girlOffset.drot;
                    if (prevGdrot == Vector3.zero)
                    {
                        tr.localRotation = prevGrotBase;
                    }
                }

            }

        }


        public AnimationClip getAnimeClip(int layer = 0)
        {
            try
            {
                var go = girlCtrl.FindModel();
                var ani = go.GetComponent<Animator>();
                var clips = ani.GetCurrentAnimatorClipInfo(layer);

                return clips.OrderByDescending(x => x.weight).FirstOrDefault(x => x.weight > 0).clip;
            }
            catch (Exception e)
            {
                Debug.LogError("getAnimeClipエラー\n" + e);
            }

            return null;
        }

        public bool getAnimeState(ref string _animeCtrlName, ref string _animeClipName, int layer = 0, bool cinemeCheck = false)
        {
            try
            {
                var go = girlCtrl.FindModel();
                var ani = go.GetComponent<Animator>();

                if (layer == 0 && cinemeCheck && ani.GetLayerWeight(0) <= 0 && ani.GetCurrentAnimatorClipInfoCount(4) > 0)
                    layer = 4; // シネマレイヤーに切り替え

                var clips = ani.GetCurrentAnimatorClipInfo(layer);

                if (clips.Length <= 0 || !ani.enabled)
                    return false;

                _animeCtrlName = ani.runtimeAnimatorController.name;
                if (string.IsNullOrEmpty(_animeCtrlName))
                {
                    // オーバーライド元の名前取っても仕方ない気もするけど、わかりやすくなるかな？
                    var oc = ani.runtimeAnimatorController as AnimatorOverrideController;
                    if (oc)
                        _animeCtrlName = oc.runtimeAnimatorController.name;
                }

                var clip = clips.OrderByDescending(x => x.weight).FirstOrDefault(x => x.weight > 0);
                if (clip.clip != null)
                    _animeClipName = clip.clip.name;
                else
                    _animeClipName = "";

            }
            catch (Exception e)
            {
                Debug.LogError("getAnimeStateエラー\n" + e);
                return false;
            }

            return true;
        }
        public string getIOAnimeStateString()
        {
            if (Free3pXtMS.IsSlave(girlCtrl))
            {
                return "#Xt@" + GetIOAnimeStateFromClipname(Free3pXtMS.GetXtMasterAnimeName(girlCtrl));
            }
            int layer = 0;
            bool cinemaCheck = false;

            if (actScene.name == "ADV")
            {
                cinemaCheck = true;
            }

            string ctrl = "", clip = "";
            if (getAnimeState(ref ctrl, ref clip, layer, cinemaCheck))
            {
                //
                //  IOではクリップ名のみで管理
                //  IOはコントローラーが同シーンに複数存在しない＆クリップ名に接頭詞で含まれるのでクリップ名のみでOK
                //
                return GetIOAnimeStateFromClipname(clip);
            }

            return null;
        }

        // クリップ名からアニメ状態（オーバーライド後）の名前を取得
        public static string GetIOAnimeStateFromClipname(string clip)
        {
            const string SUFFIX = "SMLF"; // S～Lはバストサイズ、Fはフェイス(フェイスはレイヤー0にはないはずだけど一応)

            if (clip.Length >= 4 && clip[clip.Length - 2] == '_')
            {
                if (SUFFIX.Contains(clip[clip.Length - 1]))
                {
                    // 末尾を削除(ウェイトによって変動するため)
                    clip = clip.Substring(0, clip.Length - 2);
                }
            }
            return clip;
        }

        // デバッグ用
        internal string getIOAnimeStateStringDBG(bool removeSuffix)
        {
            if (removeSuffix)
                return getIOAnimeStateString();

            string ctrl = "", clip = "";
            if (getAnimeState(ref ctrl, ref clip))
            {
                return clip;

                // 通常
                return $"{ctrl}@{clip}";
            }
            return null;
        }

        public static bool CheckAnimeFilteringList(List<string> checkList, string animeName = null)
        {
            if (string.IsNullOrEmpty(animeName))
                return false;

            if (checkList.Contains(animeName))
                return true;

            if (IO_ExSlider.cfg.EnableFuzzyAnimeFiltering && animeName.Length > 3)
            {
                animeName = animeName.Substring(0, animeName.Length -1);
                for(int i = 0; i<checkList.Count; i++)
                {
                    if (checkList[i].StartsWith(animeName, StringComparison.Ordinal))
                        return true;
                }
            }

            return false;
        }

        //public string getAnimeStateString()
        //{
        //    string ctrl = "", clip = "";
        //    if (getAnimeState(ref ctrl, ref clip))
        //    {
        //        return $"{ctrl}@{clip}";
        //    }

        //    return null;
        //}
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
    }

}
*/

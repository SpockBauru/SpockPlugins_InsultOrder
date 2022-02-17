//using System.Diagnostics;
using System.Linq;
using UnityEngine;
using OhMyGizmo2;
using static BepInEx.IO_ExSlider.Plugin.IO_ExSlider;
using System;
//using static BepInEx.IO_ExSlider.Plugin.IO_ExSlider;

namespace BepInEx.IO_ExSlider.Plugin
{
    public class ScenePos
    {
        public bool enable = false;
        public Vector3 pos;
        public Vector3 rot;
        public float scale = 1f;

        static OhMyGizmo _gizmoPos;
        static OhMyGizmo _gizmoRot;
        //public static bool gizmo = false;

        // メインキャラ専用バックアップ系
        public Vector3 orgPos;
        public Vector3 orgRot;
        public Vector3 prevPos;
        public Quaternion prevRot;
        public float orgScale = 0.1f;
        public Vector3 orgScaleV3 = Vector3.one; // 汎用GameObject用
        public float prevScale = 0.1f;

        public void ResetPosRot(Transform tr)
        {
            if (tr)
            {
                tr.localPosition = orgPos;
                tr.localEulerAngles = orgRot;
                tr.localScale = orgScaleV3 * orgScale;
            }
        }

        // クローン用
        public static GameObject _gizmoTgt = null;
        private bool needPosReset;

        public static GameObject gizmoTgt
        {
            get { return _gizmoTgt; }
            set
            {
                _gizmoTgt = value;
                if (!value && _gizmoPos)
                {
                    _gizmoPos.resetTarget();
                    _gizmoRot.resetTarget();
                }
            }
        }

        public static ScenePos Save(GirlCtrl girlCtrl)
        {
            return girlCtrl.scnPos;
        }

        public static void Load(GirlCtrl girlCtrl, ScenePos p)
        {
            if (p != null)
                girlCtrl.scnPos = p;
        }

        internal void SaveTrs(Transform tr)
        {
            this.pos = tr.localPosition;
            this.rot = tr.localEulerAngles;
            this.scale = tr.localScale.x;
        }

        public void Load(ref ScenePos tgt)
        {
            tgt = this;
        }

        public void LoadTrs(ScenePos tgt)
        {
            tgt.pos = this.pos;
            tgt.rot = this.rot;
            tgt.scale = this.scale;
        }

        public ScenePos()
        {
            orgScaleV3 = Vector3.one;
        }

        public ScenePos(GameObject go, bool gizmo)
        {
            var tr = go.transform;
            this.pos = this.orgPos = tr.localPosition;
            this.rot = this.orgRot = tr.localEulerAngles.angle180();
            this.scale = this.orgScale = 1f;
            this.orgScaleV3 = tr.localScale; // 汎用GameObject用
            this.enable = true;

            this.prevPos = tr.localPosition;
            this.prevRot = tr.localRotation;
            prevScale = tr.localScale.x;

            if (gizmo)
                gizmoTgt = go;
        }

        public void setupGizmo()
        {
            if (!_gizmoPos)
            {
                _gizmoPos = OhMyGizmo.AddGizmo(null, "Gizmo_ScenePos-pos");
                _gizmoPos.modePos = true;
                _gizmoPos.visible = false;
                _gizmoPos.sizeHandle = 2f * 4;
                _gizmoPos.threthold = 20;
            }

            if (!_gizmoRot)
            {
                _gizmoRot = OhMyGizmo.AddGizmo(null, "Gizmo_ScenePos-rot");
                _gizmoRot.modeRot = true;
                _gizmoRot.visible = false;
                _gizmoRot.sizeRot = 0.8f * 4;
                _gizmoRot.threthold = 30;
            }
        }

        public void UpdateGizmo(bool enable, Transform tgt)
        {
            setupGizmo();

            if (enable)
            {
                _gizmoPos.setTarget(tgt, "");
                _gizmoRot.setTarget(tgt, "");

                if (!_gizmoPos.isDrag)
                    _gizmoPos.position = pos;
                else
                    pos = _gizmoPos.position;

                _gizmoRot.position = pos;

                if (!_gizmoRot.isDrag || _gizmoPos.isDrag)
                    _gizmoRot.rotation = Quaternion.Euler(rot);
                else
                    rot = _gizmoRot.rotation.eulerAngles.angle180();
            }
            else
            {
                if (_gizmoPos.visible)
                    _gizmoPos.resetTarget();

                if (_gizmoRot.visible)
                    _gizmoRot.resetTarget();
            }
        }

        // スケーリング干渉防止
        public static void WritePosRot(GirlCtrl girlCtrl)
        {
            // シーン位置
            var go = girlCtrl.ScnPosRoot();
            //bool scngizmo = gizmoTgt == girlCtrl.FindBone();
            var _scnPos = girlCtrl.scnPos;

            if (girlCtrl.scnPos.enable)
            {
                var tr = go.transform;
                if (tr)
                {
                    if (_scnPos.prevPos != tr.localPosition)
                    {
                        _scnPos.orgPos = tr.localPosition;
                    }
                    if (_scnPos.prevRot != tr.localRotation)
                    {
                        _scnPos.orgRot = tr.localEulerAngles.angle180();
                    }
                    if (_scnPos.prevScale != tr.localScale.x)
                    {
                        _scnPos.orgScale = tr.localScale.x;
                    }

                    ProcScenePos(tr, girlCtrl.boneScales.edits, _scnPos);

                    _scnPos.prevPos = tr.localPosition;
                    _scnPos.prevRot = tr.localRotation;
                    _scnPos.prevScale = tr.localScale.x;

                    bool scngizmo = gizmoTgt == go;
                    if (scngizmo)
                    {
                        _scnPos.UpdateGizmo(scngizmo, tr);
                    }

                    _scnPos.needPosReset = true;
                }
            }
            else if (_scnPos.needPosReset)
            {
                var tr = go.transform;
                if (tr)
                {
                    tr.localEulerAngles = _scnPos.orgRot;
                    if (!girlCtrl.boneScales.edits.girl_bScales.Any(c => c.Key == tr.name))
                        tr.localScale = Vector3.one * _scnPos.orgScale;
                }
                _scnPos.needPosReset = false;
            }
        }

        // 汎用
        public void WritePosRot(GameObject go, bool updateOrg = true)
        {
            if (this.enable)
            {
                var _scnPos = this;
                var tr = go.transform;
                if (tr)
                {
                    if (updateOrg)
                    {
                        if (this.prevPos != tr.localPosition)
                        {
                            this.orgPos = tr.localPosition;
                        }
                        if (this.prevRot != tr.localRotation)
                        {
                            this.orgRot = tr.localEulerAngles.angle180();
                        }
                        if (prevScale != tr.localScale.x)
                        {
                            orgScale = tr.localScale.x;
                        }
                    }
                    
                    // セット
                    tr.localPosition = _scnPos.pos;
                    tr.localRotation = Quaternion.Euler(_scnPos.rot);
                    tr.localScale = Vector3.one * _scnPos.scale;

                    this.prevPos = tr.localPosition;
                    this.prevRot = tr.localRotation;
                    prevScale = tr.localScale.x;

                    bool scngizmo = gizmoTgt == go;
                    if (scngizmo)
                    {
                        _scnPos.UpdateGizmo(scngizmo, tr);
                    }

                    this.needPosReset = true;
                }
            }
            else if (this.needPosReset)
            {
                var tr = go.transform;
                if (tr)
                {
                    tr.localPosition = this.orgPos;
                    tr.localEulerAngles = this.orgRot;
                    tr.localScale = orgScaleV3 * orgScale;
                }
                this.needPosReset = false;
            }
        }

        public static void ProcScenePos(Transform tr, BoneScales.Edits edits, ScenePos _scnPos)
        {
            tr.localPosition = _scnPos.pos;
            tr.localRotation = Quaternion.Euler(_scnPos.rot);
            tr.localScale = Vector3.one * _scnPos.scale;
            //if (edits.girl_bScales.Any(c => c.Key == tr.name))
            //    tr.localScale *= _scnPos.scale;
            //else
            //    tr.localScale = Vector3.one * _scnPos.scale;
        }
    }
}

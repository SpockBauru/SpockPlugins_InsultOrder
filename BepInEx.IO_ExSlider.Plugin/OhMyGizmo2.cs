using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Reflection;
using UnityEngine;
using mattatz.TransformControl;

namespace OhMyGizmo2
{
    /// <summary>
    /// 　TransformControlギズモを使いやすくするためのクラス
    /// </summary>
    public class OhMyGizmo : MonoBehaviour
    {
        Transform defParent;
        public bool visible;
        public bool undoDrag = false;

        //ギズモモード制御
        public bool modePos;
        public bool modeRot;
        public bool modeScl;

        public TransformControl[] trs = new TransformControl[3];

        private void updateTrc()
        {
            trs[0].mode = TransformControl.TransformMode.None;
            trs[1].mode = TransformControl.TransformMode.None;
            trs[2].mode = TransformControl.TransformMode.None;

            if (visible)
            {
                if (modePos)
                {
                    trs[0].mode = TransformControl.TransformMode.Translate;
                }
                if (modeRot)
                {
                    trs[1].mode = TransformControl.TransformMode.Rotate;
                }
                if (modeScl)
                {
                    trs[2].mode = TransformControl.TransformMode.Scale;
                }
            }
            
            foreach(var t in trs)
            {
                t.Control();
                if (t.isDragging)
                    break;
            }
        }

        FieldInfo _fi = null;
        FieldInfo _fi_SelectedDirection = null;
        bool _isdrag_bk = false;

        //差分計算用
        public Vector3 _backup_pos = Vector3.zero;
        public Quaternion _backup_rot = Quaternion.identity;

        //ギズモ位置
        public Vector3 position
        {
            get { return this.transform.position; }
            set { this.transform.position = value; }
        }

        //ギズモ回転
        public Quaternion rotation
        {
            get { return this.transform.rotation; }
            set { this.transform.rotation = value; }
        }

        //
        private Transform tr_tgt;
        public Transform target
        {
            get { return tr_tgt; }
            private set { tr_tgt = value; this.transform.SetParent(tr_tgt.parent, false); }
        }

        public string targetName { get; private set; }

        public void setTarget(Transform tgt, string name, bool on = true)
        {
            if (tgt != target)
                target = tgt;
            targetName = name;
            visible = on;
        }

        public void tryOffTarget(Transform tgt)
        {
            if (tgt == this.target || tgt == this.transform)
            {
                resetTarget();
            }
        }

        public void resetTarget()
        {
            visible = false;
            targetName = null;
            this.transform.SetParent(defParent, false);
            target = defParent;
        }

        //サイズ
        private float size_rot = 1f;
        public float sizeRot
        {
            get { return size_rot; }
            set { SetSizeRot(value); }
        }

        private void SetSizeRot(float f)
        {
            size_rot = f;
            foreach (var t in trs)
                t.SetSphereSize(f);
        }

        private float size_handle = 2.0f;
        public float sizeHandle
        {
            get { return size_handle; }
            set { SetSizeHandle(value); }
        }

        private void SetSizeHandle(float f)
        {
            size_handle = f;
            foreach (var t in trs)
                t.SetHandlerSize(f * 0.1f);
        }

        private float _threthold = 10f; // 反応性
        public float threthold
        {
            get { return _threthold; }
            set { SetThrethold(value); }
        }

        private void SetThrethold(float f)
        {
            _threthold = f;
            foreach (var t in trs)
                t.SetThrethold(f);
        }

        // 自動
        public bool procTargetCtrl = false;
        public bool checkTargetRender = false;
        public Vector3 procTargetOffest = Vector3.zero;
        public Quaternion procTargetOffestRot = Quaternion.identity;

        //差分計算用
        public void BkupPos() { _backup_pos = this.position; }
        public void BkupRot() { _backup_rot = this.rotation; }
        public void BkupPosAndRot() { this.BkupPos(); this.BkupRot(); }

        int upcnt = 0;

        //差分計算用
        public Vector3 _predrag_pos = Vector3.zero;
        public Quaternion _predrag_rot = Quaternion.identity;
        public bool _predrag_state = false;
        public Vector3 prevPos = Vector3.zero;
        public Quaternion prevRot = Quaternion.identity;
        public void Update()
        {
            // 同期用
            if (upcnt == Time.frameCount)
                return;
            upcnt = Time.frameCount;

            if (visible && (!target || !target.gameObject.activeInHierarchy))
            {
                visible = false;
            }

            if (visible && checkTargetRender && (!target.GetComponent<Renderer>().enabled))
            {
                visible = false;
            }

            prevPos = transform.position;
            prevRot = transform.rotation;


            bool dragnow = this.isDrag;

            if (dragnow != _predrag_state)
            {
                if (undoDrag && !_predrag_state)
                {
                    _predrag_pos = this.position;
                    _predrag_rot = this.rotation;
                }
                _predrag_state = dragnow;
            }

            if (dragnow != _isdrag_bk)
            {
                _isdrag_bk = dragnow;
            }

            if (undoDrag && dragnow)
            {
                if (this.isDragUndo)
                {
                    //右クリックかESCでポジション復帰
                    updateTrc();
                    foreach (var t in trs)
                    {
                        t.Unselect();
                    }
                    this.position = _predrag_pos;
                    this.rotation = _predrag_rot;

                    Input.ResetInputAxes();
                    return;
                }
            }

            updateTrc();

            if (procTargetCtrl && target && visible)
            {
                if (dragnow || isDragEnd)
                {
                    target.rotation = Quaternion.Inverse(procTargetOffestRot) * transform.rotation;
                    target.position = transform.position - procTargetOffest;
                }
                else
                {
                    transform.rotation = procTargetOffestRot * target.rotation;
                    transform.position = target.position + procTargetOffest;
                }
            }
        }

        public bool isDragUndo
        {
            get
            {
                if (!_predrag_state)
                    return false;
                return Input.GetMouseButton(1) || Input.GetKey(KeyCode.Escape);
            }
        }

        //ドラッグ判定、複数ギズモを表示中でも個別判定できるようにした
        public bool isDrag
        {
            get
            {
                if (!this.visible)
                    return false;

                foreach (var t in trs)
                {
                    if (_fi != null && _fi_SelectedDirection != null)
                    {
                        object obj = _fi.GetValue(t);
                        if (obj is bool && (bool)obj)
                        {
                            object obj2 = _fi_SelectedDirection.GetValue(t);
                            if (obj2 is Enum && (int)obj2 != 0)
                            {
                                //TransformControl.TransformDirection.None以外ならこのギズモのどこかをドラッグ中
                                return true;
                            }
                        }
                    }
                }
                
                return false;
            }
        }

        public void ClearSelectedType()
        {
            _fi_SelectedDirection.SetValue(this, 0);
        }

        //ドラッグエンド判定用（変化を見るだけなので毎フレーム呼び出す必要あり）
        public bool isDragEnd
        {
            get
            {
                bool drag = this.isDrag;
                if (drag != _isdrag_bk)
                {
                    if (drag == false)
                        return true;
                }
                return false;
            }
        }

        public void DragBkup()
        {
            {
                _isdrag_bk = this.isDrag;
            }
        }

        public void SetParent(Transform parent_tr)
        {
            base.transform.parent.SetParent(parent_tr, false);
        }

        public OhMyGizmo()
        {
            if (_fi_SelectedDirection == null)
                _fi_SelectedDirection = typeof(TransformControl).GetField("selected", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            if (_fi == null)
                _fi = typeof(TransformControl).GetField("dragging", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        }

        //ギズモ作成補助
        static List<GameObject> _gameObjects_ = new List<GameObject>();
        static HashSet<OhMyGizmo> _gizmos = new HashSet<OhMyGizmo>(); 

        static public OhMyGizmo AddGizmo(Transform parent_tr, string gizmo_name)
        {
            GameObject go = new GameObject();
            _gameObjects_.Add(go);
            if (parent_tr)
                go.transform.SetParent(parent_tr, false);
            go.name = gizmo_name;

            OhMyGizmo mg = go.AddComponent<OhMyGizmo>();
            mg.name = gizmo_name + "_TC";
            mg.transform.SetParent(go.transform, false);
            mg.defParent = go.transform;
            _gizmos.Add(mg);

            mg.trs[0] = mg.gameObject.AddComponent<TransformControl>();
            mg.trs[1] = mg.gameObject.AddComponent<TransformControl>();
            mg.trs[2] = mg.gameObject.AddComponent<TransformControl>();

            foreach(var t in mg.trs)
            {
                t.transform.SetParent(mg.transform, false);
            }

            return mg;
        }

        static public void Destroy(OhMyGizmo gizmo)
        {
            _gameObjects_.Remove(gizmo.defParent.gameObject);
            if (gizmo.defParent)
                GameObject.DestroyImmediate(gizmo.defParent.gameObject);

            if (gizmo)
                GameObject.DestroyImmediate(gizmo.gameObject);
            _gizmos.Remove(gizmo);
        }

        static public bool anyGizmoDrag()
        {
            foreach(var v in _gizmos)
            {
                if (v && v.visible && v.isDrag)
                {
                    return true;
                }
            }
            return false;
        }

        static public bool allGizmoHide()
        {
            foreach (var v in _gizmos)
            {
                if (v && v.visible)
                {
                    v.visible = false;
                }
            }
            return false;
        }

        static public bool allGizmoReset()
        {
            foreach (var v in _gizmos)
            {
                if (v && v.visible)
                {
                    v.resetTarget();
                }
            }
            return false;
        }
    }

    /// <summary>
    /// 　回転軸と移動軸を分割
    /// </summary>
    public class OhMyGizmoPosRot : MonoBehaviour
    {
        public bool visible { get => gizmoPos.visible; set { gizmoPos.visible = gizmoRot.visible = value; } }
        public bool undoDrag = false;

        new private Transform transform { get => base.transform; }

        //ギズモモード制御
        public OhMyGizmo gizmoPos;
        public OhMyGizmo gizmoRot;
        public Vector3 gizmoOffsetPos = Vector3.zero;
        public OhMyGizmo[] trs;

        //ギズモ位置
        public Vector3 position
        {
            get { return gizmoPos.transform.position - gizmoOffsetPos; }
            set { gizmoPos.transform.position = gizmoRot.transform.position = value;
                gizmoPos.transform.position = value + gizmoOffsetPos;
            }
        }

        public Vector3 draggingDPos
        {
            get { return gizmoPos.transform.position - gizmoPos.prevPos; }
        }

        public Quaternion draggingDRot
        {
            get { return Quaternion.Inverse(gizmoRot.prevRot) * gizmoRot.transform.rotation; }
        }

        //ギズモ回転
        public Quaternion rotation
        {
            get { return gizmoRot.transform.rotation; }
            set { gizmoPos.transform.rotation = gizmoRot.transform.rotation = value; }
        }

        public Quaternion rotationPos
        {
            get { return gizmoPos.transform.rotation; }
            set { gizmoPos.transform.rotation = value; }
        }

        //
        public Transform target
        {
            get { return gizmoPos.target; }
        }

        public string targetName { get => gizmoPos.targetName; }

        public void setTarget(Transform tgt, string name, bool on = true)
        {
            gizmoPos.setTarget(tgt, name, on);
            gizmoRot.setTarget(tgt, name, on);
        }

        public void tryOffTarget(Transform tgt)
        {
            gizmoPos.tryOffTarget(tgt);
            gizmoRot.tryOffTarget(tgt);
        }

        public void resetTarget()
        {
            gizmoPos.resetTarget();
            gizmoRot.resetTarget();
        }

        //サイズ
        public float sizeRot
        {
            get { return gizmoPos.sizeRot; }
            set { gizmoPos.sizeRot = gizmoRot.sizeRot = value; }
        }

        public float sizeHandle
        {
            get { return gizmoPos.sizeHandle; }
            set { gizmoPos.sizeHandle = gizmoRot.sizeHandle = value; }
        }

        public float threthold
        {
            get { return gizmoPos.threthold; }
            set { gizmoPos.threthold = gizmoRot.threthold = value; }
        }

        public void Update()
        {
            gizmoPos.Update();
            gizmoRot.Update();
        }

        //ドラッグ判定、複数ギズモを表示中でも個別判定できるようにした
        public bool isDrag
        {
            get
            {
                return gizmoPos.isDrag || gizmoRot.isDrag;
            }
        }

        //ドラッグエンド判定用（変化を見るだけなので毎フレーム呼び出す必要あり）
        public bool isDragEnd
        {
            get
            {
                return gizmoPos.isDragEnd || gizmoRot.isDragEnd;
            }
        }

        public OhMyGizmoPosRot()
        {
        }

        public void Init(Transform parent_tr, string gizmo_name)
        {
            gizmoPos = OhMyGizmo.AddGizmo(parent_tr, gizmo_name + "_Pos");
            gizmoRot = OhMyGizmo.AddGizmo(parent_tr, gizmo_name + "_Rot");
            trs = new OhMyGizmo[2] { gizmoPos, gizmoRot };

            gizmoPos.modePos = true;
            gizmoRot.modeRot = true;
            gizmoPos.transform.localRotation = Quaternion.identity;
        }

        static public OhMyGizmoPosRot AddGizmo(Transform parent_tr, string gizmo_name)
        {
            var o = new GameObject(gizmo_name).AddComponent<OhMyGizmoPosRot>();
            o.Init(parent_tr, gizmo_name);
            return o;
        }

        public void Destroy()
        {
            OhMyGizmo.Destroy(gizmoPos);
            OhMyGizmo.Destroy(gizmoRot);
        }
    }
}

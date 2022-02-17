using nnPlugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BepInEx.IO_ExSlider.Plugin
{
    class PoseCtrl
    {
    }

    public class CtrlBone
    {
        // ギズモの選択対象
        static public RotctrlBone _NowGizmoRotTargetCtrl;

        List<RotctrlBone> allCtrl = new List<RotctrlBone>();

        public T addCtrl<T>() where T : RotctrlBone, new()
        {
            return RotctrlBone.newRotctrlBone<T>(this);
        }

        public void updateCtrlList(List<RotctrlBone> list)
        {
            allCtrl = list;
        }

        public bool SetEuler(string key, Vector3 rot)
        {
            bool isNotFullBody = _NowGizmoRotTargetCtrl.GetType().IsSubclassOf(typeof(RotctrlBone));

            foreach (var v in allCtrl)
            {
                if (isNotFullBody)
                {
                    if (!v.GetType().IsSubclassOf(typeof(RotctrlBone)))
                        continue; // フルボディならスキップ
                }

                if (v.dicEulers.ContainsKey(key))
                {
#if DEBUG
                    Debug.Log(key);
                    foreach (var a in v.dicEulers)
                    {
                        Console.WriteLine(a.Key);
                    }
#endif
                    v.dicEulers[key] = rot;
                    return true;
                }
            }
            return false;
        }

        // 遅い＆信頼性が低い気がする
        //public void WriteEuler2Bone_old(Transform tgt)
        //{
        //    foreach (var c in allCtrl)
        //    {
        //        if (!c.enable)
        //            continue;
        //        foreach (var v in c.dicEulers)
        //        {
        //            var tr = tgt.FindSp(v.Key);
        //            if (tr)
        //                tr.localRotation = Quaternion.Lerp(tr.localRotation, Quaternion.Euler(v.Value), c.blend);
        //        }
        //    }
        //}

        public void WriteEuler2Bone(Transform tgt)
        {
            foreach (var c in allCtrl)
            {
                if (!c.enable)
                    continue;

                WriteProc.dic = c.dicEulers;
                WriteProc.blend = c.blend;
                WriteProc.WriteEuler2Bone2(tgt);
            }
        }

        static class WriteProc
        {
            internal static Dictionary<string, Vector3> dic;
            internal static float blend;
            static Vector3 value;
            public static void WriteEuler2Bone2(Transform tgt, string path = "")
            {
                if (dic == null || dic.Count <= 0)
                    return;

                //遅い foreach (Transform t in tgt)
                for(int i = 0; i < tgt.childCount; i++)
                {
                    var t = tgt.GetChild(i);
                    string path2 = $"{path}{t.name}";

                    if (dic.TryGetValue(path2, out value))
                    {
                        t.localRotation = Quaternion.Lerp(t.localRotation, Quaternion.Euler(value), blend);
                    }

                    if (t.childCount > 0)
                        WriteEuler2Bone2(t, $"{path2}/");
                }
            }
        }

        public class RotctrlBone
        {
            public bool enable = true;
            public float blend = 1f;
            public Dictionary<string, Vector3> dicEulers = new Dictionary<string, Vector3>();
            private CtrlBone parent;

            //static List<RotctrlBone> allCtrl = new List<RotctrlBone>();

            static public T newRotctrlBone<T>(CtrlBone ctr) where T : RotctrlBone, new()
            {
                var t = new T();
                t.parent = ctr;
                ctr.allCtrl.Add(t);
                return t;
            }

            public RotctrlBone()
            {
                //throw new Exception("RotctrlBoneを直接生成することはできません");
            }

            public RotctrlBone(CtrlBone ctr)
            {
                parent = ctr;
                parent.allCtrl.Add(this);
            }

            ~RotctrlBone()
            {
                parent.allCtrl.Remove(this);
            }

            public bool SetEuler(string key, Vector3 rot)
            {
                return parent.SetEuler(key, rot);
            }

            public virtual string[] boneroots
            {
                get { return TargetIni.targetIni.listBipBoneRoots; }
            }

            //readonly Dictionary<string, string> dicbonename = new Dictionary<string, string>()
            //{
            //    { "bip01", "Neko_Root" },
            //    { "bip01_02", "Usa_Root" },
            //};

            public virtual string transBoneName(string s)
            {
                //foreach (var v in dicbonename)
                //{
                //    s = s.Replace(v.Key, v.Value);
                //}
                return s;
            }

            public static List<string> ignoreBN = new List<string>
            {
                "Hair",
                "Collider",
                "HF01_",
                "HS_",
                "Gizmo",
            };

            public virtual void capAll(Transform target)
            {
                foreach (var s in this.boneroots)
                {
                    this.capAll(this.dicEulers, target, s);
                }
            }

            public virtual void capAll(Dictionary<string, Vector3> dicBoneEulers, Transform tr0, string s)
            {
                //MyDebug.Log(s);
                //WriteEuler2BoneでFindSp使わなくなったのでキャッシュ不要
                //var tr = tr0.FindSp(s);
                var tr = tr0.Find(s); // GUIから呼び出すだけなのでキャッシュ無し 
                if (!tr)
                    return;

                if (!ignoreBN.Any(x => tr.name.Contains(x)))
                {
                    dicBoneEulers[s] = tr.localEulerAngles;
                }

                for (int i = 0; i < tr.childCount; i++)
                //foreach (Transform t in tr)
                {
                    var t = tr.GetChild(i);
                    if (!t || string.IsNullOrEmpty(t.name))
                        continue;

                    var n = t.name;
                    //if (n.StartsWith("Left", StringComparison.Ordinal) || n.StartsWith("Right", StringComparison.Ordinal))
                    {
                        var ns = s + "/" + n;
                        capAll(dicBoneEulers, tr0, ns);
                    }
                }
            }

            //public static void CapAll<T>(T rcbCtrl, Transform target, string rootBone) where T : RotctrlBone
            //{
            //    foreach (var s in rcbCtrl.boneroot)
            //        rcbCtrl.capAll(rcbCtrl.dicEulers, target, rootBone);
            //}
        }
    }

}

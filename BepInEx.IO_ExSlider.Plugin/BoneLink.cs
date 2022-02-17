using nnPlugin;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BepInEx.IO_ExSlider.Plugin
{
    public class BoneLink
    {
        static Transform _src_root, _dst_root;
        static bool ikLink = true;

        public static void Update(Transform src_root, Transform dst_root, bool reverse = false, RigUtil.LimbsIKTargets limbsIKTargets = null)
        {
            //dst_root.position = src_root.position;
            //dst_root.localPosition += posOffset;

            ReverseLink = reverse;
            _src_root = src_root;
            _dst_root = dst_root;

            findBoneInit(src_root, dst_root);
            doBoneLink(src_root, dst_root);

            if (limbsIKTargets != null)
                ikLink = true;
            else
                ikLink = false;

            // IK
            if (ikLink)
            {
                limbsIKTargets.ikWeight = 0f;

                int i = 0;
                foreach (var v in dicMantgt)
                {
                    if (v.Value)
                    {
                        if (limbsIKTargets.limbs[i] == null)
                        {
                            limbsIKTargets.limbs[i] = new RigUtil.IKTarget();
                        }
                        limbsIKTargets.limbs[i].ikTarget = v.Value;
                        limbsIKTargets.ikWeight = 1f;
                    }
                    i++;
                }

                i = 0;
                foreach (var v in dicManbend)
                {
                    if (v.Value)
                    {
                        if (limbsIKTargets.limbs[i] != null && limbsIKTargets.limbs[i].ikTarget)
                        {
                            limbsIKTargets.limbs[i].bendGoel = v.Value;
                        }
                    }
                    i++;
                }
            }
        }

        public static void Reset(GameObject kucus)
        {
        }

        static Dictionary<string, string> dicTransM2F = new Dictionary<string, string>
        {
            {"PC00Bip", "bip01" },
            {"_syA", "" },
            {"_syB", "" },
            {"_syC", "" },
            {"syA", "02" },
            {"syB", "02" },
            {"syC", "02" },
        };

        static Dictionary<string, Quaternion> dicFixrot = new Dictionary<string, Quaternion>
        {
            //{ "Right--", Quaternion.Euler(0, 0, -45 ) },
            //{ "Left---", Quaternion.Euler(0, 0, 45 ) },
        };

        static int _tgtid0 = 0;
        static int _tgtid = 0;
        static void findBoneInit(Transform src_root, Transform dst_root)
        {
            var id0 = src_root.GetInstanceID();
            var id = dst_root.GetInstanceID();
            if (id != _tgtid || id0 != _tgtid0)
            {
                // ターゲットが変わったらキャッシュをクリア
                dicBone.Clear();
                _tgtid0 = id0;
                _tgtid = id;
            }
        }

        static Dictionary<string, Transform> dicBone = new Dictionary<string, Transform>();
        static Transform findBone(Transform t, Transform src)//string name)
        {
            var name = src.name;

            if (dicBone.TryGetValue(name, out Transform o))
                return o;


            //if (ikLink)
            {
                foreach(var v in dicMantgt.Keys.ToArray())
                {
                    if (name.Contains(v))
                        dicMantgt[v] = src;
                }
                foreach (var v in dicManbend.Keys.ToArray())
                {
                    if (name.Contains(v))
                        dicManbend[v] = src;
                }
                
                //if (dicMantgt.ContainsKey(src.name))
                //    dicMantgt[src.name] = src;
                //if (dicManbend.ContainsKey(src.name))
                //    dicManbend[src.name] = src;
            }

            var dc = t.Find(name);
            if (!dc)
            {
                foreach (var v in dicTransM2F)
                {
                    if (!name.Contains(v.Key))
                        continue;
                    
                    dc = t.Find(name.Replace(v.Key, v.Value));
                    if (_dst_root.name == "HS_kiten_02" && !dc)
                        dc = t.Find(name.Replace(v.Key, v.Value) + "_02");

                    if (dc)
                        break;
                }
            }
            dicBone[name] = dc;
            return dc;
        }

        static Dictionary<string, Transform> dicMantgt = new Dictionary<string, Transform>
        {
            { "L Foot" , null },
            { "R Foot" , null },
            { "L Hand" , null },
            { "R Hand" , null },
        };
        static Dictionary<string, Transform> dicManbend = new Dictionary<string, Transform>
        {
            { "L Calf" , null },
            { "R Calf" , null },
            { "L Forearm" , null },
            { "R Forearm" , null },
        };

        internal static Vector3 rotfixHandR = new Vector3(0f, 0f, -45f);
        internal static Vector3 rotfixHandL = new Vector3(0f, 0f, 45f);
        internal static Vector3 posOffset = Vector3.zero;

        internal static bool ReverseLink = false;
        public static void doBoneLink(Transform src, Transform dst, bool skip = false)
        {
            //dicFixrot["Left---"] = Quaternion.Euler(rotfixHandL);
            //dicFixrot["Right---"] = Quaternion.Euler(rotfixHandR);

            if (!skip)
            {
                if (!ReverseLink)
                {
                    if (dicFixrot.TryGetValue(src.name, out Quaternion q))
                        dst.localRotation = q * src.localRotation;
                    else
                        dst.localRotation = src.localRotation;

                    //if (Input.GetKey(KeyCode.B))
                    //{
                    //    dst.localPosition = src.localPosition;
                    //}
                }
                else
                {
                    if (dicFixrot.TryGetValue(src.name, out Quaternion q))
                        src.localRotation = Quaternion.Inverse(q) * dst.localRotation;
                    else
                        src.localRotation = dst.localRotation;

                    //if (Input.GetKey(KeyCode.B))
                    //{
                    //    src.localPosition = dst.localPosition;
                    //}
                }
            }
            else
            {
                //if (src.name.Contains("HS "))
                //    skip = true;
            }


            for (int i = 0; i < src.childCount; i++)
            {
                var sc = src.GetChild(i);
                //var name = sc.name;

                //if (sc.name.StartsWith("HS00_", System.StringComparison.Ordinal))
                //    continue;

                var dc = findBone(dst, sc);
                if (dc)
                {
                    doBoneLink(sc, dc, skip);
                }
                else
                {
                    //MyDebug.Log("notfound:" + sc.name);
                }
            }
        }
    }

}

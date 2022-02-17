using BepInEx.IO_ExSlider.Plugin;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace nnPlugin
{
    public struct Offset
    {
        public Vector3 dpos;
        public Vector3 drot;

        public Offset(Vector3 pos, Vector3 rot)
        {
            dpos = pos;
            drot = rot;
        }
    }

    public static class PlgExtentionsFixAnimatorIssue
    {
        public static string realName(this RuntimeAnimatorController controller) 
        {
            if (controller is AnimatorOverrideController)
            {
                // 真名()をよこせ！！１１１１
                return ((AnimatorOverrideController)controller).runtimeAnimatorController.name;
            }
            return controller.name;
        }

    }

    public static class CacheRegex
    {
        static Dictionary<string, Regex> regexs = new Dictionary<string, Regex>();

        static Regex getRegex(string pattern)
        {
            Regex regex;
            if (!regexs.TryGetValue(pattern, out regex))
                regex = regexs[pattern] = new Regex(pattern);
            return regex;
        }

        //class isMatchCache
        //{
        //    public string input;
        //    public string pattern;
        //    public bool result;

        //    public isMatchCache(string input, string pattern, bool result)
        //    {
        //        this.input = input;
        //        this.pattern = pattern;
        //        this.result = result;
        //    }
        //}
        //static Dictionary<KeyValuePair<string, string>, bool> isMatchCaches = new Dictionary<KeyValuePair<string, string>, bool>();
        static Dictionary<string, Dictionary<string, bool>> isMatchCaches = new Dictionary<string, Dictionary<string, bool>>();

        public static bool IsMatch(string input, string pattern)
        {
            //var pair = new KeyValuePair<string, string>(input, pattern);
            //if (isMatchCaches.TryGetValue(pair, out bool ret))
            //    return ret;
            //return isMatchCaches[pair] = getRegex(pattern).IsMatch(input);

            if (isMatchCaches.TryGetValue(pattern, out Dictionary<string, bool> pair))
            {
                if (pair.TryGetValue(input, out bool ret))
                    return ret;
            }
            else
                isMatchCaches[pattern] = new Dictionary<string, bool>();

            return isMatchCaches[pattern][input] = getRegex(pattern).IsMatch(input);
        }

        public static string Replace(string input, string pattern, string replacement)
        {
            return getRegex(pattern).Replace(input, replacement);
        }
    }

    public class Clone
    {
        public static T Data<T>(T obj)
        {
            try
            {
                System.Xml.Serialization.XmlSerializer serializer =
                        new System.Xml.Serialization.XmlSerializer(typeof(T));

                var sw = new System.IO.StringWriter();
                serializer.Serialize(sw, obj);
                var txt = sw.ToString();

                return (T)serializer.Deserialize(new System.IO.StringReader(txt));
            }
            catch (Exception e)
            {
                Debug.Log("Clone Error:" + e);
            }
            
            return default(T);
        }
    }

    public static class MyRotateAround
    {
        static Transform _p0, _p1;

        public static void Proc(Transform tgt, Transform axis, Vector3 euler)
        {
            if (!_p0) _p0 = new GameObject("MyRotateAround0").transform;
            if (!_p1)
            {
                _p1 = new GameObject("MyRotateAround1").transform;
                _p1.SetParent(_p0);
            }

            _p0.position = axis.position;
            _p0.rotation = axis.rotation;

            _p1.position = tgt.position;
            _p1.rotation = tgt.rotation;

            _p0.rotation *= Quaternion.Euler(euler);

            tgt.position = _p1.position;
            tgt.rotation = _p1.rotation;
        }

        /// <summary>
        ///     親子逆回転
        /// </summary>
        /// <param name="tgt"> axisの祖先 </param>
        /// <param name="axis"> tgtの子孫 </param>
        /// <param name="euler"></param>
        public static void Proc2(Transform tgt, Transform axis, Vector3 euler)
        {
            if (!_p0) _p0 = new GameObject("MyRotateAround0").transform;
            if (!_p1)
            {
                _p1 = new GameObject("MyRotateAround1").transform;
                _p1.SetParent(_p0);
            }

            _p0.position = axis.position;
            _p0.rotation = axis.rotation;

            _p1.position = tgt.position;
            _p1.rotation = tgt.rotation;

            Quaternion qlink = Quaternion.Inverse(tgt.rotation) * axis.rotation;
            tgt.rotation = ((axis.rotation * Quaternion.Euler(euler)) * Quaternion.Inverse(qlink));

            tgt.position -= (axis.position - _p0.position);
            tgt.position -= (axis.position - _p0.position);
        }
    }

    public class TransformData
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        public TransformData(Vector3 p, Quaternion r, Vector3 s)
        {
            position = p;
            rotation = r;
            scale = s;
        }

        public TransformData(Transform tr) : this(tr.position, tr.rotation, tr.localScale) { }
        public TransformData(Transform tr, bool LocalOrLocal) : this(tr.localPosition, tr.localRotation, tr.localScale) { }
    }

    public static class Test
    {
        static Transform _p0, _p1;


        static System.IO.StreamWriter sw;
        public static void allObjLog(GameObject tgt)
        {
            string s = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            using (sw = new System.IO.StreamWriter(s + "\\" + tgt.name + ".txt", false, new System.Text.UTF8Encoding(false)))
            {
                loop(tgt.transform, "");
            }
        }

        static void loop(Transform trtgt, string path)
        {
            foreach (Transform t in trtgt)
            {
                sw.WriteLine(t.name);

                string path2 = string.Format("{0}{1}", path, t.name);
                if (t.childCount > 0)
                    loop(t, path2 + "/");
            }
        }

        public static void Proc()
        {
            if (!_p0) _p0 = new GameObject("test0").transform;
            if (!_p1)
            {
                _p1 = new GameObject("test1").transform;
                _p1.SetParent(_p0);
            }
            //var obj = GetResourceByName<GameObject>("SY01");
            //var ins = GameObject.Instantiate(obj);

            _p0.rotation = Quaternion.Euler(Vector3.up);
            _p1.localRotation = Quaternion.Euler(Vector3.right);

            Quaternion qlink = Quaternion.Inverse(_p0.rotation) * _p1.rotation;
            Quaternion qlink2 = _p1.rotation * Quaternion.Inverse(_p0.rotation);

            Debug.Log($"1 {_p0.eulerAngles} - {_p1.eulerAngles} : {qlink.eulerAngles} : {qlink2.eulerAngles}");

            _p1.rotation *= Quaternion.Euler(Vector3.forward);

            qlink = (_p1.rotation) * Quaternion.Inverse(qlink);
            qlink2 = (_p1.rotation) * Quaternion.Inverse(qlink2);

            Debug.Log($"2 {_p0.eulerAngles} - {_p1.eulerAngles} : {qlink.eulerAngles} : {qlink2.eulerAngles}");

            _p0.localRotation = Quaternion.Euler(Vector3.up) * qlink;
            qlink = _p1.rotation;
            
            _p0.localRotation = Quaternion.Euler(Vector3.up) * qlink2;
            qlink2 = _p1.rotation;
            
            _p1.localRotation = Quaternion.Euler(Vector3.right);

            Debug.Log($"3 {_p0.eulerAngles} - {_p1.eulerAngles} : {qlink.eulerAngles} : {qlink2.eulerAngles}");
        }
    }

    class TransformTR
    {
        public Vector3 position;
        public Quaternion rotation;

        public TransformTR(Vector3 t, Quaternion r)
        {
            position = t;
            rotation = r;
        }

        public TransformTR(Transform tr) : this(tr.position, tr.rotation) { }
    }

    public static class PlgUtil
    {
        public static string GetFullPath(Transform t)
        {
            string path = t.name;

            while (t.parent != null)
            {
                path = $"{t.parent.name}/{path}";
                t = t.parent;
            }
            return path;
        }

        public static bool isRealNull(System.Object obj)
        {
            return (!(obj is UnityEngine.Object) && obj == null);


            // ネタ元：Unityのnullはnullじゃないかもしれない
            // https://qiita.com/satanabe1@github/items/e896303859be5d42c188
            //if (obj is UnityEngine.Object)
            //{
            //    if ((UnityEngine.Object)obj != null)
            //    {
            //        // 元気なUnityオブジェクト
            //    }
            //    else
            //    {
            //        // 死んだフリしているUnityオブジェクト
            //    }
            //}
            //else
            //{
            //    if (obj != null)
            //    {
            //        // 普通のnullでないオブジェクト
            //    }
            //    else
            //    {
            //        // ガチnull
            //    }
            //}
        }

        public static string GetObjNameFromPath(string objpath)
        {
            string name = objpath;

            if (string.IsNullOrEmpty(name))
                return name;

            int index = objpath.LastIndexOf('/');
            if (index > 0 && objpath.Length > index + 1)
            {
                name = objpath.Substring(index + 1);
            }
            return name;
        }

        public static string GetParentNameFromPath(string objpath)
        {
            string name = objpath;

            if (string.IsNullOrEmpty(name))
                return name;

            int index = objpath.LastIndexOf('/');
            if (index > 1 && objpath.Length > index + 1)
            {
                int index2 = objpath.LastIndexOf('/', index-1);

                if (index2 < 0)
                    index2 = 0;
                else
                    index2 += 1;

                return objpath.Substring(index2, index - index2);
            }
            return string.Empty;
        }

        public static T GetResourceByName<T>(string name) where T : UnityEngine.Object
        {
            var a = Resources.FindObjectsOfTypeAll<T>()
                            .Where(c => c.name == name).ToArray();

            if (a != null && a.Length > 0)
            {
                return a[0];
            }

            return default(T);
        }

        // tは0～1の値とすること
        const float HalfPi = Mathf.PI * 0.5f;
        public static float SlerpHalf(float a, float b, float t)
        {
            float c = b - a;
            return (1f - Mathf.Sin((Mathf.Lerp(0f, 1f, t) + 1f) * HalfPi)) * c + a;
        }

        // tは0～1の値とすること
        public static float Slerp(float a, float b, float t)
        {
            float c = b - a;

            if (t <= 0.5f)
                return Mathf.Sin(Mathf.Lerp(0f, 1f, t) * Mathf.PI) / 2f * c + a;
            else
                return (1f - (Mathf.Sin(Mathf.Lerp(0f, 1f, t) * Mathf.PI) / 2f)) * c + a;
        }
    }

    internal class UiUtil
    {
        // https://answers.unity.com/questions/160285/text-with-outline.html
        public static void DrawOutlineLabel(Rect r, string t, int strength, GUIStyle style)
        {
            var color = GUI.color;
            GUI.color = new Color(0, 0, 0, color.a);
            int i;
            for (i = -strength; i <= strength; i++)
            {
                GUI.Label(new Rect(r.x - strength, r.y + i, r.width, r.height), t, style);
                GUI.Label(new Rect(r.x + strength, r.y + i, r.width, r.height), t, style);
            }
            for (i = -strength + 1; i <= strength - 1; i++)
            {
                GUI.Label(new Rect(r.x + i, r.y - strength, r.width, r.height), t, style);
                GUI.Label(new Rect(r.x + i, r.y + strength, r.width, r.height), t, style);
            }
            GUI.color = color;

            GUI.Label(r, t, style);
        }

        internal class GuiEnable : IDisposable
        {
            bool enabledBk = false;
            public GuiEnable(bool enabled, bool overwrite = false)
            {
                enabledBk = GUI.enabled;
                if(GUI.enabled || overwrite)
                    GUI.enabled = enabled;
            }

            public void Dispose()
            {
                GUI.enabled = enabledBk;
            }
        }

        internal class GuiColor : IDisposable
        {
            Color colorBk = Color.white;
            public GuiColor(Color newcolor, bool enabled = true)
            {
                colorBk = GUI.color;
                if (enabled)
                    GUI.color = newcolor;
            }

            public void Dispose()
            {
                GUI.color = colorBk;
            }
        }

    }

    [Serializable]
    public class StrV3Pair
    {
        public string Key;
        public Vector3 Value;

        public StrV3Pair()
        {
        }

        public StrV3Pair(string Key, Vector3 Value)
        {
            this.Key = Key;
            this.Value = Value;
        }
    }

    [Serializable]
    public class PositionByState
    {
        public string stateName;
        public ScenePos trs;

        public PositionByState()
        {
        }

        public PositionByState(string stateName, ScenePos pos)
        {
            this.stateName = stateName;
            this.trs = pos;
        }
    }

    [Serializable]
    public class PositionByStates
    {
        public bool enabled = false;
        public List<PositionByState> list = new List<PositionByState>();

        public PositionByStates()
        {
        }
    }

    /* 今回は不使用,複数シーンのゲームで使うときはシーン移動時のキャッシュクリア必要
    internal class BlendUtil
    {
        Dictionary<string, Dictionary<string, int>> _dicBlendIndex = new Dictionary<string, Dictionary<string, int>>();
        Dictionary<string, SkinnedMeshRenderer> _dicSkm = new Dictionary<string, SkinnedMeshRenderer>();

        GameObject @object;

        public BlendUtil(GameObject rootObject)
        {
            @object = rootObject;
        }

        void initBlend(SkinnedMeshRenderer skm, string modelPath)
        {
            Mesh shm = skm.sharedMesh;
            _dicBlendIndex[modelPath] = new Dictionary<string, int>();
            for (int i = 0; i < shm.blendShapeCount; i++)
            {
                var n = shm.GetBlendShapeName(i);
                _dicBlendIndex[modelPath][n] = i;

                Debug.Log(modelPath + ":" + n);
            }
        }

        SkinnedMeshRenderer getSkinnedMeshRenderer(string modelPath)
        {
            SkinnedMeshRenderer skm;

            if (!_dicSkm.TryGetValue(modelPath, out skm))
            {
                var face = @object.transform.FindSp(modelPath);
                skm = _dicSkm[modelPath] = face.gameObject.GetComponent<SkinnedMeshRenderer>();
            }
            return skm;
        }

        public void setBlend(string modelPath, string name, float val)
        {
            //dbgstr = modelPath + " " + name;
            var skm = getSkinnedMeshRenderer(modelPath);

            if (!_dicBlendIndex.ContainsKey(modelPath))
            {
                initBlend(skm, modelPath);
            }

            int index;
            if (_dicBlendIndex != null
                && _dicBlendIndex[modelPath].TryGetValue(name, out index))
            {
                skm.SetBlendShapeWeight(index, val);
            }
        }

        public int getBlendIndex(string modelPath, string name)
        {
            //dbgstr = modelPath + " " + name;

            if (!_dicBlendIndex.ContainsKey(modelPath))
            {
                var skm = getSkinnedMeshRenderer(modelPath);
                initBlend(skm, modelPath);
            }

            int index;
            if (_dicBlendIndex != null
                && _dicBlendIndex[modelPath].TryGetValue(name, out index))
            {
                return index;
            }

            return -1;
        }

        public float getBlend(string modelPath, string name)
        {
            //dbgstr = modelPath + " " + name;

            var skm = getSkinnedMeshRenderer(modelPath);

            if (!_dicBlendIndex.ContainsKey(modelPath))
            {
                initBlend(skm, modelPath);
            }

            int index;
            if (_dicBlendIndex != null
                && _dicBlendIndex[modelPath].TryGetValue(name, out index))
            {
                return skm.GetBlendShapeWeight(index);
            }

            return -1;
        }
    }*/
}

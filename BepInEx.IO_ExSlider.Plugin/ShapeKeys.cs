using nnPlugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BepInEx.IO_ExSlider.Plugin
{
    public class ShapeKeys
    {
        PlgCharaCtrl ctrl;

        // ロード時雛型（ロード時の補填用）
        //public Dictionary<string, Dictionary<int, float>> dicShapeKeys0 = new Dictionary<string, Dictionary<int, float>>()
        //{
        //    //{ "../", new Dictionary<int, float>() },
        //};

        public class Map
        {
            public Dictionary<string, int> dicNameToIndex = new Dictionary<string, int>();
        }

        // 管理用
        internal Dictionary<string, Map> dicShapeKeysMap = new Dictionary<string, Map>();
        internal Dictionary<string, float> dicShapeKeysDefval = new Dictionary<string, float>();
        internal HashSet<string> dicShapeNames = new HashSet<string>();

        // 設定値
        public class Edits
        {
            // オブジェクト別（レガシー）
            public Dictionary<string, Dictionary<int, float>> dicShapeKeys = new Dictionary<string, Dictionary<int, float>>();
            
            // 一括（新）
            public Dictionary<string, float> dicNameAndValues = new Dictionary<string, float>();
            public bool useLegacyCtrl = false;
        }

        public Edits edits = new Edits();
        internal bool GuiOpenFlag = false;

        public ShapeKeys(PlgCharaCtrl parent)
        {
            ctrl = parent;
        }

        bool Init()
        {
            if (!ctrl.modelObject)
            {
                return false;
            }

            if (dicShapeKeysMap.Count > 0)
                return true;

            if (IO_ExSlider.cfg.AddNoseBlendShape)
                NoseShape.AddBlendShape(ctrl.modelObject);

            var root = ctrl.modelObject.transform;
            foreach (Transform t in root)
            {
                try
                {
                    var go = t.gameObject;
                    var skm = go.GetComponent<SkinnedMeshRenderer>();
                    if (!skm)
                        continue;

                    var shm = skm.sharedMesh;
                    if (!shm)
                        continue;

                    if (shm.blendShapeCount <= 0)
                        continue;

                    MyDebug.Log($"Obj:{go.name}  Blends:{shm.blendShapeCount}");

                    //dicShapeKeys0.Add(go.name, new Dictionary<int, float>());

                    if (!edits.dicShapeKeys.ContainsKey(go.name))
                        edits.dicShapeKeys.Add(go.name, new Dictionary<int, float>());

                    dicShapeKeysMap[go.name] = new Map();
                    for (int i = 0; i < shm.blendShapeCount; i++)
                    {
                        dicShapeKeysMap[go.name].dicNameToIndex[shm.GetBlendShapeName(i)] = i;

                        var fullsname = go.name + "@" + i;
                        dicShapeKeysDefval[fullsname] = skm.GetBlendShapeWeight(i);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }

            return false;
        }

        public void OnLateUpdate()
        {
            if (!Init())
                return;

            bool guiUpdate = GuiOpenFlag && IO_ExSlider.GuiFlag && Time.frameCount % 3 == 0;
                                                                 
            // シェイプキー
            if (!edits.useLegacyCtrl &&
                (edits.dicNameAndValues.Count > 0 || guiUpdate)
            ) {
                var root = ctrl.modelObject.transform;
                try
                {
                    foreach (var keys in dicShapeKeysMap)
                    {
                        if (!guiUpdate)
                        {
                            if (!keys.Value.dicNameToIndex.Any(x => edits.dicNameAndValues.ContainsKey(x.Key)))
                                continue;
                        }

                        var tr = root.FindSp(keys.Key);
                        if (!tr)
                            continue;

                        if (!tr.gameObject.activeInHierarchy)
                            continue;

                        var go = tr.gameObject;
                        var skm = go.GetComponent<SkinnedMeshRenderer>();

                        var shm = skm.sharedMesh;
                        if (!shm)
                            continue;

                        foreach (var o in keys.Value.dicNameToIndex)
                        {
                            int i = o.Value;

                            if (edits.dicNameAndValues.TryGetValue(o.Key, out float value))
                            {
                                skm.SetBlendShapeWeight(i, value);
                            }
                            else if (guiUpdate || UnityEngine.Random.Range(0, 2) == 0)
                            {
                                var fullsname = keys.Key + "@" + i;
                                dicShapeKeysDefval[fullsname] = skm.GetBlendShapeWeight(i);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                }
            }
            else if (edits.dicShapeKeys.Count > 0 && edits.useLegacyCtrl)
            {
                var root = ctrl.modelObject.transform;
                foreach (var keys in edits.dicShapeKeys)
                {
                    try
                    {
                        var tr = root.FindSp(keys.Key);
                        if (!tr)
                            continue;

                        if (!tr.gameObject.activeInHierarchy)
                            continue;

                        var go = tr.gameObject;
                        var skm = go.GetComponent<SkinnedMeshRenderer>();

                        var shm = skm.sharedMesh;
                        if (!shm)
                            continue;

                        if (shm.blendShapeCount <= 0)
                            continue;

                        if (!ctrl.isClone)
                        {
                            for (int i = 0; i < shm.blendShapeCount; i++)
                            {
                                var v = keys.Value;
                                if (v.ContainsKey(i))
                                {
                                    skm.SetBlendShapeWeight(i, v[i]);
                                }
                                else
                                {
                                    var fullsname = keys.Key + "@" + i;
                                    dicShapeKeysDefval[fullsname] = skm.GetBlendShapeWeight(i);
                                }
                            }
                        }
                        else
                        {
                            foreach (var v in keys.Value)
                            {
                                var oldv = skm.GetBlendShapeWeight(v.Key);
                                if (oldv != v.Value)
                                {
                                    var fullsname = keys.Key + "@" + v.Key;
                                    dicShapeKeysDefval[fullsname] = oldv;
                                }
                                skm.SetBlendShapeWeight(v.Key, v.Value);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning(e);
                    }
                }
            }

        }

        public void OnNewSceneLoaded()
        {
            // シーン移動時
            // マッピングをクリア
            dicShapeKeysMap.Clear();
        }


        public void RestoreAllShapes()
        {
            if (!ctrl.modelObject)
                return;

            var root = ctrl.modelObject.transform;
            
            foreach (var keys in dicShapeKeysMap)
            {
                try
                {
                    var tr = root.FindSp(keys.Key, true);
                    if (!tr)
                        continue;

                    var skm = tr.gameObject.GetComponent<SkinnedMeshRenderer>();
                    var shm = skm.sharedMesh;
                    for (int i = 0; i < shm.blendShapeCount; i++)
                    {
                        var fullsname = keys.Key + "@" + i;
                        if (dicShapeKeysDefval.ContainsKey(fullsname))
                        {
                            skm.SetBlendShapeWeight(i, dicShapeKeysDefval[fullsname]);
                        }
                        //else
                        //    skm.SetBlendShapeWeight(i, 0f);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
        }

        public void ResetAllDic()
        {
            edits.dicNameAndValues.Clear();
            edits.dicShapeKeys.Clear();

            RestoreAllShapes();
            dicShapeKeysMap.Clear();

            //ResetOldDicShapes();
        }

        public void ResetOldDicShapes()
        {
            if (!ctrl.modelObject)
                return;

            var root = ctrl.modelObject.transform;
            foreach (var keys in edits.dicShapeKeys)
            {
                try
                {
                    var go = root.FindSp(keys.Key, true).gameObject;
                    var skm = go.GetComponent<SkinnedMeshRenderer>();
                    var shm = skm.sharedMesh;
                    for (int i = 0; i < shm.blendShapeCount; i++)
                    {
                        if (keys.Value.ContainsKey(i))
                        {
                            var fullsname = keys.Key + "@" + i;
                            keys.Value.Remove(i);

                            if (dicShapeKeysDefval.ContainsKey(fullsname))
                            {
                                skm.SetBlendShapeWeight(i, dicShapeKeysDefval[fullsname]);
                                //dicShapeKeysDefval.Remove(fullsname);
                            }
                            else
                                skm.SetBlendShapeWeight(i, 0f);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
        }

    }

    public class NoseShape
    {
        const string ShapeKeyName = "@Nose_Flat";
        static Vector3 bottomV3;

        public static void AddBlendShape(GameObject model)
        {
            var face = model.transform.Find("CH01_face");
            var faceL = model.transform.Find("CH01_faceL");

            if (!face)
            {
                return;
            }

            Debug.Log("鼻高さシェイプキー作成");
            CreateShapeKey(face, false);
            Debug.Log("鼻高さシェイプキー作成(アウトライン)");
            CreateShapeKey(faceL, true);
        }

        static void CreateShapeKey(Transform face, bool outline) 
        { 
            var skmf = face.GetComponent<SkinnedMeshRenderer>();
            if (skmf.sharedMesh.GetBlendShapeIndex(ShapeKeyName) >= 0)
                return;

            var verts = skmf.sharedMesh.vertices;
            var tris = skmf.sharedMesh.triangles;

            int peak = -1;
            Vector3 peakV3 = Vector3.zero;

            for (int i = 0; i < verts.Length; i++)
            {
                if (peak < 0 || peakV3.y > verts[i].y)
                {
                    peak = i;
                    peakV3 = verts[i];
                }
            }

            if (peak < 0)
                return;

            Dictionary<int, int> listTgt = new Dictionary<int, int>();
            const int LIMIT = 6;

            listTgt.Add(peak, 0);

            // 鼻の周りはメッシュが分割されているようなので繰り返す
            while (!listTgt.Any(x => x.Value >= LIMIT))
            {
                for (int i = 0; i < verts.Length; i++)
                {
                    if (peakV3.y >= verts[i].y)
                    {
                        if (!listTgt.ContainsKey(i))
                        {
                            listTgt.Add(i, 0);
                        }
                    }
                }

                // 隣接頂点の検出
                for (int cnt = 0; cnt <= LIMIT; cnt++)
                    for (int i = 0; i < tris.Length; i += 3)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            if (listTgt.ContainsKey(tris[i + j]) && listTgt[tris[i + j]] < LIMIT)
                            {
                                if (j != 0 && !listTgt.ContainsKey(tris[i + 0]))
                                    listTgt.Add(tris[i + 0], listTgt[tris[i + j]] + 1);

                                if (j != 1 && !listTgt.ContainsKey(tris[i + 1]))
                                    listTgt.Add(tris[i + 1], listTgt[tris[i + j]] + 1);

                                if (j != 2 && !listTgt.ContainsKey(tris[i + 2]))
                                    listTgt.Add(tris[i + 2], listTgt[tris[i + j]] + 1);
                            }
                        }
                    }

                peak = -1;
                for (int i = 0; i < verts.Length; i++)
                {
                    if (peak < 0 || peakV3.y > verts[i].y)
                    {
                        if (!listTgt.ContainsKey(i))
                        {
                            peak = i;
                            peakV3 = verts[i];
                        }
                    }
                }
            }
            
            /*
            Vector3 bottomV3 = peakV3;

            foreach(var x in listTgt)
            {
                if (verts[x.Key].y > bottomV3.y)
                    bottomV3 = verts[x.Key];

                Debug.Log(x.Key +" / "+ x.Value + " " + verts[x.Key]);
            }*/

            if (false)
            {
                // 最外周で最も高い頂点を探す
                if (!outline)
                    bottomV3 = -peakV3;
                foreach (var x in listTgt)
                {
                    //Debug.Log(x.Key + " / " + x.Value + " " + verts[x.Key]);

                    if (x.Value != LIMIT)
                        continue;

                    if (verts[x.Key].y < bottomV3.y)
                        bottomV3 = verts[x.Key];
                }
            }
            else
            {
                // 外周で最も低い頂点を探す
                if (!outline)
                {
                    bottomV3 = peakV3;

                    foreach (var x in listTgt)
                    {
                        //Debug.Log(x.Key + " / " + x.Value + " " + verts[x.Key]);

                        if (x.Value > 3)
                            continue;

                        if (verts[x.Key].x != 0)
                            continue;

                        if (verts[x.Key].y > bottomV3.y)
                            bottomV3 = verts[x.Key];
                    }
                }
                else
                {
                    var btmS = bottomV3;
                    bottomV3 = peakV3;

                    foreach (var x in listTgt)
                    {
                        if (verts[x.Key].x != 0)
                            continue;

                        if (verts[x.Key].y > bottomV3.y && verts[x.Key].y <= btmS.y)
                            bottomV3 = verts[x.Key];
                    }
                }
            }
            
            //Debug.Log("bottom " + bottomV3);


            Vector3[] vOffset = new Vector3[verts.Length];
            Vector3[] nOffset = new Vector3[verts.Length];
            Vector3[] tOffset = new Vector3[verts.Length];

            var norms = skmf.sharedMesh.normals;

            for (int i = 0; i < verts.Length; i++)
            {
                vOffset[i] = nOffset[i] = tOffset[i] = Vector3.zero;

                if (listTgt.ContainsKey(i) && bottomV3.y > verts[i].y)
                {
                    vOffset[i] = new Vector3(0, bottomV3.y - verts[i].y, 0);
                    nOffset[i] = new Vector3(-norms[i].x, 0, -norms[i].z) * 0.5f;
                }
                //vOffset[i] = new Vector3(0, -12, 0);
            }

            skmf.sharedMesh.AddBlendShapeFrame(ShapeKeyName, 100, vOffset, nOffset, tOffset);    
        }
    }
}

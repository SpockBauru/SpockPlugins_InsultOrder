using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using nnPlugin;
using static BepInEx.IO_ExSlider.Plugin.IO_ExSlider;

namespace BepInEx.IO_ExSlider.Plugin
{
    public class MateProp
    {
        public Dictionary<string, MateData> dicMate = new Dictionary<string, MateData>();
        Dictionary<string, Dictionary<int, Material>> dicObjIndexMate = new Dictionary<string, Dictionary<int, Material>>();
        Dictionary<string, Dictionary<int, Material>> dicObjIndexMateOrg = new Dictionary<string, Dictionary<int, Material>>();

        //public static Dictionary<string, HashSet<string>> dicObjMate = new Dictionary<string, HashSet<string>>();
        static bool ALLOW_REPLACE_MATEINST = false;
        public static int WriteFreq = 10;   // Writeフラグ付きマテ書き換え頻度(10で毎フレーム)
        public bool mSYNC_CHEEK_BLEND = true; //赤面連動


        public class Prop<T>
        {
            public string Key;
            public T Value;
            public Prop()
            {

            }
            public Prop(string key, T val)
            {
                Key = key;
                Value = val;
            }
        }

        public class MateData
        {
            public string objpath;
            public string name;
            public string shaderName;
            Renderer render;
            //Material[] materials = null;
            public int index; // materials内での
            Material material = null;
            Material _mateInst = null;

            public string[] keywords = new string[0];
            //public List<string> addKeywords = new List<string>();

            public class FProp
            {
                public string Key;
                public float Value;
                public FProp()
                {

                }
                public FProp(string key, float val)
                {
                    Key = key;
                    Value = val;
                }
            }

            public class CProp
            {
                public string Key;
                public Color Value;
                public CProp()
                {

                }
                public CProp(string key, Color val)
                {
                    Key = key;
                    Value = val;
                }
            }

            public List<Prop<float>> fProps = new List<Prop<float>>();
            public List<Prop<Color>> cProps = new List<Prop<Color>>();

            public MateData defData;
            public bool write;

            MateProp mateProp;

            public MateData()
            {
            }

            /// <summary>
            ///     複数キャラ対応版
            /// </summary>
            public MateData(MateProp mateProp, Renderer render, int index, string objpath)
                : this(render, index, objpath)
            {
                this.mateProp = mateProp;
            }

            private MateData(Renderer render, int index, string objpath)
            {
                this.render = render;
                this.objpath = objpath;
                //this.materials = mates;
                this.index = index;

                this.material = render.sharedMaterials[index];
                readMate(material, true);
            }

            // デフォルトデータ用
            internal MateData(MateProp mateProp, Material mate)
            {
                this.mateProp = mateProp;
                this.objpath = "";
                this.material = mate;
                readMate(mate, false);
            }

            public void setParent(MateProp mateProp)
            {
                this.mateProp = mateProp;
                if (defData != null)
                {
                    defData.setParent(mateProp);
                }
            }

            public MateProp getParent()
            {
                return this.mateProp;
            }

            public void ForceRestore()
            {
                this.name = defData.name;
                this.shaderName = defData.shaderName;
                this.fProps = Clone.Data(defData.fProps);
                this.cProps = Clone.Data(defData.cProps);
                this.keywords = defData.keywords;
                this.write = false;

                OnUpdate();
            }

            public Transform Root()
            {
                return mateProp.Root();
            }

            public void OnUpdate(bool keywordUpdated = false)
            {
                if (Root())
                    OnUpdate(Root(), keywordUpdated);
            }

            public void OnUpdate(Transform root, bool keywordUpdated = false)
            {
                if (write && defData == null)
                    write = false;

                if (defData == null)
                    return;

                // 負荷軽減のため非アクティブへの処理を抑制してみる
                if (this.render && this.render.gameObject && !this.render.gameObject.activeSelf)
                    return;

                bool mateChanged = false;
                var mate = material;

                if (!mate || render == null)
                {
                    MyDebug.Log(this.name + " がNULLのため再取得");

                    if (string.IsNullOrEmpty(objpath))
                        return;
                    var t = root.transform.FindSp(objpath);
                    if (!t)
                        return;
                    var c = t.gameObject.GetComponent<Renderer>();
                    if (!c)
                        return;

                    //mate = c.materials.FirstOrDefault(x => x.name == this.name);
                    this.render = c;
                    //this.materials = c.materials;
                    var materials = render.sharedMaterials;
                    if (materials != null && materials.Length > index)
                    {
                        mate = materials[index];
                    }
                    if (!mate)
                        return;

                    this.material = mate;
                    mateChanged = true;
                }
                else
                {
                    var materials = render.sharedMaterials;
                    if (materials != null && materials.Length > index)
                    {
                        if (materials[index] != this.material)
                        {
                            mateChanged = true;
                            if (write && materials[index] == this._mateInst)
                            {
                                mateChanged = false;
                                MyDebug.Log(this.name + " materials[index] == this._mateInst");
                            }

                            this.material = materials[index];

                            mate = material;
                        }
                    }
                }

                if (mateChanged)// || mate.name != this.name && mate.name != this.name.Replace(" (Custom)", ""))
                {
                    MyDebug.Log(this.name + " != " + mate.name);

                    /*if (mate.name.StartsWith(this.name.Replace(" (Instance)", "").Replace(" (Custom)", ""), StringComparison.Ordinal))
                    {
                        // テクスチャ変えたりすると(Instance)が増える
                        this.name = mate.name;
                    }
                    else*/ 
                    if (false)//(write && ALLOW_REPLACE_MATEINST)
                    {
                        defData = new MateData(mateProp, mate); // v1.2 bugfix

                        var mname = this.name.Replace(" (Instance)", "");
                        this.material = mate = Resources.Load<Material>(mname);
                        if (mate)
                            Debug.Log(this.name.Replace(" (Instance)", "") + "のマテリアルをロード-R :" + (bool)mate);
                        if (!mate)
                        {
                            //this.material = mate = Resources.Load<Material>(this.name.Replace(" (Instance)", ""));
                            var ms = Resources
                                .FindObjectsOfTypeAll<Material>()
                                //.Where(c => (c.hideFlags & HideFlags.NotEditable) == 0)
                                //.Where(c => (c.hideFlags & HideFlags.HideAndDontSave) == 0)
                                .Where(c => c.name.StartsWith(mname))
                                .ToArray()
                            ;
                            foreach (var s in ms)
                                MyDebug.Log(s.name + " " + string.Join(",", s.shaderKeywords));

                            var ma = ms.FirstOrDefault(x => x.name == mname);
                            if (!ma)
                                ma = ms.FirstOrDefault(x => x.shaderKeywords.SequenceEqual(this.keywords));
                            if (ma)
                            {
                                Debug.Log("Hit!: " + ma.name + " " + string.Join(",", ma.shaderKeywords));

                                this.material = mate = new Material(ma);
                                this.name = this.material.name += " (Instance)";
                            }

                            Debug.Log(this.name.Replace(" (Instance)", "") + "のマテリアルをロード-F :" + (bool)mate);
                        }
                        if (mate)
                            FlushMate();
                    }
                    else if (write && this.material.shader.name == mate.shader.name)
                    {
                        MyDebug.Log(this.name + "  シェーダー同一のためリロード: " + this.material.shader.name);
                        // シェーダーが同じなら元データの更新とインスタンスの破棄
                        DestroyMateInst();

                        readMate(mate, true, true);
                        //defData = new MateData(mateProp, mate);
                        this.material = mate;
                        this.name = mate.name;
                    }
                    else
                        readMate(mate, true);
                }

                if (!mate.shaderKeywords.SequenceEqual(this.keywords))
                {
                    if (write && !keywordUpdated)
                    {
                        mate.shaderKeywords = this.keywords;
                    }
                    else
                    {
                        // キーワードが変わってたらリロード
                        readMate(mate, true);
                    }
                    return;
                }

                if (write)
                {
                    //WriteMate(mate);
                    WriteMate2Inst();
                    //FlushMate();
                }
                else
                {
                    //GUI開いてるとき以外不要？ UpdateData(mate);
                }
            }

            public void UpdateData(Material mate)
            {
                if (mate.name != name)
                {
                    throw new Exception($"別のマテリアルを適用しようとしました({mate.name}->{name})");
                }

                fProps.ForEach(x => x.Value = mate.GetFloat(x.Key));
                cProps.ForEach(x => x.Value = mate.GetColor(x.Key));
            }

            public void UpdateLink(Renderer render, int index, string objpath)
            {
                this.objpath = objpath;
                UpdateLink(render, index);
            }

            public void UpdateLink(Renderer render, int index)
            {
                this.render = render;
                //this.materials = mates;
                this.index = index;
                this.material = render.sharedMaterials[index];
                if (!write)
                    readMate(material, false);
            }

            Material findMateInst()
            {
                Material mateInst = null;

                if (mateProp.dicObjIndexMate.TryGetValue(this.objpath, out Dictionary<int, Material> mates))
                {
                    mates.TryGetValue(this.index, out mateInst);
                }
                return mateInst;
            }

            public void DestroyMateInst()
            {
                if (!this._mateInst)
                    return;

                Material mateInst = null;

                if (mateProp.dicObjIndexMate.TryGetValue(this.objpath, out Dictionary<int, Material> mates))
                {
                    if (mates.TryGetValue(this.index, out mateInst) && mateInst)
                    {
                        MyDebug.Log(mateInst.name + " を破棄");

                        mates.Remove(this.index);
                        GameObject.DestroyImmediate(mateInst);
                    }
                }

                this._mateInst = null;
                this.material = null; // 更新誘発用
            }

            public void WriteMate2Inst(bool force = false)
            {
                Material mateInst = null;
                Material[] mateWrites = this.render.sharedMaterials;
                bool inst2render = false;

                if (mateProp.dicObjIndexMate.TryGetValue(this.objpath, out Dictionary<int, Material> mates))
                {
                    //foreach (var m in mates)
                    //    if (m.Value) mateWrites[m.Key] = m.Value;

                    mates.TryGetValue(this.index, out mateInst);
                }
                else
                {
                    mateProp.dicObjIndexMate.Add(this.objpath, new Dictionary<int, Material>());
                }

                if (!mateInst)
                {
                    mateInst = GameObject.Instantiate(render.sharedMaterials[index]);
                    mateWrites[this.index] = mateInst;
                    mateProp.dicObjIndexMate[this.objpath][this.index] = mateInst;
                    mateInst.name = this.name.Replace(" (Custom)", "") + " (Custom)";

                    inst2render = true;
                }
                else if (mateWrites[this.index] != mateInst)
                {
                    mateWrites[this.index] = mateInst;

                    inst2render = true;
                }

                this._mateInst = mateInst;
                this.name = mateInst.name;
                WriteMate(mateInst);

                if (inst2render)
                {
                    //this.render.materials = mateWrites;
                    this.render.sharedMaterials = mateWrites;
                }
            }

            public void Restore2SharedMesh(bool forceClear = false)
            {
                if (!this._mateInst)
                    return;

                if (forceClear)
                {
                    this.render = null;
                }
                else if (this.render)
                {
                    //this.render.sharedMaterials = this.render.sharedMaterials;
                    //this.render.material = this.render.sharedMaterial;

                    if (this.defData.material)
                    {
                        Material[] mateWrites = this.render.sharedMaterials;
                        mateWrites[this.index] = this.defData.material;
                        //this.render.materials = mateWrites;
                        this.render.sharedMaterials = mateWrites;
                        MyDebug.Log(this.name + "　をデフォルトに復元");
                    }
                }

                DestroyMateInst();
            }

            //public void WriteMate(bool force = false)
            //{
            //    WriteMate(this.material, force);
            //}

            public void WriteMate(Material mate, bool force = false)
            {
                if (!mate)
                    return;

                if (!force)
                {
                    if (!write || defData == null)
                        return;
                }

                if (ALLOW_REPLACE_MATEINST)
                {
                    if (mate.name != name)
                    {
                        throw new Exception($"別のマテリアルに適用しようとしました({mate.name}<-{name})");
                    }
                }
                else
                {
                    // シェーダーが同じなら許容
                    if (mate.shader.name != this.shaderName)
                    {
                        throw new Exception($"別シェーダーのマテリアルに適用しようとしました({mate.name}<-{name})");
                    }
                }
                
                foreach (var v in fProps)
                {
                    mate.SetFloat(v.Key, v.Value);
                }
                foreach (var v in cProps)
                {
                    mate.SetColor(v.Key, v.Value);
                    /*var c = mate.GetColor(v.Key);
                    c.r = v.Value.r;
                    c.g = v.Value.g;
                    c.b = v.Value.b;
                    // アルファ値は維持
                    mate.SetColor(v.Key, c);*/
                }
            }

            void FlushMate()
            {
                var materials = render.materials;
                if (materials != null && materials.Length > index)
                {
                    materials[index] = this.material;
                    render.materials = materials;
                }
            }

            T getval<T>(List<KeyValuePair<string, T>> list, string key)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Key == key)
                        return list[i].Value;
                }
                return default(T);
            }


            T getval<T>(List<Prop<T>> list, string key)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Key == key)
                        return list[i].Value;
                }
                return default(T);
            }


            public float getFProps(string key, bool def = false)
            {
                if (def)
                {
                    return getval(defData.fProps, key);
                }
                return getval(this.fProps, key);
            }

            public Color getCProps(string key, bool def = false)
            {
                if (def)
                {
                    return getval(defData.cProps, key);
                }
                return getval(this.cProps, key);
            }

            void setval<T>(List<KeyValuePair<string, T>> list, string key, T val)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Key == key)
                        list[i] = new KeyValuePair<string, T>(key, val);
                }
            }

            void setval<T>(List<Prop<T>> list, string key, T val)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Key == key)
                        list[i].Value = val;
                }
            }

            public void setFProps(string key, float val)
            {
                setval<float>(fProps, key, val);
                return;
            }

            public void setCProps(string key, Color val)
            {
                setval<Color>(cProps, key, val);
                return;
            }

            internal void readMate(Material mate, bool setdef, bool append = false)
            {
                name = mate.name;
                shaderName = mate.shader.name;
                keywords = mate.shaderKeywords;

                if (!append)
                    fProps = new List<Prop<float>>();
                foreach (var s in floatProps)
                {
                    try
                    {
                        if (mate.HasProperty(s))
                        {
                            if (!append || !fProps.Any(x => x.Key == s))
                                fProps.Add(new Prop<float>(s, mate.GetFloat(s)));
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"{name}:{s}  " + e);
                    }
                }

                if (!append)
                    cProps = new List<Prop<Color>>();
                foreach (var s in colorProps)
                {
                    try
                    {
                        if (mate.HasProperty(s))
                        {
                            if (!append || !cProps.Any(x => x.Key == s))
                                cProps.Add(new Prop<Color>(s, mate.GetColor(s)));
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"{name}:{s}  " + e);
                    }
                }

                if (setdef)
                {
                    defData = new MateData(mateProp, mate);
                }
            }

            public Material getMaterial()
            {
                return this.material;
            }
        }

        // 汎用プロパティ名、チョイスは適当
        public static string[] floatProps = new string[]
        {
            "_RimPower", "_RimShift", "_RimRange", "_Outline", "_OutlineWidth",  "_Cutoff","_Glossiness","_GlossyReflections","_Metallic","_RampSmooth","_RampThreshold","_RimMax","_RimMin","_Shininess","_SpecularHighlights","_Amplitude",
            "_CustomColorLevel", "_Contrast", "_Denier", "_CheekAlpha", "_alpha",  "_Alpha", "_GW_pow", "_blend", "_SpePow", // IO用
        };

        public static string[] colorProps = new string[]
        {
            "_Color", "_RimColor", "_OutlineColor", "_Specular", "_SpecColor", "_ReflectColor", "_Tint", "_Exposure","_EmissionColor","_Emission","_HColor","_RimDir", "_SColor", "_SpecColor", "_ShadowColor", "_MainColor",
            "_CustomColor", "_StripeColor", "_PanstColor", "_LightColor0", "_LightColor", "_Cutoff",  // IO用
        };
        static bool initComonDic = false;

        // ボーン構成内は検索させないためのキーワード
        //string girlBoneRootWord = "root";
        string girlRootBoneName = "root"; // 高速化のため完全一致に変更
        string ignoreName = "_moza";

        GirlCtrl girlCtrl;

        public MateProp(GirlCtrl girlCtrl)
        {
            this.girlCtrl = girlCtrl;
            girlRootBoneName = System.IO.Path.GetFileName(girlCtrl.ini.boneRoot);

            InitMateProp();
        }

        public void InitMateProp()
        {
            if (!initComonDic)
            {
                initComonDic = true;
                
                // 念のため重複削除＆ソート
                floatProps = floatProps.Distinct().ToArray();
                colorProps = colorProps.Distinct().ToArray();
                Array.Sort(floatProps);
                Array.Sort(colorProps);
            }
        }

        // ロード後に必ず行う必要あり
        public void SetupMateDic()
        {
            foreach(var d in dicMate)
            {
                d.Value.setParent(this);
            }
        }

        public void GetMateAll()
        {
            if (!endSceneInit)
                return;

            var go = girlCtrl.FindModel();
            if (!go)
                return;

            //dicMate = new Dictionary<string, MateData>();
            //dicObjMate = new Dictionary<string, HashSet<string>>();

            dicMate = dicMate.Where(x => x.Value.write).ToDictionary(x => x.Key, x => x.Value);

            getMate(go.transform, "");
            dicMate = dicMate.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
        }

        public void ResetMateAll()
        {
            foreach (var v in dicMate)
            {
                v.Value.write = false;
                //v.Value.OnUpdate();
                //v.Value.defData.WriteMate(true);

                v.Value.Restore2SharedMesh();
                v.Value.ForceRestore();

                //foreach (var k in v.Value.addKeywords)
                //{
                //    v.Value.removeKeyword(k);
                //}
            }
            dicMate.Clear();
        }

        static string getdickey(string path, int index)
        {
            return path + "\\" + index;
        }

        private void getMate(Transform tr, string cate)
        {
            if (!tr.gameObject.activeSelf || tr.gameObject.name.Contains(ignoreName))//tr.gameObject.name == girlRootBoneName)
                return;

            var smr = tr.gameObject.GetComponent<Renderer>();
            if (smr)
            {
                for (int index = 0; index < smr.sharedMaterials.Length; index++)
                {
                    if (!smr.sharedMaterials[index] || !smr.sharedMaterials[index].shader)
                        continue;

                    //string mem_name = tr.parent.gameObject.name + "/" + tr.gameObject.name + $" mate:{mate.name}";
                    var key = getdickey(cate, index);
                    if (!dicMate.ContainsKey(key))
                    {
                        dicMate[key] = new MateData(this, smr, index, cate);
                    }
                    else
                    {
                        dicMate[key].UpdateLink(smr, index);
                    }

                    //if (!dicObjMate.ContainsKey(cate))
                    //    dicObjMate.Add(cate, new HashSet<string>());
                    //dicObjMate[cate].Add(mate.name);
                }
            }

            if (cate.Length > 0)
                cate += "/";

            //for (int i = 0; i < tr.childCount; i++)
            foreach(Transform t in tr)
            {
                //var t = tr.GetChild(i);
                if (!t || string.IsNullOrEmpty(t.name))
                    continue;

                getMate(t, cate + t.name);
            }
        }

        public Transform Root()
        {
            var go = girlCtrl.FindModel();
            return go ? go.transform : null;
        }

        bool endSceneInit = false;
        bool needAllUpdate = false;

        public IEnumerator OnNewSceneStart(int waitframe)
        {
            endSceneInit = false;
            for (int i = 0; i < waitframe; i++)
                yield return null;

            OnNewScene();

            // 書込みONのみにする
            this.dicMate = this.dicMate.Where(x => x.Value.write).ToDictionary(x => x.Key, x => x.Value);

            endSceneInit = true;
            yield break;
        }

        public void OnNewScene()
        {
            var root = Root();

            // v1.0 try
            //if (!root)
            //    return;
            bool isNotActive = !root;
            if (isNotActive && dicObjIndexMate.Count <= 0)
                return;

            foreach (var v in dicMate)
            {
                v.Value.Restore2SharedMesh(isNotActive);
                //v.Value.DestroyMateInst();
            }

            foreach (var d in dicObjIndexMate)
            {
                foreach (var o in d.Value)
                    if (o.Value) GameObject.DestroyImmediate(o.Value);
                d.Value.Clear();
            }
            dicObjIndexMate.Clear();

            if (isNotActive)
                return;

            // 次のアップデートで実行した方がよいため //UpdateAll(true);
            needAllUpdate = true;
        }

        bool flagWaxed = false;
        void setupNekoWhipWaxTex()
        {
            // 鞭、ろうそく用テクスチャの反映
            if (GameClass.FrontTopTex != null)
            {
                if (!flagWaxed)
                {
                    Console.WriteLine("Neko: 鞭、蝋燭用テクスチャ適用のためマテリロード");
                    flagWaxed = true;
                    OnNewScene();
                }
            }
            else if (flagWaxed)
            {
                flagWaxed = false;
            }
        }

        public void ExtUpdate()
        {
            if (!endSceneInit)
                return;

            if (girlCtrl.ini.id == "Neko" && actScene.name == "UC")
            {
                setupNekoWhipWaxTex();
            }

            UpdateAll(needAllUpdate);
            needAllUpdate = false; // １ショット
        }

        public void UpdateAll(bool all = false)
        {
            var root = Root();
            if (!root)
                return;

            float cheekValue = -1f;

            if (mSYNC_CHEEK_BLEND && girlCtrl.ini.isNotGirl())
                mSYNC_CHEEK_BLEND = false; // 男は無効

            if (mSYNC_CHEEK_BLEND && girlCtrl.faceCtrl.isReady())
                cheekValue = girlCtrl.faceCtrl.Cheek;

            foreach (var v in dicMate)
            {
                // チークチェックが不要になったので位置変更
                if (!all && !v.Value.write && UnityEngine.Random.Range(0, 5) != 0) // 1/5フレーム目安
                    continue;

                // 脱衣時に問題が起きるのでwriteフラグ有りは常に更新にした。負荷もそれほど変わらなそう？
                bool cheekFlag = false;
                if (mSYNC_CHEEK_BLEND && cheekValue >= 0 && v.Value.write && v.Value.shaderName == "Miconisomi/ProjectIo_ChFace")
                {
                    v.Value.setFProps("_CheekAlpha", cheekValue);
                    cheekFlag = true; // 設定済みフェイスは常に更新
                }

                //if (!cheekFlag && !all && !v.Value.write && UnityEngine.Random.Range(0, 5) != 0) // 1/5フレーム目安
                //    continue;

                // インスタンスのチェックで脱衣が改善したため、通常の書き込み有りは頻度を変更できるようにした
                if (WriteFreq < 10)
                {
                    if (!cheekFlag && !all && v.Value.write && !(UnityEngine.Random.Range(0, 10) < WriteFreq)) // WriteFreq/10フレーム目安
                        continue;
                }

                v.Value.OnUpdate(root);
            }
        }

        public void ReadMateAll()
        {
            if (!endSceneInit)
                return;

            var tr = Root();
            if (!tr)
                return;

            foreach (var v in dicMate)
            {
                if (!v.Value.write)
                    v.Value.OnUpdate(tr);
            }
        }

        public void WriteMateAll()
        {
            var tr = Root();
            if (!tr)
                return;

            foreach (var v in dicMate)
            {
                if (v.Value.write)
                    v.Value.OnUpdate(tr);
            }
        }

        public void logMat()
        {
            var tr = Root();
            if (!tr)
                return;

            logTex(tr);
        }

        private void logTex(Transform tr)
        {
            if (!tr.gameObject.activeSelf || tr.gameObject.name.Contains(ignoreName))// == girlRootBoneName)
                return;

            var smr = tr.gameObject.GetComponent<Renderer>();
            if (smr)
            {
                if (smr.sharedMaterials == null)
                    return;

                foreach (var mate in smr.sharedMaterials)
                {
                    if (!mate.shader)
                        continue;

                    string mem_name = tr.parent.gameObject.name + "/" + tr.gameObject.name + $" mate:{mate.name}";
 
                    Console.WriteLine($"LogMat: {mem_name}");
                    foreach (var s in floatProps)
                    {
                        if (mate.HasProperty(s))
                        {
                            Console.WriteLine($"{mate.name} has: " + s);
                        }
                    }

                    foreach (var s in colorProps)
                    {
                        if (mate.HasProperty(s))
                        {
                            Console.WriteLine($"{mate.name} has: " + s);
                        }
                    }

                    if (mate.shaderKeywords != null)
                        Array.ForEach(mate.shaderKeywords, x => Console.WriteLine($"Keywords: {x}"));

                    Console.WriteLine("");

                    Console.WriteLine("");
                    Console.WriteLine("");
                }
            }

            //for (int i = 0; i < tr.childCount; i++)
            foreach (Transform t in tr)
            {
                //var t = tr.GetChild(i);
                if (!t || string.IsNullOrEmpty(t.name))
                    continue;

                logTex(t);
            }
        }
    }

}

using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;

using UnityEngine;
using OhMyGizmo2;
using nnPlugin;
using static BepInEx.IO_ExSlider.Plugin.IO_ExSlider;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;

namespace BepInEx.IO_ExSlider.Plugin
{
    public class BoneScales
    {
        public const string KITEN_PATH = Extentions.FIND_PATH_CURRENT_TR;
        public static OhMyGizmoPosRot gizmoGirlPos;


        public static void gizmoGirlPosInit()
        {
            if (!gizmoGirlPos)
            {
                gizmoGirlPos = OhMyGizmoPosRot.AddGizmo(null, "PlgGirlPos");
                //gizmoGirlPos.modeRot = true;
                //gizmoGirlPos.modePos = true;
                gizmoGirlPos.visible = false;
                gizmoGirlPos.sizeRot = 4f;
                gizmoGirlPos.sizeHandle = 10f;
                gizmoGirlPos.threthold = 40;
                gizmoGirlPos.undoDrag = true;
            }
        }

        GirlCtrl girlCtrl;
        TargetIni.TargetDics targetDics;
        public string rootObjPath = "CH01/CH0001/HS_kiten";

        //初期値
        public readonly List<StrV3Pair> girl_bScales0 = new List<StrV3Pair>()
        {
        };
        
        public Dictionary<string, Vector3> dicNScale = new Dictionary<string, Vector3>();

        public Transform transform { 
                get {
                    var go = Extentions.FindSp(rootObjPath);
                    return go ? go.transform : null; 
                } 
            }

        // 位置調整
        public BonePos pPosCtrl;

        //保存用
        public Edits edits;

        [Serializable]
        public class OffsetByState
        {
            public string stateName;
            public CharOffset offset;
            
            public OffsetByState()
            {
            }

            public OffsetByState(string stateName, CharOffset girlOffset)
            {
                this.stateName = stateName;
                this.offset = girlOffset;
            }
        }

        [Serializable]
        public class OffsetByStates
        {
            public bool enabled = true;
            public List<OffsetByState> list = new List<OffsetByState>();

            public OffsetByStates()
            {
            }
        }

        [Serializable]
        public class OffsetData
        {
            public CharOffset girlOffset = new CharOffset();
            public OffsetByStates offsetByScenes = new OffsetByStates();
            public OffsetByStates offsetByMotions = new OffsetByStates();
            public bool useFuzzySearchMotions = false;
            public bool useOffsetReseet = false;

            public OffsetData()
            {
            }
        }

        public enum OffsetCoord
        {
            None,
            Pelvis,
            Breast,
            Mouth,
            Genitals,
            Anus,
        }

        public class OffsetCoordsData
        {
            public OffsetCoord coord;
            public string dispName;
            public string boneName;

            public OffsetCoordsData(OffsetCoord coord, string dispName, string bonePath)
            {
                this.coord = coord;
                this.dispName = dispName;
                this.boneName = bonePath;
            }
        }

        public readonly static OffsetCoordsData[] offsetCoordsData
        = { 
            new OffsetCoordsData(OffsetCoord.None, "通常", null),
            new OffsetCoordsData(OffsetCoord.Pelvis, "骨盤", "bip01 Pelvis"),
            new OffsetCoordsData(OffsetCoord.Breast, "胸部", "bip01 Spine2"),
            new OffsetCoordsData(OffsetCoord.Mouth, "口", "HF01_mouth01"),
            new OffsetCoordsData(OffsetCoord.Genitals, "陰部", "HS01_pussy00"),
            new OffsetCoordsData(OffsetCoord.Anus, "後穴", "HS01_anaru"),
        };

        public struct CharOffset
        {
            public Vector3 dpos;
            public Vector3 drot;
            public OffsetCoord coord;
        }

        [Serializable]
        public class Edits
        {
            BoneScales _boneScales;
            public BoneScales getControl() { return _boneScales; }
            public void setControl(BoneScales value) { _boneScales = value; }

            public bool girl_useLossyBScale = true;
            public bool hideFingers = true;

            // パーツスケール
            public List<StrV3Pair> girl_bScales = new List<StrV3Pair>();
            public List<string> useLossyBScalePaths = new List<string>();
            public List<string> useLocalPosScalePaths = new List<string>();

            // パーツ位置
            public BonePos.Edits partsPos = new BonePos.Edits();
            public float fixParticlesScale = 1f;

            // 位置調整
            public OffsetData offsetData = new OffsetData();
            //public Vector3 girl_dpos = new Vector3(0, 0, 0);
            //public Vector3 girl_drot = new Vector3(0, 0, 0);

            // 口のスケールなどを無効にするモーション名（フェラなど）
            public List<string> disapplyMouthMotionNames = new List<string>()
            {
            };

            static readonly string[] wordsIgnoreLossyBScale = new string[] { }; 
            public bool isLossyBScale(string path)
            {
                foreach (var v in wordsIgnoreLossyBScale)
                {
                    if (path.Contains(v))
                        return false;
                }

                if (useLossyBScalePaths.Contains(path))
                    return true;

                /* v0.92 なんでこうしてたのか思い出せないのでContainsに修正
                if (useLossyBScalePaths.Any(x => x == path))
                    return true;*/

                return false;
            }

            public Edits()
            {
            }
            public Edits(BoneScales boneScales)
            {
                this._boneScales = boneScales;

                this.partsPos.setControl(boneScales.pPosCtrl);
            }

            //public void Load(Edits edits)
            //{
            //    this.girl_bScales = edits.girl_bScales;
            //    this.girl_dpos = edits.girl_dpos;
            //    this.girl_drot = edits.girl_drot;
            //}

            public void setupScales()
            {
                _setupScales();
            }

            void _setupScales()
            {
                MyDebug.Log("setScales-1");
                var go = Extentions.FindSp(this._boneScales.rootObjPath);
                Transform gtr = null;
                if (go)
                {
                    gtr = go.transform;

                    // ボーン指定があれば一度リセット
                    _boneScales.resetBoneLocscl();
                }

                MyDebug.Log("setScales-2");

                var tmp = girl_bScales;
                girl_bScales = new List<StrV3Pair>();
                foreach (var v in _boneScales.girl_bScales0)
                {
                    var hit = tmp.FirstOrDefault(x => x.Key == v.Key);
                    //if (hit.Equals(default(StrV3Pair)))
                    if (hit == null)
                    {
                        girl_bScales.Add(new StrV3Pair(string.Empty, v.Value));
                    }
                    else
                    {
                        girl_bScales.Add(new StrV3Pair(v.Key, hit.Value));

                        _boneScales.dicNScale[v.Key] = v.Value;
                        if (gtr)
                        {
                            var tr = gtr.FindSp(v.Key, true);
                            if (tr)
                                _boneScales.dicNScale[v.Key] = tr.localScale;
                        }
                    }
                }

                MyDebug.Log("setScales-3");

                // ユーザーがiniで追加した分は後で足す
                foreach (var v in tmp)
                {
                    if (string.IsNullOrEmpty(v.Key))
                        continue;
                    var hit = _boneScales.girl_bScales0.FirstOrDefault(x => x.Key == v.Key);
                    //if (hit.Equals(default(StrV3Pair)))
                    if (hit == null)
                    {
                        girl_bScales.Add(new StrV3Pair(v.Key, v.Value));

                        // このシーンにないボーンなのでデフォ値は持たない
                        _boneScales.girl_bScales0.Add(new StrV3Pair(v.Key, v.Value));
                    }
                }
            }
        }

        HashSet<string> listFixscalingBone = new HashSet<string>
        {
        };
        
        //internal static bool hideNoacts = true;

        bool isFixScaling(string path) // 値が一定値（変化しない）ボーン
        {
            string s = path;
            if (s.IndexOf("/") >= 0)
                s = s.Substring(path.LastIndexOf("/"));

            foreach (var t in listFixscalingBone)
            {
                if (s.IndexOf(t) >= 0)
                    return true;
            }
            return false;
        }

        public BoneScales(GirlCtrl girlCtrl, string rootObjPath)
        {
            this.girlCtrl = girlCtrl;
            this.targetDics = girlCtrl.ini.dics;

            gizmoGirlPosInit();
            Init(rootObjPath);

            pPosCtrl = new BonePos(girlCtrl, rootObjPath);
        }

        public void Init(string rootObjPath)
        {
            this.rootObjPath = rootObjPath;
            this.edits = new Edits(this);

        }

        //Animator _animator;
        string animeCtrlName;
        string animeClipName;

        public void OnUpdate()
        {
            gizmoGirlPosInit();

            if (this.edits.offsetData.offsetByMotions.enabled)
            {
                var go = girlCtrl.FindModel();
                if (go)
                {
                    var oldCtrlName = animeCtrlName;
                    var oldClipName = animeClipName;

                    // IOではコントロール名の判定不要 if (girlCtrl.charaCtrl.getAnimeState(ref animeCtrlName, ref animeClipName))
                    animeCtrlName = string.Empty;
                    string keyword = animeClipName = girlCtrl.charaCtrl.getIOAnimeStateString();
                    
                    if (animeClipName != null)
                    {
                        if (oldClipName != animeClipName || oldCtrlName != animeCtrlName)
                        {
                            MyDebug.Log($"{girlCtrl.ini.name} =>  Ctrl: {animeCtrlName}  Clip: {animeClipName}");

                            //string keyword = $"{animeCtrlName}@{animeClipName}";
                            //
                            //  IOではクリップ名のみで管理
                            //
                            //string keyword = PlgCharaCtrl.GetIOAnimeStateFromClipname(animeClipName); //girlCtrl.charaCtrl.getIOAnimeStateString();

                            var set = this.edits.offsetData.offsetByMotions.list.FirstOrDefault(x => x.stateName == keyword);
                            //if (set == null)
                            //{
                            //    // 完全一致しなければコントロール名で探す →　今回はコントロール名無い模様 → OverrideControllerに遮蔽されてただけだったけどほぼシーンIDなので判定不能
                            //    set = this.edits.offsetByMotions.FirstOrDefault(x => x.stateName.StartsWith($"{animeCtrlName}@", StringComparison.Ordinal));
                            //}
                            //if (set == null && !string.IsNullOrEmpty(animeCtrlName) && !string.IsNullOrEmpty(animeClipName))
                            //{
                            //    // 完全一致しなければクリップ名のみでも探す
                            //    set = this.edits.offsetData.offsetByMotions.list.FirstOrDefault(x => x.stateName.EndsWith(animeClipName, StringComparison.Ordinal));
                            //}
                            if (set == null && this.edits.offsetData.useFuzzySearchMotions && animeClipName.Length >= 6)
                            {
                                // 末尾は先に削るようにしたので、単純に1文字削ってトライ
                                keyword = keyword.Substring(0, keyword.Length - 1);
                                set = this.edits.offsetData.offsetByMotions.list.FirstOrDefault(x => x.stateName.StartsWith(keyword, StringComparison.Ordinal));
                                keyword += "*"; // LOG用

                                //// 完全一致しなければ文字数を削って前方一致で近い名前を探してみる
                                //// IOのクリップ名は MUC1001S01_M、MUC1001F01_00_M のような形式なので_M部分を削除後、1文字ずつ削る
                                //// 1文字削ってなければ同じカテゴリにはないと判断
                                //if (keyword[keyword.Length-2] == '_')
                                //{
                                //    keyword = keyword.Substring(0, keyword.Length - 3);
                                //    set = this.edits.offsetData.offsetByMotions.list.FirstOrDefault(x => x.stateName.StartsWith(keyword, StringComparison.Ordinal));
                                //    keyword += "*";
                                //}

                            }

                            if (set != null)
                            {
                                Debug.Log($"モーション用のオフセットデータを読み込み Hit: {keyword}");
                                this.edits.offsetData.girlOffset = set.offset;
                            }
                            else if (this.edits.offsetData.useOffsetReseet)
                            {
                                // シーン登録をチェック、なければ原点に戻る
                                ApplyOffsetByScene(actScene);
                            }
                        }
                    }
                }
            }
        }

        const char SCNPLUS_CHAR = '+';
        public string GetCurrentBsSceneName(Scene scene, bool plusBgName)
        {
            var scene_name = scene.name;
            if (string.IsNullOrEmpty(scene.name))
                return string.Empty;

            if (Free3pXtMS.IsSlave(this.girlCtrl))
            {
                scene_name = Free3pXtMS.GetXtSceneName;
            }
            else if (plusBgName)//if (IO_ExSlider.cfg.CharEdits_PosOfsByScene_PlusBGName)
            {
                var bgn = BGMGR.GetNameBG();
                if (!string.IsNullOrEmpty(bgn))
                {
                    scene_name = $"{scene_name}{SCNPLUS_CHAR}{bgn}";
                }
            }
            else if (scene_name == "ADV")
            {
                var model = girlCtrl.FindModel();
                if (model)
                {
                    var ani = model.GetComponent<Animator>();
                    if (ani)
                    {
                        if (ani.GetLayerWeight(0) <= 0 && ani.GetLayerWeight(4) > 0 && ani.GetCurrentAnimatorClipInfoCount(4) > 0)
                            scene_name += "@Cinema";
                    }
                }

                /*var ms = Extentions.FindSp("MainSystem");
                if (ms)
                {
                    var adv = ms.GetComponent<ADV_Loader>();
                    if (adv)
                    {
                        if (adv.BGround3D)
                            scene_name += "@BG3D";
                    }
                }*/
            }

            return scene_name;
        }

        public void ApplyOffsetByScene(Scene scene)
        {
            if (this.edits.offsetData.useOffsetReseet)
            {
                // リセット有効時
                ResetPosRot();
            }

            var scene_name = GetCurrentBsSceneName(scene, true);
            if (string.IsNullOrEmpty(scene_name))
                return;

            if (this.edits.offsetData.offsetByScenes.enabled)
            {
                var set = this.edits.offsetData.offsetByScenes.list.FirstOrDefault(x => x.stateName == scene_name);
                if (set == null)
                {
                    // BG名無しでチェック
                    var scene_name2 = GetCurrentBsSceneName(scene, false);
                    if (scene_name != scene_name2)
                    {
                        scene_name = scene_name2;
                        set = this.edits.offsetData.offsetByScenes.list.FirstOrDefault(x => x.stateName == scene_name2);
                    }

                    /*int idx = scene_name.IndexOf(SCNPLUS_CHAR);
                    if (idx > 0)
                    {
                        scene_name = scene_name.Remove(idx);
                        set = this.edits.offsetData.offsetByScenes.list.FirstOrDefault(x => x.stateName == scene_name);
                    }*/
                }
                if (set != null)
                {
                    Debug.Log($"{girlCtrl.ini.id} {scene_name}シーン用のオフセットデータを読み込み");
                    this.edits.offsetData.girlOffset = set.offset;
                }
            }
        }

        public void InitOffsetByMotions()
        {
            animeCtrlName = string.Empty;
            animeClipName = string.Empty;
        }

        public void OnNewSceneLoaded(Scene scene)
        {
            // オフセット設定
            ApplyOffsetByScene(scene);
            InitOffsetByMotions();

            // ボーン情報再読み込み
            girl_bScales0.Clear();
            pPosCtrl.girl_bPositions0.Clear();

            // キャッシュクリア
            this.CacheClear();
        }

        public static void setBoneLocsclRL(Transform gtr, TargetIni.TargetDics dics, string bone, Vector3 scl)
        {
            var tr = gtr.FindSp(bone);
            if (tr)
                tr.localScale = scl;

#if !V091
            var nameL = dics.getTransRLStrBSC(bone);
            if (nameL != bone)
            {
                string str = nameL;
                tr = gtr.FindSp(str);

                if (tr)
                    tr.localScale = scl;
                else
                    MyDebug.LogWarning(bone + "\n\t" + str);
            }
#else
            //if (bone.IndexOf("bip01 R") >= 0)
            if (dics.bscTransNameR2L.Any(x => bone.IndexOf(x.Key) >= 0))
            {
                string str = bone;
                dics.bscTransNameR2L.ForEach(x => str = str.Replace(x.Key, x.Value));
#if DEBUG
                if (Input.GetKeyDown(KeyCode.Escape))
                    MyDebug.Log(bone + "\n\t" + str);
#endif

                tr = gtr.FindSp(str);

                //tr = gtr.FindSp(bone.Replace("bip01 R", "bip01 L").Replace("Right", "Left"));
                if (tr)
                    tr.localScale = scl;
                else
                    MyDebug.Log(bone + "\n\t" + str);
            }
#endif
        }

        // ボーンリスト構築
        void boneDicInit(GameObject ggo)
        {
            //基点を追加
            add2dic(KITEN_PATH, ggo.transform);

            //スキャン
            loop("", ggo.transform);

            foreach(var tgt in girlCtrl.ini.dics.extraFindSpTarget)
            {
                var s = tgt;
                bool addself = true;
                if (s.EndsWith("/", StringComparison.Ordinal))
                {
                    addself = false;
                    s = s.Substring(0, s.Length - 1);
                }

                var get = ggo.transform.FindSp(s, true);
                if (get)
                {
                    if (addself)
                        add2dic(s, get);
                    else
                        add2dic_loop(s, get);
                }
            }

            // セットアップ
            edits.setupScales();

            void add2dic(string path, Transform tr)
            {
                if (girl_bScales0.Any(x => x.Key == path))
                    return;

                girl_bScales0.Add(new StrV3Pair(path, tr.localScale));
                if (this.edits.girl_bScales.Count < girl_bScales0.Count)
                    this.edits.girl_bScales.Add(new StrV3Pair("", tr.localScale));
            }
            void add2dic_loop(string path, Transform get)
            {
                string tmp;
                foreach (Transform c in get)
                {
                    add2dic(tmp = $"{path}/{c.name}", c);
                    add2dic_loop(tmp, c);
                }
            }

            void loop(string path, Transform tr)
            {
                //for (int i = 0; i < tr.childCount; i++)
                foreach (Transform tr2 in tr)
                {
                    //var tr2 = tr.GetChild(i);

                    //if (tr2.name.StartsWith("bip01 L", StringComparison.Ordinal))
                    
                    //if (this.targetDics.bscIgnoreObjNamesC.Any(x => tr2.name.Contains(x)))
                    if (this.targetDics.bscIgnoreObjNamesRE.Any(x => CacheRegex.IsMatch(tr2.name, x)))
                        continue;

                    var tr2name = pPosCtrl.RevTransNameUsaBonesFH(tr2.name);

                    var newpath = $"{path}/{tr2name}";
                    if (path == "")
                        newpath = $"{tr2name}";

                    //Extentions.RevTransNameUsaBonesFH(ref newpath);

                    //if (!tr2.name.StartsWith("bip", StringComparison.Ordinal))
                    //continue;
                    //if (this.targetDics.bscValidObjNamesSW.Any(x => x.Value.Any(y => tr2.name.StartsWith(y, StringComparison.Ordinal))))
                    //if (this.targetDics.bscValidObjNamesSW.Any(x => x.Value.Any(y => CacheRegex.IsMatch(tr2.name, "^" + y))))
                    if (this.targetDics.checkBSclObjNameValid(tr2.name))
                        {
                        girl_bScales0.Add(new StrV3Pair(newpath, tr2.localScale));
                        if (this.edits.girl_bScales.Count < girl_bScales0.Count)
                            this.edits.girl_bScales.Add(new StrV3Pair("", tr2.localScale));

                        MyDebug.Log($"{newpath}  {tr2.localScale}  {tr2.lossyScale}");
                    }
                    loop(newpath, tr2);
                }
            }
        }

        // 何度か１フレーム内で検索することになるのでフレーム内キャッシュ
        //int _cacheValid_girl_bScales_lastFrame = -1;
        //List<StrV3Pair> _cacheValid_girl_bScales;
        //private List<StrV3Pair> CacheValid_girl_bScales_
        //{
        //    get
        //    {
        //        if (_cacheValid_girl_bScales_lastFrame != Time.frameCount
        //            || _cacheValid_girl_bScales == null)
        //        {
        //            _cacheValid_girl_bScales_lastFrame = Time.frameCount;
        //            _cacheValid_girl_bScales = this.edits.girl_bScales.FindAll(x => !string.IsNullOrEmpty(x.Key));
        //        }
        //        return _cacheValid_girl_bScales;
        //    }
        //}

        private static IEnumerable<StrV3Pair> IEnumValidScales(List<StrV3Pair> list)
        {
            return list.Where(x => !string.IsNullOrEmpty(x.Key));
        }

        // キャッシュクリア
        private void CacheClear()
        {
            //_cacheValid_girl_bScales_lastFrame = -1;
            //if (_cacheValid_girl_bScales != null)
            //    _cacheValid_girl_bScales.Clear();

            //_cacheValid_girl_bScales = null;
        }



        public void OnLateUpdate()
        {
            var ggo = Extentions.FindSp(rootObjPath);
            if (!ggo)
                return;

            // 目の位置などゲームと干渉するので位置だけ後回し
            //// パーツ位置
            //pPosCtrl.OnLateUpdate();

            // 初期化
            if (girl_bScales0.Count <= 0)
            {
                boneDicInit(ggo);
            }

            // スケーリング
            ProcBoneScale(ggo, this.edits, this.edits.girl_bScales, this.targetDics);
            //ProcBoneScale(ggo, this.edits, this.CacheValid_girl_bScales, this.targetDics);
        }

        // IO用ヘッドスケールのみ上書き
        public void OverWriteHeadScales()
        {
            var ggo = Extentions.FindSp(rootObjPath);
            if (!ggo)
                return;

            List<StrV3Pair> headScales = this.edits.girl_bScales.FindAll(x => isHeadScale(x.Key));
            //var headScales = this.CacheValid_girl_bScales.FindAll(x => isHeadScale(x.Key));
            
            if (headScales.Count > 0)
                ProcBoneScale(ggo, this.edits, headScales, TargetIni.targetIni.headScalesDic, false);

            bool isHeadScale(string path)
            {
                if (string.IsNullOrEmpty(path))
                    return false;

                var s = PlgUtil.GetObjNameFromPath(path);
                return TargetIni.targetIni.headScalesDic.bscValidObjNamesSW[0].Value.Any(x => s.StartsWith(x, StringComparison.Ordinal));
            }
        }

        //public static void ProcBoneScale(GameObject ggo, BoneScales.Edits edits, List<StrV3Pair> scales, TargetIni.TargetDics inidics)
        public void ProcBoneScale(GameObject ggo, BoneScales.Edits edits, List<StrV3Pair> scales, TargetIni.TargetDics inidics, bool isFullList = true)
        {
            /* とりあえず不要に
            if (edits.girl_useLossyBScale)
            {
                //dic = dic.Where(x => !string.IsNullOrEmpty(x.Key)).OrderBy(x => x.Key).ToList();

#if DEBUG
                if (Input.GetKeyDown(KeyCode.B))
                {
                    foreach (var v in dic)
                    {
                        Debug.Log(v.Key);
                    }
                }
#endif
            }*/


            // ルートのスケール
            float gs = ggo.transform.lossyScale.x;
#if test
            var dic = scales;//edits.girl_bScales;
            for (int i = 0; i < dic.Count; i++)
            {
                var v = dic[i];

                // FindAllの方が速いので、呼び出し側でチェックするよう変更した
                //if (string.IsNullOrEmpty(v.Key))
                //    continue;
#else
            foreach (var v in IEnumValidScales(scales))
            {
#endif
                var tr = ggo.transform.FindSp(v.Key);
                if (!tr)
                    continue;

                // 口スケール除外モーションチェック
                if (isFullList && edits.disapplyMouthMotionNames.Count > 0)
                {
                    //var anime = PlgCharaCtrl.GetIOAnimeStateFromClipname(this.animeClipName);
                    //if (edits.disapplyMouthMotionNames.Contains(anime) && TargetIni.targetIni.IsMouthBone(v.Key))
                    if (PlgCharaCtrl.CheckAnimeFilteringList(edits.disapplyMouthMotionNames, this.animeClipName))
                    {
                        continue;
                    }
                }

                //bool bofix = true;//isFixScaling(v.Key);
                if (edits.girl_useLossyBScale && edits.isLossyBScale(v.Key))
                {
                    Vector3 tgt = v.Value;//new Vector3(v.Value.x * rootScale.x, v.Value.y * rootScale.y, v.Value.z * rootScale.z);
                    var lossy = tr.lossyScale;
                    var local = tr.localScale;
                    tr.localScale = new Vector3(local.x / lossy.x * tgt.x, local.y / lossy.y * tgt.y, local.z / lossy.z * tgt.z) * gs;
                }
                else
                {
                    tr.localScale = v.Value;
                }
                //else
                //{
                //    // スケール変更がありそうなのは乗算
                //    tr.localScale = new Vector3(tr.localScale.x * v.Value.x, tr.localScale.y * v.Value.y, tr.localScale.z * v.Value.z);
                //}

                if (edits.useLocalPosScalePaths.Contains(v.Key))
                {
                    tr.localPosition = new Vector3(
                        tr.localPosition.x * v.Value.x,
                        tr.localPosition.y * v.Value.y,
                        tr.localPosition.z * v.Value.z
                        );
                }

#if V091
                //if (v.Key.IndexOf("bip01 R") >= 0)
                if (inidics.bscTransNameR2L.Any(x => v.Key.IndexOf(x.Key) >= 0))
                {
                    //tr = ggo.transform.FindSp(v.Key.Replace("bip01 R", "bip01 L").Replace("Right", "Left"));

                    string str = v.Key;
                    inidics.bscTransNameR2L.ForEach(x => str = str.Replace(x.Key, x.Value));
#else

                var str = inidics.getTransRLStrBSC(v.Key);
                if (str != v.Key)
                {
#endif
                    tr = ggo.transform.FindSp(str);
                    
                    if (!tr)
                    {
#if DEBUG
                        if (Input.GetKeyDown(KeyCode.L))
                            Debug.Log(v.Key + "\n\t" + str);
                        //Debug.Log(v.Key.Replace("bip01 R", "bip01 L"));
#endif
                        continue;
                    }

                    if (edits.girl_useLossyBScale && edits.isLossyBScale(v.Key))
                    {
                        Vector3 tgt = v.Value;//new Vector3(v.Value.x * rootScale.x, v.Value.y * rootScale.y, v.Value.z * rootScale.z);
                        var lossy = tr.lossyScale;
                        var local = tr.localScale;
                        tr.localScale = new Vector3(local.x / lossy.x * tgt.x, local.y / lossy.y * tgt.y, local.z / lossy.z * tgt.z) * gs;
                    }
                    else
                    {
                        tr.localScale = v.Value;
                    }
                    //else
                    //{
                    //    // スケール変更がありそうなのは乗算
                    //    tr.localScale = new Vector3(tr.localScale.x * v.Value.x, tr.localScale.y * v.Value.y, tr.localScale.z * v.Value.z);
                    //}

                    if (edits.useLocalPosScalePaths.Contains(v.Key))
                    {
                        tr.localPosition = new Vector3(
                            tr.localPosition.x * v.Value.x,
                            tr.localPosition.y * v.Value.y,
                            tr.localPosition.z * v.Value.z
                            );
                    }
                }
            }
        }

        // ボーンスケールリセット
        public void ResetBoneScales()
        {
            var go = Extentions.FindSp(rootObjPath);

            for (int i = 0; i < this.edits.girl_bScales.Count; i++)
            {
                if (string.IsNullOrEmpty(this.edits.girl_bScales[i].Key))
                    continue;

                var def = Vector3.one;
                if (dicNScale.ContainsKey(this.edits.girl_bScales[i].Key))
                {
                    def = dicNScale[this.edits.girl_bScales[i].Key];
                    dicNScale.Remove(this.edits.girl_bScales[i].Key);
                }
                //cfg.edits.girl_bScales[i] = new KeyValuePair<string, Vector3>(cfg.edits.girl_bScales[i].Key, def);
                setBoneLocsclRL(go.transform, this.targetDics, this.edits.girl_bScales[i].Key, def);
                this.edits.girl_bScales[i] = new StrV3Pair(string.Empty, def);
            }

            //this.edits.girl_bScales.Clear();
            //foreach (var v in girl_bScales0)
            //{
            //    this.edits.girl_bScales.Add(new StrV3Pair(string.Empty, v.Value));
            //}

            foreach (var s in dicNScale)
            {
                setBoneLocsclRL(go.transform, this.targetDics, s.Key, s.Value);
            }
            dicNScale.Clear();

            this.girl_bScales0.Clear();
            this.edits.girl_bScales.Clear();
            this.CacheClear();


            // 位置設定クリア
            if (pPosCtrl != null)
                pPosCtrl.ResetBonePos();

            // オフセット情報もクリア
            ClearOffsetData();

            // パーティクルサイズもリセット
            this.edits.fixParticlesScale = 1f;
        }

        public void ClearOffsetData()
        {
            ResetPosRot();
            this.edits.offsetData.offsetByScenes.list.Clear();
            this.edits.offsetData.offsetByMotions.list.Clear();
        }

        public void ResetPosRot()
        {
            this.edits.offsetData.girlOffset.dpos = Vector3.zero;
            this.edits.offsetData.girlOffset.drot = Vector3.zero;
            this.edits.offsetData.girlOffset.coord = OffsetCoord.None;
        }

        // リセット（内部用)
        private void resetBoneLocscl()
        {
            var go = Extentions.FindSp(rootObjPath);

            foreach (var s in dicNScale)
            {
                setBoneLocsclRL(go.transform, this.targetDics, s.Key, s.Value);
            }
            dicNScale.Clear();
        }

        // 一時的に設定を解除
        public void ResetBoneTemp()
        {
            //if (edits.girl_bScales.Count <= 0)
            //    return;

            var go = Extentions.FindSp(rootObjPath);
            if (!go)
                return;

            var tr = go.transform;
#if test
            //var dic = this.edits.girl_bScales.FindAll(s => !string.IsNullOrEmpty(s.Key));
            var dic = this.CacheValid_girl_bScales;

            for (int i = 0; i < dic.Count; i++)
            {
                var s = dic[i];
                //if (string.IsNullOrEmpty(s.Key))
                //    continue;
#else
            foreach (var s in IEnumValidScales(this.edits.girl_bScales))
            {
#endif
                // パーティクルを除外
                if (s.Key.EndsWith("(Clone)", StringComparison.Ordinal))
                    continue;

                if (dicNScale.TryGetValue(s.Key, out Vector3 def))
                {
                    setBoneLocsclRL(tr, this.targetDics, s.Key, def);
                }
            }

            // テスト用簡易版
            //foreach (var s in dicNScale)
            //{
            //    setBoneLocsclRL(tr, this.targetDics, s.Key, s.Value);
            //}

            // 位置はアニメーションに基本データが無いので戻さなくてもよさそう
            //pPosCtrl.ResetBoneLocposTemp();
        }

        public void ResetBoneTempTgt(string targetBone)
        {
            var tr = Extentions.FindSp(rootObjPath).transform;
            for (int i = 0; i < this.edits.girl_bScales.Count; i++)
            {
                var s = this.edits.girl_bScales[i];
                if (s.Key != targetBone)
                    continue;

                if (dicNScale.TryGetValue(s.Key, out Vector3 def))
                {
                    setBoneLocsclRL(tr, this.targetDics, s.Key, def);
                }
                break;
            }
        }

        public void PreSaveEdits()
        {
            this.edits.setupScales();
            pPosCtrl.edits.setupPos();
        }

        public void LoadEdits(Edits edits)
        {
            this.edits.setControl(null);
            
            this.edits = edits;
            this.edits.setControl(this);
            this.edits.setupScales();

            pPosCtrl.LoadEdits(edits.partsPos);

            //// シーンオフセット適用
            //this.ApplyOffsetByScene(IO_ExSlider.actScene);
            //// モーションオフセット初期化
            //this.InitOffsetByMotions();

            // シーン内データリセット
            OnNewSceneLoaded(IO_ExSlider.actScene);
        }
    }
}

using nnPlugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using static BepInEx.IO_ExSlider.Plugin.IO_ExSlider;

namespace BepInEx.IO_ExSlider.Plugin
{
    [Serializable]
    public class TargetIni
    {
        public static TargetIni targetIni = new TargetIni( 0092 );
        public static readonly string _fname = $"{SaveFileName}-TargetIni.xml";

        // v0.92で追加
        public int iniVersion = 0; // iniファイルを更新用

        public static void Save()
        {
            MyXML.XML_Save(targetIni, _fname);
        }

        public static bool Load()
        {
            if (targetIni != null && targetIni.dics != null)
            {
                targetIni.dics.ForEach(x => x.cacheClear());
            }

            if (MyXML.XML_Load(out TargetIni data, _fname))
            {
                // v0.92 iniファイルのバージョンチェック
                if (data.iniVersion < targetIni.iniVersion)
                {
                    Debug.LogWarning($"{_fname}ファイルが更新されました。{data.iniVersion:0000}→{targetIni.iniVersion:0000}");
                    // 古かったら読込中止
                    for(int i = 0; i < Math.Min(data.girls.Length, targetIni.girls.Length); i++)
                    {
                        // 名前だけは継承
                        targetIni.girls[i].name = data.girls[i].name;
                    }
                    return false;
                }

                targetIni = data;
                return true;
            }
            return false;
        }

        public enum TargetChar
        {
            Neko = 0,
            Usa = 1,
            Player = 2,
            Shota01 = 3,
            Shota02 = 4,
            Shota03 = 5,
        }
        public const string PlayerCharID = "Player";

        public CharaObjPaths[] girls = new CharaObjPaths[]
        {
            new CharaObjPaths("Neko", "ネコ子", "CH01", "CH0001/HS_kiten", "CH0001"),
            new CharaObjPaths("Usa", "ウサ子", "CH02", "CH0002/HS_kiten_02", "CH0002"),
            //new CharaObjPaths(PlayerCharID, "プレイヤー", "PC00", "PC0000", "PC0000", 1),
            new CharaObjPaths(PlayerCharID, "プレイヤー", "PC00", "PC0000/HS_kiten_PC", "PC0000", 1),
            new CharaObjPaths("SY01", "ショタ01", "SY01", "SY0001", "SY0001", 2), //ショタは常にモデルルートごと動かさないと崩れる
            new CharaObjPaths("SY02", "ショタ02", "SY02", "SY0002", "SY0002", 2),
            new CharaObjPaths("SY03", "ショタ03", "SY03", "SY0003", "SY0003", 2),
        };

        // フィルタリング用 口ボーン名（部分一致）
        public string[] listFilterMouthBonesC = { "HF01_tongue", "HS01_MouthSub", "BF01_teethT", };


        public string[] listBipBoneRoots = new string[]
        {
                "bip01",
                "bip01_02",
                "PC00Bip",
                "HS_kiten_syA", //bip01_syA",
                "HS_kiten_syB", //bip01_syB",
                "HS_kiten_syC", //bip01_syC",
        };

        public string[] listFKboneRoots = new string[]
        {
                "HS_kiten",
                "HS_kiten_02",
                "HS_kiten_PC",
                "HS_kiten_syA",
                "HS_kiten_syB",
                "HS_kiten_syC",
        };
        public bool IsMouthBone(string bone)
        {
            var name = PlgUtil.GetObjNameFromPath(bone);
            if (listFilterMouthBonesC.Any(x => name.Contains(x)))
                return true;

            return false;
        }

        public TargetDics[] dics;
        public TargetDics headScalesDic;

        [Serializable]
        public class TargetDics
        {
            public string[] ignoreShapeKeys = { "no", };

            //public string[] keywordOjbRs = { "bip01 R", "HS_mesenR", "BP01_aniEar_R", "HF01_hohoR" };
            //public string[] validObjNamesSW = { "bip", "BP01_ago", "HS_mesenR", "HS_mesenL", "HF01_tongue01", "HS01_MouthSub", "HS_Hair", "BP01_aniEar_R02", "BP01_aniEar_R12", "BP01_hair" };    // 前方一致含む

            /// <summary>
            /// Key=カテゴリ, Value=ボーン名（前方一致）
            /// </summary>
            public KeyValuePair<string, string[]>[] bscValidObjNamesSW = new[]
            {
                new KeyValuePair<string, string[]>("体形", new [] { @"\"+Extentions.FIND_PATH_CURRENT_TR, "bip", } ),
                new KeyValuePair<string, string[]>("体形2 (細部)", new [] { "BP_", "HS", } ),
                new KeyValuePair<string, string[]>("顔", new [] {"bip01 Head", "HS_Head", "BP01_ago", "HS_eyePosi", "HS_mesenR", "HF01_tongue01", "HS01_MouthSub", "BF01_teethT", "HF01_hohoR" } ),
                new KeyValuePair<string, string[]>("髪", new [] { "HS_Hair", "BP01_hair" } ),
                new KeyValuePair<string, string[]>("ケモノ", new [] { "BP01_aniEar_R", "BP01_tail", } ),
                new KeyValuePair<string, string[]>("スカート", new [] { "BP01_skirt", } ),
                new KeyValuePair<string, string[]>("アイテム", new [] { @"HS[0-9][0-9]_IT", @"HS[0-9][0-9]_item", } ),
                new KeyValuePair<string, string[]>("パーティクル", new [] { "Particle System", "uLiquid", "Pool", "Soap", "SubEmitter", } ),
            };

            public KeyValuePair<string, string>[] bscTransNameR2L = new[]
            {
                new KeyValuePair<string, string>("bip01 R", "bip01 L" ),
                new KeyValuePair<string, string>("HS_mesenR", "HS_mesenL" ),
                new KeyValuePair<string, string>("_RR", "_LL" ),   // v0.92
                new KeyValuePair<string, string>("_R", "_L" ),
                //new KeyValuePair<string, string>("BP01_aniEar_R", "BP01_aniEar_L" ),
                new KeyValuePair<string, string>("hohoR", "hohoL" ),
                new KeyValuePair<string, string>("BP_legR", "BP_legL" ),   // v0.92
                new KeyValuePair<string, string>("HS_legR", "HS_legL" ),   // v0.92
                new KeyValuePair<string, string>("shoR", "shoL" ),   // v0.92
                new KeyValuePair<string, string>("siriR", "siriL" ),   // v0.92
                new KeyValuePair<string, string>("wakiR", "wakiL" ),   // v0.92
                new KeyValuePair<string, string>("hijiR", "hijiL" ),   // v0.92
                new KeyValuePair<string, string>("hizaR", "hizaL" ),   // v0.92
                new KeyValuePair<string, string>("HitomiR", "HitomiL" ),   // v0.92
                new KeyValuePair<string, string>("hutaeR", "hutaeL" ),   // v0.92
                new KeyValuePair<string, string>("MayuR", "MayuL" ),   // v0.92
                new KeyValuePair<string, string>("tekubiR", "tekubiL" ),   // v0.92
                new KeyValuePair<string, string>("asikubiR", "asikubiL" ),   // v0.92
                // 乱暴すぎるかも
                //new KeyValuePair<string, string>("R00", "L00" ),   // v0.92
                //new KeyValuePair<string, string>("R01", "L01" ),   // v0.92
                //new KeyValuePair<string, string>("R02", "L02" ),   // v0.92
                //new KeyValuePair<string, string>("R03", "L03" ),   // v0.92
                //new KeyValuePair<string, string>("Right", "Left" ),
            };

            public string[] bscIgnoreObjNamesRE = { "Nub", "bip01 L", @"HS_mesen[RL]E", @"HS_eye[RL]",/*@"aniEar_[RL][0-9]{2}_02",*/ @"[0-9_RLCF]end",/*, "1_02", "2_02"*/
                "HS_mesenL", "legL", "shoL", "hohoL", "siriL", "wakiL", "_LL", "_L00", "hizaL", "hijiL", "HitomiL", "hutaeL", "MayuL", "tekubiL", "asikubiL", /*"L00", "L01", "L02", "L03",*/ // v0.92
            };  // 部分一致含む


            public KeyValuePair<string, string[]>[] bpsValidObjNamesSW = new[]
            {
                new KeyValuePair<string, string[]>("体形 配置", new [] { "bip", } ),
                //new KeyValuePair<string, string[]>("体形 配置", new [] { @"bip01$", "bip01 Pelvis", "bip01 Neck", "bip01 Head", "bip01 [RL] Thigh", "bip01 [RL]ThighTwist", "bip01 [RL] Clavicle", } ), //"Particle System[Nyo]"
                new KeyValuePair<string, string[]>("体形2 配置", new [] { "BP_", "HS", } ),
                new KeyValuePair<string, string[]>("顔 配置", new [] { "bip01 Head", "HS_Head", "BP01_ago", "HS_eyePosi", "HF01_tongue01", "HS01_MouthSub", "BF01_teethT", "HS_mesen[RL]", /*"HS_Mayu", */"HF01_hoho", } ),
                new KeyValuePair<string, string[]>("髪 配置", new [] { "HS_Hair", "BP01_hair" } ),
                new KeyValuePair<string, string[]>("ケモノ 配置", new [] { "BP01_aniEar_", "BP01_tail", } ),
                new KeyValuePair<string, string[]>("アイテム 配置", new [] { @"HS[0-9][0-9]_IT", @"HS[0-9][0-9]_item", } ),
                new KeyValuePair<string, string[]>("パーティクル 配置", new [] { "Particle System", "uLiquid", "Pool", "Soap", "SubEmitter", } ),
            };

            public string[] bpsIgnoreObjNamesRE = { "Nub", @"HS_mesen[RL]E", @"HS_eye[RL]", /*@"aniEar_[RL][0-9]{2}_02",*/ @"[0-9_RLCF]end", };  // 部分一致含む

            public string[] extraFindSpTarget = {
                "../MilkR/",
                "../MilkL/",
                //"../MilkR/Particle System[BonyuR](Clone)/uLiquid - Water - Soapy-E",
                //"../MilkR/Particle System[BonyuR](Clone)/",
                //"../MilkR/Particle System[Bonyu2](Clone)(Clone)",
                //"../MilkL/Particle System[BonyuL](Clone)/uLiquid - Water - Soapy-E",
                //"../MilkL/Particle System[BonyuL](Clone)/",
                //"../MilkL/Particle System[Bonyu2](Clone)",
            }; // 相対パス可

            public TargetDics()
            {

            }

            // GUI用にキャッシュして高速化
            Dictionary<string, bool> cacheBSclValid = new Dictionary<string, bool>();
            public bool checkBSclObjNameValid(string name)
            {
                bool ret;
                if (cacheBSclValid.TryGetValue(name, out ret))
                    return ret;

                //if (bscValidObjNamesSW.Any(x => x.Value.Any(y => CacheRegex.IsMatch(name, "^" + y))))
                if (bscValidObjNamesSW.Any(x => x.Value.Any(y => Regex.IsMatch(name, "^" + y)))) //正規表現のキャッシュなし
                    ret = true;
                else
                    ret = false;

                return cacheBSclValid[name] = ret;
            }

            // GUI用にキャッシュして高速化
            Dictionary<string, bool> cacheBPosValid = new Dictionary<string, bool>();
            public bool checkBPosObjNameValid(string name)
            {
                bool ret;
                if (cacheBPosValid.TryGetValue(name, out ret))
                    return ret;

                //if (bpsValidObjNamesSW.Any(x => x.Value.Any(y => CacheRegex.IsMatch(name, "^" + y))))
                if (bpsValidObjNamesSW.Any(x => x.Value.Any(y => Regex.IsMatch(name, "^" + y))))
                    ret = true;
                else
                    ret = false;

                return cacheBPosValid[name] = ret;
            }


            Dictionary<string, string> cacheDispStrBSC = new Dictionary<string, string>();
            public string getDispStrBSC(string dic_name, string bone_name)
            {
                string ret;
                if (cacheDispStrBSC.TryGetValue(dic_name + "#" + bone_name, out ret))
                    return ret;

                var pair = this.bscValidObjNamesSW.First(x => x.Key == dic_name);
                return cacheDispStrBSC[dic_name + "#" + bone_name] = _getDispStrBSC(pair, bone_name);
            }

            public string getDispStrBSC(string bone_name)
            {
                string ret;
                if (cacheDispStrBSC.TryGetValue(bone_name, out ret))
                    return ret;
                return cacheDispStrBSC[bone_name] = _getDispStrBSC(new KeyValuePair<string, string[]>(null, null), bone_name);
            }

            string _getDispStrBSC(KeyValuePair<string, string[]> pair, string bone_name)
            {
                string ret = null;

                string objname = System.IO.Path.GetFileName(bone_name);
                //if (!filter.Any(x => objname.Contains(x)))
                //if (!TargetIni.TargetDics.checkObjNameBsclValid(pair, objname))

                if (pair.Value != null && !pair.Value.Any(x => CacheRegex.IsMatch(objname, "^" + x)))
                    return null;

                //if (BoneScales.hideNoacts)
                //{
                //    var root = girlCtrl.FindBone().transform;
                //    var bonetr = root.FindSp(bone_name);
                //    if (!bonetr || !bonetr.gameObject.activeInHierarchy)
                //    {
                //        continue;
                //    }
                //}

                //bone_name = bone_name.Replace(" R", " R(L)");
                if (bone_name.IndexOf("/") >= 0)
                {
                    string level = $"[{bone_name.Count(x => x == '/')}]";
                    //for (int n = 0; n < bone_name.Count(x => x == '/'); n++)
                    //    level += "+";

                    bone_name = level + bone_name.Substring(bone_name.LastIndexOf("/"));
                }

                if (this.bscTransNameR2L.Any(x => bone_name.IndexOf(x.Key) >= 0))
                {
                    string str = bone_name;
                    this.bscTransNameR2L.ForEach(x => str = str.Replace(x.Key, x.Value));

                    int si = 0;
                    for (int s = 0; s < bone_name.Length; s++)
                    {
                        if (bone_name[s] != str[s])
                        {
                            si = s;
                            break;
                        }
                    }

                    bone_name = $"{bone_name}({str.Substring(si)})";
                }

                if (bone_name != null)
                    ret = TargetIni.TranslateToDisp(bone_name);

                return ret;
            }


            Dictionary<string, string> cacheDispStrBPS = new Dictionary<string, string>();
            public string getDispStrBPS(string dic_name, string bone_name)
            {
                string ret;
                if (cacheDispStrBPS.TryGetValue(dic_name + "#" + bone_name, out ret))
                    return ret;

                var pair = this.bpsValidObjNamesSW.First(x => x.Key == dic_name);
                return cacheDispStrBPS[dic_name + "#" + bone_name] = _getDispStrBPS(pair, bone_name);
            }

            string _getDispStrBPS(KeyValuePair<string, string[]> pair, string bone_name)
            {
                string ret = null;

                string objname = System.IO.Path.GetFileName(bone_name);
                //if (!filter.Any(x => objname.Contains(x)))
                //if (!TargetIni.TargetDics.checkObjNameBposValid(pair, objname))
                if (!pair.Value.Any(x => CacheRegex.IsMatch(objname, "^" + x)))
                    return null;

                if (bone_name.IndexOf("/") >= 0)
                    bone_name = $"[{bone_name.Count(x => x == '/')}]" + bone_name.Substring(bone_name.LastIndexOf("/"));


                if (bone_name != null)
                    ret = TargetIni.TranslateToDisp(bone_name);

                return ret;
            }

            // v0.92でLRのバリエーションが倍増したため追加
            Dictionary<string, string> cacheTransRLStrBSC = new Dictionary<string, string>();
            /*最初に作っとくとも考えたけどメモリ的にもったいない気もする
             * public void initCacheTransRLStrBSC(string[] bone_names)
            {
                if (this.cacheTransRLStrBSC == null)
                    this.cacheTransRLStrBSC = new Dictionary<string, string>();
                else
                    this.cacheTransRLStrBSC.Clear();

                foreach(var key in bone_names)
                {
                    var str = key;
                    this.bscTransNameR2L.ForEach(x => str = str.Replace(x.Key, x.Value));
                    
                    if (str != key)
                        cacheTransRLStrBSC[key] = str;
                }
            }*/

            // ボーンのRL名変換のキャッシュ有り版
            public string getTransRLStrBSC(string bone_name)
            {
                string ret;
                if (cacheTransRLStrBSC != null && cacheTransRLStrBSC.TryGetValue(bone_name, out ret))
                    return ret;

                var str = bone_name;
                this.bscTransNameR2L.ForEach(x => str = str.Replace(x.Key, x.Value));

                // 置換がなかった場合はkey = valueで記録されるので判断可能
                cacheTransRLStrBSC[bone_name] = str;

                return str;
            }

            internal void cacheClear()
            {
                cacheBPosValid.Clear();
                cacheBSclValid.Clear();
                cacheDispStrBPS.Clear();
                cacheDispStrBSC.Clear();
                
                //cacheTransRLStrBSC = null;
                cacheTransRLStrBSC.Clear();
            }



            //// GUI用にキャッシュして高速化
            //static Dictionary<string, bool> cacheBposValid = new Dictionary<string, bool>();
            //public static bool checkObjNameBposValid(KeyValuePair<string, string[]> pair, string objname)
            //{
            //    bool ret;
            //    if (cacheBposValid.TryGetValue(pair.Key + "/" + objname, out ret))
            //        return ret;

            //    //if (!filter.Any(x => CacheRegex.IsMatch(objname, "^" + x)))
            //    if (pair.Value.Any(y => Regex.IsMatch(objname, "^" + y))) //正規表現のキャッシュなし
            //        ret = true;
            //    else
            //        ret = false;

            //    return cacheBposValid[pair.Key + "/" + objname] = ret;
            //}

            //// GUI用にキャッシュして高速化
            //static Dictionary<string, bool> cacheBsclValid = new Dictionary<string, bool>();
            //public static bool checkObjNameBsclValid(KeyValuePair<string, string[]> pair, string objname)
            //{
            //    bool ret;
            //    if (cacheBsclValid.TryGetValue(pair.Key + "/" + objname, out ret))
            //        return ret;

            //    //if (!filter.Any(x => CacheRegex.IsMatch(objname, "^" + x)))
            //    if (pair.Value.Any(y => Regex.IsMatch(objname, "^" + y))) //正規表現のキャッシュなし
            //        ret = true;
            //    else
            //        ret = false;

            //    return cacheBsclValid[pair.Key + "/" + objname] = ret;
            //}

        }

        public TargetIni(int version)
            : this()
        {
            this.iniVersion = version;
        }


        public TargetIni()
        {
            dics = new TargetDics[3];

            girls[0].numberOfDics = 0;
            girls[1].numberOfDics = 0;
            dics[0] = new TargetDics();

            girls[2].numberOfDics = 1;
            
            // プレイヤー用
            dics[1] = new TargetDics();

            dics[1].bscValidObjNamesSW = new[] {
                new KeyValuePair<string, string[]>("体形", new [] { "PC00Bip", } ),
                new KeyValuePair<string, string[]>("体形2 (細部)", new [] { "HS", } ),
            };
            dics[1].bscTransNameR2L = new[]
            {
                new KeyValuePair<string, string>(" R", " L" ),
            };
            dics[1].bscIgnoreObjNamesRE = new string[] { "Nub", "PC00Bip L", };
            dics[1].bpsValidObjNamesSW = new[]
            {
                new KeyValuePair<string, string[]>("体形 配置", new [] { "PC00Bip", } ),
                new KeyValuePair<string, string[]>("体形2 配置", new [] { "HS", } ),
            };

            // ショタ用
            dics[2] = new TargetDics();

            dics[2].bscValidObjNamesSW = new[] {
                new KeyValuePair<string, string[]>("体形", new [] { "bip01", } ),
                new KeyValuePair<string, string[]>("体形2 (細部)", new [] { "HS", } ),
            };
            dics[2].bscTransNameR2L = new[]
            {
                new KeyValuePair<string, string>(" R", " L" ),
            };
            dics[2].bscIgnoreObjNamesRE = new string[] { "Nub", "bip01 L", };
            dics[2].bpsValidObjNamesSW = new[]
            {
                new KeyValuePair<string, string[]>("体形 配置", new [] { "bip01", } ),
                new KeyValuePair<string, string[]>("体形2 配置", new [] { "HS", } ),
            };

            // ヘッドスケール上書き用
            headScalesDic = new TargetDics();
            headScalesDic.bscValidObjNamesSW = new[] {
                new KeyValuePair<string, string[]>("ヘッドスケール上書き用", new [] { "HS_Head", "HS_HeadScale", "HS00_Head" } ),
            };
            headScalesDic.bscTransNameR2L = new KeyValuePair<string, string>[0];
            headScalesDic.bscIgnoreObjNamesRE = new string[0];
            headScalesDic.bpsValidObjNamesSW = new KeyValuePair<string, string[]>[0];
            headScalesDic.bpsIgnoreObjNamesRE = new string[0];
        }

        public KeyValuePair<string, string>[] translateDispStrsRE =
        {
            new KeyValuePair<string, string>(@"^\"+Extentions.FIND_PATH_CURRENT_TR, "基点"),
            new KeyValuePair<string, string>(@"aniEar(_[RL]0[1-3]_02)", "ウサ耳$1"),
            new KeyValuePair<string, string>(@"aniEar(_[RL]0[1-3])", "ネコ耳$1"),
            new KeyValuePair<string, string>(@"aniEar(_[RL]1[1-3]_02)", "たれウサ耳$1"),
            new KeyValuePair<string, string>(@"aniEar(_[RL]1[1-3])", "たれネコ耳$1"),
        };

        //public KeyValuePair<string, string>[] translateDispStrsC =
        //{
        //    new KeyValuePair<string, string>(Extentions.FIND_PATH_CURRENT_TR, "基点"),
        //};

        public static string TranslateToDisp(string path)
        {
            //foreach (var p in targetIni.translateDispStrsC)
            //{
            //    path = path.Replace(p.Key, p.Value);
            //}

            foreach (var p in targetIni.translateDispStrsRE)
            {
                path = Regex.Replace(path, p.Key, p.Value);
            }

            return path;
        }

        public TargetDics getDics(CharaObjPaths paths)
        {
            return dics[paths.numberOfDics];
        }




        [Serializable]
        public struct CharaObjPaths
        {
            public string id;
            public string name;
            public string root;
            public string boneRoot;
            public string modelRoot;

            // 後設定必要
            public int numberOfDics;

            public GameObject rootObj => find("");
            public GameObject boneRootObj => find(boneRoot);
            public GameObject modelRootObj => find(modelRoot);

            public TargetDics dics => targetIni.dics[this.numberOfDics];

            public GameObject find(string objpath)
            {
                if (objpath.StartsWith(root, StringComparison.Ordinal))
                    return Extentions.FindSp(objpath);

                return Extentions.FindSp($"{root}/{objpath}");
            } 

            public CharaObjPaths(string id, string name, string root, string bone, string model, int dicId = 0)
                : this()
            {
                this.id = id;
                this.name = name;
                this.root = root;
                this.boneRoot = bone;
                this.modelRoot = model;
                this.numberOfDics = dicId;
            }

            public string getFullPath(string objpath)
            {
                //if (objpath.StartsWith(root, StringComparison.Ordinal))
                if (objpath == root)
                    return objpath;

                return $"{root}/{objpath}";
            }

            public bool isNotGirl()
            {
                return numberOfDics > 0;
            }
            public bool isGirl()
            {
                return numberOfDics == 0;
            }

            public bool isPlayer()
            {
                return numberOfDics == 1;
            }
            public bool isShota()
            {
                return numberOfDics == 2;
            }
        }
    }
}

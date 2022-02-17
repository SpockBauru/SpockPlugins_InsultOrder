using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using nnPlugin;
using static BepInEx.IO_ExSlider.Plugin.IO_ExSlider;
using Hook.nnPlugin.Managed;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections;
using UnityEngine.SceneManagement;
using OhMyGizmo2;

namespace BepInEx.IO_ExSlider.Plugin
{
    public class Free3P
    {
        public static OhMyGizmo gizmoPosroot;

        public static bool IsLoaded = false;
        public static bool FlagStart = false;
        static bool FlagPreStart = false; 

        static AnimatorOverrideController overrideControllerMain;
        static AnimatorOverrideController overrideControllerSubmem;
        static AnimatorOverrideController overrideControllerSubmem_PlayM;

        public static Free3pXtMS XtMS = new Free3pXtMS();

        // IC&FH バックアップ用   
        static List<AudioClip> VoiceDataSubmem;
        static List<AudioClip> GyagVoiceSubmem;
        // FH バックアップ用
        static List<AudioClip> WordPlaySubmem;
        static List<AudioClip> UraVoiceSubmem;

        static List<string> VoiceDataSubmemS;
        static List<string> GyagVoiceSubmemS;
        // IC バックアップ用
        static List<string> WordPlaySubmemS;
        static List<string> UraVoiceSubmemS;

        // ループ化ボイス管理用
        static List<AudioSource> LooperVoices = new List<AudioSource>();


        public static GameObject mainMemModel = null;
        public static GameObject subMemModel = null;
        public static GameObject playerModel = null;
        //static string NextSceneID;// = SaveLoad_Game.Data.savegamedata.SceneID;

        public static bool CheckSubMem()
        {
            bool ok = subMemModel
                && VoiceDataSubmem != null && VoiceDataSubmem.Count > 0
                && GyagVoiceSubmem != null && GyagVoiceSubmem.Count > 0 
                && overrideControllerSubmem
                && overrideControllerSubmem.overridesCount > 0;

            return ok;
        }


        public static void GizmoInit()
        {
            if (!gizmoPosroot)
            {
                gizmoPosroot = OhMyGizmo.AddGizmo(null, "Gizmo_SubMemRoot");
                gizmoPosroot.modePos = true;
                gizmoPosroot.modeRot = true;
                gizmoPosroot.sizeHandle = 2f * 6;
                gizmoPosroot.threthold = 20;
                gizmoPosroot.procTargetCtrl = true;
            }

            gizmoPosroot.visible = false;
            gizmoPosroot.resetTarget();
        }
        public static void Init(string nextScene)
        {
            Debug.Log("Free3P Init");

            FlagStart = false;
            //NextSceneID = null;

            if (IsLoaded)
            {
                IsLoaded = false;

                overrideControllerMain = null;
                overrideControllerSubmem = null;
                overrideControllerSubmem_PlayM = null;

                if (VoiceDataSubmem != null)
                    VoiceDataSubmem.Clear();

                if (GyagVoiceSubmem != null)
                    GyagVoiceSubmem.Clear();

                if (WordPlaySubmem != null)
                    WordPlaySubmem.Clear();

                if (UraVoiceSubmem != null)
                    UraVoiceSubmem.Clear();

                VoiceDataSubmem = null;
                GyagVoiceSubmem = null;
                WordPlaySubmem = null;
                UraVoiceSubmem = null;

                subMemModel = null;
                mainMemModel = null;
                playerModel = null;

                subMemFaceClip = null;

                MotionAndVoices.LastStateName = string.Empty;
                MotionAndVoices.fixIC2FH_bkup = null;
            }

            GizmoInit();

            
            if (!MotionAndVoices.IsLoaded && !MotionAndVoices.IsLoading)
                return; // クリップロード前なら

            if (GameClass.First3P)
                return; // 元々3P有効のシーンなら

            if (HsysMgr.Config.UseFree3P && GameClass.FreeMode
                && SaveLoad_Game.Data.savegamedata.SceneID.Substring(2, 2) != "03")
            {
                if (nextScene == "UC" || nextScene == "IC" || nextScene == "FH")
                {
                    Debug.Log("Free3P Ready");

                    FlagStart = FlagPreStart = true;
                    GameClass.First3P = true; // 対象以外のキャラが破棄されるのを防ぐ


                    //if (nextScene == "IC" || nextScene == "FH")
                    //{
                    //    NextSceneID = SaveLoad_Game.Data.savegamedata.SceneID;
                    //    // AwakeでキャラクターIDを保持する系に03＝3Pを　→　別の問題起きすぎ問題
                    //    SaveLoad_Game.Data.savegamedata.SceneID = SaveLoad_Game.Data.savegamedata.SceneID.Substring(0, NextSceneID.Length -2) + "03";
                    //}
                }
            }

        }

        static Vector3 subKitenPos;
        static Vector3 subKitenRot;

        public static void PreStart(string nextScene)
        {
            if (!FlagPreStart)
                return;

            var orgid = SaveLoad_Game.Data.savegamedata.SceneID;
            var chrid = orgid.Substring(2, 2);

            GirlCtrl girlCtrlSub = CtrlGirlsInst[1];
            if (chrid == "02")
            {
                girlCtrlSub = CtrlGirlsInst[0];
            }

            // ロード完了チェック
            var rootobj = GameObject.Find(girlCtrlSub.ini.root);
            if (!rootobj)
                return;
            
            // 初期位置設定
            FlagPreStart = false;

            var br = rootobj.transform.FindSp(girlCtrlSub.ini.boneRoot, true);
            if (br)
            {
                subKitenPos = br.transform.localPosition ;
                subKitenRot = br.localEulerAngles.angle180();
            }

            if (nextScene == "FH" || nextScene == "UC")
            {
                // 重なり除け
                rootobj.transform.position = new Vector3(1000f, 0, 0);
            }
        }

        public static void Start(string nextScene)
        {
            if (FlagPreStart)
            {
                PreStart(nextScene);
                return;
            }

            if (!FlagStart)
                return;

            FlagStart = false;
            Free3pLoadGui.Start();
        }

        public static void StartProc(bool start)
        {
            if (!start)
                return;

            // シーンロード完了後
            var nextScene = actScene.name;

            GameClass.First3P = false; // 解除

            var mainSystem = GameObject.Find("MainSystem");
            if (!mainSystem)
                return;

            //if (!string.IsNullOrEmpty(NextSceneID))
            //    SaveLoad_Game.Data.savegamedata.SceneID = NextSceneID;

            // 対象設定
            var orgid = SaveLoad_Game.Data.savegamedata.SceneID;
            var chrid = orgid.Substring(2, 2);

            GirlCtrl girlCtrlMain = CtrlGirlsInst[0];
            GirlCtrl girlCtrlSub = CtrlGirlsInst[1];
            int levelSub = SaveLoad_Game.Data.savegamedata.Level_CH02;
            if (chrid == "02")
            {
                girlCtrlMain = CtrlGirlsInst[1];
                girlCtrlSub = CtrlGirlsInst[0];
                levelSub = SaveLoad_Game.Data.savegamedata.Level_CH01;
            }

            // モデルチェック
            if (chrid == "01")
            {
                mainMemModel = CtrlGirlsInst[0].FindModel(true);
                subMemModel = CtrlGirlsInst[1].FindModel(true);
                if (!subMemModel || !mainMemModel)
                    return;
            }
            else
            {
                mainMemModel = CtrlGirlsInst[1].FindModel(true);
                subMemModel = CtrlGirlsInst[0].FindModel(true);
                if (!subMemModel || !mainMemModel)
                    return;
            }
            playerModel = CtrlGirlsInst.Find(x => x.ini.id == TargetIni.PlayerCharID).FindModel(true);

            // レベルチェック
            if (nextScene == "FH")
            {
                // サブキャラのレベルを同等以上に(モーション・ボイスNULL回避)
                if (chrid == "01")
                {
                    if (SaveLoad_Game.Data.savegamedata.Level_CH01 > (SaveLoad_Game.Data.savegamedata.Level_CH02 + 1))
                    {
                        levelSub = HsysMgr.SetGameLevel_CH02 = SaveLoad_Game.Data.savegamedata.Level_CH01 - 1;
                    }
                }
                else
                {
                    if (SaveLoad_Game.Data.savegamedata.Level_CH01 < (SaveLoad_Game.Data.savegamedata.Level_CH02 + 1))
                    {
                        levelSub = HsysMgr.SetGameLevel_CH01 = SaveLoad_Game.Data.savegamedata.Level_CH02 + 1;
                    }
                }
            }

            if (nextScene != "UC")
            {
                if (chrid == "01")
                {
                    // サブキャラ用のアセット読込用
                    SaveLoad_Game.Data.savegamedata.SceneID = orgid.Substring(0, 2) + "02";
                }
                else
                {
                    // サブキャラ用のアセット読込用
                    SaveLoad_Game.Data.savegamedata.SceneID = orgid.Substring(0, 2) + "01";
                }

                /*if (nextScene == "IC")
                {
                    // ボイス＆モーション再ロード
                    resetupScene(mainSystem, nextScene);
                    overrideControllerSubmem = subMemModel.GetComponent<Animator>().runtimeAnimatorController as AnimatorOverrideController;
                    if (!overrideControllerSubmem)
                        Debug.LogError("overrideControllerSubmemがNULL");

                    // 再ロード２(First3pのない正規シーン)
                    SaveLoad_Game.Data.savegamedata.SceneID = orgid;

                    resetupScene(mainSystem, nextScene, true);

                    subMemModel.GetComponent<Animator>().runtimeAnimatorController = overrideControllerSubmem;

                    // シーン再開
                    restartScene(true, mainSystem, nextScene);
                    overrideControllerMain = mainMemModel.GetComponent<Animator>().runtimeAnimatorController as AnimatorOverrideController;
                }*/
                if (nextScene == "IC")
                {
                    // メイン退避 (NekoかPC固定でコントローラ取得)
                    overrideControllerMain = CtrlGirlsInst[0].FindModel().GetComponent<Animator>().runtimeAnimatorController as AnimatorOverrideController;

                    // サブ用ロード
                    resetupScene(mainSystem, nextScene, true);
                    overrideControllerSubmem = subMemModel.GetComponent<Animator>().runtimeAnimatorController as AnimatorOverrideController;
                    if (!overrideControllerSubmem)
                        Debug.LogError("overrideControllerSubmemがNULL");

                    // メインを戻す
                    SaveLoad_Game.Data.savegamedata.ICID = chrid;
                    SaveLoad_Game.Data.savegamedata.SceneID = orgid;
                    mainMemModel.GetComponent<Animator>().runtimeAnimatorController = overrideControllerMain;
                    _ChangeVoiceTable(mainSystem, nextScene);
                    if (playerModel)
                        playerModel.GetComponent<Animator>().runtimeAnimatorController = overrideControllerMain;

                    // シーン再開
                    restartScene(true, mainSystem, nextScene);
                }
                else // FH
                {
                    // メイン退避
                    overrideControllerMain = mainMemModel.GetComponent<Animator>().runtimeAnimatorController as AnimatorOverrideController;

                    // サブ用ロード
                    resetupScene(mainSystem, nextScene, true);
                    overrideControllerSubmem = subMemModel.GetComponent<Animator>().runtimeAnimatorController as AnimatorOverrideController;
                    if (!overrideControllerSubmem)
                        Debug.LogError("overrideControllerSubmemがNULL");

                    // メインを戻す
                    SaveLoad_Game.Data.savegamedata.SceneID = orgid;
                    mainMemModel.GetComponent<Animator>().runtimeAnimatorController = overrideControllerMain;
                    _ChangeVoiceTable(mainSystem, nextScene);
                    if (playerModel)
                        playerModel.GetComponent<Animator>().runtimeAnimatorController = overrideControllerMain;

                    // シーン再開
                    restartScene(true, mainSystem, nextScene);
                }

            }
            else
            {
                // UC
                
                //var setup = mainSystem.GetComponent<UC_SetUp>();
                //var bundle = setup.GetNonPublicField<AssetBundle>("bundle");
                //if (chrid == "01")
                //{
                //    var fi = typeof(UC_SetUp).GetField("CH01ForwardVoice", BindingFlags.NonPublic | BindingFlags.Instance);
                //    var fv = (string[])fi.GetValue(setup); //setup.GetNonPublicField<string[]>("CH02ForwardVoice");

                //    Debug.Log("F3P UCボイス設定 " + (bool)bundle);
                //    foreach (var v in fv)
                //    {
                //        Console.WriteLine(v);
                //    }

                //    setup.CH02VoiceData = new List<AudioClip>();
                //    for (int i = 0; i < fv.Length; i++)
                //    {
                //        setup.CH02VoiceData.Add(bundle.LoadAsset("Kara") as AudioClip);
                //    }
                //    mainSystem.GetComponent<UC_AnimeController>().InvokeNonPublicMethod("Start", null);
                //}
            }

            // 完了
            IsLoaded = true;

            if (nextScene == "FH" || nextScene == "UC")
            {
                subMemModel.transform.root.position = mainMemModel.transform.root.position;
            }

            // サブ位置変更
            if (nextScene == "IC")
            {
                // 前戯の角度・位置修正
                mainMemModel.transform.root.localPosition = new Vector3(0f, 8.7f, 3.93647E-07f);
                mainMemModel.transform.root.eulerAngles = Vector3.zero;
            }

            OffsetSubMemPos(nextScene);

            if (nextScene == "IC")
                Free3P.MotionAndVoices.Play(girlCtrlSub, "(IC)待機", levelSub);
            if (nextScene == "FH")
                HoldSubmemVMState(girlCtrlSub); // モーション固定化
            //    Free3P.MotionAndVoices.Play(girlCtrlSub, "(FH)待機(寝)", levelSub); 立ちプレイなどで違和感ありすぎてNG
            if (nextScene == "UC")
                Free3P.MotionAndVoices.Play(girlCtrlSub, "(CS)立ち0", levelSub);
                //Free3P.MotionAndVoices.Play(girlCtrlSub, "(IC)待機", levelSub);
            
            // その他のスクリプト調整
            if (nextScene == "FH")
            {
                orgid = SaveLoad_Game.Data.savegamedata.SceneID;
                var subid = orgid.Substring(0, orgid.Length - 2);

                int i = 0;

                CtrlGirlsInst.ForEach(x =>
                {
                    i++;
                    var root = GameObject.Find(x.ini.root);
                    if (root && x.ini.isGirl())
                    {
                        SaveLoad_Game.Data.savegamedata.SceneID = $"{subid}{i:00}";

                        Debug.Log($"GetVertex再設定 {SaveLoad_Game.Data.savegamedata.SceneID} {x.ini.id}" );
                        // 頂点アタッチ処理をfix
                        var gv = root.GetComponent<GetVertex>();
                        gv.enabled = true;
                        gv.InvokeNonPublicMethod("Awake", null); // 全コンポーネントに配信されちゃうので .SendMessage("Awake");
                    }
                });

                SaveLoad_Game.Data.savegamedata.SceneID = orgid;
            }

            // 念のため
            setMemAnimeBreastSize(nextScene);

            Debug.Log("F3P: SE不具合回避");
            // Free3p有効状態で、兎フリーH→猫に交代→拘束姦などと遷移するとSEクリップの読込失敗が発生する
            foreach (var go in actScene.GetRootGameObjects())
            {
                resetComp<SE_Particle_Manager>(go, true, true);
            }
            return;
        }


        // サブキャラ位置オフセット
        internal static void OffsetSubMemPos(string newScene)
        {
            Debug.Log("F3P: サブメンバー位置オフセット");
            // クリア
            MotionAndVoices.fixIC2FH_bkup = null;

            if (HsysMgr.Config.Free3p_OffsetByState.list.Count > 0)
            {
                var stateNow = newScene + SaveLoad_Game.Data.savegamedata.SceneID;
                var f = HsysMgr.Config.Free3p_OffsetByState.list.Find(x => x.stateName == stateNow);
                if (f != null)
                {
                    subMemModel.transform.root.position = f.trs.pos;
                    subMemModel.transform.root.eulerAngles = f.trs.rot;
                    return;
                }
            }

            if (HsysMgr.Config.Free3p_OffestSubMem == Vector3.zero)
            {
                subMemModel.transform.root.localPosition = mainMemModel.transform.root.localPosition + new Vector3(6.3f, 0f, 0f);//new Vector3(4.7f, 0, 0);
                subMemModel.transform.root.eulerAngles = mainMemModel.transform.root.eulerAngles;
                if (newScene == "FH")
                    subMemModel.transform.root.localPosition += new Vector3(1.1f, 0f, 0f);//new Vector3(2.7f, 0f, 0f);

                if (newScene == "UC")
                {
                    subMemModel.transform.root.localPosition = mainMemModel.transform.root.localPosition + new Vector3(6.3f, 0f, 0f);
                    subMemModel.transform.root.eulerAngles = mainMemModel.transform.root.eulerAngles + new Vector3(0f, -90f, 0f);
                }
            }
            else
            {
                subMemModel.transform.root.localPosition = mainMemModel.transform.root.localPosition + HsysMgr.Config.Free3p_OffestSubMem;
                subMemModel.transform.root.eulerAngles = mainMemModel.transform.root.eulerAngles + HsysMgr.Config.Free3p_OffestRotSubMem;
            }


            /* シーン位置を流用したかったけどモーションによる基点の位置補正が効かないのでNG
            girlCtrlSub.scnPos.enable = true;
            girlCtrlSub.scnPos.pos = girlCtrlMain.ScnPosRoot().transform.localPosition + OffestSubMem;//girlCtrlSub.ScnPosRoot().transform.position + OffestSubMem;
            if (nextScene == "FH")
                girlCtrlSub.scnPos.pos += new Vector3(2.7f, 0f, 0f);

            girlCtrlSub.scnPos.rot = girlCtrlMain.ScnPosRoot().transform.localEulerAngles;
            girlCtrlSub.scnPos.scale = 0.1f;
            */
        }

        static void resetupScene(GameObject mainSystem, string nextScene, bool last = false)
        {
            switch (nextScene)
            {
                case "FH":
                    {
                        var setup = mainSystem.GetComponent<FH_SetUp>();

                        if (last)
                        {
                            VoiceDataSubmemS = setup.VoiceData.Select(x => x.name).ToList();
                            GyagVoiceSubmemS = setup.GyagVoice.Select(x => x.name).ToList();
                            WordPlaySubmemS = setup.WordPlay.Select(x => x.name).ToList();
                            UraVoiceSubmemS = setup.UraVoice.Select(x => x.name).ToList();
                        }

                        setup.Unload();
                        setup.VoiceData.Clear();
                        setup.GyagVoice.Clear();
                        setup.UraVoice.Clear();
                        setup.WordPlay.Clear();

                        if (!last)
                            Resources.UnloadUnusedAssets();

                        setup.InvokeNonPublicMethod("Awake", null);

                        if (last)
                        {
                            var bundle = setup.GetNonPublicField<AssetBundle>("bundle");
                            VoiceDataSubmem = VoiceDataSubmemS.Select(x => bundle.LoadAsset<AudioClip>(x)).ToList();
                            GyagVoiceSubmem = GyagVoiceSubmemS.Select(x => bundle.LoadAsset<AudioClip>(x)).ToList();
                            WordPlaySubmem = WordPlaySubmemS.Select(x => bundle.LoadAsset<AudioClip>(x)).ToList();
                            UraVoiceSubmem = UraVoiceSubmemS.Select(x => bundle.LoadAsset<AudioClip>(x)).ToList();

                            Debug.Log($"Vnum:{setup.VoiceData.Count} /Snum:{VoiceDataSubmem.Count}");
                            Debug.Log($"Gnum:{setup.GyagVoice.Count} /Snum:{GyagVoiceSubmem.Count}");
                            Debug.Log($"Wnum:{setup.WordPlay.Count} /Snum:{WordPlaySubmem.Count}");
                            Debug.Log($"Unum:{setup.UraVoice.Count} /Snum:{UraVoiceSubmem.Count}");
                        }
                    }
                    break;

                case "IC":
                    {
                        var setup = mainSystem.GetComponent<IC_SetUp>();

                        if (last)
                        {
                            VoiceDataSubmemS = setup.VoiceData.Select(x => x.name).ToList();
                            GyagVoiceSubmemS = setup.GyagVoice.Select(x => x.name).ToList();
                        }

                        setup.Unload();
                        setup.VoiceData.Clear();
                        setup.GyagVoice.Clear();

                        if (!last)
                            Resources.UnloadUnusedAssets();

                        setup.InvokeNonPublicMethod("Awake", null);

                        if (last)
                        {
                            var bundle = setup.GetNonPublicField<AssetBundle>("bundle");
                            VoiceDataSubmem = VoiceDataSubmemS.Select(x => bundle.LoadAsset<AudioClip>(x)).ToList();
                            GyagVoiceSubmem = GyagVoiceSubmemS.Select(x => bundle.LoadAsset<AudioClip>(x)).ToList();

                            Debug.Log($"Vnum:{setup.VoiceData.Count} /Snum:{VoiceDataSubmem.Count}");
                            Debug.Log($"Gnum:{setup.GyagVoice.Count} /Snum:{GyagVoiceSubmem.Count}");
                        }
                    }
                    break;
            }
        }

        // フリーHシーンレベルアップ用
        static void resetupSubVoices(GameObject mainSystem, string scene)
        {
            switch (scene)
            {
                case "FH":
                    {
                        var setup = mainSystem.GetComponent<FH_SetUp>();

                        var bundle = setup.GetNonPublicField<AssetBundle>("bundle");
                        VoiceDataSubmem = VoiceDataSubmemS.Select(x => bundle.LoadAsset<AudioClip>(x)).ToList();
                        GyagVoiceSubmem = GyagVoiceSubmemS.Select(x => bundle.LoadAsset<AudioClip>(x)).ToList();
                        WordPlaySubmem = WordPlaySubmemS.Select(x => bundle.LoadAsset<AudioClip>(x)).ToList();
                        UraVoiceSubmem = UraVoiceSubmemS.Select(x => bundle.LoadAsset<AudioClip>(x)).ToList();

                        Debug.Log($"Vnum:{setup.VoiceData.Count} /Snum:{VoiceDataSubmem.Count}");
                        Debug.Log($"Gnum:{setup.GyagVoice.Count} /Snum:{GyagVoiceSubmem.Count}");
                        Debug.Log($"Wnum:{setup.WordPlay.Count} /Snum:{WordPlaySubmem.Count}");
                        Debug.Log($"Unum:{setup.UraVoice.Count} /Snum:{UraVoiceSubmem.Count}");
                    }
                    break;

                case "IC":
                    {
                        var setup = mainSystem.GetComponent<IC_SetUp>();

                        var bundle = setup.GetNonPublicField<AssetBundle>("bundle");
                        VoiceDataSubmem = VoiceDataSubmemS.Select(x => bundle.LoadAsset<AudioClip>(x)).ToList();
                        GyagVoiceSubmem = GyagVoiceSubmemS.Select(x => bundle.LoadAsset<AudioClip>(x)).ToList();

                        Debug.Log($"Vnum:{setup.VoiceData.Count} /Snum:{VoiceDataSubmem.Count}");
                        Debug.Log($"Gnum:{setup.GyagVoice.Count} /Snum:{GyagVoiceSubmem.Count}");
                    }
                    break;
            }
        }



        static void restartScene(bool end, GameObject mainSystem, string nextScene)
        {
            switch (nextScene)
            {
                case "FH":
                    mainSystem.GetComponent<FH_AnimeController>().InvokeNonPublicMethod("Awake", null); // フリー
                    mainSystem.GetComponent<FH_AnimeController>().InvokeNonPublicMethod("Start", null); // フリー

                    if (end)
                    {
                    }
                    break;

                case "IC":
                    mainSystem.GetComponent<IC_AnimeController>().InvokeNonPublicMethod("Awake", null); // 前
                    mainSystem.GetComponent<IC_AnimeController>().InvokeNonPublicMethod("Start", null); // 前

                    if (end)
                    {
                        var ui = GameObject.Find("UI Root(Menu)");
                        if (ui)
                        {
                            ui.GetComponent<MenuUI>().ZengiStart();

                            subMemModel.transform.root.localPosition = new Vector3(0f, 8.7f, 3.93647E-07f);
                            subMemModel.transform.root.localEulerAngles = new Vector3(0f, 0f, 0f);
                        }
                    }
                    break;
            }
        }

        static void _ChangeVoiceTable(GameObject mainSystem, string scene)
        {
            if (scene == "IC")
            {
                var setup = mainSystem.GetComponent<IC_SetUp>();
                var VoiceDataSubmem2 = setup.VoiceData;
                var GyagVoiceSubmem2 = setup.GyagVoice;

                setup.VoiceData = VoiceDataSubmem;
                setup.GyagVoice = GyagVoiceSubmem;

                VoiceDataSubmem = VoiceDataSubmem2;
                GyagVoiceSubmem = GyagVoiceSubmem2;

                // リスト更新
                VoiceDataSubmemS = VoiceDataSubmem.Select(x => x.name).ToList();
                GyagVoiceSubmemS = GyagVoiceSubmem.Select(x => x.name).ToList();
            }

            if (scene == "FH")
            {
                var setup = mainSystem.GetComponent<FH_SetUp>();
                var VoiceDataSubmem2 = setup.VoiceData;
                var GyagVoiceSubmem2 = setup.GyagVoice;
                var UraVoiceSubmem2 = setup.UraVoice;
                var WordPlaySubmem2 = setup.WordPlay;

                setup.VoiceData = VoiceDataSubmem;
                setup.GyagVoice = GyagVoiceSubmem;
                setup.UraVoice = UraVoiceSubmem;
                setup.WordPlay = WordPlaySubmem;

                VoiceDataSubmem = VoiceDataSubmem2;
                GyagVoiceSubmem = GyagVoiceSubmem2;
                UraVoiceSubmem = UraVoiceSubmem2;
                WordPlaySubmem = WordPlaySubmem2;

                // リスト更新
                VoiceDataSubmemS = VoiceDataSubmem.Select(x => x.name).ToList();
                GyagVoiceSubmemS = GyagVoiceSubmem.Select(x => x.name).ToList();
                WordPlaySubmemS = WordPlaySubmem.Select(x => x.name).ToList();
                UraVoiceSubmemS = UraVoiceSubmem.Select(x => x.name).ToList();
            }
        }

        public static void ChangeMainChar(bool keepAnimeNow)
        {
            if (subMemModel && overrideControllerSubmem && mainMemModel)
            {
                var scene = actScene.name;
                if (scene == "UC")
                    return;

                var mainSystem = GameObject.Find("MainSystem");
                if (!mainSystem)
                    return;

                Debug.Log("F3P: 入れ替え処理");

                var orgid = SaveLoad_Game.Data.savegamedata.SceneID;
                var chrid = orgid.Substring(2, 2);

                // ボイス変更
                _ChangeVoiceTable(mainSystem, scene);


                if (chrid == "01")
                {
                    SaveLoad_Game.Data.savegamedata.SceneID = orgid.Substring(0, 2) + "02";
                }
                else
                {
                    SaveLoad_Game.Data.savegamedata.SceneID = orgid.Substring(0, 2) + "01";
                }

                var comp = subMemModel.GetComponent<Free3PVoicePlayer>();
                if (comp)
                    comp.Close();

                // アニメ入れ替え
                subMemModel.GetComponent<Animator>().runtimeAnimatorController = overrideControllerSubmem;
                if (playerModel)
                    playerModel.GetComponent<Animator>().runtimeAnimatorController = overrideControllerSubmem;

                // ターゲット入れ替え
                var sub = mainMemModel;
                mainMemModel = subMemModel;
                subMemModel = sub;

                var subCtrl = CtrlGirlsInst.FirstOrDefault(x => x.FindModel() == subMemModel);
                string clipname = "", ctrlname = "";
                subCtrl.charaCtrl.getAnimeState(ref ctrlname, ref clipname);
                if (actScene.name == "FH")
                    subMemFaceClip = subCtrl.charaCtrl.getAnimeClip(2);

                overrideControllerSubmem = subMemModel.GetComponent<Animator>().runtimeAnimatorController as AnimatorOverrideController;
                overrideControllerMain = mainMemModel.GetComponent<Animator>().runtimeAnimatorController as AnimatorOverrideController;

                // 位置入れ替え
                var pos = subMemModel.transform.root.position;
                subMemModel.transform.root.position = mainMemModel.transform.root.position;
                mainMemModel.transform.root.position = pos;

                var rot = subMemModel.transform.root.rotation;
                subMemModel.transform.root.rotation = mainMemModel.transform.root.rotation;
                mainMemModel.transform.root.rotation = rot;
                //var mainCtrl = CtrlGirlsInst.FirstOrDefault(x => x.FindModel() == mainMemModel);
                //var pos = mainCtrl.scnPos;
                //mainCtrl.scnPos = subCtrl.scnPos;
                //subCtrl.scnPos = pos;

                Debug.Log("F3P: オーディオ削除1");

                // ループ解除（メイン）
                foreach (var a in LooperVoices)
                {
                    if (a && a.loop)
                    {
                        a.Stop();
                        a.loop = false;
                    }
                }
                LooperVoices.Clear();


                //// テスト
                //resetupScene(mainSystem, scene, false);
                //overrideControllerMain = mainMemModel.GetComponent<Animator>().runtimeAnimatorController as AnimatorOverrideController;
                //subMemModel.GetComponent<Animator>().runtimeAnimatorController = overrideControllerSubmem;

                Debug.Log("F3P: オーディオ削除2");

                if (actScene.name == "FH")
                {
                    // AudioSourceを全削除
                    var hs_head = mainMemModel.FindDeep("HS_Head");
                    foreach (var s in hs_head.GetComponents<AudioSource>().ToArray())
                    {
                        if (!s) continue;

                        if (s.isPlaying) s.Stop();
                        GameObject.DestroyImmediate(s);
                    }
                }
                else if (actScene.name == "IC")
                {
                    // SoundUnitを全削除
                    var su = mainMemModel.FindDeep("SoundUnit_");
                    if (su)
                    {
                        var hs_head = su.transform.parent;
                        foreach (var s in hs_head.GetComponents<SoundUnit>().ToArray())
                        {
                            if (s && s.gameObject.name == "SoundUnit_")
                            {
                                GameObject.DestroyImmediate(s);
                            }
                        }
                    }
                }

                Debug.Log("F3P: シーン再スタート");
                // シーン再スタート
                restartScene(false, mainSystem, scene);

                Debug.Log("F3P: モーション設定");

                if (playerModel)
                {
                    playerModel.GetComponent<Animator>().ForceStateNormalizedTime(0);
                    mainMemModel.GetComponent<Animator>().ForceStateNormalizedTime(0);
                }

                if (keepAnimeNow)
                {
                    subMemModel.GetComponent<Animator>().runtimeAnimatorController = overrideControllerSubmem;
                    HoldSubmemVMState(subCtrl, clipname);
                }
                else
                {
                    if (scene == "FH")
                        LockAnimeClip(subCtrl, clipname); // 表情を固定する

                    AudioClip voiceLast = null;

                    //foreach (var a in subMemModel.GetComponentsInChildren<AudioSource>())
                    //{
                    //    if (a && a.isPlaying && a.gameObject.name == "HS_Head")
                    //    {
                    //        if (a.clip && a.volume > 0)
                    //            voiceLast = a.clip;
                    //        // 元のボイス停止→ アニメセット時に設定するのでここはなくて問題なくなったはず
                    //        //a.Stop();
                    //    }
                    //}
                    voiceLast = GetPlayingAudioClip(subMemModel);

                    // サブ再設定
                    Recovery(false, false);

                    if (scene == "FH")
                    {
                        // FHは表情がモーションと別管理なのでボイスを合わせないと不自然になる
                        if (voiceLast)
                        {
                            Free3PVoicePlayer.Inst.SetVoice(voiceLast);
                        }
                    }
                }

                Debug.Log("F3P: その他初期化");

                // その他のスクリプト 手抜き実装
                foreach (var go in actScene.GetRootGameObjects())
                {
                    resetComp<SeiekiPath>(go, true, false);
                    resetComp<SeiekiPathController>(go, true, false);
                    //resetComp<SE_Particle_Manager>(go, true, true);
                    resetComp<SE_Particle_Manager>(go, false, true);
                    resetComp<LiquidCounter>(go, true, true);
                    resetComp<CameraAuto>(go, true, false);
                    //resetComp<CameraPosition>(go, false, true);
                    resetComp<TenKeyPad>(go, true, false);
                    
                    if (scene == "IC")
                        resetComp<MenuUI>(go, false, false);
                }

                // 念のため
                setMemAnimeBreastSize(scene);
                return;

            }
        }

        static AudioClip GetPlayingAudioClip(GameObject obj)
        {
            AudioClip voiceLast = null;

            foreach (var a in obj.GetComponentsInChildren<AudioSource>())
            {
                if (a && a.isPlaying && (a.gameObject.name == "HS_Head" || a.gameObject.name == "SoundUnit_"))
                {
                    if (a.clip && a.volume > 0)
                        voiceLast = a.clip;
                }
            }
            return voiceLast;
        }

        static void setMemAnimeBreastSize(string scene)
        {
            Debug.Log("F3P: 胸サイズをアニメーターに反映");
            // 逆IDでチェック
            if (SaveLoad_Game.Data.savegamedata.SceneID.Substring(2, 2) == "01")
            {
                mainMemModel.GetComponent<Animator>().SetFloat("BreastSize", SaveLoad_CharaData01.Data.savegamedata.BreastSize); // Neko
                subMemModel.GetComponent<Animator>().SetFloat("BreastSize", SaveLoad_CharaData02.Data.savegamedata.BreastSize); // Usa
            }
            else
            {
                mainMemModel.GetComponent<Animator>().SetFloat("BreastSize", SaveLoad_CharaData02.Data.savegamedata.BreastSize); // Usa
                subMemModel.GetComponent<Animator>().SetFloat("BreastSize", SaveLoad_CharaData01.Data.savegamedata.BreastSize); // Neko
            }

            if (scene == "FH" || scene == "IC")
            {
                //アニメコントローラーのスタートの中でやってるので不要？
                var pc = CtrlGirlsInst.Find(x => x.ini.id == TargetIni.PlayerCharID);
                if (pc.isActive())
                {
                    // 新しいIDでチェック
                    if (SaveLoad_Game.Data.savegamedata.SceneID.Substring(2, 2) == "01")
                        pc.FindModel().GetComponent<Animator>().SetFloat("BreastSize", SaveLoad_CharaData01.Data.savegamedata.BreastSize); // Neko
                    else
                        pc.FindModel().GetComponent<Animator>().SetFloat("BreastSize", SaveLoad_CharaData02.Data.savegamedata.BreastSize); // Usa
                }
            }
        }

        private static void resetComp<T>(GameObject go, bool awake, bool start) where T : UnityEngine.MonoBehaviour
        {
            foreach (T m in go.GetComponentsInChildren<T>())
            {
                if (!m || !m.isActiveAndEnabled || !m.gameObject)
                    continue;

                try
                {
                    Debug.Log("reset : " + PlgUtil.GetFullPath(m.transform) + " " + m.GetType().Name);

                    // 個別
                    if (m is SE_Particle_Manager && !awake)
                    {
                        // SE再生停止
                        var sem = m as SE_Particle_Manager;
                        var fis = typeof(SE_Particle_Manager).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                        foreach (var f in fis)
                        {
                            if (f.Name.StartsWith("Speaker") && f.FieldType == typeof(AudioSource))
                            {
                                var speaker = f.GetValue(sem) as AudioSource;
                                if (speaker)
                                {
                                    Debug.Log("Stop " + f.Name);
                                    speaker.volume = 0;
                                    //speaker.Stop();
                                    //era-deru speaker.clip = null;
                                }
                            }
                        }

                        if (!awake)
                        {
                            // Awakeがなしならidだけ書き換え
                            var _fif = sem.GetNonPublicFieldInfo("FullID");
                            var _fic = sem.GetNonPublicFieldInfo("CharaID");

                            _fif.SetValue(sem, SaveLoad_Game.Data.savegamedata.SceneID);
                            _fic.SetValue(sem, SaveLoad_Game.Data.savegamedata.SceneID.Substring(2, 2));
                        }
                        else
                        {
                            //sem.Unload(); // Awake前にUnload
                        }

                        // 再生フラグクリア
                        sem.SetNonPublicField("VibraBool_CH01", false);
                        sem.SetNonPublicField("RotorBool_CH01", false);
                        sem.SetNonPublicField("VibraBool_CH02", false);
                        sem.SetNonPublicField("RotorBool_CH02", false);
                    }

                    // IC用
                    if (m is MenuUI)
                    {
                        // Zengiスタートコルーチンでの位置リセット対策
                        var mui = m as MenuUI;
                        mui.SetNonPublicField("CH", mainMemModel.transform.root);
                    }


                    // 一括
                    if (awake) 
                        m.ActionNonPublicMethod<T>("Awake", null);
                    
                    if (start) 
                        m.ActionNonPublicMethod<T>("Start", null);
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                }
            }
        }


        // サブメンバーのアニメ状態固定用
        static AnimationClip subMemFaceClip = null; // FH用
        static void HoldSubmemVMState(GirlCtrl subCtrl, string clipname = null)
        {
            if (clipname == null)
            {
                clipname = "";
                string ctrlname = "";
                subCtrl.charaCtrl.getAnimeState(ref ctrlname, ref clipname);

                if (actScene.name == "FH")
                    subMemFaceClip = subCtrl.charaCtrl.getAnimeClip(2);
            }

            LockAnimeClip(subCtrl, clipname);

            // 再生オーディオループ化（サブ）
            //foreach (var a in subMemModel.GetComponentsInChildren<AudioSource>())
            //{
            //    if (a && a.isPlaying && !a.loop)
            //    {
            //        a.loop = true;
            //        LooperVoices.Add(a);
            //    }
            //}

            var voiceLast = GetPlayingAudioClip(subMemModel);

            // ボイス設定
            MotionAndVoices.SetVoicePlayer(subCtrl, voiceLast);
        }


        internal static void Recovery(bool keepAnimeNow, bool resetPos)
        {
            if (!Free3P.IsLoaded || !subMemModel)
                return;

            Debug.Log("F3P リカバリ");

            if (subMemModel && overrideControllerSubmem)
                subMemModel.GetComponent<Animator>().runtimeAnimatorController = overrideControllerSubmem;

            var chrid = SaveLoad_Game.Data.savegamedata.SceneID.Substring(2, 2);

            GirlCtrl girlCtrlSub = CtrlGirlsInst[1];
            int levelSub = SaveLoad_Game.Data.savegamedata.Level_CH02;
            if (chrid == "02")
            {
                girlCtrlSub = CtrlGirlsInst[0];
                levelSub = SaveLoad_Game.Data.savegamedata.Level_CH01;
            }

            if (resetPos && actScene.name == "IC")
            {
                // 前戯の角度・位置復元
                mainMemModel.transform.root.localPosition = new Vector3(0f, 8.7f, 3.93647E-07f);
                mainMemModel.transform.root.eulerAngles = Vector3.zero;

                // サブ位置変更
                OffsetSubMemPos(actScene.name);
            }

            if (!keepAnimeNow)
            {
                if (resetPos)
                {
                    // シーンレベルアップ後
                    resetupSubVoices(GameObject.Find("MainSystem"), actScene.name);

                    if (actScene.name == "IC")
                        Free3P.MotionAndVoices.Play(girlCtrlSub, "(IC)待機", levelSub);
                    //if (actScene.name == "FH")
                    //    Free3P.MotionAndVoices.Play(girlCtrlSub, "(FH)待機(寝)", levelSub);
                    if (actScene.name == "FH" && overrideControllerSubmem_PlayM)
                        subMemModel.GetComponent<Animator>().runtimeAnimatorController = overrideControllerSubmem_PlayM;
                }
                else
                {
                    // キャラチェンジ後
                    if (actScene.name == "IC")
                        Free3P.MotionAndVoices.Play(girlCtrlSub, "(IC)余韻", levelSub);
                    if (actScene.name == "FH")
                        Free3P.MotionAndVoices.Play(girlCtrlSub, "(FH)余韻(寝)", levelSub);
                }
            }
        }


        public static void LockAnimeClip(GirlCtrl girlCtrl, string clipname)
        {
            // プレイモーションからの移行
            MotionAndVoices.LastStateName = string.Empty;
            if (MotionAndVoices.fixIC2FH_bkup != null)
            {
                subMemModel.transform.root.localPosition = MotionAndVoices.fixIC2FH_bkup.position;
                subMemModel.transform.root.localRotation = MotionAndVoices.fixIC2FH_bkup.rotation;
                MotionAndVoices.fixIC2FH_bkup = null;
            }

            var anim = girlCtrl.FindModel().GetComponent<Animator>();

            // クリップのオーバーライドコントローラーを作る
            var overrideController = girlCtrl.FindModel().GetComponent<Animator>().runtimeAnimatorController as AnimatorOverrideController;
            AnimatorOverrideController overrideController1 = new AnimatorOverrideController();
            overrideController1.runtimeAnimatorController = overrideController.runtimeAnimatorController;

            List<KeyValuePair<AnimationClip, AnimationClip>> overrides;
            overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
            overrideController.GetOverrides(overrides);

            Dictionary<char, AnimationClip> smlClips = new Dictionary<char, AnimationClip>();

            var orgname = clipname;
            if (clipname.Length >= 3)
                clipname = clipname.Substring(0, clipname.Length - 2);
            smlClips['S'] = overrides.FirstOrDefault(x => x.Value && x.Value.name == clipname + "_S").Value;
            smlClips['M'] = overrides.FirstOrDefault(x => x.Value && x.Value.name == clipname + "_M").Value;
            smlClips['L'] = overrides.FirstOrDefault(x => x.Value && x.Value.name == clipname + "_L").Value;

            if (!smlClips['S'] && smlClips['M']) smlClips['S'] = smlClips['M'];
            if (!smlClips['M'] && smlClips['S']) smlClips['M'] = smlClips['S'];
            if (!smlClips['L'] && smlClips['M']) smlClips['L'] = smlClips['M'];

            if (!smlClips['M'])
            {
                Debug.LogError("固定するクリップがみつからないよ " + clipname);
            }

            if (!smlClips.Any(x => x.Value.name == orgname))
            {
                Debug.LogError("固定するクリップ名が変わってるよ " + orgname);
                orgname = smlClips['M'].name;
            }

            if (actScene.name == "FH")
            {
                // フェイスモーションステートの取得（IOはHシーンでは多分FHのみフェイスモーションをレイヤー２で別管理、体モーションは猫兎共通のため）
                var state = overrides.FirstOrDefault(x => x.Value && x.Value.name == orgname).Key.name;
                if (state.Length >= 3)
                    state = state.Substring(0, state.Length - 2);
                var face = overrides.FirstOrDefault(x => x.Key && x.Key.name == state + "_F").Value;

                if (!face)
                {
                    Debug.LogError("FHで固定するフェイスモーションがみつからないよ " + state + "_F");
                    face = overrides.FirstOrDefault(x => x.Key && x.Key.name == "H01_F").Value; // 余韻フェイス
                }
                if (!face)
                {
                    // 元のオーバーライド参照
                    var overridesSubFH = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                    overrideControllerSubmem.GetOverrides(overridesSubFH);
                    face = overridesSubFH.FirstOrDefault(x => x.Key && (x.Key.name == state + "_F")).Value;

                    if (!face && subMemFaceClip)
                    {
                        Debug.LogError("固定フェイスモーションのロールバック " + state + "_F" + " <= " + subMemFaceClip.name);
                        face = subMemFaceClip;
                    }

                    if (!face)
                        face = overrides.FirstOrDefault(x => x.Key && x.Key.name == "H01_F").Value; // 余韻フェイス

                    if (!face)
                    {
                        Debug.LogError("FHで固定するフェイスモーションがみつからないよ2 " + state + "_F");
                        face = overridesSubFH.FirstOrDefault(x => x.Key && (x.Key.name == state + "_F")).Key; // 余韻フェイス
                    }
                }

                smlClips['F'] = face;
            }


            for (int i = 0; i < overrides.Count; ++i)
            {
                MyDebug.Log($"{overrides[i].Key.name} {(overrides[i].Value != null ? overrides[i].Value.name : "")}");
                if (overrides[i].Value == null)
                    continue;

                if (overrides[i].Key.name.StartsWith("Brea_", StringComparison.Ordinal))
                {
                    // FH用。とりあえず触らない
                    overrideController1[overrides[i].Key.name] = overrides[i].Value;
                    continue;
                }

                //overrideController1[overrides[i].Key.name] = GetOverrideClip(overrides[i], anim.GetCurrentAnimatorClipInfo(0));
                overrideController1[overrides[i].Key.name] = GetOverrideClip(overrides[i], smlClips, orgname);
            }

            // コントローラー更新
            girlCtrl.FindModel().GetComponent<Animator>().runtimeAnimatorController = overrideController1;
            overrideControllerSubmem_PlayM = overrideController1;
        }


        static AnimationClip GetOverrideClip(KeyValuePair<AnimationClip, AnimationClip> overridePair, Dictionary<char, AnimationClip> smlClips, string orgname)
        {
            int index = overridePair.Key.name.Length - 1;
            char c = overridePair.Key.name[index];
            if (overridePair.Key.name[index - 1] == '_'
                && (c == 'S' || c == 'M' || c == 'L' || c == 'F')
            )
            {
                return smlClips[c];
            }
            else
                return smlClips.First(x => x.Value.name == orgname).Value;
        }

        static AnimationClip GetOverrideClip(KeyValuePair<AnimationClip, AnimationClip> overridePair, AnimatorClipInfo[] list)
        {
            int index = overridePair.Key.name.Length - 1;
            char c = overridePair.Key.name[index];
            if (overridePair.Key.name[index - 1] == '_'
                && (c == 'S' || c == 'M' || c == 'L')
            )
            {
                return list.FirstOrDefault(x => x.clip.name.EndsWith(c.ToString(), StringComparison.Ordinal)).clip;
            }
            else
                return list.OrderByDescending(x => x.weight).FirstOrDefault(x => x.weight > 0).clip;
        }

        public class MotionAndVoices
        {
            // 最後に設定したプラグインステート
            internal static string LastStateName = string.Empty;

            // ICモーションをFHで使うための位置角度補正（座り→寝）
            static Vector3 fixIC2FH_rot = new Vector3(-52f, 0, 0);
            static Vector3 fixIC2FH_pos = new Vector3(0, -0.7f, 0);

            static Vector3 fixIC2FH_Kunni_rot = new Vector3(-17f, 0, 0);
            static Vector3 fixIC2FH_Kunni_pos = new Vector3(0, 0.6f, 0);

            static Vector3 fixIC2FH_Anu_rot = new Vector3(-32f, 0, 0);
            static Vector3 fixIC2FH_Anu_pos = new Vector3(0, 0.8f, 0);

            internal static TransformData fixIC2FH_bkup = null;

            // 定義リスト
            public static List<MotionAndVoices>[] data =
                new List<MotionAndVoices>[2] { new List<MotionAndVoices>(), new List<MotionAndVoices>() };
            public static List<MotionAndVoices> nekoData => data[0];
            public static List<MotionAndVoices> usaData => data[1];
            private static bool _loaded = false;
            public static bool IsLoaded => _loaded && !IsLoading;

            public int level = 0;
            public int charId = 0;
            public MotionAndVoiceData[] motionAndVoiceDataList;
            //protected Dictionary<string, AudioClip> _animeName2voice = new Dictionary<string, AudioClip>(); // アニメ名->ボイス
            protected Dictionary<string, Dictionary<string, AnimationClip>> _state2animeSML = new Dictionary<string, Dictionary<string, AnimationClip>>(); // ステート名->アニメクリップ
            protected Dictionary<string, AudioClip> _state2voice = new Dictionary<string, AudioClip>(); // ステート名->ボイス
            protected Dictionary<string, MotionAndVoiceData> _animeName2mvdata = new Dictionary<string, MotionAndVoiceData>(); // アニメ名->ボイスデータ


            static IniDataMV iniDataMV = new IniDataMV();
            // 初期設定XMLシリアライズ用
            static MotionAndVoiceData_S[] motionAndVoiceDataList_S => iniDataMV.motionAndVoiceDataList_S;

            // 名前生成に失敗する例外用
            static Dictionary<string, string> fixVoiceNames { get; set; }

            public MotionAndVoices(int charId, int level)
            {
                this.charId = charId;
                this.level = level;

                this.motionAndVoiceDataList = MotionAndVoiceData_S.LoadData(motionAndVoiceDataList_S);
            }

            // ユーザー拡張用に定義ファイル作成＆読み込み
            public static readonly string _fname = $"{SaveFileName}-Free3pSubMV.xml";
            public static void Save()
            {
                IniDataMV.SaveXml(iniDataMV, _fname);
            }
            public static bool Load()
            {
                if (IniDataMV.LoadXml(out IniDataMV data, _fname))
                {
                    iniDataMV = data;
                    fixVoiceNames = iniDataMV.fixVoiceNames.ToDictionary(x => x.Key, x => x.Value);
                    return true;
                }
                return false;
            }


            [Serializable]
            public class IniDataMV// シリアライズ用
            {
                // 初期設定XMLシリアライズ用
                public MotionAndVoiceData_S[] motionAndVoiceDataList_S =
                {
                    //new MotionAndVoiceData(state:"待機", M:"IC01{ID}S{LV}_{S}", MsLv:1, V:"FH01{ID}H00_00_{LV}", VsLv:4, climax:false),
                    new MotionAndVoiceData_S("(IC)待機", "IC01{ID}S{LV/2}_M", 1, "FH01{ID}S00_{00_LV/M}", 3, "FH01{ID}GS00_00_{LVM}", false), // 胸サイズが大きくなると胸揺れモーションが入るのでM固定
                    new MotionAndVoiceData_S("(IC)余韻", "IC01{ID}H01_{S}", 1, "FH01{ID}H00_00_{LV}", 4, "FH01{ID}GH00_00_{LVM}", false),
                    new MotionAndVoiceData_S("(IC)ローター", "IC01{ID}S{LV/2+2}_{S}", 1, "UC20{ID}E00_01_02", 4, "FH01{ID}GS00_00_{LVM}", false),
                    //new MotionAndVoiceData_S("(IC)M字", "IC01{ID}D01_{S}", 1, "FH01{ID}H00_00_{LV}", 4, false),    // 今のところクンニモーションが止められないので不自然
                    new MotionAndVoiceData_S("(IC)オナ-A", "IC01{ID}G01_{S}", 1, "IC00{ID}FA00_{-1_LV/M}", 3, "FH01{ID}GA00_00_{LVM}", false),
                    new MotionAndVoiceData_S("(IC)オナ-B", "IC01{ID}G02_{S}", 1, "IC00{ID}FB00_{-1_LV/M}", 3, "FH01{ID}GB00_00_{LVM}", false),
                    new MotionAndVoiceData_S("(IC)オナ-C", "IC01{ID}G03_{S}", 1, "IC00{ID}DB00_{-2_LV/M}", 3, "FH01{ID}GC00_00_{LVM}", false),
                    new MotionAndVoiceData_S("(IC)オナ-X0", "IC01{ID}F04_00_{S}", 1, "IC00{ID}F00_{LV}", 1, "FH01{ID}GF0000_00_{LVM}", true),
                    new MotionAndVoiceData_S("(IC)オナ-X1", "IC01{ID}F04_01_{S}", 1, "IC00{ID}F01_{LV}", 1, "FH01{ID}GF0100_00_{LVM}", true),
                    new MotionAndVoiceData_S("(IC)オナ-X2", "IC01{ID}F04_02_{S}", 1, "IC00{ID}F01_{LV}", 1, "FH01{ID}GF0100_00_{LVM}", true),
                    new MotionAndVoiceData_S("(IC)オナ-X3", "IC01{ID}F04_03_{S}", 1, "IC00{ID}F01_{LV}", 1, "FH01{ID}GF0100_00_{LVM}", true),
                    new MotionAndVoiceData_S("(IC)手マン-A", "IC01{ID}C01_{S}", 1, "IC00{ID}AA01_{-2_LV/M}", 3, "FH01{ID}GA00_00_{LVM}", false),
                    new MotionAndVoiceData_S("(IC)手マン-B", "IC01{ID}C02_{S}", 1, "IC00{ID}CB00_{-2_LV/M}", 3, "FH01{ID}GB00_00_{LVM}", false),
                    new MotionAndVoiceData_S("(IC)手マン-C", "IC01{ID}C03_{S}", 1, "IC00{ID}BC00_{-2_LV/M}", 3, "FH01{ID}GC00_00_{LVM}", false),
                    new MotionAndVoiceData_S("(IC)手マン-X0", "IC01{ID}F01_00_{S}", 1, "IC00{ID}F00_{LV}", 1, "FH01{ID}GF0000_00_{LVM}", true),
                    new MotionAndVoiceData_S("(IC)手マン-X1", "IC01{ID}F01_01_{S}", 1, "IC00{ID}F01_{LV}", 1, "FH01{ID}GF0100_00_{LVM}", true),
                    new MotionAndVoiceData_S("(IC)手マン-X2", "IC01{ID}F01_02_{S}", 1, "IC00{ID}F01_{LV}", 1, "FH01{ID}GF0100_00_{LVM}", true),
                    new MotionAndVoiceData_S("(IC)手マン-X3", "IC01{ID}F01_03_{S}", 1, "IC00{ID}F01_{LV}", 1, "FH01{ID}GF0100_00_{LVM}", true),
                    new MotionAndVoiceData_S("(IC)クンニ-A", "IC01{ID}D01_{S}", 1, "IC00{ID}AA00_{-2_LV/M}", 3, "FH01{ID}GA00_00_{LVM}", false),
                    new MotionAndVoiceData_S("(IC)クンニ-B", "IC01{ID}D02_{S}", 1, "IC00{ID}DB01_{-2_LV/M}", 3, "FH01{ID}GB00_00_{LVM}", false),
                    new MotionAndVoiceData_S("(IC)クンニ-C", "IC01{ID}D03_{S}", 1, "IC00{ID}DC00_{-2_LV/M}", 3, "FH01{ID}GC00_00_{LVM}", false),
                    new MotionAndVoiceData_S("(IC)クンニ-X0", "IC01{ID}F02_00_{S}", 1, "IC00{ID}F00_{LV}", 1, "FH01{ID}GF0000_00_{LVM}", true),
                    new MotionAndVoiceData_S("(IC)クンニ-X1", "IC01{ID}F02_01_{S}", 1, "IC00{ID}F01_{LV}", 1, "FH01{ID}GF0100_00_{LVM}", true),
                    new MotionAndVoiceData_S("(IC)クンニ-X2", "IC01{ID}F02_02_{S}", 1, "IC00{ID}F01_{LV}", 1, "FH01{ID}GF0100_00_{LVM}", true),
                    new MotionAndVoiceData_S("(IC)クンニ-X3", "IC01{ID}F02_03_{S}", 1, "IC00{ID}F01_{LV}", 1, "FH01{ID}GF0100_00_{LVM}", true),
                    new MotionAndVoiceData_S("(IC)A愛撫-A", "IC01{ID}E01_{S}", 1, "IC00{ID}AA00_{-2_LV/M}", 3, "FH01{ID}GA00_00_{LVM}", false),
                    new MotionAndVoiceData_S("(IC)A愛撫-B", "IC01{ID}E02_{S}", 1, "IC00{ID}EB00_{-2_LV/M}", 3, "FH01{ID}GB00_00_{LVM}", false),
                    new MotionAndVoiceData_S("(IC)A愛撫-C", "IC01{ID}E03_{S}", 1, "IC00{ID}DB00_{-2_LV/M}", 3, "FH01{ID}GC00_00_{LVM}", false),
                    new MotionAndVoiceData_S("(IC)A愛撫-X0", "IC01{ID}F03_00_{S}", 1, "IC00{ID}F00_{LV}", 1, "FH01{ID}GF0000_00_{LVM}", true),
                    new MotionAndVoiceData_S("(IC)A愛撫-X1", "IC01{ID}F03_01_{S}", 1, "IC00{ID}F01_{LV}", 1, "FH01{ID}GF0100_00_{LVM}", true),
                    new MotionAndVoiceData_S("(IC)A愛撫-X2", "IC01{ID}F03_02_{S}", 1, "IC00{ID}F01_{LV}", 1, "FH01{ID}GF0100_00_{LVM}", true),
                    new MotionAndVoiceData_S("(IC)A愛撫-X3", "IC01{ID}F03_03_{S}", 1, "IC00{ID}F01_{LV}", 1, "FH01{ID}GF0100_00_{LVM}", true),
                    new MotionAndVoiceData_S("(FH)待機(寝)", "FH03{ID}S01_{S}", 1, "FH01{ID}S00_{00_LV/M}", 3, "FH01{ID}GS00_00_{LVM}", false),
                    new MotionAndVoiceData_S("(FH)余韻(寝)", "FH03{ID}H01_{S}", 1, "FH01{ID}H00_00_{LV}", 4, "FH01{ID}GH00_00_{LVM}", false),
                    new MotionAndVoiceData_S("(FH)待機(開脚)", "FH00{ID}S11_{S}", 1, "FH01{ID}S00_{00_LV/M}", 3, "FH01{ID}GS00_00_{LVM}", false),
                    new MotionAndVoiceData_S("(CS)立ち0", "ST{ID}_00", 1, "FH01{ID}S00_{00_LV/M}", 3, "FH01{ID}GS00_00_{LVM}", false),
                    new MotionAndVoiceData_S("(CS)立ち1", "ST{ID}_01", 1, "FH01{ID}S00_{00_LV/M}", 3, "FH01{ID}GS00_00_{LVM}", false),
                    new MotionAndVoiceData_S("(CS)立ち2", "ST{ID}_02", 1, "FH01{ID}S00_{00_LV/M}", 3, "FH01{ID}GS00_00_{LVM}", false),
                    new MotionAndVoiceData_S("(CS)立ち3", "ST{ID}_03", 1, "FH01{ID}S00_{00_LV/M}", 3, "FH01{ID}GS00_00_{LVM}", false),
                    new MotionAndVoiceData_S("(CS)立ち4", "ST{ID}_04", 1, "FH01{ID}S00_{00_LV/M}", 3, "FH01{ID}GS00_00_{LVM}", false),
                    new MotionAndVoiceData_S("(CS)立ち5", "ST{ID}_05", 1, "FH01{ID}S00_{00_LV/M}", 3, "FH01{ID}GS00_00_{LVM}", false),
                    new MotionAndVoiceData_S("(CS)立ち10", "ST{ID}_10", 1, "FH01{ID}S00_{00_LV/M}", 3, "FH01{ID}GS00_00_{LVM}", false),
                    new MotionAndVoiceData_S("(CS)立ち11", "ST{ID}_11", 1, "FH01{ID}S00_{00_LV/M}", 3, "FH01{ID}GS00_00_{LVM}", false),
                    new MotionAndVoiceData_S("(CS)立ち12", "ST{ID}_12", 1, "FH01{ID}S00_{00_LV/M}", 3, "FH01{ID}GS00_00_{LVM}", false),
                    new MotionAndVoiceData_S("(CS)立ち13", "ST{ID}_13", 1, "FH01{ID}S00_{00_LV/M}", 3, "FH01{ID}GS00_00_{LVM}", false),
                };


                // 名前生成に失敗する例外用
                public KeyValuePair<string, string>[] fixVoiceNames = new []
                {
                    new KeyValuePair<string, string> ( "UC2002E00_01_02", "IC0002S00_00_03" ), // ローター、UsaとNekoでファイル名が異なる
                };


                public static void SaveXml(IniDataMV data_S, string fname)
                {
                    MyXML.XML_Save(data_S, fname);
                }

                public static bool LoadXml(out IniDataMV data_S, string fname)
                {
                    if (MyXML.XML_Load(out data_S, fname))
                    {
                        return true;
                    }
                    return false;
                }
            }


            [Serializable]
            public class MotionAndVoiceData_S // シリアライズ用
            {
                public string State;
                public string MotionCode;
                public int StartLevelM;
                public string VoiceCode;
                public int StartLevelV;
                public string GagvoiceCode;
                public bool FlagClimax;

                public MotionAndVoiceData_S()
                {
                }

                public MotionAndVoiceData_S(string state, string motionCode, int startLevelM, string voiceCode, int startLevelV, string gagvoCode, bool flagClimax)
                {
                    State = state;
                    MotionCode = motionCode;
                    StartLevelM = startLevelM;
                    VoiceCode = voiceCode;
                    StartLevelV = startLevelV;
                    GagvoiceCode = gagvoCode;
                    FlagClimax = flagClimax;
                }

                public MotionAndVoiceData Load()
                {
                    return new MotionAndVoiceData(State, MotionCode, StartLevelM, VoiceCode, StartLevelV, GagvoiceCode, FlagClimax);
                }

                public static MotionAndVoiceData[] LoadData(MotionAndVoiceData_S[] data_S)
                {
                    return data_S.Select(x => x.Load()).ToArray();
                }
            }


            public class MotionAndVoiceData
            {
                static readonly string[] BUST_SIZE_LIST = { "S", "M", "L" };

                private MotionAndVoices owner;
                public string state { get; protected set; }
                public bool isClimax { get; protected set; }
                public int startLevelM { get; protected set; }
                public int startLevelV { get; protected set; }
                private string _voiceCode;
                private string _motionCode;
                private string _gagvoCode;

                public bool loaded { get; private set; }

                public AudioClip audioClip { get; private set; }
                public AudioClip audioClipGag { get; private set; }
                public Dictionary<string, AnimationClip> animationClips { get; private set; }
                public string motionName = string.Empty;

                private MotionAndVoiceData()
                {
                }

                public MotionAndVoiceData(string state, string M, int MsLv, string V, int VsLv, string GV, bool climax)
                    : this()
                {
                    this.state = state;
                    this.isClimax = climax;
                    this._motionCode = M;
                    this._voiceCode = V;
                    this.startLevelM = MsLv;
                    this.startLevelV = VsLv;
                    this._gagvoCode = GV;
                }

                static string CodeToName(string code, int charId, int level, string bustsize)
                {
                    charId += 1; // 0 = "01"

                    if (code.Contains("{LV/2}"))
                    {
                        // Lv1-2が1, Lv3-4が2になってるタイプ
                        code = code.Replace("{LV/2}", $"{((level + 1) / 2):00}");
                    }
                    else if (code.Contains("{LV/2+2}"))
                    {
                        // Lv1-2が3, Lv3-4が4になってるタイプ
                        code = code.Replace("{LV/2+2}", $"{((level + 1) / 2 + 2):00}");
                    }

                    if (code.Contains("{LVM}"))
                        level = 4;

                    if (charId == 2)
                        level -= 1;

                    code = code.Replace("{LVM}", $"{level:00}");

                    string repRangeLevel(string tag, string s, int dec)
                    {
                        // 開始Lv_終了Lvとアセット名にあるタイプ  e.g. FA00_02_03, FA00_04
                        if (level == 4 || (charId == 2 && level == 3)) // max
                        {
                            s = s.Replace(tag, $"{level:00}");
                        }
                        else
                        {
                            if (dec > level)
                                dec = level;

                            s = s.Replace(tag, $"{level - dec:00}_{level:00}");
                        }
                        return s;
                    }

                    if (code.Contains("{-1_LV/M}"))
                    {
                        // 開始Lv_終了Lvとアセット名にあるタイプ  e.g. FA00_02_03, FA00_04
                        code = repRangeLevel("{-1_LV/M}", code, 1);
                    }
                    else if (code.Contains("{-2_LV/M}"))
                    {
                        // 開始Lv_終了Lvとアセット名にあるタイプ  e.g. FA00_01_03, FA00_04
                        code = repRangeLevel("{-2_LV/M}", code, 2);
                    }
                    else if (code.Contains("{00_LV/M}"))
                    {
                        // 開始Lv_終了Lvとアセット名にあるタイプ  e.g. FA00_00_03, FA00_04
                        code = repRangeLevel("{00_LV/M}", code, level);
                    }
                    else
                        code = code.Replace("{LV}", $"{level:00}");
                    
                    // 通常処理
                    code = code.Replace("{ID}", $"{charId:00}").Replace("{S}", bustsize);

                    // ボイス名例外処理
                    foreach (var rep in fixVoiceNames)
                        code = code.Replace(rep.Key, rep.Value);

                    return code;
                }

                public bool test()
                {
                    Debug.Log("test");
                    return true;
                }

                public bool LoadAssetsFH(MotionAndVoices parent, AssetBundle voiceAsset)
                {
                    var hid = this._motionCode.Substring(2, 2);
                    var bundleFH = AssetBundle.LoadFromFile(Application.dataPath + $"/Data/motion_fh{hid}.unity3d");
                    bool ret;

                    try
                    {
                        ret = LoadAssets(parent, bundleFH, voiceAsset);
                    }
                    finally
                    {
                        if (bundleFH)
                            bundleFH.Unload(false);
                    }
                    return ret;
                }

                // ADV＆Custom
                public bool LoadAssetsEtc(MotionAndVoices parent, AssetBundle voiceAsset)
                {
                    if (this.state.StartsWith("(CI)"))
                    {
                        var bundleAdv = AssetBundle.LoadFromFile(Application.dataPath + "/Data/motion_ci.unity3d");
                        //bundleAdv.AllAssetNames().ForEach(x => Console.WriteLine(x));
                        bool ret;

                        try
                        {
                            ret = LoadAssets(parent, bundleAdv, voiceAsset);
                        }
                        finally
                        {
                            if (bundleAdv)
                                bundleAdv.Unload(false);
                        }
                        return ret;
                    }

                    if (this.state.StartsWith("(CS)"))
                    {
                        var cname = CodeToName(this._motionCode, parent.charId, 0, string.Empty);

                        var o = LoadAnime_Resource(CodeToName(this._motionCode, parent.charId, 0, string.Empty));
                        if (o == null)
                            return false;

                        this.animationClips = new Dictionary<string, AnimationClip>();
                        foreach (var s in BUST_SIZE_LIST)
                        {
                            this.animationClips[s] = o;
                        }
                        motionName = o.name;

                        var ret = LoadAssets(parent, null, voiceAsset);
                        return ret;
                    }

                    return false;
                }

                static Dictionary<string, AudioClip> __voiceCache = new Dictionary<string, AudioClip>();
                static Dictionary<string, AnimationClip> __animeCache = new Dictionary<string, AnimationClip>();
                
                static AnimationClip LoadAnime_Asset(AssetBundle bundle, string name)
                {
                    if (__animeCache.ContainsKey(name))
                    {
                        return __animeCache[name];
                    }
                    else
                    {
                        return __animeCache[name] = bundle.LoadAsset<AnimationClip>(name);
                    }
                }

                static AnimationClip LoadAnime_Resource(string name)
                {
                    if (__animeCache.ContainsKey(name))
                    {
                        return __animeCache[name];
                    }
                    else
                    {
                        var o = PlgUtil.GetResourceByName<AnimationClip>(name);
                        #region bkup
                        /*if (!o)
                        {
                            Debug.Log("シーンを検索");
                            var adv = SceneManager.GetSceneByName("ADV");
                            if (adv != null)
                            {
                                foreach(GameObject r in adv.GetRootGameObjects())
                                {
                                    var aa = r.GetComponentsInChildren<Animator>();
                                    foreach(var a in aa)
                                    {
                                        var inf = a.GetCurrentAnimatorClipInfo(0).FirstOrDefault(x => x.clip.name == cname);
                                        if (inf.clip)
                                        {
                                            o = inf.clip;
                                            break;
                                        }
                                    }
                                }
                            }
                        }*/
                        #endregion

                        //var o = Resources.Load<AnimationClip>(CodeToName(this._motionCode, parent.charId, 0, string.Empty));
                        Debug.Log(name + " r:" + (bool)o);
                        if (!o || o == null)
                            return null;

                        o = GameObject.Instantiate(o);
                        GameObject.DontDestroyOnLoad(o);
                        o.name = o.name.Replace("(Clone)", "");

                        return __animeCache[name] = o;
                    }
                }

                static AudioClip LoadVoice_Asset(AssetBundle bundle, string voiceName)
                {
                    if (__voiceCache.ContainsKey(voiceName))
                    {
                        return __voiceCache[voiceName];
                    }
                    else
                    {
                        return __voiceCache[voiceName] = bundle.LoadAsset<AudioClip>(voiceName);
                    }
                }

                public bool LoadAssets(MotionAndVoices parent, AssetBundle motionAsset, AssetBundle voiceAsset)
                {
                    this.owner = parent;

                    var vl = Mathf.Max(owner.level, this.startLevelV);
                    var voiceName = CodeToName(_voiceCode, owner.charId, vl, string.Empty);

                    this.audioClip = LoadVoice_Asset(voiceAsset, voiceName);
                    if (!this.audioClip)
                        Debug.LogWarning($"V: NOT FOUND " + voiceName);
                    else
                    {
                        // GyagVoice
                        voiceName = CodeToName(_gagvoCode, owner.charId, 4, string.Empty);
                        this.audioClipGag = LoadVoice_Asset(voiceAsset, voiceName);
                        if (!this.audioClipGag)
                        {
                            Debug.LogWarning($"GV: NOT FOUND " + voiceName);
                            this.audioClipGag = this.audioClip;
                        }
                    }

                    if (motionAsset == null)
                        return this.audioClip;

                    this.animationClips = new Dictionary<string, AnimationClip>();
                    
                    foreach (var s in BUST_SIZE_LIST)
                    {
                        var ml = Mathf.Max(owner.level, this.startLevelM);
                        //this.animationClips[s] = motionAsset.LoadAsset<AnimationClip>(CodeToName(_motionCode, owner.charId, ml, s));
                        this.animationClips[s] = LoadAnime_Asset(motionAsset, CodeToName(_motionCode, owner.charId, ml, s));
                    }

                    if (this.animationClips["M"] || this.animationClips["S"])
                    {
                        if (!this.animationClips["S"] && this.animationClips["M"])
                            this.animationClips["S"] = this.animationClips["M"];

                        if (this.animationClips["S"] && !this.animationClips["M"])
                            this.animationClips["M"] = this.animationClips["S"];

                        if (!this.animationClips["L"] && this.animationClips["M"])
                            this.animationClips["L"] = this.animationClips["M"];

                        motionName = this.animationClips["M"].name;

                        if (motionName.Length >= 3 && motionName[motionName.Length - 2] == '_')
                            motionName = motionName.Substring(0, motionName.Length - 2);
                    }
                    else
                    {
                        Debug.LogWarning($"M: NOT FOUND " + CodeToName(_motionCode, owner.charId, Mathf.Max(owner.level, this.startLevelM), "M"));
                    }

                    return this.audioClip && this.animationClips["M"];
                }
            }

            public static bool IsLoading { get; private set; }
            //static bool __addLoad = true;
            const string __addScene = "Custom";
            //const string __addScene = "ADV";

            public static void Setup()
            {
                if (IsLoading || _loaded)
                    return;

                // Unload前にロードしたシーンのUpdateが走るのが気になるので没
                //_Instance.StartCoroutine(SetupCoroutine());

                ProcSetupCoroutine(true);
            }

            static IEnumerator ienumSetup = null;
            public static void ProcSetupCoroutine(bool start)
            {
                if (start)
                    ienumSetup = SetupCoroutine();

                if (ienumSetup != null)
                {
                    // フリーズ中の誤クリックをクリア
                    Input.ResetInputAxes();

                    if (!ienumSetup.MoveNext())
                        ienumSetup = null;
                }
            }

            public static IEnumerator SetupCoroutine()
            {
                if (_loaded)
                    yield break;//return;

                IsLoading = true;
                IO_ExSlider.LoadingTextAnchor = TextAnchor.UpperRight;
                IO_ExSlider.LoadingText = "(Assets Loading...)  ";

                ////AsyncOperation async = null;
                //if (__addLoad)
                //{
                //    __addLoad = false;

                //    //async = SceneManager.LoadSceneAsync(__addScene, LoadSceneMode.Additive);
                //    //async.allowSceneActivation = false;   // Falseのままでもリソースは読めるけどUnloadができなくて意味なしなのでやめ
                //    //while(async.progress < 0.9f)
                //    //    yield return null;

                //    SceneManager.LoadScene(__addScene, LoadSceneMode.Additive);
                //    //_Instance.gameObject.AddComponent<Unloader>();
                //}
                //var adv = SceneManager.GetSceneByName(__addScene);
                //if (adv.IsValid())
                //{
                //    //Asyncじゃなくてもリソースを参照するだけなら次フレームまで待たなくてよいっぽい
                //    //if (!adv.isLoaded)
                //    //    yield return null;//return;

                //    if (adv.isLoaded)
                //    {
                //        // ここにはこないはず
                //        foreach (GameObject r in adv.GetRootGameObjects())
                //        {
                //            r.SetActive(false);
                //        }
                //    }
                //}

                var bundle = AssetBundle.LoadFromFile(Application.dataPath + "/Data/motion_ic.unity3d");
                AssetBundle bundleV = null; //ゲーム本体と呼び出しが干渉する 
                bool needUnloadV = false;

                var mainSystem = GameObject.Find("MainSystem");
                while (true)
                {
                    switch (actScene.name)
                    {
                        case "FH":
                            {
                                var setup = mainSystem.GetComponent<FH_SetUp>();
                                if (setup)
                                    bundleV = setup.GetNonPublicField<AssetBundle>("bundle");
                            }
                            break;
                        case "IC":
                            {
                                var setup = mainSystem.GetComponent<IC_SetUp>();
                                if (setup)
                                    bundleV = setup.GetNonPublicField<AssetBundle>("bundle");
                            }
                            break;
                        case "UC":
                            {
                                var setup = mainSystem.GetComponent<UC_SetUp>();
                                if (setup)
                                    bundleV = setup.GetNonPublicField<AssetBundle>("bundle");
                            }
                            break; // 特殊

                        /*default:
                            try
                            {
                                bundleV = AssetBundle.LoadFromFile(Application.dataPath + "/Data/voice_h_part.unity3d");
                                needUnloadV = true;
                            }
                            catch (Exception e)
                            {
                                Debug.LogWarning(e);
                            }
                            break;*/
                    }

                    if (bundleV) // アンロードが終わるまで待つ
                        yield return null;
                    else
                        break;
                }

                Scene adv = new Scene();
                try
                {
                    bundleV = AssetBundle.LoadFromFile(Application.dataPath + "/Data/voice_h_part.unity3d");
                    needUnloadV = true;

                    if (actScene.name != __addScene)
                    {
                        SceneManager.LoadScene(__addScene, LoadSceneMode.Additive);
                        adv = SceneManager.GetSceneByName(__addScene);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                }

                //Asyncじゃなくてもリソースを参照するだけなら次フレームまで待たなくてよいっぽい
                // → 環境依存で不安定っぽい
                //if (adv.IsValid() && !adv.isLoaded)
                //    yield return new WaitUntil( () => adv.isLoaded );
                ////yield return null;//return;
                while (adv.IsValid() && !adv.isLoaded)
                {
                    yield return null;
                }

                try
                {
                    if (bundle && bundleV)
                    {
                        data = new List<MotionAndVoices>[2] { new List<MotionAndVoices>(), new List<MotionAndVoices>() };

                        for (int c = 0; c <= 1; c++)
                        {
                            for (int lv = 0; lv <= 4; lv++)
                            {
                                var o = new MotionAndVoices(c, lv);

                                o.LoadAssets(bundle, bundleV);
                                data[c].Add(o);
                            }
                        }
                        _loaded = true;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                }
                finally
                {
                    bundle.Unload(false);
                    if (needUnloadV)
                        bundleV.Unload(false);

                    if (adv.IsValid() && adv.isLoaded)
                    {
                        Debug.Log($"{__addScene}のUnload");
                        //async.allowSceneActivation = true;

                        foreach (GameObject r in adv.GetRootGameObjects())
                        {
                            r.SetActive(false);
                            //デストロイしてもLateUpdateが走ることがある… GameObject.Destroy(r);
                        }
                        SceneManager.UnloadSceneAsync(adv);
                        Resources.UnloadUnusedAssets();
                    }
                }

                //var adv = SceneManager.GetSceneByName("ADV");
                //if (adv != null && adv.IsValid())

                if (adv.IsValid())
                {
                    //// さすがに破棄は無理poi
                    ////var csgo = Resources.FindObjectsOfTypeAll<GameObject>()
                    //var csgo = Resources.FindObjectsOfTypeAll<BgmPlayer>()
                    //        .Where(c => c /*&& !c.scene.IsValid()*/);
                    //foreach (var o in csgo)
                    //{
                    //    //Debug.Log(o + "  " + (o.scene.IsValid() ? o.scene.name : o.scene.IsValid().ToString()) + " " + o.scene.isLoaded);
                    //    //if (o && !o.gameObject || !o.gameObject.scene.IsValid())
                    //    {
                    //        Debug.Log(o + "  " + o.gameObject.name);
                    //        GameObject.Destroy(o);

                    //    }
                    //}

                    if (adv.IsValid() && !adv.isLoaded)
                        yield return null;//return;

                    if (adv.IsValid() && adv.isLoaded)
                    {
                        Debug.Log($"{__addScene}のUnload2");

                        foreach (GameObject r in adv.GetRootGameObjects())
                        {
                            r.SetActive(false);
                        }
                        SceneManager.UnloadSceneAsync(adv);
                        Resources.UnloadUnusedAssets();
                    }
                }


                IsLoading = false;
                IO_ExSlider.LoadingText = null;

                // フリーズ中の誤クリックをクリア
                Input.ResetInputAxes();

                yield break;//return;
            }
            
            public class Unloader : MonoBehaviour
            {
                private void Start()
                {
                    //Update();
                }

                private void Update()
                {
                    var adv = SceneManager.GetSceneByName("ADV");
                    if (adv.IsValid() && adv.isLoaded)
                    {
                        Debug.Log("UpdateでUnload");

                        foreach (GameObject r in adv.GetRootGameObjects())
                        {
                            r.SetActive(false);
                        }
                        SceneManager.UnloadSceneAsync(adv);
                        Destroy(this);
                    }
                }
            }


            public void LoadAssets(AssetBundle motionAsset, AssetBundle voiceAsset)
            {
                foreach (MotionAndVoiceData v in this.motionAndVoiceDataList)
                {
                    Console.WriteLine($"LoadAssets {this.charId} {this.level} " + v.state);

                    if ((v.state.StartsWith("(IC)", StringComparison.OrdinalIgnoreCase) && v.LoadAssets(this, motionAsset, voiceAsset))
                        || (v.state.StartsWith("(FH)", StringComparison.OrdinalIgnoreCase) && v.LoadAssetsFH(this, voiceAsset))
                        || v.LoadAssetsEtc(this, voiceAsset)
                    )
                    {
                        _state2animeSML.Add(v.state, v.animationClips);
                        _state2voice.Add(v.state, v.audioClip);

                        //_animeName2voice.Add(v.motionName, v.audioClip);
                        _animeName2mvdata.Add(v.motionName, v);
                    }
                }
            }

            AnimationClip GetOverrideClip(KeyValuePair<AnimationClip, AnimationClip> overridePair, MotionAndVoiceData dataMV)
            {
                return GetOverrideClip(overridePair.Key.name, dataMV);
            }
            AnimationClip GetOverrideClip(string baseClipName, MotionAndVoiceData dataMV)
            {
                int index = baseClipName.Length - 1;
                char c = baseClipName[index];
                if (index >= 1 && baseClipName[index - 1] == '_'
                    && (c == 'S' || c == 'M' || c == 'L')
                )
                {
                    return dataMV.animationClips[c.ToString()];
                }
                else 
                {
                    float bs = 0;
                    if (this.charId == 1)
                        bs = SaveLoad_CharaData02.Data.savegamedata.BreastSize;
                    else
                        bs = SaveLoad_CharaData01.Data.savegamedata.BreastSize;

                    if (bs < 0.05f)
                        return dataMV.animationClips["S"];
                    else if (bs < 0.6f)
                        return dataMV.animationClips["M"];
                    else
                        return dataMV.animationClips["L"];
                }
            }

            public void Play(GirlCtrl girlCtrl, string state)
            {
                Debug.Log("アニメコントローラ設定 " + state);
                LastStateName = state;

                if (!_state2animeSML.ContainsKey(state))
                    return;

                var datum = this.motionAndVoiceDataList.FirstOrDefault(x => x.state == state);
                if (datum == null)
                    return;

                List<MotionAndVoiceData> mf = null;
                var ss = datum.state.Split('-');
                if (ss.Length > 1)
                {
                    mf = new List<MotionAndVoiceData>();
                    for (int i = 0; i < 4; i++)
                    {
                        mf.Add(this.motionAndVoiceDataList.FirstOrDefault(x => x.state == $"{ss[0]}-X{i}"));
                    }
                    if (!mf.All(x => x != null))
                        mf = null;
                }

                // クリップのオーバーライドコントローラーを作る
                var overrideController = girlCtrl.FindModel().GetComponent<Animator>().runtimeAnimatorController as AnimatorOverrideController;
                AnimatorOverrideController overrideController1 = new AnimatorOverrideController();
                overrideController1.runtimeAnimatorController = overrideController.runtimeAnimatorController;

                List<KeyValuePair<AnimationClip, AnimationClip>> overrides;
                overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
                overrideController.GetOverrides(overrides);
                for (int i = 0; i < overrides.Count; ++i)
                {
                    MyDebug.Log($"{overrides[i].Key.name} {(overrides[i].Value != null ? overrides[i].Value.name : "")}");
                    //overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, clip);

                    if (overrides[i].Value == null)
                        continue;

                    if (overrides[i].Key.name.StartsWith("Brea_", StringComparison.Ordinal))
                    {
                        // FH用。とりあえず触らない
                        overrideController1[overrides[i].Key.name] = overrides[i].Value;
                        continue;
                    }

                    if (overrides[i].Key.name.EndsWith("_F", StringComparison.Ordinal))// 表情
                    {
                        // FH用表情レイヤー
                        if (mf == null)
                        {
                            if (subMemFaceClip)
                                overrideController1[overrides[i].Key.name] = subMemFaceClip;
                            else
                               overrideController1[overrides[i].Key.name] = overrides[i].Value;

                        }
                        else if (actScene.name == "FH")
                        {
                            // 表情変化がある場合、元のオーバーライド参照
                            var overridesSubFH = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                            overrideControllerSubmem.GetOverrides(overridesSubFH);

                            overrideController1[overrides[i].Key.name] = overridesSubFH.FirstOrDefault(x => x.Key.name == overrides[i].Key.name).Value;
                        }
                        continue;
                    }

                    var key_name = overrides[i].Key.name;
                    var state_name = key_name;
                    var p = datum;

                    if (actScene.name == "UC")
                    {
                        // e.g. MUC4202H01_S
                        if (state_name.StartsWith("MUC") && state_name.Length > 8)
                            state_name = state_name.Substring(7);
                    }

                    if (mf != null && state_name[0] == 'F')
                    {
                        //var key = state_name.Substring(3, 4);
                        //p = mf.FirstOrDefault(x => x.motionName.Contains(key));

                        // _00_*部分抜出
                        var key = state_name.Substring(3, 3); // ICのみ?
                        //var key = state_name.Substring(state_name.Length - "_00_*".Length, "_00".Length);　だめぽ

                        if (key[2] < '4')
                        {
                            // 絶頂モーション
                            p = mf.FirstOrDefault(x => x.motionName.EndsWith(key, StringComparison.Ordinal));
                        }
                        else
                        {
                            // 外だしなど。_01から分岐する
                            p = mf.FirstOrDefault(x => x.motionName.EndsWith("_02", StringComparison.Ordinal));
                        }

                        Debug.Log($"  state:{state_name} key:{key} {(p != null ? p.motionName : "")}");
                    }
                    else
                    if (mf != null && state_name.StartsWith("H01_", StringComparison.Ordinal))
                    {
                        var key = "H01";
                        if (HsysMgr.Config.ChangeYoinAnime)
                        {
                            p = mf.FirstOrDefault(x => x.motionName.EndsWith("_03", StringComparison.Ordinal));
                        }
                        else
                        {
                            p = this.motionAndVoiceDataList.FirstOrDefault(x => x.motionName.EndsWith(key, StringComparison.Ordinal));
                        }
                        Debug.Log($"  state:{state_name} key:{key} {(p != null ? p.motionName : "")}");
                    }
                    //else
                    //if (state_name == "Stay" || state_name.StartsWith("S0"))
                    //{
                    //    //overrideController1["Stay"] = overrides[i].Value;

                    //    var key = $"S0{this.level}";
                    //    p = this.voiceAndMotionDataList.FirstOrDefault(x => x.motionName.Contains(key));
                    //    Debug.Log($"{key} {(p != null ? p.motionName : "")}");
                    //}

                    if (p != null)
                    {
                        if (state_name.EndsWith("_M"))
                        {
                            // 胸サイズの適用チェック
                            float bs = 0;
                            if (this.charId == 1)
                                bs = SaveLoad_CharaData02.Data.savegamedata.BreastSize;
                            else
                                bs = SaveLoad_CharaData01.Data.savegamedata.BreastSize;

                            var s = key_name.TrimEnd('M');
                            if (bs < 0.05f && !overrides.Any(x => x.Key.name == s + "_S"))
                            {
                                // Sがなかったら
                                overrideController1[key_name] = p.animationClips["S"];
                                continue;
                            }
                            if (bs >= 0.6f && !overrides.Any(x => x.Key.name == s + "_L"))
                            {
                                // Lがなかったら
                                overrideController1[key_name] = p.animationClips["L"];
                                continue;
                            }
                        }
                    }

                    if (p != null)
                        overrideController1[key_name] = GetOverrideClip(overrides[i], p);
                    else
                        overrideController1[key_name] = GetOverrideClip(overrides[i], datum);
                }

                // コントローラー更新
                girlCtrl.FindModel().GetComponent<Animator>().runtimeAnimatorController = overrideController1;
                overrideControllerSubmem_PlayM = overrideController1;

                Debug.Log("ボイス設定 " + state);
                // ボイス設定
                SetVoicePlayer(girlCtrl);
            }

            public void SetVoicePlayer(GirlCtrl girlCtrl)
            {
                // 元のボイス停止
                foreach (var a in girlCtrl.FindModel().GetComponentsInChildren<AudioSource>())
                {
                    if (a && a.isPlaying && (a.gameObject.name == "HS_Head" || a.gameObject.name == "SoundUnit_"))
                    {
                        a.Stop();
                    }
                }

                // ボイス再生付与
                var comp = girlCtrl.FindModel().GetComponent<Free3PVoicePlayer>();
                if (!comp)
                {
                    comp = girlCtrl.FindModel().AddComponent<Free3PVoicePlayer>();
                    comp.Init(girlCtrl);
                    comp.audioVo = SysVoiceMgr.setupGirlAudio(this.charId);
                }

                // ボイステーブル更新
                //comp.anime2voice = this._animeName2voice;
                //comp.anime2mvdata = this._animeName2mvdata;
                //comp.mvDatas = this;
                comp.SetVoiceTable(this._animeName2mvdata, this);
            }


            public static void SetVoicePlayer(GirlCtrl girlCtrl, AudioClip clip)
            {
                // 元のボイス停止
                foreach (var a in girlCtrl.FindModel().GetComponentsInChildren<AudioSource>())
                {
                    if (a && a.isPlaying && (a.gameObject.name == "HS_Head" || a.gameObject.name == "SoundUnit_"))
                    {
                        a.Stop();
                    }
                }

                // ボイス再生付与
                var comp = girlCtrl.FindModel().GetComponent<Free3PVoicePlayer>();
                if (!comp)
                {
                    comp = girlCtrl.FindModel().AddComponent<Free3PVoicePlayer>();
                    comp.Init(girlCtrl);
                    comp.audioVo = SysVoiceMgr.setupGirlAudio(girlCtrl.ini.id == "Neko" ? 0 : 1);
                }
                
                //comp.anime2mvdata = null;
                //comp.mvDatas = null;
                comp.SetVoiceTable(null, null);

                comp.SetVoice(clip);
            }


            public static void Play(GirlCtrl girlCtrl, string state, int level)
            {
                if (!_loaded)
                    return;

                int id = 0;
                switch (girlCtrl.ini.id)
                {
                    case "Neko":
                        id = 0;
                        break;
                    case "Usa":
                        id = 1;
                        break;
                    default:
                        return;
                }

                Debug.Log($"F3P: Play {id} {state}");

                bool nekoAniDeUsaVoice = false;

                if (actScene.name == "FH")
                {
                    if (id == 1
                        && (state.StartsWith("(IC)") || state.StartsWith("(CS)") || state.StartsWith("(CI)")))
                    {
                        id = 0; // FHでうさのボーンはネコと共通
                        nekoAniDeUsaVoice = true;
                    }

                    if (fixIC2FH_bkup != null)
                    {
                        var root = subMemModel.transform.root;
                        root.localRotation = fixIC2FH_bkup.rotation;//-= fixIC2FH_rot;
                        root.localPosition = fixIC2FH_bkup.position;// -= fixIC2FH_pos;
                        fixIC2FH_bkup = null;
                    }

                    if (HsysMgr.Config.Free3p_EnableAngleFixIC2FH)
                       // && !Free3pSubUtil.IsXtSlave(girlCtrl))
                    {
                        var root = subMemModel.transform.root;
                        var angle = root.localEulerAngles;

                        if (state.StartsWith("(IC)"))
                        {
                            // fixIC2FH_rot.xプラスマイナス50%くらいの範囲まではユーザー補正を許容
                            //if (angle.x > fixIC2FH_rot.x * 0.5f)
                            if  (fixIC2FH_bkup == null)
                            {
                                fixIC2FH_bkup = new TransformData(root, true);

                                if (state.Contains("A愛撫"))
                                {
                                    root.localEulerAngles += fixIC2FH_Anu_rot;
                                    root.localPosition += fixIC2FH_Anu_pos;
                                }
                                else if(state.Contains("クンニ"))
                                {
                                    root.localEulerAngles += fixIC2FH_Kunni_rot;
                                    root.localPosition += fixIC2FH_Kunni_pos;
                                }
                                else
                                {
                                    root.localEulerAngles += fixIC2FH_rot;
                                    root.localPosition += fixIC2FH_pos;
                                }
                            }
                        }
                        //else if (fixIC2FH_bkup != null)
                        //{
                        //    //if (angle.x < fixIC2FH_rot.x * 0.5f)
                        //    //{
                        //    //    root.localEulerAngles -= fixIC2FH_rot;
                        //    //    root.localPosition -= fixIC2FH_pos;
                        //    //}
                        //    root.localRotation = fixIC2FH_bkup.rotation;//-= fixIC2FH_rot;
                        //    root.localPosition = fixIC2FH_bkup.position;// -= fixIC2FH_pos;
                        //    fixIC2FH_bkup = null;
                        //}
                    }
                }

                if (data[id].Count > 0)
                {
                    data[id][level].Play(girlCtrl, state);

                    if (nekoAniDeUsaVoice)
                    {
                        var comp = girlCtrl.FindModel().GetComponent<Free3PVoicePlayer>();

                        /*
                        var vt_usa = data[0][level]._state2voice.ToDictionary(
                            x => data[0][level].motionAndVoiceDataList.FirstOrDefault(y => y.state == x.Key).motionName,
                            x => data[1][level]._state2voice.FirstOrDefault(y => y.Key == x.Key).Value);
                        
                        comp.anime2voice = vt_usa;*/

                        // ギャグボール対応
                        var vt_usa = data[0][level]._state2voice.ToDictionary(
                            x => data[0][level].motionAndVoiceDataList.FirstOrDefault(y => y.state == x.Key).motionName,
                            x => data[1][level].motionAndVoiceDataList.FirstOrDefault(y => y.state == x.Key));

                        //comp.anime2mvdata = vt_usa;
                        //comp.mvDatas = data[0][level]; // モーション参照だけなのでneko
                        comp.SetVoiceTable(vt_usa, data[0][level]);
                    }
                }
            }
        }


        public static bool SubGirlFilter_OnAnimatorHook(Animator animator)
        {
            if (!Free3P.IsLoaded || animator.gameObject != subMemModel)
                return false;

            return true;
        }
           
        static int __uc_prevStateHash = 0;
        public static bool SubGirlFilter_UC_Mecanim_OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo)
        {
            if (!Free3P.IsLoaded || animator.gameObject != subMemModel)
                return false;

            if (__uc_prevStateHash != stateInfo.fullPathHash || stateInfo.normalizedTime > 1f)
            {
                __uc_prevStateHash = stateInfo.fullPathHash;

                // クリップ名はサブのを使う
                var clip = animator.GetCurrentAnimatorClipInfo(0).OrderByDescending(x => x.weight).FirstOrDefault(x => x.weight > 0).clip;

                if (clip && clip.name != null && clip.name.Length > 7)
                {
                    var state = clip.name.Substring(clip.name.Length - "F01_00_M".Length, "F01_00_".Length);
                    //Debug.Log(state);
                    if (('F' == state[0]) && char.IsNumber(state[1]) && char.IsNumber(state[2]))
                        state = "F01" + state.Substring(3);
                    else
                        return true; // 絶頂モーション以外はカット


                    var mainAni = mainMemModel.GetComponent<Animator>();
                    // 絶頂をシンクロさせるためにメインのアニメ参照
                    //var clip = mainAni.GetCurrentAnimatorClipInfo(0).OrderByDescending(x => x.weight).FirstOrDefault(x => x.weight > 0).clip;
                    if (mainAni)
                    {
                        // refじゃないので差し替えはない
                        stateInfo = mainAni.GetCurrentAnimatorStateInfo(0);
                    }

                    // プラグインにはステートの処理を持ちたくなかったけどUCだけ…
                    switch (state)
                    {
                        case "F01_00_":
                        case "F11_00_":
                            if (stateInfo.normalizedTime > 10f)
                            {
                                animator.SetTrigger("Spurt_A");
                            }
                            else
                            {
                                animator.ResetTrigger("Spurt_A");
                            }
                            break;

                        case "F01_02_":
                        case "F11_02_":
                            if (stateInfo.normalizedTime > 3f)
                            {
                                animator.SetTrigger("Biku_A");
                            }
                            else
                            {
                                animator.ResetTrigger("Biku_A");
                            }

                            //if (stateInfo.normalizedTime > 2f)
                            //{
                            //    animator.SetTrigger("Biku_A");
                            //}
                            //else
                            //{
                            //    animator.ResetTrigger("Biku_A");
                            //}
                            break;
                    }

                }
            }

            return true; // UCは個別プレイなのでこれで
        }

        public static bool SubGirlFilter_FH_Mecanim_OnStateUpdate(Animator animator, ref AnimatorStateInfo stateInfo)
        {
            if (!Free3P.IsLoaded || animator.gameObject != subMemModel)
                return false;

            //return true;　// 同じモーションが走ることはないはず…？なのでこれでも良いかテスト → おなモーションの絶頂処理に不具合

            if (!MotionAndVoices.LastStateName.Contains("-")) // 絶頂への派生がなければ
                return true;

            if (stateInfo.normalizedTime >= 1f)
            {
                bool filter = false;

                // アニメ遷移系をシャットアウト
                if (stateInfo.IsName("S02") || stateInfo.IsName("S03"))
                {
                    filter = true;
                }
                else if (stateInfo.IsName("F41_03") || stateInfo.IsName("F51_03") || stateInfo.IsName("F61_03") || stateInfo.IsName("F71_03"))
                {
                    filter = true;
                }
                else if (stateInfo.IsName("F01_04") || stateInfo.IsName("F11_04"))
                {
                    filter = true;
                }
                else if (stateInfo.IsName("F01_05") || stateInfo.IsName("F01_06") || stateInfo.IsName("F01_07") || stateInfo.IsName("F11_05")
                    || stateInfo.IsName("F11_06") || stateInfo.IsName("F11_07"))
                {
                    filter = true;
                }
                else if (stateInfo.IsName("F01_01_05") || stateInfo.IsName("F01_01_06") || stateInfo.IsName("F01_01_07")
                    || stateInfo.IsName("F11_01_05") || stateInfo.IsName("F11_01_06") || stateInfo.IsName("F11_01_07"))
                {
                    filter = true;
                }

                if (filter)
                {
                    return true; // フィルタON
                }
            }

            if (mainMemModel)
            {
                var mainAni = mainMemModel.GetComponent<Animator>();
                if (mainAni)
                {
                    // メインキャラのアニメを妨害しないようにステートを挿げ替える
                    stateInfo = mainAni.GetCurrentAnimatorStateInfo(0);
                }
            }
            return false; // 元の処理を実行
        }

        // ICはプレイヤーのステートで遷移するので不要だった
        //public static bool SubGirlFilter_IC_Mecanim_OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo)
        //{
        //    if (animator.gameObject != subMemModel)
        //        return false;
        //    if (stateInfo.normalizedTime < 1f)
        //        return false;
        //    return false;
        //}
    }

    class Free3pLoadGui : MonoBehaviour
    {
        Font font;
        bool loaded = false;
        bool loading = false;
        Texture2D tex;

        public static Free3pLoadGui Instance { get; private set; }

        public static Free3pLoadGui Start()
        {
            if (Instance)
                return Instance;

            return Instance = new GameObject("Free3pLoad").AddComponent<Free3pLoadGui>();
        }

        private void Awake()
        {
            //font = PlgUtil.GetResourceByName<Font>("VeraMono-Bold-Italic");
            font = PlgUtil.GetResourceByName<Font>("GenShinGothic-P-Bold");
            
            tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.Lerp(Color.clear, Color.black, 0.5f));
            tex.Apply();
        }

        private void Update()
        {
            if (loaded)
            {
                Instance = null;
                GameObject.Destroy(tex);
                GameObject.Destroy(this.gameObject);
                GameObject.Destroy(this); 
            }
        }

        private void OnGUI()
        {
            GUIStyle gUIStyle = new GUIStyle("label");
            if (this.font)
                gUIStyle.font = this.font;

            if (actScene.name != "UC")
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), tex);

            var cc = GUI.contentColor;
            if (!loaded)
            {
                gUIStyle.fontSize = (int)(Screen.width * 28 / 1600f);
                var notice = $"(Free-3P Loading...)  ";
                loading = true;
                gUIStyle.alignment = TextAnchor.LowerRight;

                using (new UiUtil.GuiColor(Color.yellow))
                    UiUtil.DrawOutlineLabel(new Rect(0, 0, Screen.width, Screen.height), notice, 2, gUIStyle);
            }
            GUI.contentColor = cc;
        }

        private void LateUpdate()
        {
            if (!loaded && loading)
            {
                try
                {
                    Free3P.StartProc(true);
                }
                finally
                {
                    loaded = true;
                }
            }
        }
    }

    public static class Free3pSubUtil
    {
        internal static bool CheckClimax(GirlCtrl girlCtrl)
        {
            if (Free3PVoicePlayer.Inst == null || !Free3PVoicePlayer.Inst
                || Free3PVoicePlayer.Inst.girlCtrl != girlCtrl)
                return false;

            //Debug.Log("Sub絶頂 " + Free3PVoicePlayer.Inst.isClimax);

            return Free3PVoicePlayer.Inst.isClimax;
        }

        internal static bool IsSub(GirlCtrl girlCtrl)
        {
            return Free3P.IsLoaded && girlCtrl.FindModel() == Free3P.subMemModel && Free3P.mainMemModel && Free3P.subMemModel;
        }

        //internal static bool IsXtSlave(GirlCtrl girlCtrl)
        //{
        //    //return Free3pXtMS.BoneLink && Free3pXtMS.xSlave != null && girlCtrl == Free3pXtMS.xSlave.girlCtrl && Free3pXtMS.xMaster.root && Free3pXtMS.xSlave.root;
        //    return Free3pXtMS.IsSlave(girlCtrl);
        //}

        //internal static string GetXtMasterAnimeName()
        //{
        //    var animator = Free3pXtMS.xMaster.rootModel.GetComponent<Animator>();
        //    var clip = animator.GetCurrentAnimatorClipInfo(0).OrderByDescending(x => x.weight).FirstOrDefault(x => x.weight > 0).clip;

        //    return clip ? clip.name : "";
        //}
    }

    [DisallowMultipleComponent]
    public class Free3PVoicePlayer : MonoBehaviour
    {
        public static Free3PVoicePlayer Inst;
        public AudioSource audioVo;
        public GirlCtrl girlCtrl { get; private set; }
        public bool isClimax { get; private set; } = false;

        public Free3P.MotionAndVoices mvDatas;
        //public Dictionary<string, AudioClip> anime2voice;
        public Dictionary<string, Free3P.MotionAndVoices.MotionAndVoiceData> anime2mvdata;
        string prevanime = "";

        AudioClip _setClip = null;
        NyouController nyouController;

        Transform obj_vibe;
        Transform obj_anap;
        Transform bone_pus;
        Transform bone_ana;

        Transform tr_gagbo;
        public Transform mTrGagball => this.tr_gagbo; 
        bool flag_gagbo_on = false;

        public TransformData offsetVibe = new TransformData(new Vector3(0, 0.76f, 0), Quaternion.Euler(new Vector3(-90f, 0, 0)), Vector3.zero);
        public TransformData offsetAna = new TransformData(new Vector3(0, 0, -0.7f), Quaternion.identity, Vector3.zero);

        bool mChangeYoinAnime = false;

        public void SetVoice(AudioClip clip)
        {
            Debug.Log("F3p subVoiceSet; " + (clip ? clip.name : "null"));
            _setClip = clip;
        }

        // v0.92で追加
        public void SetVoiceTable(Dictionary<string, Free3P.MotionAndVoices.MotionAndVoiceData> anime2mvdata, Free3P.MotionAndVoices mvDatas)
        {
            this.anime2mvdata = anime2mvdata;
            this.mvDatas = mvDatas;

            // 途中で変更されると不自然になるのでセット時の状態を保持
            mChangeYoinAnime = HsysMgr.Config.ChangeYoinAnime;
        }

        public void Close()
        {
            Inst = null;
            audioVo.Stop();
            GameObject.DestroyImmediate(audioVo);
            GameObject.DestroyImmediate(this);
        }

        private void Awake()
        {
            Inst = this;
            nyouController = base.gameObject.FindDeep("Particle System[Nyo](Clone)").GetComponent<NyouController>();
        }

        public void Init(GirlCtrl girlCtrl)
        {
            this.girlCtrl = girlCtrl;

            if (actScene.name != "IC")
            {
                // ICはFaceCtrl側でまとめて行う
                obj_vibe = base.gameObject.transform.Find("IT10_baibu");
                obj_anap = base.gameObject.transform.Find("IT13_anapa");
                bone_pus = girlCtrl.FindBone().transform.Find("bip01/bip01 Pelvis/HS01_pussy00") ?? girlCtrl.FindBone().transform.Find("bip01_02/bip01 Pelvis_02/HS01_pussy00_02");
                bone_ana = girlCtrl.FindBone().transform.Find("bip01/bip01 Pelvis/HS01_anaru") ?? girlCtrl.FindBone().transform.Find("bip01_02/bip01 Pelvis_02/HS01_anaru_02");
            }

            tr_gagbo = base.gameObject.transform.Find("IT02_Gyagbole");
            if (tr_gagbo)
                flag_gagbo_on = tr_gagbo.gameObject.activeSelf;
        }

        private void Update()
        {
            if (audioVo && audioVo.isPlaying)
            {
                // 要同期項目
                audioVo.spatialBlend = ConfigClass.Blend3D;

                if (girlCtrl.ini.id == "Neko")
                    audioVo.volume = Mathf.Max(0.001f, ConfigClass.Neco_out);
                else if (girlCtrl.ini.id == "Usa")
                    audioVo.volume = Mathf.Max(0.001f, ConfigClass.Tomoe_out);
            }
        }

        private void LateUpdate()
        {
            if (FadeManager_GUI.FadeStart)
            {
                // 次シーン移行開始
                Close();
                return;
            }

            if (!audioVo)
                return;

            // バイブ位置補正
            if (obj_vibe && obj_vibe.gameObject.activeSelf)
            {
                var skmr = obj_vibe.GetComponent<SkinnedMeshRenderer>();

                var root = skmr.rootBone;

                if (!root.name.StartsWith("HS", StringComparison.Ordinal))
                {
                    root = root.parent;
                }
                if (Vector3.Distance(root.position, bone_pus.position) > 0.10f
                    && Vector3.Distance(root.position, bone_ana.position) > 0.10f)
                {
                    root.rotation = bone_ana.rotation * offsetVibe.rotation;

                    var offset = (skmr.rootBone.position - root.position);
                    root.position = bone_ana.position - offset + root.rotation * (offsetVibe.position + new Vector3(UnityEngine.Random.Range(-0.015f, 0.015f), UnityEngine.Random.Range(0, 0.1f), UnityEngine.Random.Range(-0.015f, 0.015f)));
                }
                else
                {
                    //Debug.Log("v d:" + Vector3.Distance(root.position, bone_pus.position));
                    //Debug.Log("v da" + Vector3.Distance(root.position, bone_ana.position));
                    //Debug.Log("v da2" + Vector3.Distance(root.position, skmr.rootBone.position));
                }
            }
            if (obj_anap && obj_anap.gameObject.activeSelf)
            { 
                var skmr = obj_anap.GetComponent<SkinnedMeshRenderer>();
                var root = skmr.rootBone;
                if (!root.name.StartsWith("HS", StringComparison.Ordinal))
                {
                    root = root.parent;
                }

                //if (Vector3.Distance(root.position, bone_ana.position) > 0.3f)
                {
                    root.rotation = bone_ana.rotation * offsetAna.rotation;
                    root.position = bone_ana.position + root.rotation * (offsetAna.position + new Vector3(UnityEngine.Random.Range(-0.01f, 0.01f), UnityEngine.Random.Range(-0.01f, 0.01f), UnityEngine.Random.Range(-0.02f, 0.05f)));
                }
                //else
                //{
                //    Debug.Log("a d:" + Vector3.Distance(root.position, bone_ana.position));
                //}
            }

            // v0.92 ギャグボールの変更を常に監視するよう変更
            if (tr_gagbo && tr_gagbo.gameObject.activeSelf != flag_gagbo_on)
            {
                flag_gagbo_on = tr_gagbo.gameObject.activeSelf;
                if (audioVo.isPlaying)
                    audioVo.Stop(); // ギャグボイスに切り替えるため一度止める
            }

            string ctrl = "", clip = "";
            if (anime2mvdata != null && mvDatas != null
                 && girlCtrl.charaCtrl.getAnimeState(ref ctrl, ref clip))
            {
                if (prevanime != clip || !audioVo.isPlaying)
                {
                    if (prevanime != clip)
                        Debug.Log("m : " + clip);
                    prevanime = clip;

                    if (clip.Length >= 3 && clip[clip.Length - 2] == '_')
                        clip = clip.Substring(0, clip.Length - 2); // バストサイズ削除     

                    //if (clip.EndsWith("D01", StringComparison.Ordinal))
                    //{
                    //    // くんにモーション
                    //    Debug.Log($"レイヤー6 : " + girlCtrl.FindModel().GetComponent<Animator>().GetLayerWeight(6));
                    //    if (girlCtrl.FindModel().GetComponent<Animator>().GetLayerWeight(6) == 0)
                    //        girlCtrl.FindModel().GetComponent<Animator>().SetLayerWeight(6, 100f);
                    //    else
                    //        girlCtrl.FindModel().GetComponent<Animator>().SetLayerWeight(6, 0f);
                    //}

                    Free3P.MotionAndVoices.MotionAndVoiceData mvdata = null;
                    AudioClip audioClip = null;
                    if (anime2mvdata.TryGetValue(clip, out mvdata))
                    {
                        //var gagbo = girlCtrl.FindModel().transform.FindSp("IT02_Gyagbole");
                        //if (tr_gagbo && tr_gagbo.gameObject.activeSelf)
                        if (flag_gagbo_on)
                            audioClip = mvdata.audioClipGag;
                        else
                            audioClip = mvdata.audioClip;


                        if (this.mChangeYoinAnime && mvdata.isClimax && !GameClass.Climax && clip[clip.Length - 1] != '0' && (!audioVo.isPlaying || audioVo.loop))
                        {
                            // 余韻状態（モーション変更なし）
                            var mv = mvDatas.motionAndVoiceDataList.FirstOrDefault(x => x.state.Contains("余韻"));
                            if (mv != null)
                            {
                                //if (tr_gagbo && tr_gagbo.gameObject.activeSelf)
                                if (flag_gagbo_on)
                                    audioClip = mv.audioClipGag;
                                else
                                    audioClip = mv.audioClip;
                             
                                mvdata = mv;
                            }
                        }
                    }

                    //if (anime2voice.TryGetValue(clip, out AudioClip audioClip)
                    //    && audioClip && ((!tgt.isPlaying && tgt.loop) || tgt.clip.name != audioClip.name))
                    if (audioClip && ((!audioVo.isPlaying && audioVo.loop) || audioVo.clip.name != audioClip.name))
                    {
                        //var pmv = mvDatas.motionAndVoiceDataList.FirstOrDefault(x => x.motionName == clip);
                        var pmv = mvdata;

                        if (pmv != null)
                        {
                            Debug.Log($"ボイス再生 : M:{clip} V:{audioClip.name} Climax:{pmv.isClimax}");

                            audioVo.loop = !pmv.isClimax;
                        }
                        else
                        {
                            Debug.Log($"ボイス再生 : M:{clip} V:{audioClip.name}");

                            audioVo.loop = !GameClass.Climax;
                        }
                        audioVo.clip = audioClip;
                        audioVo.Play();
                    }
                }

                //if (tgt.isPlaying)
                //{
                //    if (!GameClass.Climax && !tgt.loop)
                //        tgt.loop = true;
                //    if (GameClass.Climax && tgt.loop)
                //        tgt.loop = false;
                //}
            }


            if (_setClip)
            {
                Debug.Log("F3p sub ボイス指定再生: " + _setClip.name);
                audioVo.clip = _setClip;
                audioVo.loop = true;
                audioVo.Play();

                _setClip = null;
            }


            // ボーンリンク中のマスターorスレイブ時は尿キャンセル
            //bool isBonelink = Free3pXtMS.BoneLink && (Free3pXtMS.xMaster.girlCtrl == this.girlCtrl || Free3pXtMS.xSlave.girlCtrl == this.girlCtrl);

            bool isXtMaster = Free3pXtMS.IsMaster(this.girlCtrl);
            bool isBonelink = isXtMaster || Free3pXtMS.IsSlave(this.girlCtrl);

            // 母乳用、マスター時はキャンセル
            //isClimax = !audioVo.loop && !(isBonelink && Free3pXtMS.xMaster.girlCtrl == this.girlCtrl);
            isClimax = !audioVo.loop && !isXtMaster;

            if ((audioVo.isPlaying || isBonelink) && GameClass.Climax && nyouController.Nyou)
            {
                // 絶頂音声再生中でなければ尿を止める
                if (audioVo.loop || isBonelink)
                {
                    MyDebug.Log("F3p sub 尿を停止");
                    nyouController.Nyou = false;
                    nyouController.gameObject.SetActive(false);
                    nyouController.gameObject.SetActive(true);
                }
            }
        }

    }

    public class Free3pXtMS
    {
        static List<Free3pXtMS> __ListInst = new List<Free3pXtMS>();

        public bool BoneLink;
        public XtCharProp xMaster;
        public XtCharProp xSlave;

        bool bkupBodyVisibled;
        bool bkupPeniVisibled;

        bool bkupSlaveActive;
        List<GameObject> meshObjMaster = null;

        // CharProp側に移動
        //static TransformData bkupPos;
        //static TransformData bkupPosS;

        public Free3pXtMS()
        {
            __ListInst.Add(this);
        }
        ~Free3pXtMS()
        {
            if (__ListInst != null)
                __ListInst.Remove(this);
        }

        internal static string GetXtSceneName => "#XtMS";

        public static bool CheckAll(Func<Free3pXtMS, bool> func)
        {
            for (int i = 0; i < __ListInst.Count; i++)
            {
                if (__ListInst[i].Ready && func(__ListInst[i]))
                    return true;
            }
            return false;
        }

        public static bool IsMaster(GirlCtrl gc)
        {
            return CheckAll(x => x.xMaster.girlCtrl == gc);
        }

        public static bool IsSlave(GirlCtrl gc)
        {
            return CheckAll(x => x.xSlave.girlCtrl == gc);
        }

        public string GetMasterAnimeName()
        {
            var animator = this.xMaster.rootModel.GetComponent<Animator>();
            if (!animator)
                return string.Empty;

            var clip = animator.GetCurrentAnimatorClipInfo(0).OrderByDescending(x => x.weight).FirstOrDefault(x => x.weight > 0).clip;

            return clip ? clip.name : string.Empty;
        }

        public static string GetXtMasterAnimeName(GirlCtrl gcslave)
        {
            for (int i = 0; i < __ListInst.Count; i++)
            {
                if (__ListInst[i].xSlave.girlCtrl == gcslave)
                    return __ListInst[i].GetMasterAnimeName();
            }
            return string.Empty;
        }

        public static bool IsMasterOrSlave(GameObject rootModelObj)
        {
            return rootModelObj && CheckAll(x => x.xSlave.rootModel == rootModelObj.transform || x.xMaster.rootModel == rootModelObj.transform);
        }

        public static void OnLateUpdateAll()
        {
            for (int i = 0; i < __ListInst.Count; i++)
            {
                __ListInst[i].OnLateUpdate();
            }
        }

        public bool Ready => this.BoneLink && this.xMaster != null && this.xSlave != null && this.xMaster.root && this.xSlave.root;

        public void OnLateUpdate()
        {
            if (!BoneLink || xMaster == null || xSlave == null || !xMaster.root || !xSlave.root || FadeManager_GUI.FadeStart)
            {
                if (BoneLink)
                    ResetMS();
                //if (xSlave != null)
                //{
                //    if (xSlave.rootModel && xSlave.rootModel == Free3P.subMemModel)
                //    {
                //        Free3P.OffsetSubMemPos(actScene.name);
                //    }
                //    xSlave = null;
                //}
                //xMaster = null;
                return;
            }

            if (xMaster.root && xSlave.root)
            {
                bool playerMaster = xMaster.charIndex == TargetIni.TargetChar.Player;
                bool shotaSlave = xSlave.charIndex >= TargetIni.TargetChar.Shota01;

                Dictionary<Transform, Quaternion> plvOld = null;

                if (shotaSlave)
                {
                    plvOld = new Dictionary<Transform, Quaternion>();

                    var t = xSlave.pelvis;
                    while (true)
                    {
                        plvOld[t] = t.localRotation;

                        t = t.parent;
                        if (!t)
                            break;
                    }
                }

                if (!playerMaster || (GameClass.ManBody || (!GameClass.ManPenis && (GameClass.ManHand_L || GameClass.ManHand_R || GameClass.ManTan))))
                {
                    if (xMaster.girlCtrl.ini.isNotGirl())
                    {
                        RigUtil.LimbsIKTargets limbsIKTargets = null;

                        var plgIK = xSlave.girlCtrl.plgIK;
                        if (plgIK != null && plgIK.ready && plgIK.ikConfig.ikTargetingXtMaster)
                            limbsIKTargets = plgIK.helperIK.limbsIKTargets;

                        Plugin.BoneLink.Update(xMaster.rootBone, xSlave.rootBone, false, limbsIKTargets);
                    }
                    else
                        Plugin.BoneLink.Update(xSlave.rootBone, xMaster.rootBone, true, null); // M2Fの逆転モードで
                }
                else
                {
                    // Tポーズ回避
                    xSlave.rootBone.transform.localRotation = xMaster.rootBone.transform.localRotation;
                    xSlave.pelvis.transform.localRotation = xMaster.pelvis.transform.localRotation;
                    xSlave.pelvis.localRotation = xMaster.pelvis.localRotation;
                }

                if (!playerMaster || (GameClass.ManBody || GameClass.ManPenis || GameClass.ManHand_L || GameClass.ManHand_R || GameClass.ManTan))
                {
                    xSlave.root.position = xMaster.root.position;
                    xSlave.root.rotation = xMaster.root.rotation;
                    //xSlave.root.transform.position += (xMaster.pelvis.position - xSlave.pelvis.position);
                    
                    if (!shotaSlave)
                        xSlave.rootBone.transform.position += (xMaster.pelvis.position - xSlave.pelvis.position);

                    if (shotaSlave)
                    {
                        // ねじれ防止
                        var plvNew = xSlave.pelvis.rotation;
                        var q = plvNew;
                        foreach (var t in plvOld)
                        {
                            if (t.Key == xSlave.root)
                                break;

                            t.Key.localRotation = t.Value;
                            q *= Quaternion.Inverse(t.Value);
                        }
                        xSlave.root.rotation = q;
                        //xSlave.root.transform.position += (xMaster.pelvis.position - xSlave.pelvis.position);
                        xSlave.rootBone.parent.transform.position += (xMaster.pelvis.position - xSlave.pelvis.position);
                    }
                }

                hideMasterMeshObj();
            }
        }

        public void SetMS_Free3pSub(TargetIni.TargetChar master)
        {
            if (!Free3P.subMemModel || BoneLink)
                return;

            var sub = TargetIni.TargetChar.Neko;
            //var sub = IO_ExSlider.CtrlGirlsInst.FirstOrDefault(x => x.FindModel() == Free3P.subMemModel);
            if (Free3P.subMemModel.transform.root.name == "CH02")
                sub = TargetIni.TargetChar.Usa;

            SetMS(master, sub);
        }

        internal void SetMS_Free3pSubRevShota(TargetIni.TargetChar master)
        {
            if (!Free3P.subMemModel || BoneLink)
                return;

            if (!IO_ExSlider.CtrlGirlsInst[(int)master].isActive())
            {
                var go = actScene.GetRootGameObjects().FirstOrDefault(x => x.name == IO_ExSlider.CtrlGirlsInst[(int)master].ini.root);
                if (!go)
                    return;

                go.SetActive(true);
            }

            var sub = TargetIni.TargetChar.Neko;
            //var sub = IO_ExSlider.CtrlGirlsInst.FirstOrDefault(x => x.FindModel() == Free3P.subMemModel);
            if (Free3P.subMemModel.transform.root.name == "CH02")
                sub = TargetIni.TargetChar.Usa;

            SetMS(sub, master);
        }


        void hideMasterMeshObj()
        {
            if (meshObjMaster != null)
            {
                if (xMaster.charIndex == TargetIni.TargetChar.Player)
                {
                    if (ConfigClass.Man_Dysp != 3 && meshObjMaster.Any(x => x.activeSelf))
                    {
                        var old = ConfigClass.Man_Dysp;
                        ConfigClass.Man_Dysp = 3;
                        xMaster.root.GetComponent<CostumeSetUp_PC>().CharacterSetUp();
                        ConfigClass.Man_Dysp = old;
                    }
                    return;
                }
                else if (xMaster.charIndex >= TargetIni.TargetChar.Shota01)
                {
                    if (ConfigClass.Man_Dysp != 3 && meshObjMaster.Any(x => x.activeSelf))
                    {
                        var old = ConfigClass.Man_Dysp;
                        ConfigClass.Man_Dysp = 3;
                        xMaster.root.GetComponent<CostumeSetUp_SY>().CharacterSetUp();
                        ConfigClass.Man_Dysp = old;
                    }
                    return;
                }

                foreach (var o in meshObjMaster)
                    if (o && o.activeSelf && !o.name.StartsWith("IT", StringComparison.Ordinal)) o.SetActive(false);
            }
            //毎フレームは重い
            //else if (xMaster.charIndex == TargetIni.TargetChar.Player)
            //{
            //    if (ConfigClass.Man_Dysp != 3)
            //    {
            //        var old = ConfigClass.Man_Dysp;
            //        ConfigClass.Man_Dysp = 3;
            //        xMaster.root.GetComponent<CostumeSetUp_PC>().CharacterSetUp();
            //        ConfigClass.Man_Dysp = old;
            //    }
            //}
            //else if (xMaster.charIndex >= TargetIni.TargetChar.Shota01)
            //{
            //    if (ConfigClass.Man_Dysp != 3)
            //    {
            //        var old = ConfigClass.Man_Dysp;
            //        ConfigClass.Man_Dysp = 3;
            //        xMaster.root.GetComponent<CostumeSetUp_SY>().CharacterSetUp();
            //        ConfigClass.Man_Dysp = old;
            //    }
            //}
        }


        public void SetMS(TargetIni.TargetChar master, TargetIni.TargetChar slave)
        {
            if (BoneLink)
                return;

            if (!IO_ExSlider.CtrlGirlsInst[(int)slave].isActive())
            {
                var go = actScene.GetRootGameObjects().FirstOrDefault(x => x.name == IO_ExSlider.CtrlGirlsInst[(int)slave].ini.root);
                if (!go)
                    return;

                bkupSlaveActive = false;
                go.SetActive(true);
            }
            else
            {
                bkupSlaveActive = true;
            }

            xMaster = new XtCharProp(master);
            xSlave = new XtCharProp(slave);

            if (!xMaster.root || !xSlave.root)
            {
                xMaster = null;
                xSlave = null;
                return;
            }

            // XtCharPropに移動
            //bkupPos = new TransformData(xMaster.root);
            //bkupPosS = new TransformData(xSlave.root);

            List<GameObject> meshObj = null;

            bkupBodyVisibled = false;
            bkupPeniVisibled = false;

            switch (master)
            {
                case TargetIni.TargetChar.Player:
                    if (!GameObject.Find("PC00"))
                        return;

                    bkupBodyVisibled = GameClass.ManBody;
                    bkupPeniVisibled = GameClass.ManPenis;

                    //GameClass.ManBody = false;
                    //GameClass.ManPenis = true;
                    //GameObject.Find("PC00").GetComponent<CostumeSetUp_PC>().CharacterSetUp();
                    meshObj = new List<GameObject> { xMaster.rootModel.Find("PC01_BD00_hadaka").gameObject,
                                                        xMaster.rootModel.Find("PC01_FT").gameObject};
                    break;

                case TargetIni.TargetChar.Neko:
                    meshObj = xMaster.root.GetComponent<CostumeSetUp_CH01>().MeshObj;
                    break;

                case TargetIni.TargetChar.Usa:
                    meshObj = xMaster.root.GetComponent<CostumeSetUp_CH02>().MeshObj;
                    break;

                case TargetIni.TargetChar.Shota01:
                case TargetIni.TargetChar.Shota02:
                case TargetIni.TargetChar.Shota03:
                    //meshObj = xMaster.root.GetComponent<CostumeSetUp_SY>().MeshObj;
                    meshObj = new List<GameObject> { xMaster.rootModel.Find("SY01_BD00").gameObject };
                    break;
            }

            //if (meshObj != null)
            //{
            //}
            meshObjMaster = meshObj;
            hideMasterMeshObj();

            BoneLink = true;

            switch (slave)
            {
                case TargetIni.TargetChar.Shota01:
                case TargetIni.TargetChar.Shota02:
                case TargetIni.TargetChar.Shota03:
                    if (!bkupSlaveActive)
                    {
                        //xSlave.rootModel.GetComponent<Animator>().runtimeAnimatorController = xMaster.rootModel.GetComponent<Animator>().runtimeAnimatorController;
                    }
                    break;
            }
        }

        public void ResetMS()
        {
            if (!BoneLink)
                return;

            BoneLink = false;

            if (xMaster != null)
            {
                if (xMaster.root)
                {
                    switch (xMaster.charIndex)
                    {
                        case TargetIni.TargetChar.Player:
                            if (GameObject.Find("PC00"))
                            {
                                GameClass.ManBody = bkupBodyVisibled;
                                GameClass.ManPenis = bkupPeniVisibled;
                                GameObject.Find("PC00").GetComponent<CostumeSetUp_PC>().CharacterSetUp();
                            }
                            break;

                        case TargetIni.TargetChar.Neko:
                            xMaster.root.GetComponent<CostumeSetUp_CH01>().ReLoad();
                            break;

                        case TargetIni.TargetChar.Usa:
                            xMaster.root.GetComponent<CostumeSetUp_CH02>().ReLoad();
                            break;

                        case TargetIni.TargetChar.Shota01:
                        case TargetIni.TargetChar.Shota02:
                        case TargetIni.TargetChar.Shota03:
                            xMaster.root.GetComponent<CostumeSetUp_SY>().CharacterSetUp();
                            break;
                    }

                    xMaster.RestorePosRot();
                    //xMaster.root.position = bkupPos.position;
                    //xMaster.root.rotation = bkupPos.rotation;
                }
                xMaster = null;
            }

            if (xSlave != null)
            {
                if (xSlave.root)
                {
                    xSlave.RestorePosRot();
                    //xSlave.root.position = bkupPosS.position;
                    //xSlave.root.rotation = bkupPosS.rotation;
                    xSlave.root.gameObject.SetActive(bkupSlaveActive);

                    if (HsysMgr.Config.Free3p_EnableAngleFixIC2FH && actScene.name == "FH" &&
                        Free3pSubUtil.IsSub(xSlave.girlCtrl))
                    {
                        Free3P.OffsetSubMemPos(actScene.name); 
                        
                        if (!string.IsNullOrEmpty(Free3P.MotionAndVoices.LastStateName))
                        {
                            int levelSub = SaveLoad_Game.Data.savegamedata.Level_CH01;
                            if (xSlave.charIndex == TargetIni.TargetChar.Usa)
                            {
                                levelSub = SaveLoad_Game.Data.savegamedata.Level_CH01;
                            }
                            Free3P.MotionAndVoices.Play(xSlave.girlCtrl, Free3P.MotionAndVoices.LastStateName, levelSub);
                        }
                    }

                    if (xSlave.girlCtrl.plgIK != null && xSlave.girlCtrl.plgIK.helperIK
                        && xSlave.girlCtrl.plgIK.helperIK.limbsIKTargets != null)
                        xSlave.girlCtrl.plgIK.helperIK.limbsIKTargets.Reset();
                }
                xSlave = null;
            }

        }


        public class XtCharProp
        {
            public Transform root;
            public Transform rootModel;
            public Transform rootBone;
            public Transform pelvis;
            public GirlCtrl girlCtrl;
            public TargetIni.TargetChar charIndex;

            public Transform posRoot;
            public TransformData bkupRootTrs;
            public TransformData bkupPosRootTrs;

            public XtCharProp(TargetIni.TargetChar tgt)
            {
                this.charIndex = tgt; 
                var gc = IO_ExSlider.CtrlGirlsInst[(int)tgt];

                if (!gc.isActive(true))
                    return;

                this.girlCtrl = gc;
                root = gc.ini.rootObj.transform;
                rootBone = gc.ini.boneRootObj.transform;
                rootModel = gc.ini.modelRootObj.transform;

                posRoot = gc.ScnPosRoot().transform;
                bkupRootTrs = new TransformData(root);
                bkupPosRootTrs = new TransformData(posRoot, true);

                switch (tgt)
                {
                    case TargetIni.TargetChar.Neko:
                        pelvis = rootBone.Find("bip01/bip01 Pelvis");
                        break;
                    case TargetIni.TargetChar.Usa:
                        pelvis = rootBone.Find("bip01/bip01 Pelvis") ?? rootBone.Find("bip01_02/bip01 Pelvis_02");
                        break;
                    case TargetIni.TargetChar.Player:
                        pelvis = rootBone.Find("PC00Bip/PC00Bip Pelvis");
                        break;
                    case TargetIni.TargetChar.Shota01:
                        rootBone = rootBone.Find("HS_kiten_syA");
                        pelvis = rootBone.Find("bip01_syA/bip01 Pelvis_syA");
                        break;
                    case TargetIni.TargetChar.Shota02:
                        rootBone = rootBone.Find("HS_kiten_syB");
                        pelvis = rootBone.Find("bip01_syB/bip01 Pelvis_syB");
                        break;
                    case TargetIni.TargetChar.Shota03:
                        rootBone = rootBone.Find("HS_kiten_syC");
                        pelvis = rootBone.Find("bip01_syC/bip01 Pelvis_syC");
                        break;
                }
            }

            public void RestorePosRot()
            {
                root.position = bkupRootTrs.position;
                root.rotation = bkupRootTrs.rotation;

                posRoot.localPosition = bkupPosRootTrs.position;
                posRoot.localRotation = bkupPosRootTrs.rotation;
            }
        }
    }


#if DEBUG || DEVMODE_old
        class onaVM
        {
            public static onaVM ona1neko = new onaVM("01", 3);

            public string charId = "00";
            public int level = 1;

            public static Dictionary<string, AnimationClip> animes = new Dictionary<string, AnimationClip>();
            public static Dictionary<string, AudioClip> voices = new Dictionary<string, AudioClip>();

            // 末尾はバストサイズS/M/L
            public static Dictionary<string, string> onaABC = new Dictionary<string, string> {
                        { "IC01<ID>G01_S", "IC00<ID>FA00_02_<LV>" },
                        { "IC01<ID>G02_S", "IC00<ID>FB00_02_<LV>" },
                        { "IC01<ID>G03_S", "IC00<ID>DB00_01_<LV>" },
                    };

            public static Dictionary<string, string> onaCmax = new Dictionary<string, string> {
                        { "IC01<ID>F04_00_S", "IC00<ID>F00_<LV>" },
                        { "IC01<ID>F04_01_S", "IC00<ID>F00_<LV>" },
                        { "IC01<ID>F04_02_S", "IC00<ID>F01_<LV>" },
                        { "IC01<ID>F04_03_S", "IC00<ID>F01_<LV>" },
                    };

            static bool loaded = false;

            public onaVM(string charId, int level)
            {
                this.charId = charId;
                this.level = level;
            }

            public void load()
            {
                var bundle = AssetBundle.LoadFromFile(Application.dataPath + "/Data/motion_ic.unity3d");
                var bundle2 = AssetBundle.LoadFromFile(Application.dataPath + "/Data/voice_h_part.unity3d");
                //foreach (var v in onaABC.ToDictionary(
                //    x => x.Key.Replace("<ID>", charId).Replace("<LV>", $"{level:00}"),
                //    x => x.Value.Replace("<ID>", charId).Replace("<LV>", $"{level:00}")
                //    ))


                foreach (var v in onaABC)
                {
                    var ml = level < 1 ? 1 : level;
                    if (charId == "01")
                        ml = level < 2 ? 2 : level;

                    var m = v.Key.Replace("<ID>", charId).Replace("<LV>", $"{level:00}");
                    var c = v.Value.Replace("<ID>", charId).Replace("<LV>", $"{ml:00}");

                    Debug.Log(m);
                    Debug.Log(c);

                    animes[v.Key] = bundle.LoadAsset<AnimationClip>(m);
                    voices[v.Value] = bundle2.LoadAsset<AudioClip>(c);
                }
                foreach (var v in onaCmax)
                {
                    var ml = level < 2 ? 2 : level;
                    if (charId == "01")
                        ml = level < 3 ? 3 : level;

                    var m = v.Key.Replace("<ID>", charId).Replace("<LV>", $"{level:00}");
                    var c = v.Value.Replace("<ID>", charId).Replace("<LV>", $"{ml:00}");

                    Debug.Log(m);
                    Debug.Log(c);

                    animes[v.Key] = bundle.LoadAsset<AnimationClip>(m);
                    voices[v.Value] = bundle2.LoadAsset<AudioClip>(c);
                }

                bundle.Unload(false);
                bundle2.Unload(false);
                loaded = true;
            }

            List<KeyValuePair<AnimationClip, AnimationClip>> overrides;
            public void Play(GirlCtrl girlCtrl, int state)
            {
                if (!loaded)
                    load();

                if (state < 3)
                {
                    var d = onaABC.Skip(state).First();
                    var clip = animes[d.Key];
                    //clip = this.animes["IC01<ID>F04_00_S"];
                    if (!clip)
                        return;

                    var overrideController = girlCtrl.FindModel().GetComponent<Animator>().runtimeAnimatorController as AnimatorOverrideController;
                    AnimatorOverrideController overrideController1 = new AnimatorOverrideController();
                    overrideController1.runtimeAnimatorController = overrideController.runtimeAnimatorController;

                    overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
                    overrideController.GetOverrides(overrides);
                    for (int i = 0; i < overrides.Count; ++i)
                    {
                        Debug.Log($"{overrides[i].Key.name} {(overrides[i].Value != null ? overrides[i].Value.name : "")}");
                        //overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, clip);

                        if (overrides[i].Value == null)
                            continue;

                        if (overrides[i].Key.name[0] == 'F')
                        {
                            var key = overrides[i].Key.name.Substring(3, 4);
                            var p = onaCmax.FirstOrDefault(x => x.Key.Contains(key));
                            Debug.Log($"{key} {p.Key} {animes[p.Key]}");

                            //if (!string.IsNullOrEmpty(p.Key))
                                overrideController1[overrides[i].Key.name] = animes[p.Key]; 
                            //else
                            //    overrideController1[overrides[i].Key.name] = clip;
                        }
                        else
                        {
                            overrideController1[overrides[i].Key.name] = clip;
                        }
                    }

                    //overrideController.ApplyOverrides(overrides);
                    girlCtrl.FindModel().GetComponent<Animator>().runtimeAnimatorController = overrideController1;
                }

                _Instance.StartCoroutine(voiceplay(girlCtrl, SysVoiceMgr.getGirlAudio(0)));
            }

            string prevanime = "";
            IEnumerator voiceplay(GirlCtrl girlCtrl, AudioSource tgt)
            {
                Debug.Log("voiceplay : " + (bool)tgt);
                string ctrl = "", clip = "";
                while (tgt)
                {
                    yield return null;

                    if (!tgt || !girlCtrl.isActive())
                    {
                        if (tgt)
                        {
                            tgt.Stop();
                            GameObject.DestroyImmediate(tgt);
                        }
                        yield break;
                    }

                    if (girlCtrl.charaCtrl.getAnimeState(ref ctrl, ref clip))
                    {

                        if (prevanime != clip)
                        {
                            Debug.Log("m : " + clip);

                            prevanime = clip;
                            foreach (var v in onaABC)
                            {
                                var m = v.Key.Replace("<ID>", charId).Replace("<LV>", $"{level:00}");

                                if (clip == m)
                                {
                                    Debug.Log("ボイス再生 : " + clip );
                                    if (tgt.clip != voices[v.Value])
                                    {
                                        tgt.clip = voices[v.Value];
                                        tgt.loop = true;
                                        tgt.Play();
                                    }
                                }
                            }
                            foreach (var v in onaCmax)
                            {
                                var m = v.Key.Replace("<ID>", charId).Replace("<LV>", $"{level:00}");

                                if (clip == m)
                                {
                                    Debug.Log("ボイス再生 : " + clip);
                                    if (tgt.clip != voices[v.Value])
                                    {
                                        tgt.clip = voices[v.Value];
                                        tgt.loop = true;
                                        tgt.Play();
                                    }
                                }
                            }
                        }
                    }

                }
            }

        }
#endif
}

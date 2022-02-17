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
using UnityEngine.SceneManagement;
using System.Collections;

namespace BepInEx.IO_ExSlider.Plugin
{
    public class HsysMgr
    {
        public static HsysMgrConfig Config = new HsysMgrConfig();

        public class HsysMgrConfig // 保存される設定 
        {
            public bool AutoRestart = false;
            public bool ChangeYoinAnime = false;
            public int FreqBG3Car = 0;
            public bool ClimaxMilk1 = false;
            public bool ClimaxMilk2 = false;
            public bool DebugTopToFreeMode = false;
            public bool AutoLevelUpFreeMode = false;
            public int AutoLevelUpTolerance = 2;

            public bool UseFree3P = false;
            public Vector3 Free3p_OffestSubMem = Vector3.zero;
            public Vector3 Free3p_OffestRotSubMem = Vector3.zero;
            public PositionByStates Free3p_OffsetByState = new PositionByStates();
            public bool Free3p_EnableAngleFixIC2FH = true;
        }

        // 保存されない一時設定
        public static bool EnabledStLock = false;

        // bool gotState = false;
        //static float prevDrunk = 0f;
        //static float prevHP = 0f;
        //static float prevHpara = 0f;
        static UC_AnimeController ucac = null;
        static MethodInfo miPointUpStop;

        readonly static float[] NEKO_LV_TH = { 0, 21f, 41f, 61f, 81f, 101f };
        readonly static float[] USA_LV_TH = { 0, 26f, 51f, 76f, 101f };
        public static int SetGameLevel_CH01
        {
            get => SaveLoad_Game.Data.savegamedata.Level_CH01;
            set
            {
                SaveLoad_Game.Data.savegamedata.Level_CH01 = value;

                var param = SaveLoad_Game.Data.savegamedata.MainParaCH01;
                int i = Mathf.Clamp( value, 0, NEKO_LV_TH.Length - 2);
                SaveLoad_Game.Data.savegamedata.MainParaCH01 = Mathf.Clamp(param, NEKO_LV_TH[i], NEKO_LV_TH[i+1] - 1);
            }
        }
        public static int SetGameLevel_CH02
        {
            get => SaveLoad_Game.Data.savegamedata.Level_CH02;
            set
            {
                SaveLoad_Game.Data.savegamedata.Level_CH02 = value;

                var param = SaveLoad_Game.Data.savegamedata.MainParaCH02;
                int i = Mathf.Clamp(value, 0, USA_LV_TH.Length - 2);
                SaveLoad_Game.Data.savegamedata.MainParaCH02 = Mathf.Clamp(param, USA_LV_TH[i], USA_LV_TH[i + 1] - 1);
            }
        }

        public static bool EnableGyagboWithIC { get; internal set; }

        public static void StateLock()
        {
            //if (gotState)
            {
                if (actScene.name == "UC")
                {
                    if (!ucac)
                    {
                        ucac = GameObject.Find("MainSystem").GetComponent<UC_AnimeController>(); // 特殊
                        miPointUpStop = ucac.GetNonPublicMethod("PointUpStop");
                    }
                    else 
                        miPointUpStop.Invoke(ucac, null);

                    //if (SaveLoad_Game.Data.savegamedata.Drunk != prevDrunk)
                    //{
                    //    SaveLoad_Game.Data.savegamedata.Drunk = prevDrunk;
                    //}

                    //if (SaveLoad_Game.Data.savegamedata.HP != prevHP)
                    //{
                    //    SaveLoad_Game.Data.savegamedata.HP = prevHP;
                    //}

                    //if (SaveLoad_Game.Data.savegamedata.H_Para != prevHpara)
                    //{
                    //    SaveLoad_Game.Data.savegamedata.H_Para = prevHpara;
                    //}
                    return;
                }
            }

            //gotState = true;
            //prevDrunk = SaveLoad_Game.Data.savegamedata.Drunk; 
            //prevHP = SaveLoad_Game.Data.savegamedata.HP; 
            //prevHpara = SaveLoad_Game.Data.savegamedata.H_Para;
        }


        static bool _prevClimax;
        static int _climaxCnt = 0;

        // メイン更新
        public static void HsysUpdate()
        {
            if (EnabledStLock)
                StateLock();
            //else if (gotState)
            //    gotState = false;

            if (Config.FreqBG3Car > 0 && actScene.name == "UC"
                && Extentions.FindSp("BG03/Car"))
            {
                CarWithUC.OnUpdate();
            }
            else if (actScene.name != "UC")
            {
                CarWithUC.ClearClone();
            }

            if (HsysMgr.Config.AutoLevelUpFreeMode && GameClass.FreeMode)
            {
                if (_prevClimax != GameClass.Climax && !(_prevClimax = GameClass.Climax))
                {
                    _climaxCnt++;

                    // v0.92 バグ修正でUCの判定追加
                    if (actScene.name != "UC" && (_climaxCnt - Config.AutoLevelUpTolerance) > UnityEngine.Random.Range(1, 11))
                    {
                        _climaxCnt = 0;

                        var tenkeyGo = Extentions.FindSp("UI Root(FH)/TenKey/TenKey_BG");
                        if (tenkeyGo != null)
                        {
                            var tenKeyPad = tenkeyGo.GetComponent<TenKeyPad>();

                            var level = (int)fiTenKeyPad_SceneLevel.GetValue(tenKeyPad) +1;
                            SetSceneLevel(level);

                        }
                    }
                }
            }
        }

        static FieldInfo _fiKey_Period_Label = typeof(TenKeyPad).GetField("Key_Period_Label", BindingFlags.Instance | BindingFlags.NonPublic);
        static MethodInfo _miKey_Period_efin = typeof(TenKeyPad).GetMethod("Key_Period_efin", BindingFlags.Instance | BindingFlags.NonPublic);
        public static void HsysLateUpdate()
        {
            // FPSに絶頂回数追記
            if (ConfigClass.FPSSetting &&
                HsysMgr.Config.AutoLevelUpFreeMode && GameClass.FreeMode)
            {
                if (_climaxCnt > 0 && !uILabelLevelUp)
                {
                    var ui = Extentions.FindSp("UI Root(UI)");
                    if (ui)
                    {
                        var fps = ui.transform.Find("Label_FPS");
                        if (fps)
                        {
                            uILabelLevelUp = fps.GetComponent<UILabel>();
                        }
                    }
                }
                
                if (_climaxCnt > 0 && uILabelLevelUp)
                {
                    var i = uILabelLevelUp.text.IndexOf("FPS");
                    if (i < 0)
                        uILabelLevelUp.text = "";
                    else
                        uILabelLevelUp.text = uILabelLevelUp.text.Substring(0, i + 3);

                    uILabelLevelUp.text += $"   絶頂x{_climaxCnt}";
                }
            }

            if (GameClass.FreeMode && EnableGyagboWithIC && actScene.name == "IC")
            {
                var tenkeyGo = Extentions.FindSp("UI Root(FH)/TenKey/TenKey_BG");
                if (tenkeyGo != null)
                {
                    var tenKeyPad = tenkeyGo.GetComponent<TenKeyPad>();

                    if (tenKeyPad.IconSprite.spriteName == "KetPadIcon02")
                    {
                        var go = _fiKey_Period_Label.GetValue(tenKeyPad) as GameObject;
                        var label = go.GetComponent<UILabel>();
                        if (string.IsNullOrEmpty(label.text))
                        {
                            label.text = "\nギャグボ";
                            _miKey_Period_efin.Invoke(tenKeyPad, new object[] { true });
                        }
                    }
                }
            }
            return;

            void selfguitest()
            {
                // 上手くいかないので保留
                //var ui = Extentions.FindSp("UI Root(UI)");
                //if (ui)
                //{
                //    var fps = ui.transform.Find("Label_FPS");
                //    if (fps)
                //    {
                //        var newobj = GameObject.Instantiate(fps.gameObject);
                //        newobj.name = "PlgZecchoCounter";
                //        newobj.SetActive(true);
                //        newobj.transform.SetParent(ui.transform, true);
                //        uILabelLevelUp = newobj.GetComponent<UILabel>();
                //        if (uILabelLevelUp)
                //        {
                //            uILabelLevelUp.topAnchor.absolute += (int)uILabelLevelUp.printedSize.y;
                //        }
                //        newobj.GetComponent<FPSCounter>().enabled = false;
                //        GameObject.Destroy(newobj.GetComponent<FPSCounter>());
                //    }
                //}

                // ネタ元 https://qiita.com/hanapage/items/fd2720997fe84294b107
                //UIPanel rootPanel = NGUITools.CreateUI(false, 5);
                //rootPanel.depth = 6;
                //GameObject rootObject = rootPanel.gameObject;
                //rootObject.name = "UIRoot(ExSlider)";
                //UIRoot uiRoot = rootObject.GetComponent("UIRoot") as UIRoot;
                //uILabelLevelUp = NGUITools.AddWidget<UILabel>(rootObject);
                //uILabelLevelUp.fontSize = 16;
                //uILabelLevelUp.trueTypeFont = PlgUtil.GetResourceByName<Font>("GenShinGothic-P-Bold");
                //uILabelLevelUp.effectStyle = UILabel.Effect.Outline8;

                //if (uILabelLevelUp)
                //{
                //    uILabelLevelUp.text = $"絶頂回数:{_climaxCnt+1}";
                //}

                //if (!uILabelLevelUp)
                //    uILabelLevelUp = new UILabel();
                //if (uILabelLevelUp)
                //    uILabelLevelUp.text = "絶頂シーンレベルアップ  LEVEL " + level;
            }
        }

        public static class CarWithUC
        {
            static int prevFreqBG3Car;
            static float carTimer = -1000;
            //public static float carTimerMin => 2;
            public static float carTimerMin => 1.5f;
            public static float carTimerMax => carTimerMin * (1f / (((float)Config.FreqBG3Car / 100) * ((float)Config.FreqBG3Car / 100))) * 2f;
            static GameObject carClone = null;

            public static void OnUpdate()
            {
                if (!carClone)
                {
                    var go = Extentions.FindSp("BG03/Car");
                    Debug.Log("Carクローン作成");
                    //carClone = GameObject.Instantiate(go.transform.gameObject).transform.Find("Car").gameObject;
                    carClone = GameObject.Instantiate(go);
                    carClone.transform.SetParent(go.transform.parent, true);
                }

                carTimer -= Time.deltaTime;

                if (carTimer < 0 && carTimer > -10)
                {
                    var go = Extentions.FindSp("BG03/Car");
                    if (go)
                    {
                        var state = go.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0);
                        if (state.normalizedTime >= 1 || !state.IsName("BG03Car"))
                        {
                            MyDebug.Log("CarStart");
                            go.GetComponent<CarStart>().car(); // １再生、約5秒
                        }
                        else if (state.normalizedTime > 0.15f && state.normalizedTime < 0.85f && UnityEngine.Random.Range(0f, 2f) <= (1f * Config.FreqBG3Car / 100f))
                        {
                            if (carClone)
                            {
                                state = carClone.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0);
                                if (state.normalizedTime >= 1 || !state.IsName("BG03Car"))
                                {
                                    MyDebug.Log("CarStartクローン");
                                    carClone.GetComponent<CarStart>().car();
                                }
                            }
                        }
                    }
                }

                if (carTimer < 0 || prevFreqBG3Car != Config.FreqBG3Car)
                {
                    carTimer = UnityEngine.Random.Range(carTimerMin, carTimerMax);
                    MyDebug.Log($"CarTimer set => {carTimer}");
                    prevFreqBG3Car = Config.FreqBG3Car;
                }
            }

            public static void ClearClone()
            {
                if (carClone)
                {
                    Debug.Log("Carクローン破棄");
                    GameObject.DestroyImmediate(carClone);
                    carClone = null;
                }
            }
        }

        public static bool IsDebugPanel()
        {
            var title = Extentions.FindSp("UI Root(Title)");
            if (title)
            {
                if ((bool)fiDebugPanel.GetValue(title.GetComponent<TitleScript>()))
                {
                    // デバッグ画面 フリーモードを有効にする
                    return true;
                }
            }
            return false;
        }

        static FieldInfo fiDebugPanel = typeof(TitleScript).GetField("DebugPanel", BindingFlags.NonPublic | BindingFlags.Instance);
        static string prevScene;
        public static bool FreeDbgFlag = false; //20200712fix
        public static void HsysOnNewScene()
        {
            if (Config.DebugTopToFreeMode && SaveLoad_Game.Data.savegamedata.DebugModeShortCut)// GameClass.DebugTop)
            {
                if (actScene.name == "Title")
                {
                    FreeDbgFlag = false; //20200712fix

                    if (IsDebugPanel())
                    {
                        // デバッグ画面 フリーモードを有効にする
                        GameClass.FreeMode = true;
                        FreeDbgFlag = true; //20200712fix
                        Debug.Log("FreeMode ON-2");
                    }
                }
                else if (prevScene == "Title" && GameClass.FreeMode
                    && (SaveLoad_Game.Data.savegamedata.SceneID != SaveLoad_Game.Data.savegamedata.FreeSceneID
                        || SaveLoad_Game.Data.savegamedata.FreeLevel_CH01 != SaveLoad_Game.Data.savegamedata.Level_CH01
                        || SaveLoad_Game.Data.savegamedata.FreeLevel_CH02 != SaveLoad_Game.Data.savegamedata.Level_CH02
                    )
                    && (actScene.name == "IC" || actScene.name == "UC" || actScene.name == "FH" || actScene.name == "ADV" || actScene.name == "Custom"))
                {
                    // 書き戻し用(タイトルに戻った時反映）
                    //GameClass.FreeMode = true;
                    SaveLoad_Game.Data.savegamedata.FreeSceneID = SaveLoad_Game.Data.savegamedata.SceneID;
                    SaveLoad_Game.Data.savegamedata.FreeLevel_CH01 = SaveLoad_Game.Data.savegamedata.Level_CH01;
                    SaveLoad_Game.Data.savegamedata.FreeLevel_CH02 = SaveLoad_Game.Data.savegamedata.Level_CH02;
                }
            }
            prevScene = actScene.name;
        }


        #region シーンレベル変更
        static UILabel uILabelLevelUp = null;

        internal static FieldInfo fiTenKeyPad_SceneLevel = typeof(TenKeyPad).GetField("SceneLevel", BindingFlags.NonPublic | BindingFlags.Instance);
        public static void SetSceneLevel(int newlevel)
        {
            switch (actScene.name)
            {
                case "FH":
                    break;
                case "IC":
                    break;
                case "UC":
                    return; // 特殊
                default:
                    return;
            }

            var tenkeyGo = Extentions.FindSp("UI Root(FH)/TenKey/TenKey_BG");
            if (!tenkeyGo)
                return;

            var tenKeyPad = tenkeyGo.GetComponent<TenKeyPad>();
            var maxlvl = tenKeyPad.CharaID == "01" ? 4 : 3;

            if (newlevel <= maxlvl)
            {
                if (!sceneLevelUp)
                {
                    Debug.Log("絶頂シーンレベルアップ: " + newlevel);
                    sceneLevelUp = new GameObject("Plg-SceneLevelUpper").AddComponent<SceneLevelUp>();
                    sceneLevelUp.newLevel = newlevel;
                    sceneLevelUp.maxLevel = maxlvl;
                }
                return;

#if DEBUG
                fiTenKeyPad_SceneLevel.SetValue(tenKeyPad, newlevel);
                Debug.Log("シーンレベル: " + newlevel);

                int prevlvl = 0;
                if (tenKeyPad.CharaID == "01")
                {
                    prevlvl = SaveLoad_Game.Data.savegamedata.Level_CH01;
                    SaveLoad_Game.Data.savegamedata.Level_CH01 = newlevel;

                }
                else if (tenKeyPad.CharaID == "02")
                {
                    prevlvl = SaveLoad_Game.Data.savegamedata.Level_CH02;
                    SaveLoad_Game.Data.savegamedata.Level_CH02 = newlevel;
                }

                //FadeManager_GUI.Instance.LoadLevel(actScene.name, 0.5f);
                //return;
                
                // ボイス・モーションのバンドルのリロード
                var mainSystem = GameObject.Find("MainSystem");
                switch (actScene.name)
                {
                    case "FH":
                        {
                            var setup = mainSystem.GetComponent<FH_SetUp>();
                            setup.Unload();
                            setup.VoiceData.Clear();
                            setup.GyagVoice.Clear();
                            setup.UraVoice.Clear();
                            setup.WordPlay.Clear();
                            setup.InvokeNonPublicMethod("Awake", null);
                        }

                        mainSystem.GetComponent<FH_AnimeController>().InvokeNonPublicMethod("Start", null); // フリー
                        break;

                    case "IC":
                        {
                            var setup = mainSystem.GetComponent<IC_SetUp>();
                            setup.Unload();
                            setup.VoiceData.Clear();
                            setup.GyagVoice.Clear();
                            setup.InvokeNonPublicMethod("Awake", null);
                        }

                        mainSystem.GetComponent<IC_AnimeController>().InvokeNonPublicMethod("Start", null); // 前
                        break;
                }

                //// セーブデータのパラメータ戻す
                //if (tenKeyPad.CharaID == "01")
                //{
                //    SaveLoad_Game.Data.savegamedata.Level_CH01 = prevlvl;
                //}
                //else if (tenKeyPad.CharaID == "02")
                //{
                //    SaveLoad_Game.Data.savegamedata.Level_CH02 = prevlvl;
                //}
#endif
            }
        }

        static SceneLevelUp sceneLevelUp;
        class SceneLevelUp : MonoBehaviour
        {
            TenKeyPad tenKeyPad;
            int oldLevel;
            public int newLevel = -1;
            public int maxLevel = 0;
            float alpha = 0f;
            bool unloaded = false;
            Texture2D texture;
            Font font;
            bool loaded = false;
            const float SPEED = 3f;
            bool loading = false;

            private void Awake()
            {
                var tenkeyGo = Extentions.FindSp("UI Root(FH)/TenKey/TenKey_BG");
                if (!tenkeyGo)
                    return;

                tenKeyPad = tenkeyGo.GetComponent<TenKeyPad>();
                oldLevel = (int)fiTenKeyPad_SceneLevel.GetValue(tenKeyPad);

                texture = new Texture2D(1, 1);
                texture.SetPixel(0, 0, Color.Lerp(Color.clear, Color.black, 0.5f));
                texture.Apply();

                //font = PlgUtil.GetResourceByName<Font>("VeraMono-Bold-Italic");
                font = PlgUtil.GetResourceByName<Font>("GenShinGothic-P-Bold");
            }

            private void Update()
            {
                if (!loaded)
                    alpha = Mathf.Clamp01(alpha + Time.deltaTime * SPEED);
                else
                    alpha = Mathf.Clamp01(alpha - Time.deltaTime * SPEED);

                if (loaded && alpha <= 0)
                {
                    //this.enabled = false;

                    //if (this.font)
                    //    GameObject.DestroyImmediate(this.font);

                    if (this.texture)
                        GameObject.DestroyImmediate(this.texture);

                    GameObject.Destroy(this.gameObject);
                    GameObject.Destroy(this);
                }
            }

            private void OnGUI()
            {
                if (newLevel < 0)
                    return;

                GUIStyle gsBox = new GUIStyle("box");
                GUIStyle gUIStyle = new GUIStyle("label");
                //gUIStyle.fontSize = Screen.width / 1600 * 25;
                //gUIStyle.fontStyle = FontStyle.BoldAndItalic;
                //gUIStyle.alignment = TextAnchor.MiddleCenter;

                if (this.font)
                    gUIStyle.font = this.font;

                var bc = GUI.color;
                GUI.color = new Color(1,1,1, alpha);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), this.texture, ScaleMode.StretchToFill);
                GUI.color = bc;

                var cc = GUI.contentColor;
                //GUI.contentColor = Color.Lerp(Color.clear, Color.red, alpha);
                GUI.contentColor = Color.Lerp(Color.clear, Color.white, alpha);

                string notice = $"H-LEVEL UP!! {oldLevel} ⇒ " + ((newLevel < maxLevel) ? newLevel.ToString() : "MAX");

                var fontSize = Screen.width * 28 / 1600f;
                gUIStyle.fontSize = (int)fontSize;
                gUIStyle.alignment = TextAnchor.MiddleLeft;
                UiUtil.DrawOutlineLabel(new Rect(fontSize * 3.5f, 0, Screen.width, Screen.height), notice, 2, gUIStyle);

                if (alpha >= 1 && !loaded)
                {
                    //gUIStyle.fontSize = Screen.width / 1600 * 18;
                    notice = $"(Now Loading...)  ";
                    loading = true;
                    gUIStyle.alignment = TextAnchor.LowerRight;

                    using(new UiUtil.GuiColor(Color.yellow))
                        UiUtil.DrawOutlineLabel(new Rect(0, 0, Screen.width, Screen.height), notice, 2, gUIStyle);
                }

                //GUI.Label(new Rect(0, 0, Screen.width, Screen.height), notice, gUIStyle);
                GUI.contentColor = cc;
            }

            private void LateUpdate()
            {
                if (newLevel < 0)
                    return;

                if (alpha >= 1f && !unloaded && loading)
                {
                    unloaded = true;

                    try
                    {
                        // ボイス・モーションのバンドルのアンロード
                        var mainSystem = GameObject.Find("MainSystem");
                        switch (actScene.name)
                        {
                            case "FH":
                                {
                                    var setup = mainSystem.GetComponent<FH_SetUp>();
                                    setup.Unload();
                                    setup.VoiceData.Clear();
                                    setup.GyagVoice.Clear();
                                    setup.UraVoice.Clear();
                                    setup.WordPlay.Clear();
                                }
                                break;

                            case "IC":
                                {
                                    var setup = mainSystem.GetComponent<IC_SetUp>();
                                    setup.Unload();
                                    setup.VoiceData.Clear();
                                    setup.GyagVoice.Clear();
                                }
                                break;
                        }

                        // 使用中のSEとかもUnloadされるっぽい...？
                        if (false)
                            Resources.UnloadUnusedAssets();

                        //    return;
                        //}
                        //if (alpha >= 1f && unloaded)
                        //{

                        fiTenKeyPad_SceneLevel.SetValue(tenKeyPad, newLevel);
                        Debug.Log("シーンレベル: " + newLevel);

                        int prevlvl = 0;
                        if (tenKeyPad.CharaID == "01")
                        {
                            prevlvl = SaveLoad_Game.Data.savegamedata.Level_CH01;
                            HsysMgr.SetGameLevel_CH01 = newLevel;

                        }
                        else if (tenKeyPad.CharaID == "02")
                        {
                            prevlvl = SaveLoad_Game.Data.savegamedata.Level_CH02;
                            HsysMgr.SetGameLevel_CH02 = newLevel;
                        }

                        // ボイス・モーションのバンドルのリロード
                        //var mainSystem = GameObject.Find("MainSystem");
                        switch (actScene.name)
                        {
                            case "FH":
                                {
                                    var setup = mainSystem.GetComponent<FH_SetUp>();
                                    setup.InvokeNonPublicMethod("Awake", null);
                                }

                                mainSystem.GetComponent<FH_AnimeController>().InvokeNonPublicMethod("Start", null); // フリー

                                tenKeyPad.KeyText();
                                break;

                            case "IC":
                                {
                                    var setup = mainSystem.GetComponent<IC_SetUp>();
                                    setup.InvokeNonPublicMethod("Awake", null);
                                }

                                mainSystem.GetComponent<IC_AnimeController>().InvokeNonPublicMethod("Start", null); // 前

                                var ui = GameObject.Find("UI Root(Menu)");
                                if (ui)
                                {
                                    ui.GetComponent<MenuUI>().ZengiStart();
                                }
                                break;
                        }

                        Free3P.Recovery(false, true);
                    }
                    finally
                    {
                        loaded = true;
                    }
                }
            }
        }
        #endregion


        #region キャラ別更新

        public class HsysMgrGirl
        {
            List<string> animeState0 = new List<string>();
            List<int> animeStateId = new List<int>();
            int prevId = 0;
            bool hideChar = false;
            bool hideChar2 = false;
            bool showChar = false;
            GirlCtrl girlCtrl;

            public HsysMgrGirl(GirlCtrl girlCtrl)
            {
                this.girlCtrl = girlCtrl;
            }

            bool needsMilkStop = false;
            bool prevMilk1 = false;
            bool prevMilk2 = false;
            bool prevMilkPlay = false;

            // キャラ別更新
            public void OnUpdate()
            {
                if (girlCtrl.ini.isGirl() && (Config.ClimaxMilk1 || Config.ClimaxMilk2))
                {
                    var curid = SaveLoad_Game.Data.savegamedata.SceneID.Substring(2, 2);
                    var thisid = (girlCtrl.ini.id == "Neko" ? "01" : "02");

                    // 母乳
                    if (GameClass.Climax && (curid == "03" || curid == thisid || Free3pSubUtil.CheckClimax(girlCtrl)))
                    {
                        if (!needsMilkStop)
                        {
                            prevMilk1 = ConfigClass.MilkPerformance;
                            prevMilk2 = ConfigClass.MilkPerformance2;
                            ConfigClass.MilkPerformance = Config.ClimaxMilk1;
                            ConfigClass.MilkPerformance2 = Config.ClimaxMilk2;

                            needsMilkStop = true;

                            var psys = girlCtrl.ini.rootObj.GetComponent<ParticleSys>();
                            if (psys)
                            {
                                prevMilkPlay = psys.BonyuL.transform.Find("uLiquid - Water - Soapy-E").GetComponent<ParticleSystem>().isPlaying;
                                prevMilkPlay |= psys.BonyuL2.GetComponent<ParticleSystem>().isPlaying;
                            }

                            psys.MilkOn();
                        }
                    }
                    else if (needsMilkStop)
                    {
                        needsMilkStop = false;

                        var psys = girlCtrl.ini.rootObj.GetComponent<ParticleSys>();
                        if (!prevMilkPlay && psys)
                            psys.MilkOff();

                        if (Config.ClimaxMilk1 && !prevMilk1)
                        {
                            ConfigClass.MilkPerformance = prevMilk1;
                            if (prevMilkPlay && psys)
                            {
                                psys.BonyuL.transform.Find("uLiquid - Water - Soapy-E").GetComponent<ParticleSystem>().Stop();
                                psys.BonyuR.transform.Find("uLiquid - Water - Soapy-E").GetComponent<ParticleSystem>().Stop();
                            }
                        }
                        if (Config.ClimaxMilk2 && !prevMilk2)
                        {
                            ConfigClass.MilkPerformance2 = prevMilk2;
                            if (prevMilkPlay && psys)
                            {
                                psys.BonyuL2.GetComponent<ParticleSystem>().Stop();
                                psys.BonyuR2.GetComponent<ParticleSystem>().Stop();
                            }
                        }
                    }
                }

                if (Config.ChangeYoinAnime || this.overrideControllerBkup)
                {
                    overrideYoinAnime(Config.ChangeYoinAnime);
                }

                // 絶頂時オートリスタート
                if (!Config.AutoRestart)
                {
                    if (hideChar || hideChar2)
                    {
                        hideChar = hideChar2 = false;
                        showChar = true;
                    }
                    return;
                }

                var objBone = girlCtrl.FindBone();
                if (!objBone)
                    return;

                if (girlCtrl.ini.isGirl())
                {
                    object gc = null;
                    switch (actScene.name.Substring(0, 2))
                    {
                        case "FH":
                            gc = GameObject.Find("MainSystem").GetComponent<FH_AnimeController>(); // フリー
                            break;
                        case "IC":
                            gc = GameObject.Find("MainSystem").GetComponent<IC_AnimeController>(); // 前
                            break;
                        case "UC":
                            gc = GameObject.Find("MainSystem").GetComponent<UC_AnimeController>(); // 特殊
                            break;
                    }

                    bool flagFin = false;
                    int id = -1;

                    if (gc != null)
                    {
                        if (gc is FH_AnimeController)
                        {
                            id = ((FH_AnimeController)gc).AnimeID_Cash;
                        }
                        if (gc is IC_AnimeController)
                        {
                            id = ((IC_AnimeController)gc).AnimeID_Cash;

                            flagFin = MyHook.flagH01;
                            if (flagFin)
                            {
                                MyDebug.Log("IC_hフラグON");
                                MyHook.flagH01 = false;

                                if (animeState0.Any(x => x != "H01" && x.Length < 3))
                                    hideChar2 = true;
                            }

                            //if (MyHook.ic_Mecanim)
                            //{
                            //    flagFin = (bool)typeof(IC_Mecanim).GetField("h", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(MyHook.ic_Mecanim);
                            //    if (flagFin)
                            //        MyDebug.Log("IC_hフラグON");
                            //}
                        }
                        if (gc is UC_AnimeController)
                        {
                            id = ((UC_AnimeController)gc).AnimeID_Cash;
                        }
                    }

                    if (prevId != id)
                    {
                        Debug.Log($"  AnimeId : {prevId} -> {id}");
                        prevId = id;
                    }

                    if (gc != null && MyHook.LastAnimeStateByGameObj.TryGetValue(girlCtrl.FindModel().name, out Dictionary<int, string> dic))
                    {
                        if (dic.ContainsKey(0))
                        {
                            if (animeState0.Count == 0 || animeState0[animeState0.Count - 1] != dic[0])
                            {
                                if (animeState0.Count > 0)
                                    Debug.Log($"  AnimeState : {animeState0[animeState0.Count - 1]} -> {dic[0]}");

                                animeState0.Add(dic[0]);
                                animeStateId.Add(id);

                                if (animeState0.Count > 10)
                                {
                                    animeState0.RemoveAt(0);
                                    animeStateId.RemoveAt(0);
                                }

                                var newid = -1;
                                if (dic[0] == "H01" || dic[0].EndsWith("H01_M", StringComparison.Ordinal))
                                {
                                    Debug.Log(string.Join(", ", animeState0.ToArray()));
                                    Debug.Log(string.Join(", ", animeStateId.Select(x => x.ToString()).ToArray()));

                                    int idf = 0;
                                    for (int i = animeState0.Count - 1; i > 0; i--)
                                    {
                                        if (animeState0[i] == "H01" || animeStateId[i] < 0 || animeState0[i].EndsWith("H01_M", StringComparison.Ordinal)) //UCの一部はクリップ名
                                            continue;

                                        //if (animeState0[i].Length > 3)
                                        if (CacheRegex.IsMatch(animeState0[i], @"_0.$") || CacheRegex.IsMatch(animeState0[i], @"_0._M$"))
                                        {
                                            idf = animeStateId[i];
                                            MyDebug.Log("  Skip  " + animeState0[i]);
                                            continue;
                                        }
                                        newid = animeStateId[i];
                                        Debug.Log($"リスタート  {animeState0[i]} / {animeStateId[i]}");
                                        break;
                                    }

                                    hideChar = true;
                                    hideChar2 = false;

                                    if (gc != null && newid >= 0)
                                    {
                                        if (newid == 0)
                                            GameClass.Weakness = true;

                                        if (id == newid)
                                        {
                                            if (gc is FH_AnimeController)
                                            {
                                                ((FH_AnimeController)gc).AnimeID_Cash = -1;
                                            }
                                            if (gc is IC_AnimeController)
                                            {
                                                ((IC_AnimeController)gc).AnimeID_Cash = -1;
                                            }
                                            if (gc is UC_AnimeController)
                                            {
                                                ((UC_AnimeController)gc).AnimeID_Cash = -1;
                                            }
                                        }

                                        if (gc is FH_AnimeController)
                                        {
                                            ((FH_AnimeController)gc).StopCoroutine("Fade");
                                            ((FH_AnimeController)gc).Anime(newid);
                                        }
                                        if (gc is IC_AnimeController)
                                        {
                                            ((IC_AnimeController)gc).StopCoroutine("Fade");
                                            ((IC_AnimeController)gc).Anime(newid);
                                        }
                                        if (gc is UC_AnimeController)
                                        {
                                            ((UC_AnimeController)gc).StopCoroutine("Fade");
                                            ((UC_AnimeController)gc).Anime(newid);
                                        }

                                        if (!ConfigClass.MiniMenu)
                                            ConfigClass.MiniMenu = true;
                                    }
                                }
                                else if (hideChar)
                                {
                                    showChar = true;
                                    hideChar = false;
                                }
                                if (flagFin)
                                {
                                    showChar = false;
                                    hideChar = true;
                                }
                            }

                        }
                    }
                }
            }

            public void OnNewSceneLoaded()
            {
                animeState0.Clear();
                animeStateId.Clear();
            }

            public void OnLateUpdate()
            {
                return;

                // バグが起きるので

                if (hideChar || hideChar2)
                {
                    girlCtrl.ini.rootObj.transform.localScale = Vector3.one * 0.0000000000001f;
                }
                else if (showChar)
                {
                    showChar = false;
                    girlCtrl.ini.rootObj.transform.localScale = Vector3.one;
                }
            }

            GameObject modelBkup;
            AnimatorOverrideController overrideControllerBkup;
            string prevClipName;
            void overrideYoinAnime(bool enable)
            {
                var model = girlCtrl.FindModel();
                if (!model || (Free3P.IsLoaded && model == Free3P.subMemModel))
                    return;

                if (modelBkup != model)
                {
                    // インスタンスが変わっていたら破棄
                    modelBkup = model;
                    overrideControllerBkup = null;
                    prevClipName = null;
                }

                var animator = girlCtrl.FindModel().GetComponent<Animator>();
                var clips = animator.GetCurrentAnimatorClipInfo(0).Where(x => x.clip);

                var clipo = clips.OrderByDescending(x => x.weight).FirstOrDefault(x => x.weight > 0).clip;
                if (!clipo)
                    return;

                string clipn;
                bool needOvr = false;
                
                if (enable)
                {
                    clipn = PlgCharaCtrl.GetIOAnimeStateFromClipname(clipo.name); // バストサイズは除去

                    if (prevClipName == clipn)
                        return;
                    prevClipName = clipn;

                    // *F01_00や*F01_00_00のモーション名に対応
                    if (clipn.Length >= 6 && (clipn[clipn.Length - 6] == 'F')
                        && (clipn[clipn.Length - 3] == '_') && (clipn[clipn.Length - 1] >= '3'))
                    {
                        // 余韻前のモーション
                        if ((clipn[clipn.Length - 5] == '0') && (clipn[clipn.Length - 4] == '1'))
                        {
                            // 基本形、とりあえずそのまま
                        }
                        else
                        {
                            Debug.Log($"余韻オーバーライド({girlCtrl.ini.id}): {clipn}");
                            needOvr = true;
                        }
                    }
                }
                else
                {
                    clipn = string.Empty;
                }

                if (!needOvr)
                {
                    if (!clipn.EndsWith("H01") && overrideControllerBkup)
                    {
                        Debug.Log($"余韻オーバーライド解除({girlCtrl.ini.id}): {clipn}");
                        // 戻す
                        animator.runtimeAnimatorController = overrideControllerBkup;
                        overrideControllerBkup = null;
                    }
                    return;
                }

                var cset = new Dictionary<char, AnimationClip>();
                cset['S'] = clips.FirstOrDefault(x => x.clip.name == clipn + "_S").clip;
                cset['M'] = clips.FirstOrDefault(x => x.clip.name == clipn + "_M").clip;
                cset['L'] = clips.FirstOrDefault(x => x.clip.name == clipn + "_L").clip;
                if (!cset['S'] && cset['M']) cset['S'] = cset['M'];
                if (!cset['M'] && cset['S']) cset['M'] = cset['S'];
                if (!cset['L'] && cset['M']) cset['L'] = cset['M'];

                if (!cset['M'])
                {
                    Debug.Log("cset['M']がnullです");
                    return;
                }

                var overrideController = animator.runtimeAnimatorController as AnimatorOverrideController;
                if (!overrideController)
                    return;
                if (!overrideControllerBkup)
                    overrideControllerBkup = overrideController;

                // クリップのオーバーライドコントローラーを作る
                AnimatorOverrideController overrideController1 = new AnimatorOverrideController();
                overrideController1.runtimeAnimatorController = overrideController.runtimeAnimatorController;

                List<KeyValuePair<AnimationClip, AnimationClip>> overrides;
                overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
                overrideController.GetOverrides(overrides);
                for (int i = 0; i < overrides.Count; ++i)
                {
                    MyDebug.Log($"{overrides[i].Key.name} {(overrides[i].Value != null ? overrides[i].Value.name : "")}");
                    //overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, clip);

                    if (overrides[i].Value == null || overrides[i].Key == null)
                        continue;

                    var state_name = overrides[i].Key.name;

                    if (state_name.StartsWith("H01_", StringComparison.Ordinal))
                    {
                        var s = state_name[state_name.Length - 1];

                        if (cset.ContainsKey(s) && cset[s])
                        {
                            overrideController1[state_name] = cset[s];
                            Debug.Log($"  ORC_H01 state:{state_name} s:{s} {cset[s].name}");
                        }
                    }
                    else
                    {
                        overrideController1[state_name] = overrides[i].Value;
                    }
                }

                // コントローラー更新
                animator.runtimeAnimatorController = overrideController1;
            }
        }

        #endregion


        public class CustomPlayNyoSE
        {
            static SE_Particle_Manager nyoSemgr;
            static AudioSource nyoAudio;
            static AudioClip nyoClip;
            static bool playing = false;
            static Coroutine nyoCor;
            static NyouController nyoCon;
            public static void PlayNyoSEinCustom(GirlCtrl ctrl, NyouController comp)
            {
                if (nyoAudio && playing)
                    return;

                Transform tr;
                if (ctrl.ini.id == "Neko")
                    tr = ctrl.FindBone().transform.Find("bip01/bip01 Pelvis");
                else
                    tr = ctrl.FindBone().transform.Find("bip01_02/bip01 Pelvis_02");

                if (tr)
                {
                    nyoAudio = tr.GetComponent<AudioSource>();
                    if (!nyoAudio)
                    {
                        nyoAudio = tr.gameObject.AddComponent<AudioSource>();
                        nyoSemgr = new SE_Particle_Manager();
                        nyoClip = null;
                    }

                    if (!nyoClip)
                    {
                        var bundle = AssetBundle.LoadFromFile(Application.dataPath + "/Data/se.unity3d");
                        if (bundle)
                        {
                            nyoClip = bundle.LoadAsset("TAP_Kitchen_Water_Tap_Running_loop_mono") as AudioClip;
                            bundle.Unload(false);
                        }
                    }

                    if (nyoClip && nyoAudio)
                    {
                        nyoSemgr.SetNonPublicField("Jororororo", nyoClip);
                        nyoSemgr.SetNonPublicField("Speaker0111", nyoAudio);
                        nyoSemgr.InvokeNonPublicMethod("Start", null);
                        nyoCon = comp;
                        nyoCor = IO_ExSlider._Instance.StartCoroutine(playNyoSEinCustom());
                    }
                }
            }

            static IEnumerator playNyoSEinCustom()
            {
                playing = true;

                while (nyoCon && nyoCon.Nyou && !FadeManager_GUI.isFading)
                {
                    nyoSemgr.InvokeNonPublicMethod("Update", null);
                    yield return null;
                }

                playing = false;
            }
        }
    }

}

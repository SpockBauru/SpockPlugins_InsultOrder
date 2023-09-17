#define USING_XML

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
//using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

using HarmonyLib;
using OhMyGizmo2;
using static BepInEx.IO_ExSlider.Plugin.IO_ExSlider.MyXML;
using nnPlugin;
using static BepInEx.IO_ExSlider.Plugin.CtrlBone;
using static BepInEx.IO_ExSlider.Plugin.TargetIni;
using Hook.nnPlugin.Managed;
using BepInEx.IO_ExSlider.Plugin;
using System.Text.RegularExpressions;

namespace BepInEx.IO_ExSlider.Plugin
{
    [BepInPlugin("jp.nn.BepInEx.IO_ExSlider.Plugin", "BepInEx.IO_ExSlider.Plugin", Version)]
    public partial class IO_ExSlider : BaseUnityPlugin
    {
        public static IO_ExSlider _Instance;

        public const string Version = "0.94.1";
        
        public static readonly string PLUGIN_CAPTION = "    IO ExSlider";
        public static readonly string PLUGIN_VERSION = Version + " (Preview)";

        private const int WINID_COFIG = 79831;

        //保存先
        public const string SaveFileName = "BepInEx.IO_ExSlider";

        static List<GirlCtrl> ctrlGirls = new List<GirlCtrl>();
        static List<GirlCtrl> ctrlGirls_Targeted { get { return ctrlGirls.FindAll(s => s.isCtrlTargeted); } }
        public static List<GirlCtrl> CtrlGirlsInst => ctrlGirls; 

        public static OhMyGizmo gizmoRot;
        public static bool FlagUsaFH = false;
        public static string FlagNowTgtID = "";
        public static KeyMacro keyMacro = new KeyMacro();

        public class GirlCtrl
        {
            public TargetIni.CharaObjPaths ini;
            //public string rootPath;
            public BoneScales boneScales;
            public PlgCharaCtrl charaCtrl;
            public ScenePos scnPos;

            public ShapeKeys shapeKeys;
            public CtrlBone ctrlBone;
            public RotctrlBone rcbFullbody;
            public PlgIK plgIK;
            public MilkCtrl milkCtrl;
            public FaceCtrl faceCtrl;
            public HsysMgr.HsysMgrGirl hsysMgr;
            public FixParticlesPos fixParticles;
            public MateProp mateProp;

            bool _Init = false;
            private GameObject _objBone; // フレーム毎更新
            private GameObject _objModel; // フレーム毎更新

            public string pathScnPosRoot = null; // nullならボーン基点。クローン時のみ変更

            public GameObject ScnPosRoot()
            {
                if (string.IsNullOrEmpty(pathScnPosRoot))
                    return FindBone(); // ini.boneRootObj;

                return Extentions.FindSp(pathScnPosRoot);
                //return ini.rootObj;
                //return ini.modelRootObj;
            }

            public GameObject FindModel(bool find)
            {
                if (!find)
                    return FindModel();

                return _objModel = Extentions.FindSp(ini.getFullPath(ini.modelRoot), NeedRetry:true);
            }

            public GameObject FindModel()
            {
                if (_objModel || _Init)
                    return _objModel;

                return ini.modelRootObj;
            }

            public GameObject FindBone()
            {
                if (_objBone || _Init)
                    return _objBone;

                return ini.boneRootObj;
            }

            public bool isActive()
            {
                return ini.boneRootObj;
            }
            public bool isActive(bool find)
            {
                if (!find)
                    return isActive();

                return Extentions.FindSp(ini.getFullPath(ini.boneRoot), NeedRetry: true);
            }

            public GirlCtrl(TargetIni.CharaObjPaths paths)
            {
                this.ini = paths;
                //this.rootPath = rootPath;
                boneScales = new BoneScales(this, paths.getFullPath(paths.boneRoot));
                charaCtrl = new PlgCharaCtrl();
                scnPos = new ScenePos();
                fixParticles = new FixParticlesPos(this);
                mateProp = new MateProp(this);

                if (this.ini.isGirl())
                {
                    shapeKeys = new ShapeKeys(charaCtrl); // 男にはシェイプキーがほぼないので
                    faceCtrl = new FaceCtrl(this);
                    milkCtrl = new MilkCtrl(this);
                    hsysMgr = new HsysMgr.HsysMgrGirl(this);
                }

                if (this.ini.isGirl() || cfg.EnableMensPosectrl)
                {
                    // 男キャラにも使えるけど需要がないと思われるのでこっち
                    ctrlBone = new CtrlBone();
                    // 順番重要
                    rcbFullbody = ctrlBone.addCtrl<RotctrlBone>();
                }

                plgIK = new PlgIK();
            }

            bool PreCheckOnUpdate()
            {
                this._Init = false;

                _objBone = FindBone();
                if (!_objBone)
                    _objModel = null;
                else
                    _objModel = FindModel();

                this._Init = true;
                return _objBone;
            }

            public SkinnedMeshRenderer MilkMesh_L;
            public SkinnedMeshRenderer MilkMesh_R;
            public Transform Milk_L;
            public Transform Milk_R;

            public void OnUpdate()
            {
                if (!PreCheckOnUpdate())
                {
                    return;
                }

                charaCtrl.Init(this);
                charaCtrl.scenePos = scnPos;
                charaCtrl.edits = boneScales.edits;

                charaCtrl.ExtUpdate();

                if (this.ini.isGirl())
                {
                    this.faceCtrl.OnUpdate();
                    plgIK.OnUpdate(this);

                    //hsysMgr.OnUpdate();
                }

                //if (!fixParticles.fixParticlesPosComp)
                //{
                //    fixParticles.fixParticlesPosComp = FixParticlesPos.FixParticlesPosComp.Add(FindModel(), fixParticles);
                //}
                //// パーティクルオフセット
                //fixParticles.Update_3();

                fixParticles.Update_3pre();

                //色々問題を回避するため一度スケールを戻す
                if (!GameClass.BreastSliderPress) // 胸スライダー操作中は荒ぶるので停止
                    boneScales.ResetBoneTemp();
                else
                    boneScales.ResetBoneTempTgt(BoneScales.KITEN_PATH); // 基点だけは戻す必要あり(基点サイズ変更があると胸のボーンが荒ぶるが…)
            }

            internal void OnPostUpdate()
            {
                if (!_objBone)
                    return;

                // Hシーン
                if (this.ini.isGirl())
                {
                    hsysMgr.OnUpdate();
                }

                // パーティクルUpdate後処理
                fixParticles.Update_3post(); 
            }

            public void OnNewSceneLoaded()
            {
                _Init = false;

                if (shapeKeys != null)
                    shapeKeys.OnNewSceneLoaded();

                boneScales.OnNewSceneLoaded(actScene);

                //mateProp.OnNewScene();
                _Instance.StartCoroutine(mateProp.OnNewSceneStart(60));

                plgIK.ResetOnNewScene();

                fixParticles.OnNewScene();

                if (hsysMgr != null)
                    hsysMgr.OnNewSceneLoaded();

                if (faceCtrl != null)
                    faceCtrl.OnNewScene();
            }

            //TransformData headPos;

            public void OnPreLateUpdate()
            {
                if (!_objBone)
                    return;

                //fixParticles.LateUpdate();
                fixParticles.LateUpdate_3();

                // 揺れボーンなどゲーム側LateUpdateと干渉しない物
                charaCtrl.ExtPreLateUpdate();

                // 揺れボーンとの干渉が改善できたのでこっちに移動
                // ただしUpdateのresetBoneTempが生命線。あれが動かなくなると色々問題が起きる
                charaCtrl.ExtLateUpdate();

                if (ini.isGirl())
                {
                    Vector3 tempOffset = Vector3.zero;
                    bool noLookAt = false;

                    if (plgIK.ready)
                    {
                        if (boneScales.edits.offsetData.girlOffset.coord == BoneScales.OffsetCoord.Mouth)
                        {
                            noLookAt = true;
                        }

                        //if (headPos != null)
                        //{
                        //    tempOffset = plgIK.bipedIK.references.head.position - headPos.position;
                        //    objBone.transform.position -= tempOffset;
                        //}
                    }
                    plgIK.OnLateUpdate(noLookAt ? 0f : 1f);
                    //if (tempOffset != Vector3.zero)
                    //    objBone.transform.position += tempOffset;
                }
            }


            public void OnLateUpdate()
            {
                if (!_objBone)
                    return;

                //fixParticles.LateLateUpdate();
                fixParticles.LateLateUpdate_3();

                // 揺れボーンがおかしくなるのでここにしてたが、Updateでスケールを戻すと治ったので
                // 目の位置設定などと干渉する物だけをここに残す
                //charaCtrl.ExtLateUpdate();

                // ヘッドスケールのみ上書き
                boneScales.OverWriteHeadScales();
                // ボーン位置
                boneScales.pPosCtrl.OnLateUpdate();

                // マテリアル更新 ゲーム側のマテリアル処理も上書きする
                mateProp.ExtUpdate();

                if (ini.isGirl())
                {
                    milkCtrl.OnLateUpdate();

                    // 安全なのはここだけど、揺れボーンに顔追従の角度が反映されなくなるので違和感…
                    //bool noLookAt = false;
                    //if (boneScales.edits.offsetData.girlOffset.coord == BoneScales.OffsetCoord.Mouth)
                    //{
                    //    noLookAt = true;
                    //}
                    //plgIK.OnLateUpdate(noLookAt ? 0f : 1f);
                }
                //hsysMgr.OnLateUpdate();

                //fixParticles.OnRender();

                //if (plgIK.ready)
                //    headPos = new TransformData(plgIK.bipedIK.references.head);
            }

            public void OnRenderObject()
            {
                fixParticles.OnRender();
            }

            public bool isCtrlTargeted
            {
                get
                {
                    if (ini.isNotGirl())
                    {
                        if (ini.isPlayer())
                            return cfg.ctrlIncludePlayer;

                        else if (ini.isShota())
                            return cfg.ctrlIncludeShota;
                    }
                    return true;
                }
            }
        }

        public static class InputEx
        {
            [FlagsAttribute]
            public enum ModifierKey
            {
                None = 0x00,
                Control = 0x01,
                Alt = 0x02,
                Shift = 0x04
            }

            static int fCnt_last = -1;
            static ModifierKey m_getMdfKeys = ModifierKey.None;
            static public void GetModifierKeys()
            {
                GetModifierKeys(false);
            }
            static public void GetModifierKeys(bool ForceUpdate)
            {
                ModifierKey getmdfkey = ModifierKey.None;

                //基本的には1フレームに一度だけチェック
                if (Time.frameCount == fCnt_last && !ForceUpdate)
                    return;
                fCnt_last = Time.frameCount;

                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    getmdfkey = getmdfkey | ModifierKey.Control;

                if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                    getmdfkey = getmdfkey | ModifierKey.Alt;

                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    getmdfkey = getmdfkey | ModifierKey.Shift;

                // ResetInputAxes対策（Eventならリセットはかからない）
                var evt = Event.current;
                if (evt != null)
                {
                    var mde = evt.modifiers.ToString();
                    if (mde.Contains("Control"))
                        getmdfkey = getmdfkey | ModifierKey.Control;

                    if (mde.Contains("Alt"))
                        getmdfkey = getmdfkey | ModifierKey.Alt;

                    if (mde.Contains("Shift"))
                        getmdfkey = getmdfkey | ModifierKey.Shift;
                }

                m_getMdfKeys = getmdfkey;
            }

            static public bool CheckModifierKeys(ModifierKey mdfkey)
            {
                return m_getMdfKeys.HasFlag(mdfkey);
            }

            static public bool GetKeyDownEx(KeyCode key, ModifierKey mdfkey)
            {
                GetModifierKeys();
                if (m_getMdfKeys != mdfkey)
                    return false;

                return Input.GetKeyDown(key);
            }
        }

        void Awake()
        {
            _Instance = this;
            GameObject.DontDestroyOnLoad(this);

            // for Bepinex
            MyHook.InstallHooks();

            // 基本設定
#if !DEBUG && !DEVMODE
            if (!TargetIni.Load())
                TargetIni.Save();

            if (!Free3P.MotionAndVoices.Load())
                Free3P.MotionAndVoices.Save();
#else
            TargetIni.Save();
            TargetIni.Load();
            Free3P.MotionAndVoices.Save();
            Free3P.MotionAndVoices.Load();
#endif

            // 初期化
            targetIni.girls.ForEach(
                    x => ctrlGirls.Add(new GirlCtrl(x))
                ); ;

            // 設定ロード
            XML_Load(ref cfg);
            XML_Save(cfg);      //項目追加に備えて

            // GUI初期化
            ScWidth = 0;
            ScHeight = 0;

            if (!WinMouseOff && !cfg.WinMin_Def)
                GuiFlag = cfg.WinShow_Def;

            WinMin = cfg.WinMin_Def;
        }

        void Start()
        {
            // GUI初期化
            ScWidth = 0;
            ScHeight = 0;
        }

        class LocalData
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;

            public LocalData(Vector3 p, Quaternion r, Vector3 s)
            {
                position = p;
                rotation = r;
                scale = s;
            }
            public LocalData(Transform tr) : this(tr.localPosition, tr.localRotation, tr.localScale) { }

            public void putData(Transform dist)
            {
                dist.localPosition = this.position;
                dist.localRotation = this.rotation;
                dist.localScale = this.scale;
            }
        }

        static bool prevFadeState = false;
        public static Scene actScene;
        public static bool _PluginEnabled = true;
        public static bool _PluginPause = false;
        //bool addUnloadSceneFlag = false;

        private void Update()
        {
            // キーチェック
            if (InputEx.GetKeyDownEx(cfg.Hotkey, InputEx.ModifierKey.Control | InputEx.ModifierKey.Alt | InputEx.ModifierKey.Shift))
            {   //プラグイン　オン/オフ
                _PluginEnabled = !_PluginEnabled;
                Debug.LogWarning(PLUGIN_CAPTION + "の有効状態が変更されました: " + _PluginEnabled);

                if (!_PluginEnabled)
                {
                    // 最低限必要な無効化処理
                    ctrlGirls.ForEach(x =>
                    {
                        var root = x.ini.rootObj;
                        if (root)
                        {
                            // 頂点アタッチ処理を戻す
                            var gv = root.GetComponent<GetVertex>();
                            if (gv) gv.enabled = true;

                            // パーティクル戻す
                            if (x.fixParticles != null)
                            {
                                x.fixParticles.OnDisabled();
                            }
                        }
                    });
                }
            }
            if (!_PluginEnabled)
                return;

            // スクリーンショット
            ScreenCap.OnUpdate();

            try
            {
                var act = SceneManager.GetActiveScene();

                // タイトル処理
                if (act.name == "Title")
                {
                    if (HsysMgr.Config.DebugTopToFreeMode && SaveLoad_Game.Data.savegamedata.DebugModeShortCut // GameClass.DebugTop)
                        && HsysMgr.IsDebugPanel())
                    {
                        if (!GameClass.FreeMode)
                        {
                            Debug.Log("FreeMode ON");
                            GameClass.FreeMode = true;
                            HsysMgr.FreeDbgFlag = true; //20200712fix
                        }
                    }
                    //20200712 bugfix フリーモードで体位変更できなくなる不具合修正
                    //else if (GameClass.FreeMode)
                    else if (GameClass.FreeMode && HsysMgr.FreeDbgFlag)
                    {
                        Debug.Log("FreeMode OFF");
                        GameClass.FreeMode = false;
                        HsysMgr.FreeDbgFlag = false; //20200712fix
                    }
                }

                // フェードチェック
                if (FadeManager_GUI.FadeStart2 != prevFadeState)
                {
                    // フェード状態変更（シーン変更開始）
                    prevFadeState = FadeManager_GUI.FadeStart2;
                    if (FadeManager_GUI.FadeStart2)
                    {
                        Debug.Log("Fade in");

                        // シーンを抜ける前の処理
                        if (HsysMgr.Config.DebugTopToFreeMode && SaveLoad_Game.Data.savegamedata.DebugModeShortCut
                            && HsysMgr.IsDebugPanel() && act.name == "Title")
                        {
                            // 間に合わないことがある？
                            GameClass.FreeMode = true;
                            HsysMgr.FreeDbgFlag = true; //20200712fix
                            Debug.Log("FreeMode ON-1");
                        }

                        if (HsysMgr.Config.UseFree3P && (act.name == "Title"))// カスタムだと片方のモーションしか取れない問題あり || act.name == "Custom"))
                        {
                            Free3P.MotionAndVoices.Setup();
                        }

                        if (Free3P.IsLoaded || HsysMgr.Config.UseFree3P && GameClass.FreeMode
                            && SaveLoad_Game.Data.savegamedata.SceneID.Substring(2, 2) != "03")//(act.name == "Title")
                        {
                            var scnName = MyHook.NextSceneName;
                            Free3P.Init(scnName);
                        }

                        // キャッシュクリア
                        Extentions.CacheClear();

                        //// フラグセット
                        //if (FadeManager_GUI.isFading)
                        //{
                        //    var inst = FadeManager_GUI.Instance;
                        //    if (inst)
                        //    {
                        //        addUnloadSceneFlag = true;
                        //        Console.WriteLine(" Fading A=0");
                        //        inst.SetNonPublicField("fadeAlpha", 0f);
                        //    }
                        //}
                    }
                    else
                    {
                        // Unload完了後(まだフェード中)
                        Debug.Log("Fade out 2");
                        //if (HsysMgr.Config.UseFree3P)
                        //{
                        //    Free3P.MotionAndVoices.Setup();
                        //}

                        // シーン位置解除
                        ctrlGirls.ForEach( x =>
                        {
                            x.scnPos.enable = false;
                        });
                    }
                }

                //if (FadeManager_GUI.isFading)
                //{
                //    Input.ResetInputAxes();
                //}
                //if (FadeManager_GUI.FadeStart && addUnloadSceneFlag)
                //{
                //    var inst = FadeManager_GUI.Instance;
                //    if (inst)
                //    {
                //        var f = inst.GetNonPublicField("fadeAlpha");
                //        if (f is float && (float)f == 1f)
                //        {
                //            Console.WriteLine(" Scene Unload (Additional)");
                //            FadeManager_GUI.Instance.SetNonPublicField("async", null);
                //            //System.GC.Collect();
                //            //SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene());
                //            //inst.SetNonPublicField("fadeAlpha", 0f);
                //            addUnloadSceneFlag = false;
                //        }
                //    }
                //}
                //if (FadeManager_GUI.FadeStart && !FadeManager_GUI.FadeStart2
                //    && FadeManager_GUI.Instance)
                //{
                //    var async = FadeManager_GUI.Instance.GetNonPublicField<AsyncOperation>("async");
                //    //if (async != null && !async.isDone)
                //    //{
                //    //    Debug.Log("async.progress "+ async.progress);
                //    //    Debug.Log("async.isDone " + async.isDone);
                //    //}
                //    if (async != null && !async.allowSceneActivation)
                //    {
                //        Debug.Log("allowSceneActivationをTrueに変更");
                //        async.allowSceneActivation = true;
                //    }
                //}

                if ((FadeManager_GUI.FadeStart2 && FadeManager_GUI.Instance.LoadingRogo.activeSelf)
                    || !act.IsValid() || !act.isLoaded) // 不安定なことがあるので念のため
                {
                    _PluginPause = true;
                    return;
                }
                _PluginPause = false;

                // シーン変更検出
                if (act != actScene && act != null && act.isLoaded)
                {
                    // シーン変更検出
                    Debug.Log("-> Scene: " + act.name);
                    actScene = act;

                    CameraFovDefValueThisScene = ConfigClass.Fov;

                    // Hシステム
                    var scnName = actScene.name;
                    if (scnName == "UC" || scnName == "IC" || scnName == "FH" || scnName == "ADV")
                        MyHook.UnHookAnimator = false;
                    else
                        MyHook.UnHookAnimator = true;

                    if (HsysMgr.Config.UseFree3P)
                    {
                        if (actScene.name == "Title")
                            Free3P.MotionAndVoices.Setup();
                    }

                    if (scnName == "Custom")
                    {
                        CustomDragAndDrop.Start();
                    }

                    // 卓上カレンダー位置修正etc
                    BGMGR.OnNewScene(this, scnName);

                    // キャッシュクリア
                    Extentions.CacheClear();

                    // GUIリセット
                    GuiResetOnNewScene();

                    // ギズモ初期化
                    OhMyGizmo.allGizmoReset();

                    if (!gizmoRot)
                    {
                        gizmoRot = OhMyGizmo.AddGizmo(null, "PlgGizmoRot");
                        gizmoRot.modeRot = true;
                        gizmoRot.visible = false;
                        gizmoRot.sizeRot = 0.1f * 15;
                        gizmoRot.threthold = 20;
                        gizmoRot.undoDrag = true;
                    }

                    FlagUsaFH = false;

                    if (GameObject.Find("CH02/CH0002/HS_kiten/bip01"))
                    {
                        // ウサ子Hシーン　ボーン名が変化
                        FlagUsaFH = true;
                        Debug.Log("Usa FHシーン");
                    }

                    ctrlGirls_Targeted.ForEach(x => {
                            FlagNowTgtID = x.ini.id;
                            x.OnNewSceneLoaded();
                        });

                    // ゲーム設定
                    SetSysCnfQuality(cfg.QualitySetting);
                    HsysMgr.HsysOnNewScene();

                    // ボイスピッチ
                    SysVoiceMgr.OnUpdate();
                }

                // Free3Pセットアップコルーチン
                Free3P.MotionAndVoices.ProcSetupCoroutine(false);

                // Free3Pシーンスタート処理
                if (Free3P.FlagStart)// && FadeManager_GUI.FadeStart && !FadeManager_GUI.FadeStart2)
                {
                    // 次シーンローディング中
                    if (!FadeManager_GUI.FadeStart2 && !FadeManager_GUI.Instance.LoadingRogo.activeSelf) 
                        Free3P.PreStart(MyHook.NextSceneName);

                    //if (!FadeManager_GUI.FadeStart2 && !FadeManager_GUI.Instance.LoadingRogo.activeSelf) // ローディングから地続きで入れるけど体感が長い
                    if (!FadeManager_GUI.FadeStart)
                    {
                        // ロード完了後
                        if (SceneManager.GetActiveScene() != null &&
                            SceneManager.GetActiveScene().name == MyHook.NextSceneName)
                        {
                            Debug.Log("Free3P Start");
                            var scnName = MyHook.NextSceneName;
                            Free3P.Start(scnName);
                        }
                    }
                }

                // 本更新
                Update2();
            }
            catch (Exception e)
            {
                Debug.LogError("Update: " + updateLvl + "\r\n" + e);
            }
        }

        static string updateLvl = "";
        static void upLog(string s) { updateLvl = s; }
        bool prevGui = false;

        private void Update2()
        {
            // キーチェック
            if (InputEx.GetKeyDownEx(cfg.Hotkey, cfg.Modfkey))
            {   //プラグイン　オン/オフ
                //GUIの切り替え
                GuiFlag = !GuiFlag;
                if (GuiFlag)
                {
                    if (WinMouseOff && !cfg.WinMin_Def)
                    {
                        WinMouseOff = false;
                    }
                }
            }

            upLog("-6");

            // GUI処理
            if (WinMouseOff)
            {
                var vm = Input.mousePosition;
                Vector2 wm = new Vector2(vm.x, Screen.height - vm.y);

                Rect rc_hitcheck = new Rect(rc_stgw);

                if (!GuiFlag)
                {
                    rc_hitcheck.height = cfg.WinMouseOver_HitCheckHight; //非表示時のチェック判定を小さくする
                    rc_hitcheck.width = rc_stgw.width / 100f * cfg.WinMouseOver_HitCheckWidePercent;
                }

                if (rc_hitcheck.Contains(wm))
                {
                    if (cfg.WinMin_Def)
                        WinMin = false;
                    else
                        GuiFlag = true;
                }
                else if (GuiFlag)
                {
                    // マウスボタンチェック（ドラッグ中はOFFにしない）
                    if ((!cfg.WinMouseOut_NeedMouseEvent && !Input.GetMouseButton(0)) || Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1)
                        || Input.GetMouseButtonUp(2) || Input.GetAxis("Mouse ScrollWheel") != 0)
                    {
                        if (cfg.WinMin_Def)
                            WinMin = true;
                        else
                            GuiFlag = false;
                    }
                }
            }

            if (prevGui != GuiFlag)
            {
                prevGui = GuiFlag;
                if (!GuiFlag)
                {
                    MyXML.ClearExistsOnce();
                    xmlNote.ClearNoteCache();
                }
                else
                {
                    GuiUpdateFlag = true;
                }
            }

            upLog("-5");

            if (GuiFlag)
            {
                //onguiだけだと無効化しきれない場合がある
                if (rc_stgw.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
                {
                    bGuiOnMouse = true;
                    CameraZoomCtrl.SetZoomEnabled(false);
                }
            }

            upLog("-2");

            // ギズモ更新
            if (gizmoRot && gizmoRot.visible)
            {
                var tr = gizmoRot.target;
                if (tr)
                {
                    gizmoRot.Update();

                    if (gizmoRot.isDrag || gizmoRot.isDragEnd)
                    {
                        var v = gizmoRot.transform.localRotation.eulerAngles;
                        if (!string.IsNullOrEmpty(gizmoRot.targetName))
                            _NowGizmoRotTargetCtrl.SetEuler(gizmoRot.targetName, v);
                    }
                    else
                    {
                        gizmoRot.transform.localRotation = tr.localRotation;
                        gizmoRot.transform.position = tr.position;
                    }
                }
            }
            
            upLog("-1");

            // キャラクター
            ctrlGirls_Targeted.ForEach(x => {
                    FlagNowTgtID = x.ini.id;
                    x.OnUpdate();
                });

            //if (Time.frameCount % 21 == 0) //時々更新、シーン遷移以外は基本書き換え不要
            if (Time.frameCount % 2 == 0) // ICとかは頻度上げる必要ありそう
            {
                // ボイスピッチ
                SysVoiceMgr.OnUpdate();
            }

            //システム
            HsysMgr.HsysUpdate();
            keyMacro.Update();

            Invoke("PostUpdate", 0);
        }

        private void PostUpdate()
        {
            // キャラクター
            ctrlGirls_Targeted.ForEach(x => {
                    FlagNowTgtID = x.ini.id;
                    x.OnPostUpdate();
                });

            // エフェクト
            EffectMgr.OnUpdate();
        }

        //private void FixedUpdate()
        //{
        //}

        private void LateUpdate()
        {
            if (!_PluginEnabled || _PluginPause)
                return;

            Free3pXtMS.OnLateUpdateAll();

            //if (Input.GetKey(KeyCode.T))
            //    ctrlGirls.ForEach(x => x.OnLateUpdate());

            foreach (var cg in ctrlGirls_Targeted)
            {
                FlagNowTgtID = cg.ini.id;

                //var root = cg.ini.rootObj;
                //if (!root)
                //    continue;

                cg.OnPreLateUpdate();
            }

            //システム
            HsysMgr.HsysLateUpdate();
            BGMGR.BGscnPosUpdate();

            Invoke("LateLateUpdate", 0);
        }

        MethodInfo miGetVertexLateUpdate;

        private void LateLateUpdate()
        {
            foreach (var cg in ctrlGirls_Targeted)
            {
                FlagNowTgtID = cg.ini.id;

                //var root = cg.ini.rootObj;
                //if (!root)
                //    continue;
                var model = cg.FindModel(); // こっちの方がキャッシュされてるので
                if (!model)
                    continue;

                // 適用
                cg.OnLateUpdate();

                var root = model.transform.parent;

                // 頂点位置を反映
                var gv = root.GetComponent<GetVertex>();
                if (gv)
                {
                    // 元のLateUpdateを止めるためDisableに
                    if (gv.enabled)
                        gv.enabled = false;

                    if (miGetVertexLateUpdate == null)
                        miGetVertexLateUpdate = gv.GetNonPublicMethod("LateUpdate");
                    miGetVertexLateUpdate.Invoke(gv, null);
                    //gv.InvokeNonPublicMethod("LateUpdate", null);

                    //gv.Milk_L.localScale = syncScale(gv.Milk_L, gv.Spine2);
                    //gv.Milk_R.localScale = syncScale(gv.Milk_R, gv.Spine2);
                    gv.HS01_ribonP.localScale = syncScale(gv.HS01_ribonP, gv.Spine2);
                    gv.HS01_ribonPD.localScale = syncScale(gv.HS01_ribonPD, gv.Spine2);

                    Vector3 syncScale(Transform tr, Transform tr0)
                    {
                        return new Vector3(
                             tr.localScale.x * tr0.lossyScale.x / tr.lossyScale.x,
                             tr.localScale.y * tr0.lossyScale.y / tr.lossyScale.y,
                             tr.localScale.z * tr0.lossyScale.z / tr.lossyScale.z
                         );
                    }
                }

                cg.OnRenderObject();
            }
        }

        public void OnApplicationFocus(bool hasFocus)
        {
            if (keyMacro != null)
                keyMacro.OnApplicationFocus(hasFocus);
        }

        //int frameCountOnRenderObject = 0;
        //private void OnRenderObject()
        //{
        //    if (frameCountOnRenderObject == Time.frameCount)
        //        return;
        //    frameCountOnRenderObject = Time.frameCount;

        //    foreach (var cg in ctrlGirls)
        //    {
        //        FlagNowTgtID = cg.ini.id;

        //        var root = cg.ini.rootObj;
        //        if (!root)
        //            continue;

        //        cg.OnRenderObject();
        //    }
        //}

        const int PRESETS = 25;
        public static xmlinfo cfg = new xmlinfo();

        [Serializable]
        public class xmlinfo
        {
            public KeyCode Hotkey = KeyCode.M;
            public InputEx.ModifierKey Modfkey = InputEx.ModifierKey.Alt;

            public KeyValuePair<string, string[]>[] PresetSlotnames = new KeyValuePair<string, string[]>[]
                {
                };

            // ウインドウ設定
            public bool WinShow_Def = true;
            public bool WinMin_Def = false;
            public bool WinMouseOff_Def = true;
            public bool WinHalf = false;

            public bool WinPin_Def = true;

            public int WinMouseOver_HitCheckHight = 20;
            public int WinMouseOver_HitCheckWidePercent = 70;

            public bool WinMouseOut_NeedMouseEvent = false;

            public int FontSize = 12;

            // プラグイン設定
            public int QualitySetting = -1;

            public bool EnableMensPosectrl {
                get => _EnableMensPosectrl;
                set
                {
                    _EnableMensPosectrl = value;
                    if (_EnableMensPosectrl != value)
                    {
                        _EnableMensPosectrl = value;

                        if (IO_ExSlider.ctrlGirls != null)
                        {
                            foreach (var g in IO_ExSlider.ctrlGirls)
                            {
                                if (g.ini.isGirl())
                                    continue;

                                g.ctrlBone = value ? new CtrlBone() : null;
                                g.rcbFullbody = value ? g.ctrlBone.addCtrl<CtrlBone.RotctrlBone>() : null;
                            }
                        }
                    } 
                }
            }
            bool _EnableMensPosectrl = true;

            public bool FixTableCalendarPos = true; // 前戯の卓上カレンダー位置修正

            public bool ctrlIncludePlayer = true;
            public bool ctrlIncludeShota = true;
            public bool CloneCtrlVisible = true;
            public bool InactiveCharCtrlVisible = true;
            public bool FixFreeModeCustomToTitleBtn = true;
            public bool EnableFuzzyAnimeFiltering = false;

            // 鼻シェイプキー作成
            public bool AddNoseBlendShape = true;

            // エフェクト
            public bool EffectMgr_Enabled { get => EffectMgr.Enabled; set => EffectMgr.Enabled = value; }
            public EffectMgr.Configs EffectConfigs { get => EffectMgr.Users; set => EffectMgr.Users = value; }

            // 基本設定
            public SaveGirlCtrl[] girlCtrls = new SaveGirlCtrl[] { };
            public int MatePropWriteFreq { get => MateProp.WriteFreq; set => MateProp.WriteFreq = value; }

            // ボーン系
            //public SaveEdits edits = new SaveEdits();
            public BoneScales.Edits[] boneScales = new BoneScales.Edits[] {  };
            public bool CharEdits_PosOfsByScene_PlusBGName = false;

            // 視線追従 → girlCtrlsに
            //public float lookAtMaxWeight = 1f;

            public bool useMyEyeLookAt
            {
                get { return MyEyeLookAt.Enabled; }
                set { MyEyeLookAt.Enabled = value; }
            }
            public float myEyeLookAtSpeed
            {
                get { return MyEyeLookAt.Speed; }
                set { MyEyeLookAt.Speed = value; }
            }
            public float myEyeLookAtWeight
            {
                get { return MyEyeLookAt.Weight; }
                set { MyEyeLookAt.Weight = value; }
            }
            public float myEyeLookAtMaxSpeed
            {
                get { return MyEyeLookAt.MaxSpeed; }
                set { MyEyeLookAt.MaxSpeed = value; }
            }
            public bool myEyeLookAt_NoCheckIkWeight
            {
                get { return MyEyeLookAt.NoCheckIkWeight; }
                set { MyEyeLookAt.NoCheckIkWeight = value; }
            }
            public bool myEyeLookAt_InhibitHitomiAnime
            {
                get { return MyEyeLookAt.InhibitHitomiAnime; }
                set { MyEyeLookAt.InhibitHitomiAnime = value; }
            }

            // シェイプキー
            public SaveShapeKeys[] saveShapeKeys = new SaveShapeKeys[] { };

            // マテリアル
            public SaveMaterials[] saveMateProps = new SaveMaterials[] { };


            // Hシーン
            public float HsysYoinTimeIC            
            {
                get { return MyHook.yoinTimeIC; }
                set { MyHook.yoinTimeIC = value; }
            }
            public float HsysYoinTimeFH
            {
                get { return MyHook.yoinTimeFH; }
                set { MyHook.yoinTimeFH = value; }
            }
            public bool HsysYoinOnlyClimax
            {
                get { return MyHook.yoinOnlyClimax; }
                set { MyHook.yoinOnlyClimax = value; }
            }
            public HsysMgr.HsysMgrConfig HSysMgrConfig { get => HsysMgr.Config; set => HsysMgr.Config = value; }

            public PositionByStates BGPositionByStates { get => BGMGR.posByStates; set => BGMGR.posByStates = value; }

            // ボイス
            public bool voicePichEnabled { get => SysVoiceMgr.Enabled; set { SysVoiceMgr.Enabled = value; } }
            public float[] voicePich { get => SysVoiceMgr.voicePich; set { SysVoiceMgr.voicePich = value; } }


            public xmlinfo()
            {
            }
        }

        //[Serializable]
        //public class SaveEdits
        //{
        //    public SaveEdits()
        //    {
        //    }

        //    BoneScales.Edits boneScalesNeko;
        //    BoneScales.Edits boneScalesUsa;

        //    public void Load(ref SaveEdits edits)
        //    {
        //        boneScalesNeko.setupScales();
        //        boneScalesUsa.setupScales();
        //        edits = this;
        //    }
        //}

        //slot
        static int saveToFileNum = 1;

        static string fileXml_ = null;
        public static string resolveSavePath(string fname)
        {
            string path;
            string s = BepInEx.Paths.PluginPath;
            //string s = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);


            if (s.ToLower().Contains(@"bepinex"))
            {
                path = s + @"\Config\" + fname;
            }
            else if (s.ToLower().Contains(@"sybaris\unityinjector"))
            {
                path = s + @"\Config\" + fname;
            }
            else if (s.ToLower().Contains(@"patches\unityinjector"))
            {
                path = s + @"\Config\" + fname;
            }
            else
            {
                path = @"UnityInjector\Config\" + fname;
            }

#if !USING_XML
            fileXml_ = path.Replace(".xml", ".json");
#endif
            return path;
        }


        public static string fileXml
        {
            get
            {
                if (fileXml_ == null)
                {
                    fileXml_ = resolveSavePath($"{SaveFileName}.xml");
                }

                if (saveToFileNum >= 2)
                    return fileXml_.Replace(".xml", "_" + saveToFileNum.ToString() + ".xml");
#if !USING_XML
                fileXml_ = fileXml_.Replace(".xml", ".json");
#endif
                return fileXml_;
            }
        }

        //XMLファイル読み書き
        public static bool XML_Load(ref xmlinfo objOtXML)
        {
            //保存先
            //string fileXml = @"UnityInjector\Config\HotKeyFilter.xml";

            try
            {
#if USING_XML
                //XmlSerializerオブジェクト
                System.Xml.Serialization.XmlSerializer serializer =
                        new System.Xml.Serialization.XmlSerializer(typeof(xmlinfo));
#endif

                if (!System.IO.File.Exists(fileXml))
                { //無かったら作る
                  //インスタンス作成
                    objOtXML = new xmlinfo();

                    try
                    {
                        string dir = System.IO.Path.GetDirectoryName(fileXml);
                        if (!System.IO.Directory.Exists(dir))
                            System.IO.Directory.CreateDirectory(dir);
#if USING_XML
                        //書き込むファイルを開く（UTF-8 BOM無し）
                        System.IO.StreamWriter sw = new System.IO.StreamWriter(
                                    fileXml, false, new System.Text.UTF8Encoding(false));
                        try
                        {
                            //シリアル化し、XMLファイルに保存する
                            serializer.Serialize(sw, objOtXML);
                        }
                        catch (Exception e)
                        {
                            if (sw != null)
                                sw.Close();
                            throw e;
                        }
                        //ファイルを閉じる
                        sw.Close();
#else
                        string jsonText = JsonUtility.ToJson(objOtXML, true).Replace("\n", "\r\n");
                        using (StreamWriter writer = new StreamWriter(fileXml, false, new System.Text.UTF8Encoding(false)))
                        {
                            writer.WriteLine(jsonText);
                            writer.Close();
                        }
#endif
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.Log("Plg_XML_Write Error:" + e);
                    }
                }
                else if (System.IO.File.Exists(fileXml))
                { //あったら読み込み
#if USING_XML
                    System.IO.StreamReader sr = new System.IO.StreamReader(
                        fileXml, new System.Text.UTF8Encoding(false));
                    //XMLファイルから読み込み、逆シリアル化する
                    objOtXML = (xmlinfo)serializer.Deserialize(sr);
                    //ファイルを閉じる
                    sr.Close();
#else
                    using (StreamReader reader = new StreamReader(fileXml, new System.Text.UTF8Encoding(false)))
                    {
                        objOtXML = JsonUtility.FromJson<xmlinfo>(reader.ReadToEnd());
                        reader.Close();
                    }
#endif
                }

            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log("Plg_XML_Load Error:" + e);
                objOtXML = new xmlinfo(); //リセット
            }

            WinMouseOff = cfg.WinMouseOff_Def;
            if (WinMouseOff)
                GuiFlag = false;

            WinPin = cfg.WinPin_Def;


            if (cfg.girlCtrls == null)
                cfg.girlCtrls = new SaveGirlCtrl[] { };

            if (cfg.boneScales == null)
                cfg.boneScales = new BoneScales.Edits[] { };

            if (cfg.saveShapeKeys == null)
                cfg.saveShapeKeys = new SaveShapeKeys[] { };

            if (cfg.saveMateProps == null)
                cfg.saveMateProps = new SaveMaterials[] { };

            while (cfg.girlCtrls.Length < ctrlGirls.Count)
            {
                cfg.girlCtrls = cfg.girlCtrls.AddItem(new SaveGirlCtrl()).ToArray();
            }

            while (cfg.boneScales.Length < ctrlGirls.Count)
            {
                cfg.boneScales = cfg.boneScales.AddItem(new BoneScales.Edits()).ToArray();
            }

            while (cfg.saveShapeKeys.Length < ctrlGirls.Count)
            {
                cfg.saveShapeKeys = cfg.saveShapeKeys.AddItem(new SaveShapeKeys()).ToArray();
            }

            while (cfg.saveMateProps.Length < ctrlGirls.Count)
            {
                cfg.saveMateProps = cfg.saveMateProps.AddItem(new SaveMaterials()).ToArray();
            }

            // 設定反映
            //cfg.edits.setScales();
            for (int i = 0; i < ctrlGirls.Count; i++)
            {
                FlagNowTgtID = ctrlGirls[i].ini.id;

                ctrlGirls[i].boneScales.LoadEdits(cfg.boneScales[i]);
                cfg.girlCtrls[i].Load(ctrlGirls[i]);

                if (ctrlGirls[i].ini.isGirl())
                {
                    // プレーヤー以外
                    cfg.saveShapeKeys[i].Load(ctrlGirls[i].shapeKeys);
                }

                if (cfg.saveMateProps[i] != null && ctrlGirls[i].mateProp != null)
                    cfg.saveMateProps[i].Load(ref ctrlGirls[i].mateProp);
            }

            // ゲーム設定
            SetSysCnfQuality(cfg.QualitySetting);

            return true;
        }

        public static bool XML_Save(xmlinfo objOtXML)
        {
            //保存先
            //マウスオーバー設定の更新
            if (cfg.WinMouseOff_Def != WinMouseOff)
            {
                cfg.WinMouseOff_Def = WinMouseOff;
                //cfg.WinShow_Def = WinMouseOff;
            }
            //ピン設定
            cfg.WinPin_Def = WinPin;
            //cfg.edits.setScales();

            // 設定保存
            cfg.boneScales = new BoneScales.Edits[ctrlGirls.Count];
            cfg.girlCtrls = new SaveGirlCtrl[ctrlGirls.Count];
            cfg.saveShapeKeys = new SaveShapeKeys[ctrlGirls.Count];
            cfg.saveMateProps = new SaveMaterials[ctrlGirls.Count];

            for (int i = 0; i < ctrlGirls.Count; i++)
            {
                FlagNowTgtID = ctrlGirls[i].ini.id;

                ctrlGirls[i].boneScales.PreSaveEdits();

                cfg.boneScales[i] = new BoneScales.Edits();
                cfg.boneScales[i] = ctrlGirls[i].boneScales.edits;

                cfg.girlCtrls[i] = new SaveGirlCtrl();
                cfg.girlCtrls[i].Save(ctrlGirls[i]);

                if (ctrlGirls[i].ini.isGirl())
                {
                    // プレーヤー以外
                    cfg.saveShapeKeys[i] = new SaveShapeKeys(ctrlGirls[i].shapeKeys.edits);
                }
                else
                    cfg.saveShapeKeys[i] = new SaveShapeKeys();

                if (ctrlGirls[i].mateProp != null)
                    cfg.saveMateProps[i] = new SaveMaterials(ctrlGirls[i].mateProp);
                else
                    cfg.saveMateProps[i] = new SaveMaterials();
            }

            try
            {
#if USING_XML
                //XmlSerializerオブジェクト
                System.Xml.Serialization.XmlSerializer serializer =
                        new System.Xml.Serialization.XmlSerializer(typeof(xmlinfo));
#endif
                try
                {
                    string dir = System.IO.Path.GetDirectoryName(fileXml);
                    if (!System.IO.Directory.Exists(dir))
                        System.IO.Directory.CreateDirectory(dir);
#if USING_XML
                    //書き込むファイルを開く（UTF-8 BOM無し）
                    System.IO.StreamWriter sw = new System.IO.StreamWriter(
                                fileXml, false, new System.Text.UTF8Encoding(false));
                    //シリアル化し、XMLファイルに保存する
                    serializer.Serialize(sw, objOtXML);
                    //ファイルを閉じる
                    sw.Close();
#else
                    string jsonText = JsonUtility.ToJson(objOtXML, true).Replace("\n", "\r\n");
                    using (StreamWriter writer = new StreamWriter(fileXml, false, new System.Text.UTF8Encoding(false)))
                    {
                        writer.WriteLine(jsonText);
                        writer.Close();
                    }
#endif
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.Log("Plg_XML_Write Error:" + e);
                }

            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log("Plg_XML_Save Error:" + e);
            }

            return true;
        }

        public static class MyXML
        {
            static Dictionary<string, bool> dicXmlExistsOnce = new Dictionary<string, bool>();

            public static bool ExistsOnce(string fname, bool fullPath = false)
            {
                if (!fullPath)
                    fname = resolveSavePath(fname);


                if (dicXmlExistsOnce.TryGetValue(fname, out bool val))
                {
                    return val;
                }
                return dicXmlExistsOnce[fname] = File.Exists(fname);
            }

            public static void ClearExistsOnce()
            {
                dicXmlExistsOnce.Clear();
            }


            public class SaveDicStrV3
            {
                public KeyValuePair<string, Vector3>[] data;

                public SaveDicStrV3()
                {

                    data = new KeyValuePair<string, Vector3>[0];
                }

                public SaveDicStrV3(Dictionary<string, Vector3> dic)
                {
                    Save(dic);
                }

                public void Save(Dictionary<string, Vector3> dic)
                {
                    data = dic.ToArray();
                }

                public void Load(ref Dictionary<string, Vector3> tgt)
                {
                    tgt = data.ToDictionary(n => n.Key, n => n.Value);
                }

                public Dictionary<string, Vector3> toDic()
                {
                    Dictionary<string, Vector3> dic = new Dictionary<string, Vector3>();

                    foreach (var v in data)
                    {
                        dic[v.Key] = v.Value;
                    }
                    return dic;
                }
            }
            public static bool XML_Save<t>(t objOtXML, string fname, bool fullPath = false)
            {
                StreamWriter sw = null;
                try
                {
                    //XmlSerializerオブジェクト
                    System.Xml.Serialization.XmlSerializer serializer =
                            new System.Xml.Serialization.XmlSerializer(typeof(t));

                    if (!fullPath)
                        fname = resolveSavePath(fname);

                    //書き込むファイルを開く（UTF-8 BOM無し）
                    sw = new System.IO.StreamWriter(
                                fname, false, new System.Text.UTF8Encoding(false));
                    //シリアル化し、XMLファイルに保存する
                    serializer.Serialize(sw, objOtXML);
                    //ファイルを閉じる
                    sw.Close();

                    Debug.Log("Save完了: " + fname);
                }
                catch (Exception e)
                {
                    if (sw != null)
                        sw.Close();

                    UnityEngine.Debug.Log("Ktm_XML_Save Error:" + e);
                    return false;
                }

                MyXML.ClearExistsOnce();
                return true;
            }

            public static bool XML_Exists(string fname, bool fullPath = false)
            {
                if (!fullPath)
                    fname = resolveSavePath(fname);

                return System.IO.File.Exists(fname);
            }

            public static bool XML_Load<t>(out t objOtXML, string fname, bool fullPath = false)
            {
                objOtXML = default(t);
                StreamReader sr = null;

                try
                {
                    if (!fullPath)
                        fname = resolveSavePath(fname);

                    //XmlSerializerオブジェクト
                    System.Xml.Serialization.XmlSerializer serializer =
                            new System.Xml.Serialization.XmlSerializer(typeof(t));

                    if (!System.IO.File.Exists(fname))
                    { //無かったら
                        return false;
                    }
                    else
                    { //あったら読み込み
                        sr = new System.IO.StreamReader(
                            fname, new System.Text.UTF8Encoding(false));
                        //XMLファイルから読み込み、逆シリアル化する
                        objOtXML = (t)serializer.Deserialize(sr);
                        //ファイルを閉じる
                        sr.Close();
                        sr = null;

                        Debug.Log("Load完了: " + fname);
                    }
                }
                catch (Exception e)
                {
                    if (sr != null)
                        sr.Close();

                    UnityEngine.Debug.Log("Ktm_XML_Load Error:" + e);
                    return false;
                }
                return true;
            }
        }

        public class xmlNote
        {
            public string note = "";
            public xmlNote() { }

            static Dictionary<string, xmlNote> dicReadNote = new Dictionary<string, xmlNote>();

            public static void ClearNoteCache()
            {
                dicReadNote.Clear();
            }

            public static void SaveNote(xmlNote xnote, string fname)
            {
                XML_Save<xmlNote>(xnote, fname + ".note");
                ClearNoteCache();
            }

            public static xmlNote ReadNote(string fname)
            {
                xmlNote xnote;
                if (dicReadNote.TryGetValue(fname, out xnote))
                {
                    return xnote;
                }

                var path = fname + ".note";

                if (XML_Exists(fname))
                {
                    if (XML_Exists(path))
                    {
                        XML_Load<xmlNote>(out xnote, path);
                    }
                    else
                    {
                        xnote = new xmlNote();
                        // 更新日の場合は末尾に改行でフラグする
                        xnote.note = File.GetLastWriteTime(resolveSavePath(fname)).ToString("(yyyy/MM/dd  HH:mm:ss)\n");
                    }
                }
                else
                    xnote = null;

                dicReadNote[fname] = xnote;
                return xnote;

            }
        }
    }

    class RectU
    {
        public static float _Scale = 1f;
        public Rect rc { get; private set; }

        public RectU(Rect rc0)
        {
            rc = new Rect(rc0.x * _Scale, rc0.y * _Scale, rc0.width * _Scale, rc0.height * _Scale);
        }

        public RectU(float x, float y, float width, float height)
        {
            rc = new Rect(x * _Scale, y * _Scale, width * _Scale, height * _Scale);
        }

        public static implicit operator Rect(RectU ru)
        {
            return ru.rc;
        }

        public static float Scaling(float f)
        {
            return f * _Scale;
        }

        public static Rect rc_;
        public static Rect sclRect(float x, float y, float width, float height)
        {
            rc_.x = x * _Scale;
            rc_.y = y * _Scale;
            rc_.width = width * _Scale;
            rc_.height = height * _Scale;
            return rc_;
        }

        public static Rect sclRect(Rect r)
        {
            r.x *= _Scale;
            r.y *= _Scale;
            r.width *= _Scale;
            r.height *= _Scale;
            return r;
        }
    }

    class GuiBox
    {
        static Dictionary<string, Rect> boxPos = new Dictionary<string, Rect>();

        public static void BoxStart(string strId, float x, float y, float width)
        {
            Rect rect;
            if (boxPos.ContainsKey(strId))
            {
                rect = boxPos[strId];
            }
            else
            {
                rect = Rect.zero;
            }

            rect.x = x;
            rect.y = y;
            rect.width = width;
            boxPos[strId] = rect;

            Rect box = RectU.sclRect(rect);
            GUI.Box(box, "");
        }

        public static void BoxEnd(string strId, float end_y)
        {
            Rect rect;
            if (!boxPos.ContainsKey(strId))
            {
                return;
            }

            rect = boxPos[strId];
            rect.height = end_y - rect.y;
            boxPos[strId] = rect;

            //Rect box = RectU.sclRect(rect);
            //GUI.Box(box, "");
            //boxPos.Remove(strId);
        }
    }

    class ListBox
    {
        static Dictionary<string, Vector2> scrollPositions = new Dictionary<string, Vector2>();
        static Dictionary<string, int> selGridInts = new Dictionary<string, int>();
        public static int ListBoxGui(string strId, Rect rect_btn, int rows, string[] slist, GUIStyle gsList)
        {
            float item_height = rect_btn.height;
            float item_width = rect_btn.width - 16;

            Rect box = RectU.sclRect(rect_btn.x, rect_btn.y, item_width, item_height * rows);
            GUI.Box(box, "");

            if (!scrollPositions.ContainsKey(strId))
            {
                scrollPositions.Add(strId, Vector2.zero);
                selGridInts.Add(strId, -1);
            }

            box = RectU.sclRect(rect_btn.x, rect_btn.y, rect_btn.width, item_height * rows);

            scrollPositions[strId] = GUI.BeginScrollView(box, scrollPositions[strId],
                RectU.sclRect(0, 0, item_width, item_height * slist.Length), false, true);

            var old = selGridInts[strId];
            try
            {
                selGridInts[strId] = GUI.SelectionGrid(RectU.sclRect(0, 0, item_width, item_height * slist.Length),
                    selGridInts[strId], slist, 1, gsList);
            }
            finally
            {
                GUI.EndScrollView();
            }

            return selGridInts[strId] == old ? -1 : selGridInts[strId];
        }
    }

}

public static class Extentions
{
    public static object GetNonPublicField(this object obj, string name)
    {
        return obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(obj);
    }
    public static T GetNonPublicField<T>(this object obj, string name)
    {
        return (T)obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(obj);
    }

    public static FieldInfo GetNonPublicFieldInfo(this object obj, string name)
    {
        return obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
    }

    public static void SetNonPublicField(this object obj, string name, object value)
    {
        obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).SetValue(obj, value);
    }

    public static void InvokeNonPublicMethod(this object obj, string name, object[] values)
    {
        obj.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Invoke(obj, values);
    }

    public static MethodInfo GetNonPublicMethod(this object obj, string name)
    {
        return obj.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
    }

    public static T InvokeNonPublicMethod<T>(this object obj, string name, object[] values)
    {
        return (T)obj.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Invoke(obj, values);
    }

    public static void ActionNonPublicMethod<T>(this T obj, string name, object[] values)
    {
        ((T)obj).GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Invoke(((T)obj), values);
    }

    public static int CountChar(this string str, char c)
    {
        int count = 0;

        for(int i = 0; i < str.Length; i += 1)
        {
            i = str.IndexOf(c, i);
            if (i < 0)
                break;

            count++;
        }

        return count;
    }

    public static void CacheClear()
    {
        dicFindDeepSp.Clear();
        dicFindSp.Clear();
        dicFindSp2.Clear();
        dicGameObject_FindSp.Clear();
    }

    /*static Dictionary<string, string> dicTsAniEarR = new Dictionary<string, string>
    {
        { "aniEar_R01/", "aniEar_R01_02/" },
        { "aniEar_R02/", "aniEar_R02_02/" },
        { "aniEar_R03/", "aniEar_R03_02/" },

        { "aniEar_R11/", "aniEar_R11_02/" },
        { "aniEar_R12/", "aniEar_R12_02/" },
        { "aniEar_R13/", "aniEar_R13_02/" },

        { "aniEar_R01_01/", "aniEar_L01/" },
        { "aniEar_R02_01/", "aniEar_L02/" },
        { "aniEar_Rend_01/", "aniEar_Lend/" },

        { "aniEar_R11_01/", "aniEar_R11/" },
        { "aniEar_R12_01/", "aniEar_R12/" },
        { "aniEar_R13end_01/", "aniEar_R13end/" },
    };
    
    static Dictionary<string, string> dicTsAniEarL = new Dictionary<string, string>
    {
        { "aniEar_L01/", "aniEar_L01_02/" },
        { "aniEar_L02/", "aniEar_L02_02/" },
        { "aniEar_L03/", "aniEar_L03_02/" },

        { "aniEar_L11/", "aniEar_L11_02/" },
        { "aniEar_L12/", "aniEar_L12_02/" },
        { "aniEar_L13/", "aniEar_L13_02/" },

        { "aniEar_L01_01/", "aniEar_L01/" },
        { "aniEar_L02_01/", "aniEar_L02/" },
        { "aniEar_Lend_01/", "aniEar_Lend/" },

        { "aniEar_L11_01/", "aniEar_L11/" },
        { "aniEar_L12_01/", "aniEar_L12/" },
        { "aniEar_L13end_01/", "aniEar_L13end/" },
    };*/

    // フリーHでうさのボーン名がねこと共通化される問題対応
    public static void TransNameUsaBonesFH(ref string name)
    {
        //if (BepInEx.IO_ExSlider.Plugin.IO_ExSlider.FlagUsaFH)
        if (IO_ExSlider.FlagNowTgtID == "Usa") {
            //Console.Write(name);

            TransNameUsaBonesFH_AniEar(ref name);
            return;

            name = name.Replace("_02/", "/");
            if (name.EndsWith("_02", StringComparison.Ordinal))
            {
                name = name.Substring(0, name.Length - 3);
            }
            //Console.WriteLine(" => " + name);
        }
    }

    public static void TransNameUsaBonesFH_AniEar(ref string path)
    {
        var names = path.Split('/');

        for(int i=0; i<names.Length; i++)
        {
            names[i] = TransNameUsaBonesFH_Name(names[i]);   
        }

        path = string.Join("/", names);
    }

    public static string TransNameUsaBonesFH_Name(string name)
    {
        if (name.StartsWith("BP01_aniEar", StringComparison.Ordinal))
        {
            if (name.EndsWith("_01", StringComparison.Ordinal))
                name = name.Substring(0, name.Length - 3);
            return name;
        }

        if (name.StartsWith("BP01_tail", StringComparison.Ordinal))
        {
            return name;
        }

        if (name.EndsWith("_02", StringComparison.Ordinal))
        {
            name = name.Substring(0, name.Length - 3);
        }
        return name;
    }

    public static void RevTransNameUsaBonesFH(ref string name)
    {
        if (IO_ExSlider.FlagUsaFH && IO_ExSlider.FlagNowTgtID == "Usa")
        {
            //if (name.StartsWith("BP01_aniEar", StringComparison.Ordinal))
            //{
            //    if (!name.EndsWith("_02", StringComparison.Ordinal))
            //        name = name + "_01";
            //    return;
            //}

            //Console.Write(name);
            if (!name.EndsWith("(Clone)", StringComparison.Ordinal)) // Particle
                name = name + "_02";
        }
    }

    static Dictionary<string, Transform> dicFindSp = new Dictionary<string, Transform>();
    static Dictionary<int, Dictionary<string, Transform>> dicFindSp2 = new Dictionary<int, Dictionary<string, Transform>>();

    public const string FIND_PATH_CURRENT_TR = @"."; // FindはFind("")がカレントパスだけど使いにくいので… 

    // 毎フレームラフに呼ぶ場合
    public static Transform FindSp(this Transform t, string name)
    {
        return t.FindSp(name, false);
    }

    // 初期化などで信頼性が必要な場合（頻度が低すぎるのは通常のFindを使うこと）
    public static Transform FindSp(this Transform t, string name, bool retry)
    {
        if (name == FIND_PATH_CURRENT_TR || name == string.Empty)
            return t;

        // 階層UPを許可
        while (name.StartsWith("../", StringComparison.Ordinal))
        {
            t = t.parent;
            name = name.Substring(3);
        }

        if (IO_ExSlider.FlagUsaFH)
            TransNameUsaBonesFH(ref name);

#if DEBUG
        if (Input.GetKey(KeyCode.Y))
            t.Find(name);
#endif

#if DEVMODE2
        if (!Input.GetKey(KeyCode.U))
#else
        if (true)
#endif
        {
            // パフォーマンス重視2(FPSで1上がるかどうか程度,リトライが増えると不利で微妙)
            var key = t.GetInstanceID();

            Dictionary<string, Transform> subDic;
            if (!dicFindSp2.TryGetValue(key, out subDic))
            {
                subDic = dicFindSp2[key] = new Dictionary<string, Transform>();
            }

            var hit = subDic.TryGetValue(name, out Transform val);
            if (hit && val)
                return val;

            if (!retry)
            {
                // カスメと違って動的にアイテムが追加されることはまずない＆こっちのFindは非アクティブで見つからないということもない
                if (hit && UnityEngine.Random.Range(0, 10) > 1) // 前回なかったら確率8/10で諦める
                    return val;
            }

            var tr = t.Find(name);
            if (tr)
                dicFindSp2[key][name] = tr;

            return tr;
        }
        else
        {

            var key = $"{t.GetInstanceID()}:{name}";

#if true
            // パフォーマンス重視
            var hit = dicFindSp.TryGetValue(key, out Transform val);
            if (hit && val)
                return val;

            if (!retry)
            {
                // カスメと違って動的にアイテムが追加されることはまずない＆こっちのFindは非アクティブで見つからないということもない
                //if (hit && UnityEngine.Random.Range(0, 9) >= 3) // 前回なかったら確率2/3で諦める
                if (hit && UnityEngine.Random.Range(0, 10) > 1) // 前回なかったら確率8/10で諦める
                    return val;
            }
#else
        if (dicFindSp.TryGetValue(key, out Transform val) && val)
            return val;
#endif
            var tr = t.Find(name);
            if (tr)
                dicFindSp[key] = tr;

            return tr;
        }
    }


    static Dictionary<string, Transform> dicFindDeepSp = new Dictionary<string, Transform>();

    public static Transform FindDeepSp(this GameObject t, string name, bool inactive = false)
    {
#if DEBUG
        if (Input.GetKey(KeyCode.Y))
            t.FindDeep(name);
#endif
        var key = t.GetInstanceID() + ":" + name;

        if (dicFindDeepSp.TryGetValue(key, out Transform val) && val)
            return val;

        var tr = t.FindDeep(name, inactive);
        if (tr)
            dicFindDeepSp[key] = tr.transform;

        return tr ? tr.transform : null;
    }   

    static Dictionary<string, GameObject> dicGameObject_FindSp = new Dictionary<string, GameObject>();

    public static GameObject FindSp(string name, bool NoCheckActive = false, bool NeedRetry = false)
    {
        var key = name;
        bool hit = false;
        GameObject go;

        if (IO_ExSlider.FlagUsaFH)
            TransNameUsaBonesFH(ref name);

#if DEBUG
        if (Input.GetKey(KeyCode.Y))
            GameObject.Find(name);
#endif
        hit = dicGameObject_FindSp.TryGetValue(key, out go);
        if (go)
        {
            if (!NoCheckActive)
            {
                if (!go.activeInHierarchy) // GameObject.FindはActiveのみ
                    return null;
            }
        }
        else if (!hit || NeedRetry || (UnityEngine.Random.Range(0,10) < 2))//Time.frameCount % 5 == 1) // 初回以外は5回に1度再チャレンジ
        {
            go = GameObject.Find(name);
            if (!hit || go)
                dicGameObject_FindSp[key] = go;
        }
        return go;
    }

    public static Vector3 add(this Vector3 v, float f)
    {
        return new Vector3(v.x + f, v.y + f, v.z + f);
    }

    public static Vector3 angle180(this Vector3 v)
    {
        // 0~360 -> -180~180
        v.x = Mathf.DeltaAngle(0f, v.x);
        v.y = Mathf.DeltaAngle(0f, v.y);
        v.z = Mathf.DeltaAngle(0f, v.z);
        return v;
    }

    public static bool HasFlag(this Enum flags, Enum check)
    {
        if (flags.GetType() != check.GetType())
        {
            throw new ArgumentException("Enum型が違います");
        }

        var i0 = Convert.ToUInt64(flags);
        var i1 = Convert.ToUInt64(check);

        return (i0 & i1) == i1;
    }

    public static bool HasFlag(this Enum flags, UInt32 check)
    {
        var i0 = Convert.ToUInt32(flags);
        var i1 = check;

        return (i0 & i1) == i1;
    }

    public static void ForEach<T>(this T[] ts, Action<T> action)
    {
        Array.ForEach<T>(ts, action);
    }

    public static void ForEach<T, T2>(this Dictionary<T, T2> ts, Action<T, T2> action)
    {
        foreach (var p in ts)
        {
            action(p.Key, p.Value);
        }
    }
}


//#pragma warning disable 0420
namespace Hook.nnPlugin.Managed
{
    public static class MyHook
    {
        // テスト用
        public static bool fixAnimePlayIssue = true;
        public static bool hookLog = false;

        // 制御用
        public static bool UnHookMecanim = false;
        public static bool UnHookAnimator = false;
        static bool onHook = false;
        
        // ステータス
        public static float yoinX = 1f;
        public static float yoinTimeIC = 0f;
        public static float yoinTimeFH = 0f;
        public static bool yoinOnlyClimax = true;
        public static string NextSceneName = "";

        public static Dictionary<string, Dictionary<int, string>> LastAnimeStateByGameObj = new Dictionary<string, Dictionary<int, string>>();

        public static IC_Mecanim ic_Mecanim;
        public static bool flagH01;

        public static void InstallHooks()
        {
            Harmony.CreateAndPatchAll(typeof(MyHook));
        }

        [System.Diagnostics.Conditional("DEBUG")]
        static void DebugLog(string name)
        {
            try
            {
                //一つ前のスタックを取得
                System.Diagnostics.StackFrame callerFrame = new System.Diagnostics.StackFrame(3);
                //メソッド名
                string methodName = callerFrame.GetMethod().Name;
                //クラス名
                string className = callerFrame.GetMethod().ReflectedType.FullName;

                Console.WriteLine("  呼び出し元：" + className + " / " + methodName + " / " + name);

                //一つ前のスタックを取得
                callerFrame = new System.Diagnostics.StackFrame(4);
                //メソッド名
                methodName = callerFrame.GetMethod().Name;
                //クラス名
                className = callerFrame.GetMethod().ReflectedType.FullName;

                Console.WriteLine("  呼び出し元-1：" + className + " / " + methodName + " / " + name);
            }
            catch (Exception e)
            {
                Console.WriteLine("例外：" + e);
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        static void DebugLog2(string key)
        {
            try
            {
                //一つ前のスタックを取得
                System.Diagnostics.StackFrame callerFrame = new System.Diagnostics.StackFrame(1);
                //メソッド名
                string methodName = callerFrame.GetMethod().Name;
                //クラス名
                string className = callerFrame.GetMethod().ReflectedType.FullName;
                Console.WriteLine("  呼び出し元-1：" + className + " / " + methodName + " / " + key);

                //一つ前のスタックを取得
                callerFrame = new System.Diagnostics.StackFrame(2);
                //メソッド名
                methodName = callerFrame.GetMethod().Name;
                //クラス名
                className = callerFrame.GetMethod().ReflectedType.FullName;

                Console.WriteLine("  呼び出し元-2：" + className + " / " + methodName + " / " + key);

                //一つ前のスタックを取得
                callerFrame = new System.Diagnostics.StackFrame(3);
                //メソッド名
                methodName = callerFrame.GetMethod().Name;
                //クラス名
                className = callerFrame.GetMethod().ReflectedType.FullName;

                Console.WriteLine( "  呼び出し元-3：" + className + " / " + methodName + " / " + key);
            }
            catch (Exception e)
            {
                Console.WriteLine("  例外：" + e);
            }
        }

        /*
        // UnityEngine.Input
        [HarmonyPostfix, HarmonyPatch(typeof(UnityEngine.Input), "GetKeyUp", new[] { typeof(KeyCode) }, null)]
        public static void GetKeyUp_ExRet(ref bool __result, KeyCode key)
        {
            if (key == KeyCode.RightArrow)
                DebugLog(key);
        }
        */

        //[HarmonyPrefix, HarmonyPatch(typeof(IC_AnimeController), "Fade_pub", new[] { typeof(string), typeof(int), typeof(int), typeof(float) }, null)]
        //public static bool Prefix_Fade_pub(string Name, int Layer, int UIID, float FadeTime)
        //{
        //    if (Name == "H01")
        //        flagH01 = true;
        //    else
        //        flagH01 = false;

        //    return true;
        //}

        // フリーモード時のカスタム、サイドメニューからタイトルに戻るときの挙動修正
        [HarmonyPrefix, HarmonyPatch(typeof(CustomSetting), "CustomExitBt", new[] { typeof(bool) }, null)]
        public static bool Prefix_CustomSetting_CustomExitBt(bool x)
        {
            Console.WriteLine("CustomSetting_CustomExitBt mini="+ GameClass.CustomMiniTitleBt);
            if (IO_ExSlider.cfg.FixFreeModeCustomToTitleBtn && GameClass.FreeMode && GameClass.CustomMiniTitleBt)
            {
                // サイドメニューからタイトルに戻るとき
                var go = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(o => o.name == "UI Root(CustomExit)");
                if (go)
                {
                    go.SetActive(true);
                    return false;
                }
                else
                {
                    Console.WriteLine("UI Root(CustomExit)が見つかりません");
                }
            }
            return true;
        }

        // プリセット制御
        [HarmonyPostfix, HarmonyPatch(typeof(CustomSetting), "SlotYesBt")]
        public static void Postfix_CustomSetting_SlotYesBt(CustomSetting __instance)
        {
            int bid = (int)_fiSaveOrLoadorReset.GetValue(__instance);
            string cid = _fiCostumeID.GetValue(__instance) as string;
            int slot = (int)_fiSlotNo.GetValue(__instance);

            if (string.IsNullOrEmpty(cid))
                return;

            var path = Path.GetDirectoryName(Path.GetDirectoryName(Application.dataPath));
            path = Path.Combine(path, $"Save/CustomData/[CH{cid}SLOT_{slot+1:00}]IoCustomData.ExPreset.xml");
            var pngpath = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Application.dataPath)), $"Save/CustomData/[CH{cid}SLOT_{slot + 1:00}]IoCustomData.png");

            int cindex = 0; // neko
            if (cid == "02")
                cindex = 1; // usa

            switch (bid)
            {
                case 0: // save
                    //XML_Save(new IO_ExSlider.SaveGirlExPresets(IO_ExSlider.CtrlGirlsInst[cindex]), path, true);

                    SaveDataUtil.StartCreateExPresetPNG(pngpath, new IO_ExSlider.SaveGirlExPresets(IO_ExSlider.CtrlGirlsInst[cindex]));
                    break;

                case 1: // load
                    //if (XML_Load(out IO_ExSlider.SaveGirlExPresets preset, path, true))
                    //{
                    //    preset.Load(IO_ExSlider.CtrlGirlsInst[cindex]);
                    //}

                    if (SaveDataUtil.ReadExPresetPNG(pngpath, out IO_ExSlider.SaveGirlExPresets preset, false))
                    {
                        preset.Load(IO_ExSlider.CtrlGirlsInst[cindex]);
                    }
                    break;

                default:
                    return;
            }
        }
        static FieldInfo _fiSaveOrLoadorReset = typeof(CustomSetting).GetField("SaveOrLoadorReset", BindingFlags.NonPublic | BindingFlags.Instance);
        static FieldInfo _fiSlotNo = typeof(CustomSetting).GetField("SlotNo", BindingFlags.NonPublic | BindingFlags.Instance);
        static FieldInfo _fiCostumeID = typeof(CustomSetting).GetField("CostumeID", BindingFlags.NonPublic | BindingFlags.Instance);

        // 次シーン名フック
        //private IEnumerator TransScene(string scene, float interval)
        [HarmonyPrefix, HarmonyPatch(typeof(FadeManager_GUI), "TransScene", new[] { typeof(string), typeof(float) }, null)]
        public static bool Prefix_FadeManager_GUI_TransScene(string scene, float interval)
        {
            NextSceneName = scene;
            return true;
        }

        // プレイヤーAwakeフック
        public static bool DISABLE_CLONE_AWAKE = false;
        [HarmonyPrefix, HarmonyPatch(typeof(CostumeSetUp_PC), "Awake")]
        public static bool Prefix_CostumeSetUp_PC_Awake(CostumeSetUp_PC __instance)
        {
            if (DISABLE_CLONE_AWAKE)
            {
                __instance.enabled = false;
                return false;
            }
            return true;
        }
        [HarmonyPrefix, HarmonyPatch(typeof(SeiekiPathController), "Awake")]
        public static bool Prefix_SeiekiPathController_Awake(SeiekiPathController __instance)
        {
            if (DISABLE_CLONE_AWAKE)
            {
                __instance.enabled = false;
                return false;
            }
            return true;
        }
        [HarmonyPrefix, HarmonyPatch(typeof(DanmenMecanm), "Awake")]
        public static bool Prefix_DanmenMecanm_Awake()
        {
            if (DISABLE_CLONE_AWAKE)
            {
                return false;
            }
            return true;
        }
        [HarmonyPrefix, HarmonyPatch(typeof(DanmenMecanm), "OnStateUpdate", new[] { typeof(Animator), typeof(AnimatorStateInfo), typeof(int) }, null)]
        public static bool Prefix_DanmenMecanm_OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // クローンのステートは処理させない
            if (CloneCtrl.CloneFilter_OnAnimatorHook(animator))
                return false;

            return true;
        }

        // アニメ制御フック
        [HarmonyPrefix, HarmonyPatch(typeof(IC_Mecanim), "OnStateEnter", new[] { typeof(Animator), typeof(AnimatorStateInfo), typeof(int) }, null)]
        public static bool Prefix_IC_Mecanim_OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (Free3P.SubGirlFilter_OnAnimatorHook(animator))
                return false;

            // クローンのステートは処理させない
            if (CloneCtrl.CloneFilter_OnAnimatorHook(animator))
                return false;

            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(IC_Mecanim), "OnStateUpdate", new[] { typeof(Animator), typeof(AnimatorStateInfo), typeof(int) }, null)]
        public static bool Prefix_IC_Mecanim_OnStateUpdate(/*IC_Mecanim __instance,*/ Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // ICはプレイヤーのステートで遷移するので不要
            //if (Free3P.SubGirlFilter_IC_Mecanim_OnStateUpdate(animator, stateInfo))
            //return false; // 元の関数が呼ばれない

            // クローンのステートは処理させない
            if (CloneCtrl.CloneFilter_OnAnimatorHook(animator))
                return false;

            //ic_Mecanim = __instance;
            if (onHook || UnHookMecanim)
                return true; // ループ防止

            if (stateInfo.normalizedTime < yoinTimeIC
                && (GameClass.Climax || !yoinOnlyClimax))
            {
                if (hookLog)
                {
                    MyDebug.WriteLine($"Prefix_IC_Mecanim_OnStateUpdate \n\tgo:{animator.gameObject.name}  time:{stateInfo.normalizedTime} layer:{layerIndex}");
                }

                return false;
            }

            return true;
        }


        // アニメ制御フック
        [HarmonyPrefix, HarmonyPatch(typeof(FH_Mecanim), "OnStateEnter", new[] { typeof(Animator), typeof(AnimatorStateInfo), typeof(int) }, null)]
        public static bool Prefix_FH_Mecanim_OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (Free3P.SubGirlFilter_OnAnimatorHook(animator))
                return false;
            
            // クローンのステートは処理させない
            if (CloneCtrl.CloneFilter_OnAnimatorHook(animator))
                return false;

            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(FH_Mecanim), "OnStateUpdate", new[] { typeof(Animator), typeof(AnimatorStateInfo), typeof(int) }, null)]
        public static bool Prefix_FH_Mecanim_OnStateUpdate(Animator animator, ref AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (Free3P.SubGirlFilter_FH_Mecanim_OnStateUpdate(animator, ref stateInfo))
            {
                return false; // 元の関数が呼ばれない
            }

            // クローンのステートは処理させない
            if (CloneCtrl.CloneFilter_OnAnimatorHook(animator))
                return false;

            if (onHook || UnHookMecanim)
                return true; // ループ防止

            if (stateInfo.normalizedTime < yoinTimeFH
                && (GameClass.Climax || !yoinOnlyClimax))
            {
                if (hookLog)
                {
                    MyDebug.WriteLine($"Prefix_FH_Mecanim_OnStateUpdate \n\tgo:{animator.gameObject.name}  time:{stateInfo.normalizedTime} layer:{layerIndex}");
                }

                return false;
            }

            return true;
        }


        // アニメ制御フック
        [HarmonyPrefix, HarmonyPatch(typeof(UC_Mecanim), "OnStateEnter", new[] { typeof(Animator), typeof(AnimatorStateInfo), typeof(int) }, null)]
        public static bool Prefix_UC_Mecanim_OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (Free3P.SubGirlFilter_OnAnimatorHook(animator))
                return false;

            // クローンのステートは処理させない
            if (CloneCtrl.CloneFilter_OnAnimatorHook(animator))
                return false;

            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UC_Mecanim), "OnStateUpdate", new[] { typeof(Animator), typeof(AnimatorStateInfo), typeof(int) }, null)]
        public static bool Prefix_UC_Mecanim_OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (Free3P.SubGirlFilter_UC_Mecanim_OnStateUpdate(animator, stateInfo))
                return false; // 元の関数が呼ばれない

            // クローンのステートは処理させない
            if (CloneCtrl.CloneFilter_OnAnimatorHook(animator))
                return false;
            
            if (onHook || UnHookMecanim)
                return true; // ループ防止

            if (stateInfo.normalizedTime < yoinTimeFH
                && (GameClass.Climax || !yoinOnlyClimax))
            {
                if (hookLog)
                {
                    MyDebug.WriteLine($"Prefix_UC_Mecanim_OnStateUpdate \n\tgo:{animator.gameObject.name}  time:{stateInfo.normalizedTime} layer:{layerIndex}");
                }

                return false;
            }

            return true;
        }


        public static void saveLastState(Animator animator, string stateName, int layer)
        {
            var objname = animator.gameObject.name;
            if (string.IsNullOrEmpty(objname) || !objname.StartsWith("CH", StringComparison.Ordinal))
                return;

            if (LastAnimeStateByGameObj.TryGetValue(animator.gameObject.name, out Dictionary<int, string> data))
            {
                if (data.ContainsKey(layer))//.TryGetValue(layer, out string name))
                {
#if DEBUG
                    if (name != stateName)
                        MyDebug.WriteLine($"  {animator.gameObject.name} state {layer}: {name} => {stateName}");
#endif
                }
                else
                {
                    LastAnimeStateByGameObj[animator.gameObject.name].Add(layer, stateName);
                }
            }
            else
            {
                LastAnimeStateByGameObj.Add(animator.gameObject.name, new Dictionary<int, string>());
            }

            LastAnimeStateByGameObj[animator.gameObject.name][layer] = stateName;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Animator), "Play", new[] { typeof(string) }, null)]
        public static bool Animator_Play_Prefix(Animator __instance, string stateName)
        {
            if (onHook || UnHookAnimator)
                return true; // ループ防止

            saveLastState(__instance, stateName, 0);

            DebugLog2("Animator_Play_Prefix");
            MyDebug.WriteLine($"Animator_Play  stateName:{stateName}");

            return true;
        }


        [HarmonyPrefix, HarmonyPatch(typeof(Animator), "Play", new[] { typeof(string), typeof(int) }, null)]
        public static bool Animator_Play_Prefix(Animator __instance, string stateName, int layer)
        {
            if (onHook || UnHookAnimator)
                return true; // ループ防止

            // CameraAutoのLateUpdateで毎フレームセットされるので影響回避
            if (/*fixAnimePlayIssue && */stateName == "New State")
            {
                return true;
            }

            //onHook = true;
            saveLastState(__instance, stateName, layer);

            return true;
#if DEBUG2
            try
            {
                if (fixAnimePlayIssue && stateName == "New State" && layer == 0)
                {
                    // CameraAutoのLateUpdateで毎フレームセットされるので影響回避
                    if (__instance.GetCurrentAnimatorStateInfo(layer).IsName(stateName))
                        return false;
                }

                if (hookLog)
                {
                    DebugLog("Animator_Play_Prefix");
                    MyDebug.WriteLine($"Animator_Play\n\tgo:{__instance.gameObject.name} stateName:{stateName} layer:{layer}");
                }
                //if (stateName.StartsWith("H01", StringComparison.Ordinal))
                //{
                //    // 絶頂後復帰
                //}
            }
            finally
            {
                onHook = false;
            }
#endif
        }

#if DEBUG2
        
        //[HarmonyPrefix, HarmonyPatch(typeof(Animator), "CrossFade", new[] { typeof(string), typeof(float), typeof(int) }, null)]
        //public static bool Animator_CrossFade_Prefix(Animator __instance, string stateName, float transitionDuration, int layer)
        //{
        //    if (onHook)
        //        return true; // ループ防止

        //    saveLastState(__instance, stateName, layer);

        //    DebugLog2("Animator_CrossFade_Prefix");
        //    MyDebug.WriteLine($"Animator_CrossFade stateName:{stateName} trans:{transitionDuration} layer:{layer}");

        //    return true;
        //}

        [HarmonyPrefix, HarmonyPatch(typeof(Animator), "SetTrigger", new[] { typeof(string) }, null)]
        public static bool Animator_SetTrigger_Prefix(Animator __instance, string name)
        {
            if (onHook)
                return true; // ループ防止

            saveLastState(__instance, name);

            DebugLog2("Animator_SetTrigger_Prefix");
            MyDebug.WriteLine($"Animator_SetTrigger  Name:{name}");

            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(IC_Mecanim), "OnStateEnter", new[] { typeof(Animator), typeof(AnimatorStateInfo), typeof(int) }, null)]
        public static bool IC_Mecanim_OnStateEnter_Prefix(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (onHook)
                return true; // ループ防止

            DebugLog2("OnStateEnter");

            Console.WriteLine($"IC_Mecanim_OnStateEnter  hash:{stateInfo.shortNameHash} layer:{layerIndex}");

            foreach (var o in animator.GetCurrentAnimatorClipInfo(layerIndex))
            {
                Console.WriteLine($"\tCLIP: {o.clip.name}  W: {o.weight}");
            }

            return true;
        }
#endif

    }
}

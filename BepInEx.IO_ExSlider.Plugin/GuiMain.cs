#define USING_XML
//#define DEVMODE

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
using nnPlugin;
using static BepInEx.IO_ExSlider.Plugin.IO_ExSlider.MyXML;
using static BepInEx.IO_ExSlider.Plugin.CtrlBone;
using Hook.nnPlugin.Managed;
using System.Text.RegularExpressions;
using System.Collections;
using System.Text;

namespace BepInEx.IO_ExSlider.Plugin
{
    public partial class IO_ExSlider : BaseUnityPlugin
    {
        //
        // GUI用
        //
        internal static bool GuiFlag = false;
        private static bool GuiUpdateFlag = false; // GUIStyleの設定タイミングによって色がおかしくなることがあるため

        const int WIN_WIDTH = 280;
        static int WIDTH_DPOS = UnityEngine.Screen.width - WIN_WIDTH;
        static int HEIGHT_DPOS = -UnityEngine.Screen.height;//-685 - 35;

        //const int WIN_H1 = 460;
        const int WIN_H1 = 680; //700
        const int WIN_H2 = 20;
        const int WINHALF_D = 340;
        const int WIN_FOOTER = 100; //180

        // Camera CameraMain;

        static Rect rc_stgw = new Rect((UnityEngine.Screen.width - WIN_WIDTH) / 2, UnityEngine.Screen.height + HEIGHT_DPOS, WIN_WIDTH, WIN_H1);
        int ScWidth = 0, ScHeight = 0;
        private static bool bGuiOnMouse = true;

        BoneScales.OffsetData _copyOffsetData = null;
        EasyCombo easyCombo = new EasyCombo("使用不可");
        static float CameraFovDefValueThisScene;

        Texture2D texFade;
        Font fontLoading;
        public static string LoadingText = null;
        public static TextAnchor LoadingTextAnchor = TextAnchor.LowerRight;

        void OnGUI()
        {
            if (!_PluginEnabled || _PluginPause)
                return;

            if (GuiFlag || WinMouseOff)
            {
                RectU._Scale = cfg.FontSize / 12f;
                rc_stgw.width = RectU.Scaling(WIN_WIDTH);
                rc_stgw.height = RectU.Scaling(WIN_H1);
            }

            if (GuiFlag || WinMouseOff)
            {
                if (ScHeight != UnityEngine.Screen.height)
                {
                    if (UnityEngine.Screen.height > 720)
                        rc_stgw.y = 0;//UnityEngine.Screen.height + HEIGHT_DPOS;
                    else
                        rc_stgw.y = 0;
                    ScHeight = UnityEngine.Screen.height;
                }
                if (ScWidth != UnityEngine.Screen.width)
                {
                    if (UnityEngine.Screen.width > 800)
                        rc_stgw.x = (UnityEngine.Screen.width - rc_stgw.width); //+ WIDTH_DPOS;
                    else
                        rc_stgw.x = UnityEngine.Screen.width - rc_stgw.width;
                    ScWidth = UnityEngine.Screen.width;
                }
            }

            if (GuiUpdateFlag || gsWin == null)
            {
                MyDebug.Log(PLUGIN_CAPTION + " GUI Update");
                GuiUpdateFlag = false;

                gsWin = new GUIStyle("box")
                {
                    fontSize = cfg.FontSize - 1,
                    alignment = TextAnchor.UpperLeft
                };

                gsLabel = new GUIStyle("label");
                gsButton = new GUIStyle("button");
                gsButton2 = new GUIStyle("button");
                gsToggle = new GUIStyle("toggle");
                gsTextField = new GUIStyle("textField");
                gsTextArea = new GUIStyle("textArea");
                gsBox = new GUIStyle("box");
                gsListButton = new GUIStyle("button");

                gsLabel.alignment = TextAnchor.MiddleLeft;
                gsButton.alignment = TextAnchor.MiddleCenter;
                gsButton2.alignment = TextAnchor.MiddleCenter;
                gsToggle.alignment = TextAnchor.MiddleLeft;
                gsTextField.alignment = TextAnchor.MiddleLeft;
                gsTextArea.alignment = TextAnchor.UpperLeft;
                gsListButton.alignment = TextAnchor.MiddleLeft;

                gsLabel.stretchHeight = false;
                gsLabel.stretchWidth = false;

            }

            if (GuiFlag)
            {
                //RectU._Scale = cfg.FontSize / 12f;
                //rc_stgw.width = RectU.Scaling(WIN_WIDTH);
                //rc_stgw.height = RectU.Scaling(WIN_H1);
                gsWin.fontSize = cfg.FontSize - 1;

                if (WinMin)
                    rc_stgw.height = RectU.Scaling(WIN_H2);
                else
                {
                    rc_stgw.height = RectU.Scaling(WIN_H1);
                    if (cfg.WinHalf)
                        rc_stgw.height = RectU.Scaling(WIN_H1 - WINHALF_D);
                }

                //CameraMain = Camera.main;

                // 前処理
                ClearIconCacheOnGui();

                //設定画面を出す
                rc_stgw = GUI.Window(WINID_COFIG, rc_stgw, WindowCallback, PLUGIN_CAPTION + " " + PLUGIN_VERSION, gsWin);

                if (rc_stgw.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
                {
                    CameraZoomCtrl.SetZoomEnabled(false);
                    bGuiOnMouse = true;
                }
                else
                {
                    if (bGuiOnMouse)
                    {
                        CameraZoomCtrl.SetZoomEnabled(true);
                    }
                    bGuiOnMouse = false;
                }

            }
            else if (bGuiOnMouse)
            {
                CameraZoomCtrl.SetZoomEnabled(true);
                bGuiOnMouse = false;
            }

            // 簡易ローディング用
            if (!string.IsNullOrEmpty(LoadingText))
            {
                if (!texFade)
                {
                    this.fontLoading = PlgUtil.GetResourceByName<Font>("GenShinGothic-P-Bold");
                    this.texFade = new Texture2D(1, 1);
                    this.texFade.SetPixel(0, 0, Color.Lerp(Color.clear, Color.black, 0.5f));
                    this.texFade.Apply();
                }

                GUIStyle guiStyle = new GUIStyle("label");
                if (this.fontLoading)
                    guiStyle.font = this.fontLoading;

                //guiStyle.fontSize = (int)(Screen.width * 28 / 1600f);
                guiStyle.fontSize = (int)(Screen.width * 25 / 1600f);
                guiStyle.alignment = LoadingTextAnchor;

                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), this.texFade);

                using (new UiUtil.GuiColor(Color.yellow))
                    UiUtil.DrawOutlineLabel(new Rect(0, 0, Screen.width, Screen.height), LoadingText, 2, guiStyle);
            }
        }//OnGUI()

        // 強制的にスクロールを無効にする
        static class CameraZoomCtrl
        {
            public static void SetZoomEnabled(bool enabled)
            {
                if (enabled)
                    return;

                if (rc_stgw.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
                {
                    if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2) || Input.GetAxis("Mouse ScrollWheel") != 0f)
                    {
                        Input.ResetInputAxes();
                    }
                }
            }
        }


        #region ウインドウプロシージャ

        public Vector2 scrollPosition = Vector2.zero;
        static int scvHeight = 100;

        static bool WinMin = true;
        static bool WinMouseOff = false;
        static bool WinPin = false;

        const int BTN2 = 30;
        const int BTN3 = 26;
        const int SL_WD = WIN_WIDTH - 20;//150;
        const int LBL_WD = WIN_WIDTH - 10;
        //const int ItemH = 20 * 2 + 50;

        string selectPresetMenu = "";
        HashSet<string> setGuiOpen = new HashSet<string>();
        string[] setGuiOpenPrev = new string[0];

        string presetTooltip = "";
        string presetTooltipTemp = "";
        GUIStyle gsWin;// = new GUIStyle("box");
        GUIStyle gsLabel;// = new GUIStyle("label");
        GUIStyle gsButton;// = new GUIStyle("button");
        GUIStyle gsButton2;// = new GUIStyle("button");
        GUIStyle gsListButton;// = new GUIStyle("button");
        GUIStyle gsToggle;// = new GUIStyle("toggle");
        GUIStyle gsTextField;// = new GUIStyle("textField");
        GUIStyle gsTextArea;// = new GUIStyle("textArea");
        GUIStyle gsBox;// = new GUIStyle("box");

        // メインスクロールビュー内容定数
        const int ItemX = 5;
        const int ItemWidth = WIN_WIDTH - 16 - 5;//- 10;
        const int ItemHeight = 20;
        const int ItemDw = 20;
        const int SLDW = ItemWidth - 20;
        const int LX = ItemX + 5;
        const int BW = 25;
        const int LW = SLDW - BW * 3 - 5;
        const int EDTW = LW + BW * 2 + 5;
        const int SLDWHF = (ItemWidth - 20) / 2 - 10;

        int posYs = 5;
        string nowEdit = "";

        void GuiResetOnNewScene()
        {
            OhMyGizmo.allGizmoReset();
            ScenePos.gizmoTgt = null;
            easyCombo.Close();
            ctrl_bones_CacheReset(null);
        }

        void WindowCallback(int id)
        {
            //test return;
            setGuiOpenPrev = setGuiOpen.ToArray();
            gsLabel.fontSize = cfg.FontSize;
            gsButton.fontSize = cfg.FontSize;
            gsButton2.fontSize = cfg.FontSize + 1;
            gsToggle.fontSize = cfg.FontSize;
            gsTextField.fontSize = cfg.FontSize;
            gsTextArea.fontSize = cfg.FontSize;
            gsListButton.fontSize = cfg.FontSize;

            nowEdit = "";

            //GUI draw
            WinPin = GUI.Toggle(RectU.sclRect(0, 0, 20, 20), WinPin, "", gsToggle);

            if (GUI.Button(RectU.sclRect(WIN_WIDTH - 20 - 40 - 50, 0, 20, 20), "+", gsButton))
            {
                cfg.FontSize++;
                Console.WriteLine(": FontSize = {0}", cfg.FontSize);

                if (WinPin)
                {
                    ScWidth = 0;
                    ScHeight = 0;
                }
            }
            if (GUI.Button(RectU.sclRect(WIN_WIDTH - 20 - 40 - 30, 0, 20, 20), "-", gsButton))
            {
                cfg.FontSize--;
                Console.WriteLine(": FontSize = {0}", cfg.FontSize);

                if (WinPin)
                {
                    ScWidth = 0;
                    ScHeight = 0;
                }
            }

            if (!WinMin)
            {
                if (GUI.Button(RectU.sclRect(WIN_WIDTH - 20 - 40, 0, 20, 20), (cfg.WinHalf ? "□" : "/"), gsButton))
                {
                    cfg.WinHalf = !cfg.WinHalf;
                }
            }

            /*if (GUI.Button(RectU.sclRect(WIN_WIDTH - 20 - 20, 0, 20, 20), (WinMin ? (!cfg.WinHalf ? "□" : "/") : "_"), gsButton))
            {
                WinMin = !WinMin;
            }*/
            using (new UiUtil.GuiColor(Color.yellow, cfg.WinMin_Def && WinMouseOff))
                if (GUI.Button(RectU.sclRect(WIN_WIDTH - 20 - 20, 0, 20, 20), (WinMin ? (!cfg.WinHalf ? "□" : "/") : "_"), gsButton))
                {
                    if (WinMouseOff)
                    {
                        cfg.WinMin_Def = !cfg.WinMin_Def;
                        WinMin = cfg.WinMin_Def;
                    }
                    else
                    {
                        WinMin = !WinMin;
                        cfg.WinMin_Def = WinMin;
                    }
                }

            if (GUI.Button(RectU.sclRect(WIN_WIDTH - 20, 0, 20, 20), "x", gsButton))
            {
                WinMouseOff = false;
                GuiFlag = false;
            }

            int pos_y = 20;

            if (WinMin)
            {
                pos_y -= 5;

                goto Label_Wndend;

                /*
                WinMouseOff = GUI.Toggle(new Rect(5, (pos_y), 200, 20), WinMouseOff, "マウスオーバーによる表示切替", gsToggle);
                GUI.DragWindow();
                return;*/
            }

            GUI.Label(RectU.sclRect(5, (pos_y), LBL_WD, 20), "【拡張設定】", gsLabel);

            if (setGuiOpen.Count > 0)
            {
                if (GUI.Button(RectU.sclRect(80, (pos_y), 45, 20), "全閉", gsButton))
                {
                    setGuiOpen.Clear();
                    GuiResetOnNewScene();
                }
            }

            //Rect rect = RectU.sclRect(5, (pos_y += 20), LBL_WD, 285 + 20);
            Rect rect = RectU.sclRect(5, (pos_y += 20), LBL_WD, WIN_H1 - WIN_FOOTER);
            if (cfg.WinHalf)
                rect.height -= RectU.Scaling(WINHALF_D);

            GUI.Box(rect, "");
            scrollPosition = GUI.BeginScrollView(rect, scrollPosition, RectU.sclRect(0, 0, LBL_WD - 16, scvHeight + 20/*ItemH * KeyHook.DicKeyGet.Count() + 20*/), false, false);
            try
            {
                posYs = 5;

                // システム
                systemCtrl();
                HsystemCtrl();
                macroCtrl();
                lookAtCtrl();
                EffectCtrl();
                BGposCtrl();

                posYs += 10;

                //var ggo = ctrlNeko.Find();
                //if (!ggo)
                //    goto NO_GIRL;

                foreach (var c in ctrlGirls_Targeted)
                {
                    //if (c.ini.isShota() && !c.isActive()) // 普段は非表示
                    if (!cfg.InactiveCharCtrlVisible && !c.isActive())
                        continue;

                    FlagNowTgtID = c.ini.id;

                    using (new UiUtil.GuiEnable(c.isActive()))
                    {
                        labelctrl(c.ini.name);

                        GUI.contentColor = Color.cyan;
                        GUI.Label(RectU.sclRect(ItemX + 20, (posYs += 20), LBL_WD, 20), $"体形スライダー ({c.ini.name})", gsLabel);
                        GUI.contentColor = Color.white;
                        boneScCtrl(c.ini.id, c);

                        GUI.contentColor = Color.cyan;
                        GUI.Label(RectU.sclRect(ItemX + 20, (posYs += 20), LBL_WD, 20), $"シーン位置設定 ({c.ini.name})", gsLabel);
                        GUI.contentColor = Color.white;
                        posCtrl(c.ini.id, c);

                        if (c.ini.isGirl())
                        {
                            GUI.contentColor = Color.cyan;
                            GUI.Label(RectU.sclRect(ItemX + 20, (posYs += 20), LBL_WD, 20), $"表情・状態変更 ({c.ini.name})", gsLabel);
                            GUI.contentColor = Color.white;
                            faceCtrl(c.ini.id, c);

                            GUI.contentColor = Color.cyan;
                            GUI.Label(RectU.sclRect(ItemX + 20, (posYs += 20), LBL_WD, 20), $"シェイプキー ({c.ini.name})", gsLabel);
                            GUI.contentColor = Color.white;
                            shapeCtrl(c.ini.id, c);
                        }

                        GUI.contentColor = Color.cyan;
                        GUI.Label(RectU.sclRect(ItemX + 20, (posYs += 20), LBL_WD, 20), $"マテリアル設定 ({c.ini.name})", gsLabel);
                        GUI.contentColor = Color.white;
                        mateCtrl(c.ini.id, c);

                        if (c.ini.isGirl() || cfg.EnableMensPosectrl)
                        {
                            GUI.contentColor = Color.cyan;
                            GUI.Label(RectU.sclRect(ItemX + 20, (posYs += 20), LBL_WD, 20), $"ポーズキャプチャ ({c.ini.name})", gsLabel);
                            GUI.contentColor = Color.white;
                            poseCtrl(c.ini.id, c);
                        }

                        if (cfg.CloneCtrlVisible && c.ini.isNotGirl())//c.ini.isShota()) // しょた以外はエラーはきまくるのを放置してある。他のメンバーはAwakeとかフックして改変必須
                        {
                            GUI.contentColor = Color.cyan;
                            GUI.Label(RectU.sclRect(ItemX + 20, (posYs += 20), LBL_WD, 20), $"クローン ({c.ini.name})", gsLabel);
                            GUI.contentColor = Color.white;
                            cloneCtrl(c.ini.id, c);
                        }

                        posYs += 10;
                    }
                }

                // スクロールリスト末尾
                //NO_GIRL:
                posYs += 5;

                scvHeight = posYs;
            }
            catch (Exception e)
            {
                Console.WriteLine("KtM例外エラー：" + e);
            }
            finally
            {
                GUI.EndScrollView();
            }

            pos_y += (int)((rect.height) / RectU._Scale);
            pos_y += 5;


            if (GUI.Button(RectU.sclRect(5, (pos_y += 5), 100 + 30, 20), "設定SAVE", gsButton))
            {
                XML_Save(cfg);
            }
            if (GUI.Button(RectU.sclRect(5 + 105 + 30, (pos_y), 100 + 30, 20), "設定LOAD", gsButton))
            {
                if (InputEx.CheckModifierKeys(InputEx.ModifierKey.Control | InputEx.ModifierKey.Alt))
                {
                    // TargetIniも読込、完全初期化
                    if (TargetIni.Load())
                    {
                        ctrlGirls.Clear();

                        // 初期化
                        TargetIni.targetIni.girls.ForEach(
                                x => ctrlGirls.Add(new GirlCtrl(x))
                            ); ;
                    }
                }

                XML_Load(ref cfg);
                GuiFlag = true;
                GuiUpdateFlag = true;
            }
            pos_y += 20;

            WinMouseOff = GUI.Toggle(RectU.sclRect(5, (pos_y), 200, 20), WinMouseOff, "マウスオーバーによる表示切替", gsToggle);

            //////////////////////////////////////////
            Label_Wndend:
            //////////////////////////////////////////

            if (!WinPin)
                GUI.DragWindow();
        }

        #endregion

        #region コントロールセット
        void ClearIconCacheOnGui()
        {
            foreach (var o in colorIcons)
                GameObject.DestroyImmediate(o);
            colorIcons.Clear();
        }

        float floatctrl2(string name, float _f, float def, float min = 0f, float max = 100f, float scale = 1f)
        {
            var bkf = _f;
            _f *= scale;

            //GUI.Label(RectU.sclRect(10, (posYs += 20), LBL_WD, 20), name + ": " + (_f = (float)Math.Round(_f, 3)), gsLabel);
            GUI.Label(RectU.sclRect(LX, (posYs += ItemHeight - 5), LBL_WD, ItemHeight), name + ": " + (float)Math.Round(_f, 2), gsLabel);
            //_f = btnset_LR(RectU.sclRect(LX + LW + 30, (posYs), 12, 20), 12, _f, 0.01f, gsButton);　// 極小
            _f = btnset_LR(RectU.sclRect(LX + LW, (posYs), BW, 20), BW, _f, 0.01f, gsButton);
            if (GUI.Button(RectU.sclRect(LX + EDTW, (posYs), 28, 20), "CL", gsButton))
            {
                _f = def * scale;
            }
            var nf = GUI.HorizontalSlider(RectU.sclRect(LX, (posYs += 15), SLDW, 15), _f, min, max);
            if (nf != _f)
                _f = (float)Math.Round(nf, 2);

            _f /= scale;
            return _f;
        }

        bool floatctrl(string name, ref float _f, float def, float min = 0f, float max = 1f, float scale = 1f, int digits = 3, bool rimitFlag = false)
        {
            var bkf = _f;
            _f *= scale;

            var digits2 = digits - 1;
            if (digits2 < 0)
                digits2 = 0;

            //GUI.Label(RectU.sclRect(10, (posYs += 20), LBL_WD, 20), name + ": " + (_f = (float)Math.Round(_f, 3)), gsLabel);
            GUI.Label(RectU.sclRect(LX, (posYs += ItemHeight - 5), LBL_WD, ItemHeight), name + ": " + (float)Math.Round(_f, digits), gsLabel);
            _f = btnset_LR(RectU.sclRect(LX + LW, (posYs), BW, 20), BW, _f, Mathf.Pow(0.1f, digits), gsButton);
            if ((digits == 0 && !rimitFlag) || (digits != 0 && rimitFlag))
            {
                _f = Mathf.Clamp(_f, min, max); // 整数値はオーバーするとやばいのが多いので
            }
            if (GUI.Button(RectU.sclRect(LX + EDTW, (posYs), 28, 20), "CL", gsButton))
            {
                _f = def * scale;
            }
            var nf = GUI.HorizontalSlider(RectU.sclRect(LX, (posYs += 15), SLDW, 15), _f, min, max);
            if (nf != _f)
                _f = (float)Math.Round(nf, digits2);

            _f /= scale;

            return _f != bkf;
        }

        int getintctrl(string name, int _f, int def, int min = 0, int max = 1)
        {
            float f = _f;
            floatctrl(name, ref f, def, min, max, 1, 0);
            return (int)f;
        }

        float getfloatctrl(string name, float _f, float def, float min = 0f, float max = 1f, float scale = 1f)
        {
            float f = _f;
            floatctrl(name, ref f, def, min, max, scale);
            return f;
        }

        float getfloatctrl_test(string name, float _f, float def, float min = 0f, float max = 1f, float scale = 1f)
        {
            var r = (max - min) / 2;
            int i = (int)(_f / r);
            if (i == 0)
                return getfloatctrl(name, _f, def, min, max, scale);

            //if (min < 0)
            //    return getfloatctrl(name, _f, def, min + (i * r), max + (i * r), scale);

            var c = r * i;
            r = Mathf.Abs(r);
            return getfloatctrl(name, _f, def, c - r, c + r, scale);
        }

        bool gettoggle(string name, bool val, int dx = 0, int dy = 0, int dxw = 0, int dyh = 0)
        {
            return GUI.Toggle(RectU.sclRect(LX + dx, (posYs += (20 + dy)), LBL_WD + dxw, 20 + dyh), val, name, gsToggle);
        }

        bool togglectrl(string name, ref bool val, int dx = 0, int dy = 0)
        {
            var old = val;
            val = gettoggle(name, val, dx, dy);
            return old != val;
        }

        bool togglectrl2(string name, ref bool val, int dx = 0, int dy = 0)
        {
            var old = val;
            val = gettoggle(name, val, dx, dy);
            return (old != val) && val;
        }

        bool togglectrl2noref(string name, bool val, int dx = 0, int dy = 0)
        {
            var old = val;
            val = gettoggle(name, val, dx, dy);
            return (old != val) && val;
        }

        List<Texture2D> colorIcons = new List<Texture2D>();
        Color colorctrl(string name, Color _c, Color def, float min = -0.5f, float max = 2f)
        {
            var bkc = _c;

            Texture2D color_icon = null;

            int iconsize = (int)gsLabel.lineHeight;
            if (color_icon && color_icon.width != iconsize)
            {
                GameObject.DestroyImmediate(color_icon);
                color_icon = null;
            }

            if (!color_icon)
                color_icon = new Texture2D(1, 1, TextureFormat.ARGB32, false);


            //var colors = new Color[iconsize * iconsize];
            //for (int i = 0; i < colors.Length; i++)
            //    colors[i] = _c;

            //color_icon.filterMode = FilterMode.Point;
            //color_icon.SetPixels(colors);
            color_icon.SetPixel(0,0,_c);
            color_icon.Apply();

            colorIcons.Add(color_icon);

            using(new UiUtil.GuiColor(Color.white))
                GUI.DrawTexture(RectU.sclRect(LX, (posYs + ItemHeight - 5 + 2), ItemHeight - 4, ItemHeight - 4), color_icon, ScaleMode.StretchToFill);
                //GUI.Label(RectU.sclRect(LX, (posYs + ItemHeight - 5), ItemHeight, ItemHeight), color_icon, GUIStyle.none);

            if (name != null)
                GUI.Label(RectU.sclRect(LX + ItemHeight, (posYs += ItemHeight - 5), LBL_WD, ItemHeight), name + ": ", gsLabel);
            posYs += 5;
            _c.r = getfloatctrl(" *R", _c.r, def.r, min, max);
            _c.g = getfloatctrl(" *G", _c.g, def.g, min, max);
            _c.b = getfloatctrl(" *B", _c.b, def.b, min, max);
            _c.a = getfloatctrl(" *A", _c.a, def.a, min, max);
            return _c;
        }

        float a180(float rot)
        {
            return Mathf.DeltaAngle(0f, rot);
            //if (rot > 180f) rot -= 360f;
            //return rot;
        }

        float a360(float rot)
        {
            if (rot < 0f) rot += 360f;
            return rot;
        }

        Vector3 v3ctrl2(string name, string sub, Vector3 _v, Vector3 def, float min = 0f, float max = 1f)
        {
            var bkc = _v;
            labelctrl(name + ": ", (LX - ItemX));
            posYs += -5;
            return v3ctrl(sub, _v, def, min, max);
        }
        Vector3 v3ctrl2_test(string name, string sub, Vector3 _v, Vector3 def, float min = 0f, float max = 1f)
        {
            var bkc = _v;
            labelctrl(name + ": ", (LX - ItemX));
            posYs += -5;
            return v3ctrl_test(sub, _v, def, min, max);
        }

        Vector3 v3ctrl(string sub, Vector3 _v, Vector3 def, float min = 0f, float max = 1f)
        {
            var bkc = _v;
            posYs += 10;
            _v.x = getfloatctrl(" " + sub + "X", _v.x, def.x, min, max);
            _v.y = getfloatctrl(" " + sub + "Y", _v.y, def.y, min, max);
            _v.z = getfloatctrl(" " + sub + "Z", _v.z, def.z, min, max);
            return _v;
        }

        Vector3 v3ctrl_test(string sub, Vector3 _v, Vector3 def, float min = 0f, float max = 1f)
        {
            var bkc = _v;
            posYs += 10;
            _v.x = getfloatctrl_test(" " + sub + "X", _v.x, def.x, min, max);
            _v.y = getfloatctrl_test(" " + sub + "Y", _v.y, def.y, min, max);
            _v.z = getfloatctrl_test(" " + sub + "Z", _v.z, def.z, min, max);
            return _v;
        }

        bool rootbtn(string name, int x_offset = 0, int y_offset = 0)
        {
            bool open = setGuiOpen.Contains(name);
            if (open && !GUI.enabled)
            {
                open = false;
            }

            if (GUI.Button(RectU.sclRect(ItemX + x_offset, (posYs + y_offset), 20, 20), (open ? "-" : "+"), gsButton))
            {
                open = !open;
            }
            if (open)
                setGuiOpen.Add(name);
            else
                setGuiOpen.Remove(name);

            return open;
        }

        bool is_rootopen_prev(string name)
        {
            return setGuiOpenPrev.Contains(name);
        }

        bool rootbtn2(string name, string label, int x_offset = 0, int y_offset = 0, Color color = default(Color))
        {
            if (color == default(Color))
                color = Color.white;
            var bkc = GUI.color;
            GUI.color = color;

            labelctrl(label, x_offset + 20, y_offset);

            GUI.color = bkc;

            return rootbtn(name, x_offset, y_offset);
        }

        bool centerbtn(string label, int dx = 0, int dy = 0, int dxw = 0, int dyh = 0)
        {
            return (GUI.Button(RectU.sclRect(LX + 10 + dx, (posYs += 20) + dy, SLDW - 30 + dxw, 20 + dyh), label, gsButton));
        }

        void labelctrl(string str, int dx = 0, int dy = -5)
        {
            GUI.Label(RectU.sclRect(ItemX + dx, (posYs += ItemHeight + dy), LBL_WD, ItemHeight), str, gsLabel);
        }

        bool is_preset_save(string name)
        {
            return selectPresetMenu == "SAVE-" + name;
        }

        bool is_preset_load(string name)
        {
            return selectPresetMenu == "LOAD-" + name;
        }

        // ロードしたときだけtrueを返す
        bool presetctrl<T>(ref T obj, string name)
        {
            bool isLoaded = false;

            labelctrl($"プリセット ({name})", 0, 0);

            if (selectPresetMenu.EndsWith(name, StringComparison.Ordinal))
            {
                // オープン時
                if (selectPresetMenu.StartsWith("SAVE", StringComparison.Ordinal))
                    GUI.color = Color.yellow;
                else
                    GUI.color = Color.green;
                labelctrl("スロット選択: " + selectPresetMenu, 5, 0);

                var labelkv = cfg.PresetSlotnames.FirstOrDefault(s => s.Key == name);
                bool needadd = false;
                int numPresets = PRESETS;

                if (labelkv.Equals(default(KeyValuePair<string, string[]>)))
                {
#if DEBUG2
                    labelkv = new KeyValuePair<string, string[]>("", getABCstrArray(numPresets));
#else
                    labelkv = new KeyValuePair<string, string[]>("", getNumstrArray(numPresets));
#endif
                    //needadd = true;
                    cfg.PresetSlotnames.AddItem(new KeyValuePair<string, string[]>(name, labelkv.Value));
                }
                else if (labelkv.Value.Length < numPresets)
                {
                    var a = getNumstrArray(numPresets - labelkv.Value.Length, labelkv.Value.Length + 1);
                    labelkv = new KeyValuePair<string, string[]>(labelkv.Key, labelkv.Value.AddRangeToArray(a));
                    needadd = true;
                }

                if (needadd)
                {
                    for (int i = 0; i < cfg.PresetSlotnames.Length; i++)
                    {
                        if (cfg.PresetSlotnames[i].Key == name)
                            cfg.PresetSlotnames[i] = labelkv;
                    }
                }

                string gui_tooltip_old = GUI.tooltip;
                try
                {
                    GUI.tooltip = "";

                    posYs += 20;
                    const int NBW = (SLDW - 10) / 6;
                    for (int i = 0; i <= numPresets; i++)
                    {
                        if (i == 0)
                        {
                            if (GUI.Button(RectU.sclRect(LX + 10 + NBW * i, (posYs), NBW, ItemHeight), "<戻", gsButton))
                            {
                                selectPresetMenu = "";
                                break;
                            }
                            continue;
                        }
                        else
                        {
                            string slotname = labelkv.Value[i - 1];
                            int dx = i;
                            if (i >= 6)
                            {
                                dx = (i - 1) % 5 + 1;
                                if (dx == 1)
                                    posYs += ItemHeight;
                            }

                            xmlNote xnote = null;

                            var fname = $"{SaveFileName}-{name}-{i}.xml";
                            Color cbk = GUI.color;
                            if (!XML_Exists(fname))
                            {
                                if (selectPresetMenu.StartsWith("SAVE", StringComparison.Ordinal))
                                    GUI.color = Color.white;
                                else
                                    GUI.color = Color.gray;
                            }
                            else
                            {
                                xnote = xmlNote.ReadNote(fname);
                            }

                            if (xnote == null)
                            {
                                xnote = new xmlNote();
                                xnote.note = "\r";
                            }

                            if (GUI.Button(RectU.sclRect(LX + 10 + NBW * dx, (posYs), NBW, ItemHeight), new GUIContent(slotname, xnote.note), gsButton))
                            {
                                if (selectPresetMenu.StartsWith("SAVE", StringComparison.Ordinal))
                                {
                                    XML_Save<T>(obj, fname);

                                    if (!string.IsNullOrEmpty(presetTooltip))
                                    {
                                        xnote.note = presetTooltip;
                                        xmlNote.SaveNote(xnote, fname);
                                    }
                                }
                                else
                                {
                                    if (XML_Load<T>(out obj, fname))
                                        isLoaded = true;
                                }
                                selectPresetMenu = "";
                            }

                            GUI.color = cbk;
                        }
                    }
                    // tooltip試作
                    if (!string.IsNullOrEmpty(selectPresetMenu))
                    {
                        bool issave = selectPresetMenu.StartsWith("SAVE", StringComparison.Ordinal);
                        Color cbk = GUI.color;

                        GUI.Label(RectU.sclRect(LX + 10, (posYs += 20), NBW * 1.5f, ItemHeight),
                            new GUIContent("Memo:", issave ? "説明を記入できます\n" : "説明を表示/無ければ更新日"), gsTextField);

                        string note = presetTooltip;
                        if (!issave || string.IsNullOrEmpty(note))
                        {
                            note = GUI.tooltip;
                            if (issave)
                            {
                                GUI.color = Color.white;

                                // 更新日以外は再利用可能にする
                                if (note != presetTooltipTemp && !string.IsNullOrEmpty(note))
                                {
                                    if (note == "\r")
                                        note = "";
                                    presetTooltipTemp = note;
                                }
                                else if (!presetTooltipTemp.EndsWith("\n", StringComparison.Ordinal))
                                {
                                    note = presetTooltipTemp;
                                }
                            }

                            if (note.EndsWith("\n", StringComparison.Ordinal))
                                note = note.Replace("\r", "").Replace("\n", "");
                        }

                        string str = GUI.TextField(RectU.sclRect(LX + 10 + NBW * 1.5f, (posYs), NBW * 4.5f, ItemHeight), note, gsTextField);
                        if (issave && (note != str))
                            presetTooltip = str;

                        if (!presetTooltipTemp.EndsWith("\n", StringComparison.Ordinal))
                            presetTooltipTemp = str;

                        GUI.color = cbk;
                    }
                }
                finally
                {
                    GUI.tooltip = gui_tooltip_old;
                    GUI.color = Color.white;
                }
            }
            else
            {
                // クローズ時
                const int SBW = SLDW / 2 - 20;
                GUI.color = Color.yellow;
                if (GUI.Button(RectU.sclRect(LX + 10, (posYs += 20), SBW, 20), "SAVE選択", gsButton))
                {
                    selectPresetMenu = "SAVE-" + name;

                    presetTooltip = "";
                    presetTooltipTemp = "";
                    xmlNote.ClearNoteCache();
                }
                GUI.color = Color.green;
                if (GUI.Button(RectU.sclRect(LX + SBW + 20, (posYs), SBW, 20), "LOAD選択", gsButton))
                {
                    selectPresetMenu = "LOAD-" + name;

                    presetTooltip = "";
                    presetTooltipTemp = "";
                    xmlNote.ClearNoteCache();
                }
                GUI.color = Color.white;
            }
            return isLoaded;
        }
        #endregion

        public static void SetSysCnfQuality(int val)
        {
            if (val >= 0 && val <= 2 && ConfigClass.QualitySetting != val)
            {
                ConfigClass.QualitySetting = (int)val;
                SaveLoad_System.Data.systemconfig.QualitySetting = ConfigClass.QualitySetting;

                if (!actScene.IsValid() || string.IsNullOrEmpty(actScene.name) || actScene.name == "Title")
                {
                    return; // 適用NG
                }

                var go = GameObject.Find("UI Root(UI)/Wind_Conf");
                if (!go)
                    return;

                var config = go.GetComponent<ConfigSetting>();
                if (config)
                {
                    if (val >= 0 && val <= 2)
                    {
                        ConfigClass.QualitySetting = (int)val;
                        try
                        {
                            config.QualitySettingss();
                        }
                        catch (Exception e)
                        {
                            Debug.LogError("QualitySettingss()の呼び出しに失敗\n" + e);
                        }
                    }
                }
            }
        }

        Color colorDemiCyan = Color.Lerp(Color.white, Color.cyan, 0.4f);

        #region システム
        void systemCtrl()
        {
            GUI.contentColor = Color.cyan;
            GUI.Label(RectU.sclRect(ItemX + 20, (posYs), LBL_WD, 20), "システム設定", gsLabel);
            GUI.contentColor = Color.white;
            if (rootbtn(nowEdit = "System"))
            {
                posYs += 10;

                string[] disp = { "Quality", "Normal", "Performance" };

                using(new UiUtil.GuiColor(colorDemiCyan))
                    labelctrl($"描画設定: {ConfigClass.QualitySetting} = {disp[ConfigClass.QualitySetting]}", 0, 0);
                posYs += 5;
                
                using(new UiUtil.GuiColor(Color.yellow, cfg.QualitySetting >= 0))
                {
                    float val = ConfigClass.QualitySetting;
                    if (floatctrl("QualitySetting", ref val, 2, 0, 2, 1, 0)
                        && val >= 0 && val <= 2)
                    {
                        SetSysCnfQuality((int)val);
                    }
                    posYs -= 5;

                    if (gettoggle("描画設定を固定する", cfg.QualitySetting >= 0))
                    {
                        cfg.QualitySetting = ConfigClass.QualitySetting;
                    }
                    else
                    {
                        cfg.QualitySetting = -1;
                    }
                }
                posYs += 10;
                posYs += 5;


                using (new UiUtil.GuiColor(colorDemiCyan))
                    labelctrl($"カメラ", 0, 0);
                posYs += 5;
                var camo = Camera.main;
                using(new UiUtil.GuiEnable(camo))
                    ConfigClass.Fov = (getfloatctrl("FOV", Mathf.Lerp(10f, 100f, ConfigClass.Fov), Mathf.Lerp(10f, 100f, CameraFovDefValueThisScene), 10f, 100f) - 10f) / 90f;
                if (camo)
                {
                    camo.nearClipPlane = getfloatctrl("描写範囲:近(nearClip)", camo.nearClipPlane, 0.3f, 0f, 0.5f);
                    camo.farClipPlane = getfloatctrl("描写範囲:遠(farClip)", camo.farClipPlane, 1000f, 0f, 1000f);
                    var roll = camo.transform.localEulerAngles.angle180().z;
                    if (floatctrl("Roll", ref roll, 0f, -180f, 180f))
                        camo.transform.localEulerAngles = new Vector3(camo.transform.localEulerAngles.x, camo.transform.localEulerAngles.y, roll);
                }
                posYs += 5;
                posYs += 5;

                using (new UiUtil.GuiColor(colorDemiCyan))
                    labelctrl("ボイス設定", 0, 0);
                using(new UiUtil.GuiColor(Color.yellow, SysVoiceMgr.Enabled))
                {
                    var b1 = gettoggle("ボイス制御ON", SysVoiceMgr.Enabled);
                    posYs += 5;
                    using(new UiUtil.GuiEnable(b1))
                    {
                        var f1 = getfloatctrl("ボイスピッチ (ネコ)", SysVoiceMgr.voicePich[0], 1f, 0.7f, 1.3f, 1);
                        var f2 = getfloatctrl("ボイスピッチ (ウサ)", SysVoiceMgr.voicePich[1], 1f, 0.7f, 1.3f, 1);

                        if (b1 != SysVoiceMgr.Enabled || f1 != SysVoiceMgr.voicePich[0] || f2 != SysVoiceMgr.voicePich[1])
                        {
                            if (!b1)
                                f1 = f2 = 1f;

                            cfg.voicePichEnabled = b1;
                            cfg.voicePich[0] = f1;
                            cfg.voicePich[1] = f2;

                            // 設定更新
                            SysVoiceMgr.OnUpdate(true);
                        }
                    }
                }
                labelctrl("※簡易実装のため再生時間も変化します", 5, -5);

                posYs += 5;
                posYs += 10;
                //posYs += 5;

                using (new UiUtil.GuiColor(colorDemiCyan))
                    labelctrl("プラグイン設定", 0, 0);

                cfg.InactiveCharCtrlVisible = gettoggle("無効中のキャラ設定も表示", cfg.InactiveCharCtrlVisible);
                cfg.ctrlIncludePlayer = gettoggle("プレイヤーを対象にする", cfg.ctrlIncludePlayer);
                cfg.ctrlIncludeShota = gettoggle($"ショタを対象にする", cfg.ctrlIncludeShota);
                cfg.CloneCtrlVisible = gettoggle("クローン設定を表示", cfg.CloneCtrlVisible);
                cfg.FixFreeModeCustomToTitleBtn = gettoggle("ﾌﾘｰﾓｰﾄﾞ/ｶｽﾀﾑ:タイトルボタン改善", cfg.FixFreeModeCustomToTitleBtn);
                cfg.AddNoseBlendShape = gettoggle("鼻のｼｪｲﾌﾟｷｰ(@Nose_Flat)を追加生成", cfg.AddNoseBlendShape);

                posYs += 10;
#if DEBUG
                //MyHook.yoinX = getfloatctrl("余韻時間の変更x", MyHook.yoinX, 1, 1, 5, 1);
                MyHook.fixAnimePlayIssue = gettoggle("一部カメラのアニメ設定省略", MyHook.fixAnimePlayIssue);
                MyHook.hookLog = gettoggle("HookLog", MyHook.hookLog);
#endif
            }
            posYs += 10;
        }

        void HsystemCtrl()
        {
            GUI.contentColor = Color.cyan;
            GUI.Label(RectU.sclRect(ItemX + 20, (posYs += 20), LBL_WD, 20), "Hシーン設定", gsLabel);
            GUI.contentColor = Color.white;
            if (rootbtn(nowEdit = "HSystem"))
            {
                posYs += 10;
                using (new UiUtil.GuiColor(colorDemiCyan))
                    labelctrl($"Hシーン設定", 0, 0);
                //posYs += 5;

                using (new UiUtil.GuiColor(Color.yellow, HsysMgr.Config.ClimaxMilk1))
                    cfg.HSysMgrConfig.ClimaxMilk1 = gettoggle("絶頂時:母乳1 自動ON", HsysMgr.Config.ClimaxMilk1);
                using (new UiUtil.GuiColor(Color.yellow, HsysMgr.Config.ClimaxMilk2))
                    cfg.HSysMgrConfig.ClimaxMilk2 = gettoggle("絶頂時:母乳2 自動ON", HsysMgr.Config.ClimaxMilk2);
                if (togglectrl2("前戯:卓上カレンダー位置修正", ref cfg.FixTableCalendarPos))
                {
                    base.StartCoroutine(BGMGR.FixCalendarPos(actScene.name));
                }
                var calendar = Extentions.FindSp("UI Root(screen)", true);
                if (calendar)
                {
                    var hide = gettoggle("前戯/FH:卓上カレンダー非表示(一時)", !calendar.activeSelf);
                    if (hide != !calendar.activeSelf)
                        calendar.SetActive(!hide);
                }

                posYs += 10;
                using (new UiUtil.GuiColor(colorDemiCyan))
                    labelctrl($"フリーモード拡張 (β)", 0, 0);
                //posYs += 5;

                using (new UiUtil.GuiColor(Color.yellow, HsysMgr.Config.DebugTopToFreeMode && SaveLoad_Game.Data.savegamedata.DebugModeShortCut))//GameClass.DebugTop))
                {
                    var bodtf = gettoggle("デバッグモード優先時:Hをフリーモードに", HsysMgr.Config.DebugTopToFreeMode);
                    if (HsysMgr.Config.DebugTopToFreeMode != bodtf)
                    {
                        HsysMgr.Config.DebugTopToFreeMode = bodtf;

                        //20200712fix
                        /*
                        if (actScene.name == "Title")
                            GameClass.FreeMode = bodtf;*/
                    }
                }

                using (new UiUtil.GuiColor(Color.yellow, HsysMgr.Config.AutoLevelUpFreeMode && GameClass.FreeMode && (actScene.name == "IC" || actScene.name == "FH")))
                    HsysMgr.Config.AutoLevelUpFreeMode = gettoggle("前戯&フリーH:絶頂レベルアップ有", HsysMgr.Config.AutoLevelUpFreeMode);
                posYs += 5;
                posYs += 2;

                if (HsysMgr.Config.AutoLevelUpFreeMode)
                    HsysMgr.Config.AutoLevelUpTolerance = getintctrl(" 絶頂レベルアップ耐性", HsysMgr.Config.AutoLevelUpTolerance, 2, -10, 20);

                using (new UiUtil.GuiColor(Color.yellow, HsysMgr.Config.UseFree3P))
                    HsysMgr.Config.UseFree3P = gettoggle("フリー3P (次Hシーンより有効)", HsysMgr.Config.UseFree3P);
                posYs += 5;
                if (HsysMgr.Config.UseFree3P)
                {
                    //if (actScene.name == "Title")
                    //    Free3P.MotionAndVoices.Setup();

                    if (Free3P.MotionAndVoices.IsLoading || !Free3P.MotionAndVoices.IsLoaded)
                    {
                        using (new UiUtil.GuiColor(Color.yellow))
                        {
                            if (actScene.name == "UC" || actScene.name == "IC" || actScene.name == "ADV" || actScene.name == "FH")
                            {
                                labelctrl("※初期設定の完了不可", 5, 5);
                                labelctrl("※一度タイトル画面に戻って下さい", 5, 5);
                                posYs += 5;
                            }
                            else
                                labelctrl("※シーン変更待機中", 5, 5);
                        }
                        posYs += 5;
                    }

                    //using (new UiUtil.GuiEnable((actScene.name == "IC" || actScene.name == "FH") && Free3P.CheckSubMem() && !Free3pXtMS.BoneLink))
                    var boneLinked = Free3pXtMS.IsMasterOrSlave(Free3P.mainMemModel) || Free3pXtMS.IsMasterOrSlave(Free3P.subMemModel);
                    
                    using (new UiUtil.GuiEnable((actScene.name == "IC" || actScene.name == "FH") && Free3P.CheckSubMem() && !boneLinked))
                    {
                        if (centerbtn("フリー3P Main/Sub交代"))
                        {
                            if (Free3P.gizmoPosroot.visible)
                                Free3P.gizmoPosroot.resetTarget();

                            Free3P.ChangeMainChar(false);
                        }
                        posYs += 5;

                        if (centerbtn("体位を維持したまま交代"))
                        {
                            if (Free3P.gizmoPosroot.visible)
                                Free3P.gizmoPosroot.resetTarget();

                            Free3P.ChangeMainChar(true);
                        }
                    }
                    if (Free3P.IsLoaded)
                    {
                        if ((actScene.name == "IC" || actScene.name == "FH") && !Free3P.CheckSubMem())
                        {
                            labelctrl("※サブキャラがレベル不足かも…?", 10, 0);
                        }
                        else if (!(actScene.name == "IC" || actScene.name == "FH"))
                        {
                            labelctrl("※特殊Hでは交代不可", 10, 0);
                        }
                        else if (boneLinked)//(Free3pXtMS.BoneLink)
                        {
                            labelctrl("※XtMS実行中は交代不可", 10, 0);
                        }
                    }

                    posYs += 5;
                    posYs += 5;

                    var f3psubclr = (Free3P.IsLoaded && Free3P.subMemModel) ? Color.yellow : Color.white;
                    //labelctrl("3P用サブキャラ操作", 5, 0);
                    if (rootbtn2(nowEdit + "Free3Pサブキャラ", "3P:サブキャラ操作", 5, 0, f3psubclr))
                    {
                        using (new UiUtil.GuiEnable(Free3P.IsLoaded && Free3P.MotionAndVoices.usaData.Count > 0))
                            if (Free3P.MotionAndVoices.usaData.Count > 0) // ウサ子の方でチェック（兎がOKなら猫もOKのはず理論
                            {
                                int index = 0; // 非アクティブな方のインデックス
                                int level = SaveLoad_Game.Data.savegamedata.Level_CH01;
                                if (SaveLoad_Game.Data.savegamedata.SceneID.Substring(2, 2) == "01")
                                {
                                    // ネコ選択中ならウサ対象
                                    index = 1;
                                    labelctrl($"{ctrlGirls[1].ini.name} Level: {SaveLoad_Game.Data.savegamedata.Level_CH02}", 10, 0);
                                    //SaveLoad_Game.Data.savegamedata.Level_CH02 = getintctrl($" {ctrlGirls[1].ini.name} Level", SaveLoad_Game.Data.savegamedata.Level_CH02, 0, -1, 3);
                                    level = SaveLoad_Game.Data.savegamedata.Level_CH02 + 1;
                                }
                                else
                                {
                                    labelctrl($"{ctrlGirls[0].ini.name} Level: {SaveLoad_Game.Data.savegamedata.Level_CH01}", 10, 0);
                                    //SaveLoad_Game.Data.savegamedata.Level_CH01 = getintctrl($" {ctrlGirls[0].ini.name} Level", SaveLoad_Game.Data.savegamedata.Level_CH01, 0, 0, 4);
                                    level = SaveLoad_Game.Data.savegamedata.Level_CH01;
                                }

                                const int btnLNUM = 3;
                                int btnnum = 0;
                                int btnw = (SLDW - 30) / btnLNUM;
                                foreach (var s in Free3P.MotionAndVoices.data[index][level].motionAndVoiceDataList)
                                {
                                    if (s.isClimax)
                                        continue;

                                    if (btnnum % btnLNUM == 0)
                                        posYs += 20;

                                    var btnDisp = s.state.Substring("(XX)".Length);
                                    var btnTag = s.state.Substring(0, 4);

                                    using (new UiUtil.GuiEnable(!string.IsNullOrEmpty(s.motionName) && s.audioClip != null
                                            //&& !(btnTag == "(IC)" && index == 1 && actScene.name == "FH")
                                            && !(btnTag == "(FH)" && index == 1 && actScene.name != "FH")
                                            )
                                        )
                                        if (GUI.Button(RectU.sclRect(LX + 10 + btnw * (btnnum % btnLNUM), posYs, btnw, ItemHeight), btnDisp, gsButton))
                                        {
                                            Free3P.MotionAndVoices.Play(ctrlGirls[index], s.state, level);
                                            //Free3P.MotionAndVoices.data[index][level].Play(ctrlGirls[index], s.state);
                                        }
                                    btnnum++;
                                }

                            }

                        using (new UiUtil.GuiColor(Color.yellow, HsysMgr.Config.Free3p_EnableAngleFixIC2FH))
                            HsysMgr.Config.Free3p_EnableAngleFixIC2FH = gettoggle("フリーHで前戯体位を使用時:角度を補正", HsysMgr.Config.Free3p_EnableAngleFixIC2FH);
                        posYs += 5;


                        // v0.92で追加
                        if (Free3PVoicePlayer.Inst && Free3PVoicePlayer.Inst.gameObject == Free3P.subMemModel)
                        {
                            var otr = Free3P.subMemModel.transform.FindSp("IT01_mekakusi");
                            if (otr)
                            {
                                ctrl_smitem("目隠し", otr.gameObject);
                            }

                            otr = Free3P.subMemModel.transform.FindSp("IT04_chain_Neck");
                            if (otr)
                            {
                                ctrl_smitem("首輪", otr.gameObject);
                            }
                            otr = Free3P.subMemModel.transform.FindSp("IT04_chainHand00");
                            if (otr)
                            {
                                ctrl_smitem("手枷", otr.gameObject);
                            }
                            otr = Free3P.subMemModel.transform.FindSp("IT04_chainLeg00");
                            if (otr)
                            {
                                ctrl_smitem("足枷", otr.gameObject);
                            }
                            var gb = Free3PVoicePlayer.Inst.mTrGagball;
                            if (gb)
                            {
                                ctrl_smitem("ギャグボール", gb.gameObject);
                            }
                            labelctrl("※解除は[表情・状態変更]>[ｱｲﾃﾑ表示設定]", 10,0);

                            void ctrl_smitem(string name, GameObject go)
                            {
                                var hit = Free3PVoicePlayer.Inst.girlCtrl.faceCtrl.activateItems.TryGetValue(go, out bool val);

                                if (!hit)
                                    val = go.activeSelf;

                                if (togglectrl("拘束具: " + name, ref val))
                                {
                                    Free3PVoicePlayer.Inst.girlCtrl.faceCtrl.activateItems[go] = val;
                                }
                            }
                            posYs += 5;
                        }
                    }
                    posYs += 5;

                    using (new UiUtil.GuiEnable(Free3P.subMemModel))
                    {
                        if (rootbtn2(nowEdit + "Free3Pオフセット", "3P:サブ原点位置変更", 5, 0, f3psubclr))
                        {
                            posYs += 5;

                            // プリセット
                            var tag = "Free3p_Offset";
                            ScenePos obj = null;
                            if (is_preset_save(tag))
                            {
                                obj = new ScenePos(Free3P.mainMemModel.transform.root.gameObject, false);
                                obj.enable = true;
                                obj.pos = Free3P.subMemModel.transform.root.position;
                                obj.rot = Free3P.subMemModel.transform.root.eulerAngles.angle180();
                                obj.scale = 1f;
                            }
                            if (presetctrl(ref obj, tag))
                            {
                                Free3P.subMemModel.transform.root.position = obj.pos;
                                Free3P.subMemModel.transform.root.eulerAngles = obj.rot;
                            }
                            posYs += 5;

                            Free3P.subMemModel.transform.root.localPosition =
                                v3ctrl_test("Pos-", Free3P.subMemModel.transform.root.localPosition, Free3P.mainMemModel.transform.root.localPosition, -5, 5f);
                            Free3P.subMemModel.transform.root.localEulerAngles =
                                v3ctrl("Rot-", Free3P.subMemModel.transform.root.localEulerAngles.angle180(), 
                                                 Free3P.mainMemModel.transform.root.localEulerAngles.angle180(), -180f, 180f);

                            if (!Free3P.gizmoPosroot)
                                Free3P.GizmoInit();

                            if (gettoggle("Gizmo", Free3P.gizmoPosroot.visible))
                            {
                                if (!Free3P.gizmoPosroot.visible)
                                    Free3P.gizmoPosroot.setTarget(Free3P.subMemModel.transform.root, Free3P.subMemModel.transform.root.name);
                            }
                            else
                            {
                                if (Free3P.gizmoPosroot.visible)
                                    Free3P.gizmoPosroot.resetTarget();
                            }
                            posYs += 5;

                            if (centerbtn("デフォルトオフセットに設定"))
                            {
                                HsysMgr.Config.Free3p_OffestSubMem = Free3P.subMemModel.transform.root.localPosition - Free3P.mainMemModel.transform.root.localPosition;
                                HsysMgr.Config.Free3p_OffestRotSubMem = (Free3P.subMemModel.transform.root.localEulerAngles - Free3P.mainMemModel.transform.root.localEulerAngles).angle180();
                            }
                            if (HsysMgr.Config.Free3p_OffestSubMem != Vector3.zero && centerbtn("デフォルトオフセットを破棄"))
                            {
                                HsysMgr.Config.Free3p_OffestSubMem = Vector3.zero;
                                HsysMgr.Config.Free3p_OffestRotSubMem = Vector3.zero;
                            }
                            posYs += 5;

                            posYs += 5;

                            ScenePos scenePos = new ScenePos();
                            scenePos.enable = true;
                            var pos_ = scenePos.pos = Free3P.subMemModel.transform.root.position;
                            var rot_ = scenePos.rot = Free3P.subMemModel.transform.root.eulerAngles.angle180();
                            scenePos.scale = 1f;
                            BGposCtrlByState("3Pサブ", "シーンID", scenePos, HsysMgr.Config.Free3p_OffsetByState);
                            posYs += 5;

                            if (scenePos.pos != pos_ || scenePos.rot != rot_)
                            {
                                // 読み込まれた
                                Free3P.subMemModel.transform.root.position = scenePos.pos;
                                Free3P.subMemModel.transform.root.eulerAngles = scenePos.rot;
                            }
                        }
                    }
                    posYs += 5;


                    using (new UiUtil.GuiEnable(GameClass.FreeMode))
                    {
                        var free3pXtMS = Free3P.XtMS;
                        if (rootbtn2(nowEdit + "Free3P_XtMS", "Xtマスタースレイブ", 5, 0, free3pXtMS.BoneLink ? f3psubclr : Color.white))
                        {
                            var bkupYs = posYs += 5;
                            if (!free3pXtMS.BoneLink)
                            {
                                if (Free3P.subMemModel)
                                {
                                    for (int i = (int)TargetIni.TargetChar.Player; i <= (int)TargetIni.TargetChar.Shota03; i++)
                                    {
                                        if (ctrlGirls[i].isActive() && (GameClass.ManBody || GameClass.ManHand_L || GameClass.ManHand_R || GameClass.ManPenis || i != (int)TargetIni.TargetChar.Player))
                                            if (centerbtn($"M:{ctrlGirls[i].ini.name} / S:3Pサブ")) free3pXtMS.SetMS_Free3pSub((TargetIni.TargetChar)i);
                                    }
                                }

                                
                                // ショタ→♀
                                for (int j = (int)TargetIni.TargetChar.Neko; j <= (int)TargetIni.TargetChar.Usa; j++)
                                {
                                    if (!ctrlGirls[j].isActive())
                                        continue;

                                    for (int i = (int)TargetIni.TargetChar.Shota01; i <= (int)TargetIni.TargetChar.Shota03; i++)
                                    {
                                        if (ctrlGirls[i].isActive())// || actScene.name == "UC") の本格的なリンクは実装に時間が掛かりそうなので保留
                                        {
                                            if (i == (int)TargetIni.TargetChar.Shota01)
                                                posYs += 10;

                                            if (centerbtn($"M:{ctrlGirls[j].ini.name} / S:{ctrlGirls[i].ini.name}"))
                                                free3pXtMS.SetMS((TargetIni.TargetChar)j, (TargetIni.TargetChar)i);
                                        }
                                    }
                                }
                                
                            }                             
                            else
                            {
                                using(new UiUtil.GuiColor(Color.green))
                                {
                                    labelctrl($"XtMaster : {free3pXtMS.xMaster.girlCtrl.ini.name}", 10, 0);
                                    labelctrl($"XtSlave : {free3pXtMS.xSlave.girlCtrl.ini.name}", 10, 0);
                                }

                                if (centerbtn($"M/Sボーンリンク解除")) free3pXtMS.ResetMS();

                            }

                            if (bkupYs == posYs)
                                labelctrl("選択可能なターゲットが不在", 10, 0);
                            else
                            {
                                labelctrl("※表情はサブキャラ操作のオナ選択等で付与", 5, 0);
                                labelctrl("※位置は体形ｽﾗｲﾀﾞｰのキャラ位置補正で調整", 5, 0);
                                labelctrl("　（位置補正基準は骨盤を推奨）", 5, 0);
                                labelctrl("※リンク中は登録シーン/モーション名が変化", 5, 0);

                            }
                        }
                    }
                }

                posYs += 10;

                using (new UiUtil.GuiColor(colorDemiCyan))
                    labelctrl($"前戯プレイ", 0, 0);
                //posYs += 5;
                using (new UiUtil.GuiEnable(GameClass.FreeMode && actScene.name == "IC"))
                {
                    HsysMgr.EnableGyagboWithIC = gettoggle("前戯のギャグボ強制解放(一時)", HsysMgr.EnableGyagboWithIC);
                }
                labelctrl($"※フリーモードのみ", 20, 0);

                posYs += 10;

                using (new UiUtil.GuiColor(colorDemiCyan))
                    labelctrl($"特殊プレイ", 0, 0);
                //posYs += 5;

                using (new UiUtil.GuiEnable(SaveLoad_Game.Data.savegamedata.ClearBonusCH01))
                using (new UiUtil.GuiColor(Color.yellow, HsysMgr.EnabledStLock && actScene.name == "UC"))
                    HsysMgr.EnabledStLock = gettoggle("HP/発情/酔い度ホールド(クリア後/一時)", HsysMgr.EnabledStLock);
                posYs += 10;

                using (new UiUtil.GuiColor(Color.yellow, HsysMgr.Config.FreqBG3Car > 0 && Extentions.FindSp("BG03/Car")))
                    cfg.HSysMgrConfig.FreqBG3Car = getintctrl("屋外調教、車通行頻度", HsysMgr.Config.FreqBG3Car, 0, 0, 100);
                if (cfg.HSysMgrConfig.FreqBG3Car > 0)
                    labelctrl($"(車通行目安: {HsysMgr.CarWithUC.carTimerMin} ～ {HsysMgr.CarWithUC.carTimerMax:0.##} 秒×確率)", 20, -10);

                posYs += 10;
                posYs += 5;

                using (new UiUtil.GuiColor(colorDemiCyan))
                    labelctrl($"Hアニメ制御 (α)", 0, 0);

                using (new UiUtil.GuiColor(Color.yellow, HsysMgr.Config.ChangeYoinAnime))
                    cfg.HSysMgrConfig.ChangeYoinAnime = gettoggle("余韻モーションを抑制", HsysMgr.Config.ChangeYoinAnime);
                using (new UiUtil.GuiColor(Color.yellow, HsysMgr.Config.AutoRestart))
                    cfg.HSysMgrConfig.AutoRestart = gettoggle("絶頂後、前コマンドで自動再開", HsysMgr.Config.AutoRestart);
                posYs += 5;
                posYs += 5;
                using (new UiUtil.GuiColor(Color.yellow, MyHook.yoinTimeIC > 0))
                    cfg.HsysYoinTimeIC = getfloatctrl("アニメMIN時間(前戯)", MyHook.yoinTimeIC, 0, 0, 5, 1);
                using (new UiUtil.GuiColor(Color.yellow, MyHook.yoinTimeFH > 0))
                    cfg.HsysYoinTimeFH = getfloatctrl("アニメMIN時間(H)", MyHook.yoinTimeFH, 0, 0, 5, 1);
                labelctrl(" ※1以上に設定すると絶頂後の間などが延長", 5, -5);
                cfg.HsysYoinOnlyClimax = gettoggle("MIN時間を絶頂時のみ適用", MyHook.yoinOnlyClimax);
                posYs += 5;
                
                //posYs += 10;
#if DEBUG
                //MyHook.yoinX = getfloatctrl("余韻時間の変更x", MyHook.yoinX, 1, 1, 5, 1);
                MyHook.fixAnimePlayIssue = gettoggle("一部カメラのアニメ設定省略", MyHook.fixAnimePlayIssue);
                MyHook.hookLog = gettoggle("HookLog", MyHook.hookLog);
#endif


                posYs += 5;
                if (rootbtn2(nowEdit + "_PlayClip-Debug", "DEBUG", 5 + (SLDW * 2 / 3), 0))
                {
                    var tenkeyGo = Extentions.FindSp("UI Root(FH)/TenKey/TenKey_BG");
                    if (tenkeyGo != null)
                    {
                        var tenKeyPad = tenkeyGo.GetComponent<TenKeyPad>();

                        labelctrl($"FULLID: {tenKeyPad.FullID}", 0, 0);

                        var fiTenKeyPad_SceneLevel = typeof(TenKeyPad).GetField("SceneLevel", BindingFlags.NonPublic | BindingFlags.Instance);
                        var level = (int)fiTenKeyPad_SceneLevel.GetValue(tenKeyPad);
                        var deflvl = tenKeyPad.CharaID == "01" ? SaveLoad_Game.Data.savegamedata.Level_CH01 : SaveLoad_Game.Data.savegamedata.Level_CH02;
                        labelctrl($"シーンレベル: {level} ({deflvl})", 0, 0);
                        posYs += 5;
                    }

                    labelctrl($"モーション・ボイス状態", 0, 0);
                    string vstate = "";
                    for (int i = 0; i < 2; i++)
                    {
                        if (ctrlGirls[i].isActive())
                        {
                            vstate += $"{ctrlGirls[i].ini.id}, M:{ctrlGirls[i].charaCtrl.getIOAnimeStateStringDBG(false)}, V:{SysVoiceMgr.lastPlayVoice[i]}\n";
//#if DEVMODE
                            var go = ctrlGirls[i].FindModel();
                            var ani = go.GetComponent<Animator>();
                            var clipInfo = ani.GetCurrentAnimatorClipInfo(0).OrderByDescending(x => x.weight).FirstOrDefault(x => x.weight > 0);
                            var overrideController = ani.runtimeAnimatorController as AnimatorOverrideController;
                            if (overrideController && clipInfo.clip)
                            {
                                List<KeyValuePair<AnimationClip, AnimationClip>> overrides;
                                overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
                                overrideController.GetOverrides(overrides);
                                var state = overrides.FirstOrDefault(x => x.Value && x.Value.name == clipInfo.clip.name);
                                if (state.Key != null)
                                    vstate += "S: " + state.Key.name + "\n";
                            }
//#endif
                        }
                    }

#if DEVMODE
                    foreach (var s in MyHook.LastAnimeStateByGameObj)
                    {
                        foreach (var c in s.Value)
                            vstate += $" {s.Key} {c.Key}:{c.Value}\n";
                    }

                    for (int i = 0; i < 2; i++)
                    {
                        if (ctrlGirls[i].isActive())
                        {

                            vstate += $"\nClipList: {ctrlGirls[i].ini.id}\n";
                            var go = ctrlGirls[i].FindModel();
                            var ani = go.GetComponent<Animator>();

                            for (int j =0; j<ani.layerCount; j++)
                            {
                                var clipInfos = ani.GetCurrentAnimatorClipInfo(j);
                                var state = ani.GetCurrentAnimatorStateInfo(j);

                                vstate += $" {j}:{ani.GetLayerName(j)} LayerWeight:{ani.GetLayerWeight(j)}\n";

                                foreach (var c in clipInfos)
                                {
                                    if (c.clip)
                                        vstate += $"   {c.clip.name} {c.weight}\n";
                                }
                            }

                            /*var overrideController = ani.runtimeAnimatorController as AnimatorOverrideController;
                            if (overrideController)
                            {
                                List<KeyValuePair<AnimationClip, AnimationClip>> overrides;
                                overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
                                overrideController.GetOverrides(overrides);
                                foreach (var c in overrides)
                                {
                                    if (c.Key && c.Value)
                                        vstate += $" {state.IsName(ani.GetLayerName(0)+"."+c.Key.name)}/{state.IsName(ani.GetLayerName(0) + "." + c.Value.name)} {ani.GetLayerName(0) + "." + c.Key.name} {c.Value.name}\n";
                                }
                            }*/
                        }
                    }
#endif

                    GUI.TextArea(RectU.sclRect(LX, posYs += 20, SLDW, ItemHeight * 4), vstate, gsTextArea);
                    posYs += ItemHeight * 3;

                    labelctrl("※M:生ﾓｰｼｮﾝClip名(末尾_S～Lは胸ｻｲｽﾞ)", 5, 0);
                    labelctrl("※V:ﾋﾟｯﾁ変更ｷｬﾗのみ取得/Hｼｰﾝ以外は非対応", 5, 0);
                    posYs += 10;

                    if (GameObject.Find("CH01") != null)
                    {
                        var v = getfloatctrl("胸サイズ (Neko)", SaveLoad_CharaData01.Data.savegamedata.BreastSize, 0.25f, 0f, 1f);
                        if (v != SaveLoad_CharaData01.Data.savegamedata.BreastSize)
                        {
                            SaveLoad_CharaData01.Data.savegamedata.BreastSize = v;
                            GameObject.Find("CH01").GetComponent<CostumeSetUp_CH01>().SetNonPublicField("BreastSize", v);
                            GameObject.Find("CH01").GetComponent<CostumeSetUp_CH01>().ReLoad();
                        }
                    }
                    if (GameObject.Find("CH02") != null)
                    {
                        var v = getfloatctrl("胸サイズ (Usa)", SaveLoad_CharaData02.Data.savegamedata.BreastSize, 0.8f, 0f, 1f);
                        if (v != SaveLoad_CharaData02.Data.savegamedata.BreastSize)
                        {
                            SaveLoad_CharaData02.Data.savegamedata.BreastSize = v;
                            GameObject.Find("CH02").GetComponent<CostumeSetUp_CH02>().SetNonPublicField("BreastSize", v);
                            GameObject.Find("CH02").GetComponent<CostumeSetUp_CH02>().ReLoad();
                        }
                    }
                    posYs += 10;

                    //Free3pXtMS.BoneLink = gettoggle("BoneLinkTest", Free3pXtMS.BoneLink);
                    //posYs += 10;
                }

#if DEBUG || DEVMODE2
                HsysMgr.SetGameLevel_CH01 = getintctrl($" {ctrlGirls[0].ini.name} Level", SaveLoad_Game.Data.savegamedata.Level_CH01, 0, 0, 4);
                labelctrl("Mpara: " + SaveLoad_Game.Data.savegamedata.MainParaCH01);

                HsysMgr.SetGameLevel_CH02 = getintctrl($" {ctrlGirls[1].ini.name} Level", SaveLoad_Game.Data.savegamedata.Level_CH02, 0, 0, 3);
                labelctrl("Mpara: " + SaveLoad_Game.Data.savegamedata.MainParaCH02);
#endif
#if DEBUG || DEVMODE2
                posYs += 5;
                GameClass.First3P = gettoggle("3Pフラグ", GameClass.First3P);
                posYs += 5;

                if (centerbtn("おなせっとあっぷ"))
                {
                    Free3P.MotionAndVoices.Setup();
                }


                if (centerbtn("おなすたーと"))
                {
                    var orgid = SaveLoad_Game.Data.savegamedata.SceneID;
                    var chrid = orgid.Substring(2, 2);
                    if (chrid == "01")
                        Free3P.MotionAndVoices.Play(ctrlGirls[1], "オナ-B", SaveLoad_Game.Data.savegamedata.Level_CH02+1);
                    else
                        Free3P.MotionAndVoices.Play(ctrlGirls[0], "オナ-B", SaveLoad_Game.Data.savegamedata.Level_CH01);

                    //var mainSystem = GameObject.Find("MainSystem");

                    //var setup = mainSystem.GetComponent<IC_SetUp>();
                    //if (setup)
                    //    setup.GetNonPublicField<AssetBundle>("bundle").Unload(false);

                    //onaVM.ona1neko.Play(ctrlGirls[0], 1);
                }

                if (centerbtn("3P初期化テスト"))
                {
                    if (!GameClass.First3P)
                        return;

                    var mainSystem = GameObject.Find("MainSystem");

                    GameClass.First3P = false;
                    var orgid = SaveLoad_Game.Data.savegamedata.SceneID;
                    var chrid = orgid.Substring(2, 2);
                    if (chrid == "01")
                    {
                        SaveLoad_Game.Data.savegamedata.SceneID = orgid.Substring(0, 2) + "02";
                        resetup();
                        //var name = ctrlGirls[1].ini.rootObj.name;
                        //ctrlGirls[1].ini.rootObj.name += "**test**";
                        //SaveLoad_Game.Data.savegamedata.SceneID = orgid.Substring(0, 2) + "01";
                        var overrideController = ctrlGirls[1].FindModel().GetComponent<Animator>().runtimeAnimatorController as AnimatorOverrideController;
                        SaveLoad_Game.Data.savegamedata.SceneID = orgid;
                        resetup(true);
                        //ctrlGirls[1].ini.rootObj.name = name;
                        restart(true);
                        ctrlGirls[1].FindModel().GetComponent<Animator>().runtimeAnimatorController = overrideController;
                    }
                    else
                    {
                        SaveLoad_Game.Data.savegamedata.SceneID = orgid.Substring(0, 2) + "01";
                        resetup();
                        //var name = ctrlGirls[0].ini.rootObj.name;
                        //ctrlGirls[0].ini.rootObj.name += "**test**";
                        //SaveLoad_Game.Data.savegamedata.SceneID = orgid.Substring(0, 2) + "02";
                        var overrideController = ctrlGirls[0].FindModel().GetComponent<Animator>().runtimeAnimatorController as AnimatorOverrideController;


                        //var setup = mainSystem.GetComponent<IC_SetUp>();
                        //if (setup)
                        //    setup.GetNonPublicField<AssetBundle>("bundle").Unload(false);
                        //onaVM.ona1neko.Play(ctrlGirls[0], SaveLoad_Game.Data.savegamedata.Level_CH01);

                        SaveLoad_Game.Data.savegamedata.SceneID = orgid;
                        resetup(true);
                        //ctrlGirls[0].ini.rootObj.name = name;
                        restart(true);
                        ctrlGirls[0].FindModel().GetComponent<Animator>().runtimeAnimatorController = overrideController;
                    }

                    void resetup(bool end = false)
                    {
                        switch (actScene.name)
                        {
                            case "FH":
                                {
                                    var setup = mainSystem.GetComponent<FH_SetUp>();
                                    //if (!end)
                                        setup.Unload();
                                   
                                    setup.VoiceData.Clear();
                                    setup.GyagVoice.Clear();
                                    setup.UraVoice.Clear();
                                    setup.WordPlay.Clear();
                                    setup.InvokeNonPublicMethod("Awake", null);
                                }

                                //if (end)
                                //{
                                //    mainSystem.GetComponent<FH_AnimeController>().InvokeNonPublicMethod("Start", null); // フリー
                                //}

                                break;

                            case "IC":
                                {
                                    var setup = mainSystem.GetComponent<IC_SetUp>();
                                    //if (!end)
                                        setup.Unload();
                                    //else
                                    //    setup.GetNonPublicField<AssetBundle>("bundle").Unload(false);
                                    setup.VoiceData.Clear();
                                    setup.GyagVoice.Clear();
                                    setup.InvokeNonPublicMethod("Awake", null);
                                }

                                //if (end)
                                //{
                                //    mainSystem.GetComponent<IC_AnimeController>().InvokeNonPublicMethod("Start", null); // 前

                                //    var ui = GameObject.Find("UI Root(Menu)");
                                //    if (ui)
                                //    {
                                //        ui.GetComponent<MenuUI>().ZengiStart();
                                //    }
                                //}
                                break;
                        }
                    }

                    void restart(bool end = false)
                    {
                        switch (actScene.name)
                        {
                            case "FH":
                                if (end)
                                {
                                    mainSystem.GetComponent<FH_AnimeController>().InvokeNonPublicMethod("Start", null); // フリー
                                }

                                break;

                            case "IC":
                                if (end)
                                {
                                    mainSystem.GetComponent<IC_AnimeController>().InvokeNonPublicMethod("Start", null); // 前

                                    var ui = GameObject.Find("UI Root(Menu)");
                                    if (ui)
                                    {
                                        ui.GetComponent<MenuUI>().ZengiStart();
                                    }
                                }
                                break;
                        }
                    }

                }
                /*
                string vstate = "";
                for(int i = 0; i < 2; i++)
                {
                    if (ctrlGirls[i].isActive())
                        vstate += $"{ctrlGirls[i].ini.id}, M:{ctrlGirls[i].charaCtrl.getAnimeStateString()}, V:{SysVoiceMgr.lastPlayVoice[i]}\n";
                }
                GUI.TextArea(RectU.sclRect(LX, posYs += 20, SLDW, ItemHeight * 4), vstate, gsTextArea);
                posYs += ItemHeight * 4;
                */
                // テスト用
                if (actScene.name == "IC" || actScene.name == "FH" || actScene.name == "UC")
                {
                    posYs += 5;
                    var tenkeyGo = Extentions.FindSp("UI Root(FH)/TenKey/TenKey_BG");
                    if (tenkeyGo != null)
                    {
                        var tenKeyPad = tenkeyGo.GetComponent<TenKeyPad>();

                        labelctrl($"FULLID: {tenKeyPad.FullID}", 0, 0);
                        
                        var fiTenKeyPad_SceneLevel = typeof(TenKeyPad).GetField("SceneLevel", BindingFlags.NonPublic | BindingFlags.Instance);

                        var level = (int)fiTenKeyPad_SceneLevel.GetValue(tenKeyPad);
                        var deflvl = tenKeyPad.CharaID == "01" ? SaveLoad_Game.Data.savegamedata.Level_CH01 : SaveLoad_Game.Data.savegamedata.Level_CH02;

                        var newlvl = getintctrl("シーンレベル", level, deflvl, 0, tenKeyPad.CharaID == "01" ? 4 : 3);
                        if (newlvl != level)
                        {
                            HsysMgr.SetSceneLevel(newlvl);
                            KeyMacro.PushKeypad(Keybd.KeypadCode.Enter);
                        }

                        if (actScene.name == "IC" && tenKeyPad.CharaID == "01" && newlvl >= 1 && newlvl <= 2)
                        {
                            float old = SaveLoad_Game.Data.savegamedata.Drunk;
                            SaveLoad_Game.Data.savegamedata.Drunk = getintctrl("酔い度", (int)SaveLoad_Game.Data.savegamedata.Drunk, 0, 0, 100);
                            if (SaveLoad_Game.Data.savegamedata.Drunk < 50f && old >= 50f && ctrlGirls[0].FindBone())
                            {
                                var psn = ctrlGirls[0].FindBone().FindDeepSp("Particle System[Sigh](Clone)");
                                if (psn)
                                {
                                    psn.GetComponent<ParticleSystem>().Stop();
                                }
                            }
                        }
                    }
                }
#endif
            }
            posYs += 10;
        }

        void macroCtrl()
        {
            GUI.contentColor = Color.cyan;
            GUI.Label(RectU.sclRect(ItemX + 20, (posYs += 20), LBL_WD, 20), "テンキーマクロ", gsLabel);
            GUI.contentColor = Color.white;

            //if (rootbtn2(nowEdit + "-KeyMacro", "キーマクロ", 0, 0, keyMacro.Play ? Color.yellow : colorDemiCyan))
            if (rootbtn(nowEdit = "KeyMacro"))
            {
                posYs += 5;

                bool boold = keyMacro.Play;

                using (new UiUtil.GuiEnable(ConfigClass.KeyPadScene))
                    keyMacro.Play = gettoggle("Play", keyMacro.Play, 0, 0, -(SLDWHF + 60));
                if (keyMacro.Play && !boold)
                {
                    keyMacro.Reset();
                    if (keyMacro.Play && !keyMacro.enabled)
                        keyMacro.enabled = true;
                }

                keyMacro.Pause = gettoggle("Pause Timer", keyMacro.Pause, SLDWHF, -20);

                if (keyMacro.macros.Count <= 0)
                {
                    posYs += 5;
                    GUI.color = Color.yellow;
                    labelctrl("※マクロパターンが１つ以上必要です", 10, 0);
                    GUI.color = Color.white;
                    posYs += 5;
                }
                posYs += 5;

                // プリセット
                KeyMacro.SaveData obj = null;
                if (is_preset_save("KeyMacro"))
                    obj = new KeyMacro.SaveData(keyMacro);
                if (presetctrl(ref obj, "KeyMacro"))
                {
                    obj.Load(ref keyMacro);
                    keyMacro.enabled = true;
                    keyMacro.Play = true;
                }
                posYs += 10;

                var lvl = keyMacro.CurrentMacroId;

                if (!keyMacro.Play)
                    GUI.enabled = false;
                {
                    labelctrl("Status:", 5, 0);
                    float ftemp = lvl + 1;

                    //posYs -= 15;
                    if (floatctrl($" Macro No.", ref ftemp, 1f, 1, keyMacro.MaxLevel, 1, 0))
                    {
                        lvl = (int)ftemp - 1;
                        keyMacro.CurrentMacroId = lvl;
                    }
                    var bar = (keyMacro.getTimeRemains());
                    if (floatctrl(" Timer", ref bar, keyMacro.CurrentMacro.waitTime, 0, keyMacro.CurrentMacro.waitTime, 1f, 2))
                    {
                        keyMacro.setTimeRemains(bar);
                    }

                    if (GUI.enabled && KeyMacro.checkClimax())
                        labelctrl("※キーパッド無効:タイマー停止中", 5, 0);
                }
                GUI.enabled = true;
                posYs += 5;

                labelctrl("◆Macro Editor", 5, 0);
                string sid = $"キーマクロリスト";
                GuiBox.BoxStart(sid, LX - 5, posYs + 20, SLDW + 10);
                ptnCtrl(sid, keyMacro, 0);
                //for (int i = 0; i < keyMacro.MaxLevel; i++)
                //{
                //    string sid = $"Macro No.: {i + 1}  --------------------";

                //    GuiBox.BoxStart(sid, LX - 5, posYs + 20, SLDW + 10);
                //    ptnCtrl(sid, keyMacro, i);
                //    GuiBox.BoxEnd(sid, posYs + 20);
                //    posYs += 10;
                //}
                posYs += 5;
                keyMacro.Loop = gettoggle("Loop動作", keyMacro.Loop, 0, 0);
                posYs += 5;

                keyMacro.waitTimeRandomRate = getfloatctrl($"WaitTimeのランダム係数", keyMacro.waitTimeRandomRate, 0f, 0, 1f);

                posYs += 5;
                GuiBox.BoxEnd(sid, posYs + 20);

                //posYs += 5;

                posYs += 10;
            }
            posYs += 10;
        }

        void ptnCtrl(string name, KeyMacro KeyMacro, int level)
        {
            var tag = "PtnCtrl-" + name;
            var obj = KeyMacro.macros;

            posYs += 5;
            //GUI.color = Color.yellow;
            //labelctrl($"パターン Lv: {(level + 1)}  登録: {obj.Count} コ", ItemX, 0);
            //GUI.color = Color.white;

            var open = rootbtn2(tag, $"{name}  登録: {obj.Count} コ", 5, 0, obj.Count > 0 ? Color.yellow : Color.white);
            //labelctrl(name, 5, 0);
            if (open)
            {
                const int SUBBTNW = (SLDW - LX - 20) / 3;
                const int SUBLX = LX + 30 + 5;

                //bool writeDisable = ;

                int num = 0;
                foreach (var c in obj.ToArray())
                {
                    num++;
                    posYs += 20;

                    labelctrl($"{num}", (num >= 10 ? 0 : 5), -20);
                    var rectc = new Rect(LX + 10, (posYs), SLDW - 10, 20);
                    if (easyCombo.Show<Keybd.KeypadCode>(tag + level + c.GetHashCode(), rectc, Keybd.dicToDisp.FirstOrDefault(x=>x.Key == c.KeypadCode).Value, Keybd.dicToDisp, gsTextField, gsListButton))
                    {
                        if (!string.IsNullOrEmpty(easyCombo.sSelected))
                            c.KeypadCode = Keybd.dicToDisp.FirstOrDefault(x=>x.Value == easyCombo.sSelected).Key;
                    }
                    else if (easyCombo.lastPop())
                    {
                        posYs += 90;
                    }
                    posYs += 5;

                    if (floatctrl("WaitTime (秒)", ref c.waitTime, 1f, 0f, 180f, 1f, 1))
                    {
                        // タイムテーブル更新
                    }

                    posYs += 15;

                    if (GUI.Button(RectU.sclRect(SUBLX + SUBBTNW * 1, (posYs), SUBBTNW - 2, 20), "Play", gsButton))
                    {
                        keyMacro.CurrentMacroId = (obj.IndexOf(c));
                    }
                    if (GUI.Button(RectU.sclRect(SUBLX + SUBBTNW * 2, (posYs), SUBBTNW - 2, 20), "Del", gsButton))
                    {
                        obj.Remove(c);
                    }

                    if (obj.IndexOf(c) > 0)
                    {
                        if (GUI.Button(RectU.sclRect(SUBLX + SUBBTNW * 0, (posYs), (SUBBTNW - 2), 20), "Up", gsButton))
                        {
                            var i = obj.IndexOf(c);
                            if (i > 0)
                            {
                                var t = obj[i];
                                obj[i] = obj[i - 1];
                                obj[i - 1] = t;
                            }
                        }
                    }

                    posYs += 10;
                }

                //if (writeDisable)
                //{
                //    GUI.enabled = false;
                //}
                //if (GUI.Button(RectU.sclRect(LX, (posYs += 20), SLDW, 20), "Add", gsButton))
                //{
                //    KeyMacro.addPattern(Keybd.KeypadCode.None, 1);
                //}
                
                labelctrl("キーマクロ追加", 5, 0);
                int code = (int)keypadCtrl();
                if (code >= 0)
                {
                    float time = 1;
                    if (obj.Count > 0)
                        time = obj[obj.Count - 1].waitTime;
                    KeyMacro.addPattern((Keybd.KeypadCode)code, time);
                }
                labelctrl("※絶頂等の入力無効中はタイマー自動停止", 10, 0);

                GUI.enabled = true;

                posYs += 5;

            }
            posYs += 5;
        }

        Keybd.KeypadCode keypadCtrl()
        {
            posYs += 20;

            const int NBW = (SLDW - 10) / 4;
            for (int i = 0; i < 20; i++)
            {
                if (i == 0)
                {
                    if (GUI.Button(RectU.sclRect(LX + 10 + NBW * i, (posYs), NBW, ItemHeight), " ", gsButton))
                    {
                        return Keybd.KeypadCode.None;
                    }
                    continue;
                }
                else
                {
                    Keybd.KeypadCode code = (Keybd.KeypadCode)(-1);
                    string slotname = "";
                    int dx = i;
                    int dy = i / 4;
                    if (i >= 4)
                    {
                        dx = (i) % 4;
                        if (dx == 0)
                            posYs += ItemHeight;
                    }

                    int btnH = ItemHeight;
                    int btnW = NBW;

                    switch (dy)
                    {
                        case 0:
                            switch (dx)
                            {
                                case 0:
                                    code = Keybd.KeypadCode.None;
                                    break;
                                case 1:
                                    code = Keybd.KeypadCode.Divide;
                                    break;
                                case 2:
                                    code = Keybd.KeypadCode.Multiply;
                                    break;
                                case 3:
                                    code = Keybd.KeypadCode.Subtract;
                                    break;
                            }
                            break;

                        case 1:
                            switch (dx)
                            {
                                case 0:
                                    code = Keybd.KeypadCode.NumPad7;
                                    break;
                                case 1:
                                    code = Keybd.KeypadCode.NumPad8;
                                    break;
                                case 2:
                                    code = Keybd.KeypadCode.NumPad9;
                                    break;
                                case 3:
                                    code = Keybd.KeypadCode.Add;
                                    btnH *= 2;
                                    break;
                            }
                            break;

                        case 2:
                            switch (dx)
                            {
                                case 0:
                                    code = Keybd.KeypadCode.NumPad4;
                                    break;
                                case 1:
                                    code = Keybd.KeypadCode.NumPad5;
                                    break;
                                case 2:
                                    code = Keybd.KeypadCode.NumPad6;
                                    break;
                                case 3:
                                    //code = Keybd.KeypadCode.Add;
                                    break;
                            }
                            break;

                        case 3:
                            switch (dx)
                            {
                                case 0:
                                    code = Keybd.KeypadCode.NumPad1;
                                    break;
                                case 1:
                                    code = Keybd.KeypadCode.NumPad2;
                                    break;
                                case 2:
                                    code = Keybd.KeypadCode.NumPad3;
                                    break;
                                case 3:
                                    code = Keybd.KeypadCode.Enter;
                                    btnH *= 2;
                                    break;
                            }
                            break;

                        case 4:
                            switch (dx)
                            {
                                case 0:
                                    code = Keybd.KeypadCode.NumPad0;
                                    btnW *= 2;
                                    break;
                                case 1:
                                    //code = Keybd.KeypadCode.NumPad0;
                                    break;
                                case 2:
                                    code = Keybd.KeypadCode.Decimal;
                                    break;
                                case 3:
                                    //code = Keybd.KeypadCode.Enter;
                                    break;
                            }
                            break;
                    }

                    if ((int)code < 0)
                        continue;

                    slotname = Keybd.dicToDisp.FirstOrDefault(x => x.Key == code).Value.Replace("NumPad [", "").Replace("]", "");

                    if (GUI.Button(RectU.sclRect(LX + 10 + NBW * dx, (posYs), btnW, btnH), slotname, gsButton))
                    {
                        return code;
                    }
                }
            }
            return (Keybd.KeypadCode)(-1);
        }


        void lookAtCtrl()
        {
            GUI.contentColor = Color.cyan;
            //using(new UiUtil.GuiEnable( !(PlgIK.DONOT_IN_ADV && actScene.name == "ADV") ))
            GUI.Label(RectU.sclRect(ItemX + 20, (posYs += 20), LBL_WD, 20), "キャラ視線＆IK設定", gsLabel);
            GUI.contentColor = Color.white;
            if (rootbtn(nowEdit = "LookAt"))
            {
                //labelctrl($"視線追従 (β)", 0, 0);
                posYs += 5;
                if (PlgIK.DONOT_IN_ADV && actScene.name == "ADV")
                    labelctrl($"※ADVシーン：視線IK無効中", 0, 0);
                else
                    labelctrl($"※手足だけ使用したい時は視線の追従を0に", 0, 0);
                posYs += 5;

                using (new UiUtil.GuiColor(colorDemiCyan))
                    labelctrl("IK追従設定 (キャラ別)", 5, 0);
                //posYs += 5;

                foreach (var g in ctrlGirls_Targeted)
                {
                    FlagNowTgtID = g.ini.id;
                    if (g.ini.isNotGirl())
                        continue;

                    if (!cfg.InactiveCharCtrlVisible && !g.isActive())
                        continue;

                    using (new UiUtil.GuiColor(colorDemiCyan))
                        g.plgIK.enabled = gettoggle(g.ini.name, g.plgIK.enabled);
                    posYs += 5;

                    //using (new UiUtil.GuiEnable(g.plgIK.ready))
                    //using (new UiUtil.GuiColor(Color.yellow, g.plgIK.enabled))
                    {
                        //if (g.plgIK.enabled/* && g.plgIK.ready*/)
                        using (new UiUtil.GuiEnable(g.plgIK.enabled))
                        if (rootbtn2(nowEdit + "_CONF_" + g.ini.id, "視線IK:追従設定", 5, 0, GUI.enabled ? Color.yellow : GUI.color))
                        {
                            posYs += 5;
                            using (new UiUtil.GuiColor(Color.green))
                            labelctrl($"視線追従", 5, 0);

                            posYs += 5;
                            float f = g.plgIK.lookAtHeadWeight;
                            if (floatctrl("顔の追従度", ref f, PlgIK.DefWeightHead, 0f, 1f, 1f, 3))
                            {
                                g.plgIK.lookAtHeadWeight = f;
                            }

                            f = g.plgIK.lookAtBodyWeight;
                            if (floatctrl("上体の追従度", ref f, PlgIK.DefWeightBody, 0f, 1f, 1f, 3))
                            {
                                g.plgIK.lookAtBodyWeight = f;
                            }

                            Vector3 rot = g.plgIK.helperOffsetHeadRot.angle180();
                            var nr = v3ctrl2("顔の角度オフセット", "Rot+", rot, Vector3.zero, -90f, 90f);
                            if (nr != rot)
                                g.plgIK.helperOffsetHeadRot = nr;

                            using (new UiUtil.GuiColor(Color.green))
                            labelctrl($"視線のターゲット (選択)", 5, 0);
                            if (togglectrl2noref("カメラに向ける (デフォルト)", g.plgIK.lookAtTarget == PlgIK.LookAtTarget.Camera))
                                g.plgIK.lookAtTarget = PlgIK.LookAtTarget.Camera;
                            if (togglectrl2noref("カメラから背ける (ソフト)", g.plgIK.lookAtTarget == PlgIK.LookAtTarget.AntiCameraSoft))
                                g.plgIK.lookAtTarget = PlgIK.LookAtTarget.AntiCameraSoft;
                            if (togglectrl2noref("カメラから背ける (ハード)", g.plgIK.lookAtTarget == PlgIK.LookAtTarget.AntiCamera))
                                g.plgIK.lookAtTarget = PlgIK.LookAtTarget.AntiCamera;
                            if (togglectrl2noref("主人公/顔に向ける (居れば)", g.plgIK.lookAtTarget == PlgIK.LookAtTarget.PlayerFace))
                                g.plgIK.lookAtTarget = PlgIK.LookAtTarget.PlayerFace;
                            if (togglectrl2noref("主人公/股間に向ける (有れば)", g.plgIK.lookAtTarget == PlgIK.LookAtTarget.PlayerBigOne))
                                g.plgIK.lookAtTarget = PlgIK.LookAtTarget.PlayerBigOne;
                            if (togglectrl2noref("フレンド/顔に向ける (居れば)", g.plgIK.lookAtTarget == PlgIK.LookAtTarget.FriendFace))
                                g.plgIK.lookAtTarget = PlgIK.LookAtTarget.FriendFace;

                            //posYs += 5;

                            if (g.plgIK.ready)
                            {
                                posYs += 5;
                                if (rootbtn2(nowEdit + "_LookAt-Debug_" + g.ini.name, "DEBUG", 5 + (SLDW*2/3), 0))
                                {
                                    rot = g.plgIK.bipedIK.solvers.lookAt.head.axis;
                                    nr = v3ctrl2("LookAt-Head-Axis", "Axis ", rot, PlgIK.DefHeadAxis, -360f, 360f);
                                    if (nr != rot)
                                        g.plgIK.bipedIK.solvers.lookAt.head.axis = nr;


                                    rot = g.plgIK.bipedIK.solvers.lookAt.spine[0].axis;
                                    nr = v3ctrl2("LookAt-Spine[0]-Axis", "Axis ", rot, PlgIK.DefBodyAxis, -360f, 360f);
                                    if (nr != rot)
                                        g.plgIK.bipedIK.solvers.lookAt.spine[0].axis = nr;
                                }
                                posYs += 5;
                            }
                            else
                            {
                                posYs += 15;
                            }
                        }

                        bool bom = false;
                        using (new UiUtil.GuiEnable(g.FindBone()))
                            bom = rootbtn2(nowEdit + "_MTN_" + g.ini.id, "視線IK:不適用モーション", 5, 0, GUI.color);
                        if (bom)
                            {
                            var state = "モーション";
                            var stateNow = g.charaCtrl.getIOAnimeStateString();

                            labelctrl($"相性の悪いモーションを登録可", 10, 0);
                            labelctrl($"(位置補正基準が口の場合も顔追従無効)", 10, 0);

                            labelctrl($"現モーション: {stateNow}", 10, 0);

                            var list = g.plgIK.disapplyMotionNames;
                           
                            // 除外リスト
                            motionsListSubCtrl(list, state, stateNow);

                            posYs += 10;
                        }
                        //else
                        //{
                        //    posYs += 15;
                        //}

                        using (new UiUtil.GuiEnable(g.plgIK.enabled))
                            if (rootbtn2(nowEdit + "_LIK_" + g.ini.id, "手足IK:追従設定", 5, 0, GUI.enabled ? Color.yellow : GUI.color))
                            {
                                posYs += 5;
                                using (new UiUtil.GuiColor(Color.green))
                                    labelctrl($"手足のIKターゲット (複数可)", 5, 0);
                                togglectrl("足のIKも有効にする", ref g.plgIK.ikConfig.enableLegIK);
                                togglectrl("体形変更前の位置を対象", ref g.plgIK.ikConfig.ikTargetingDefSclBody);
                                togglectrl("XtMS時はマスターを対象", ref g.plgIK.ikConfig.ikTargetingXtMaster);
                                togglectrl("手足が伸び切らないよう制限", ref g.plgIK.ikConfig.limitRigDistance);
                                togglectrl("人差し指付け根を基準に補正する", ref g.plgIK.ikConfig.useFinger1Offset);
                                posYs += 10;

                                using (new UiUtil.GuiColor(Color.green))
                                    labelctrl($"手の位置補正", 5, 0);
                                g.plgIK.ikConfig.PosOffsetHandR = v3ctrl("Pos-", g.plgIK.ikConfig.PosOffsetHandR, Vector3.zero, -1f, 1f);
                                //g.plgIK.ikConfig.PosOffsetHandR = v3ctrl2("右手", "Pos-", g.plgIK.ikConfig.PosOffsetHandR, Vector3.zero, -1f, 1f);
                                //g.plgIK.ikConfig.PosOffsetHandL = v3ctrl2("左手", "Pos-", g.plgIK.ikConfig.PosOffsetHandL, Vector3.zero, -1f, 1f);
                                posYs += 10;

                                using (new UiUtil.GuiColor(Color.green))
                                    labelctrl($"IK詳細設定", 5, 0);
                                g.plgIK.ikConfig.PosWeightHand = floatctrl2("IK位置ウェイト:手", g.plgIK.ikConfig.PosWeightHand, 1f, 0f, 1f);
                                g.plgIK.ikConfig.PosWeightFoot = floatctrl2("IK位置ウェイト:足", g.plgIK.ikConfig.PosWeightFoot, 1f, 0f, 1f);
                                posYs += 5;
                                g.plgIK.ikConfig.RotWeightHand = floatctrl2("IK回転ウェイト:手", g.plgIK.ikConfig.RotWeightHand, 1f, 0f, 1f);
                                g.plgIK.ikConfig.RotWeightFoot = floatctrl2("IK回転ウェイト:足", g.plgIK.ikConfig.RotWeightFoot, 1f, 0f, 1f);
                                posYs += 10;
                            }

                        if (rootbtn2(nowEdit + "_IKPreset_" + g.ini.id, "IK設定プリセット", 5, 0))
                        {
                            posYs += 5;
                            // プリセット
                            var saveName = "IKPreset_" + g.ini.id;
                            PlgIK.SavePlgIK obj = null;
                            if (is_preset_save(saveName))
                                obj = new PlgIK.SavePlgIK(g.plgIK);
                            if (presetctrl(ref obj, saveName))
                            {
                                g.plgIK.PresetLoadFrom(obj);
                            }
                            posYs += 5;
                            labelctrl($"※不適用モーションは対象外", 10, 0);
                            posYs += 10;


                            using (new UiUtil.GuiColor(Color.green))
                                labelctrl("モーション別プリセット", 5, 0);

                            labelctrl("※対象をトリガーにしたプリセット設定", 10, 0);

                            var state = "モーション";
                            var stateNow = g.isActive() ? g.charaCtrl.getIOAnimeStateString() : string.Empty;
                            labelctrl($"現モーション: {stateNow}", 10, 0);

                            if (stateNow == null) stateNow = string.Empty;
                            ikPresetByMotionsCtrl(g.plgIK.dicIkPresetByMotions, state, stateNow, g.plgIK);
                        }
                        else
                        {
                            posYs += 15;
                        }
                    }
                }
                posYs += 5;

                using(new UiUtil.GuiColor(colorDemiCyan))
                    labelctrl("目線設定 (共通)", 5, 0);
                
                using (new UiUtil.GuiColor(Color.yellow, MyEyeLookAt.Enabled))
                    cfg.useMyEyeLookAt = gettoggle("顔追従時に目線も追従", MyEyeLookAt.Enabled);

                if (MyEyeLookAt.Enabled)
                {
                    using (new UiUtil.GuiColor(Color.yellow, MyEyeLookAt.NoCheckIkWeight && MyEyeLookAt.Enabled))
                        cfg.myEyeLookAt_NoCheckIkWeight = gettoggle("顔追従不適用時も目線は追従", MyEyeLookAt.NoCheckIkWeight);

                    using (new UiUtil.GuiColor(Color.yellow, MyEyeLookAt.InhibitHitomiAnime && MyEyeLookAt.Enabled))
                        cfg.myEyeLookAt_InhibitHitomiAnime = gettoggle("元の瞳アニメ動作を抑制 (推奨)", MyEyeLookAt.InhibitHitomiAnime);

                    posYs += 5;
                    cfg.myEyeLookAtSpeed = getfloatctrl("加速", MyEyeLookAt.Speed, MyEyeLookAt.SPEED_DEF, 0.1f, 20f);
                    cfg.myEyeLookAtWeight = getfloatctrl("強度", MyEyeLookAt.Weight, MyEyeLookAt.WEIGHT_DEF, 0f, 10f);
                    cfg.myEyeLookAtMaxSpeed = getfloatctrl("最大速度", MyEyeLookAt.MaxSpeed, MyEyeLookAt.MAXSPEED_DEF, 0.1f, 60f);
#if DEBUG
                    MyEyeLookAt.DummyOffset = v3ctrl2("オフセット", "Rot+", MyEyeLookAt.DummyOffset, MyEyeLookAt.DummyOffsetDef, -90f, 90f);
#endif
                    posYs += 5;
                }

                posYs += 5;
            }
            posYs += 10;
        }

        void motionsListSubCtrl(List<string> list, string state, string stateNow)
        {
            using(new UiUtil.GuiEnable( !list.Contains(stateNow) ))
                if (centerbtn($"{state}登録"))
                {
                    list.Add(stateNow);
                }
            posYs += 10;

            labelctrl($"登録数: {list.Count}", 5, 0);

            const int SUBBTNW = (SLDW - LX - 20) / 2;//4;
            const int SUBLX = LX + 20;//LX + 30 + 5;

            int num = 0;
            foreach (var o in list.ToArray())
            {
                num++;
                using (new UiUtil.GuiColor(Color.green, o == stateNow))
                    labelctrl($"{state}: {o}", 5, 0);
                posYs += 20;

                if (num > 1)
                if (GUI.Button(RectU.sclRect(SUBLX + SUBBTNW * 0, (posYs), SUBBTNW - 2, 20), "Up", gsButton))
                {
                    var i = list.IndexOf(o);
                    if (i > 0)
                    {
                        var t = list[i];
                        list[i] = list[i - 1];
                        list[i - 1] = t;
                    }
                }
                if (GUI.Button(RectU.sclRect(SUBLX + SUBBTNW * 1, (posYs), SUBBTNW - 2, 20), "Del", gsButton))
                {
                    list.Remove(o);
                }
                posYs += 5;
            }

            if (state == "モーション")
                cfg.EnableFuzzyAnimeFiltering = gettoggle("末尾1字違いのﾓｰｼｮﾝも対象 (共通)", cfg.EnableFuzzyAnimeFiltering, 5, 0);
        }


        void ikPresetByMotionsCtrl(Dictionary<string, PlgIK.SavePlgIK> dic, string state, string stateNow, PlgIK plgIK)
        {
            if (centerbtn($"{state}登録"))
            {
                dic[stateNow] = new PlgIK.SavePlgIK(plgIK);

            }
            posYs += 10;

            labelctrl($"登録数: {dic.Count}", 5, 0);

            const int SUBBTNW = (SLDW - LX - 20) / 4;
            const int SUBLX = LX + 20;//LX + 30 + 5;

            int num = 0;
            foreach (var o in dic.ToArray())
            {
                num++;
                using (new UiUtil.GuiColor(Color.green, o.Key == stateNow))
                    labelctrl($"{state}: {o.Key}", 5, 0);

                Func<bool, string> _f = x => (x ? "On" : "Off");

                var sb = new StringBuilder();
                sb.Append(" (IK:").Append(_f(o.Value.lookAt.lookAtEnabled));
                sb.Append("/顔:").Append(_f(o.Value.lookAt.lookAtHeadWeight > 0));
                sb.Append("/体:").Append(_f(o.Value.lookAt.lookAtBodyWeight > 0));
                sb.Append("/手d:").Append(_f(o.Value.ikConfig.ikTargetingDefSclBody));
                sb.Append("/手x:").Append(_f(o.Value.ikConfig.ikTargetingXtMaster));
                sb.Append("/足:").Append(_f(o.Value.ikConfig.enableLegIK));
                sb.Append(")");
                labelctrl(sb.ToString(), 5, 0);
                if (o.Value.lookAt.lookAtEnabled && (o.Value.lookAt.lookAtHeadWeight > 0 || o.Value.lookAt.lookAtBodyWeight > 0))
                    labelctrl($" (視線対象:{o.Value.lookAt.lookAtTarget.ToString()})", 5, 0);
                posYs += 20;

                if (GUI.Button(RectU.sclRect(SUBLX + SUBBTNW * 0, (posYs), SUBBTNW - 2, 20), "Load", gsButton))
                {
                    plgIK.PresetLoadFrom(o.Value);
                }
                if (GUI.Button(RectU.sclRect(SUBLX + SUBBTNW * 1, (posYs), SUBBTNW - 2, 20), "Set", gsButton))
                {
                    dic[o.Key] = new PlgIK.SavePlgIK(plgIK);
                }

                if (num > 1)
                    if (GUI.Button(RectU.sclRect(SUBLX + SUBBTNW * 2, (posYs), SUBBTNW - 2, 20), "Up", gsButton))
                    {
                        var list = dic.ToList();
                        var i = num - 1;
                        if (i > 0)
                        {
                            var t = list[i];
                            list[i] = list[i - 1];
                            list[i - 1] = t;
                        }
                        
                        dic.Clear();
                        foreach(var v in list)
                            dic.Add(v.Key, v.Value);
                    }
                if (GUI.Button(RectU.sclRect(SUBLX + SUBBTNW * 3, (posYs), SUBBTNW - 2, 20), "Del", gsButton))
                {
                    dic.Remove(o.Key);
                }

                posYs += 5;
            }
        }

        void ikpresetByState(string name, GirlCtrl ctrl, string state, BoneScales.OffsetByStates offsetByStates)
        {
            //if (rootbtn("BoneSlider_" + name))
            {
                BoneScales boneScales = ctrl.boneScales;

                //posYs += 5;

                Color clr = (offsetByStates.list.Count > 0) ? Color.yellow : Color.white;
                if (rootbtn2($"PosOffset-{state}_" + name, $"位置補正の{state}別登録", 0, 0, clr))
                {
                    posYs += 5;

                    togglectrl("有効", ref offsetByStates.enabled);
                    posYs += 5;

                    string stateNow = "";
                    string stateNowNote = "";
                    if (state == "シーン")
                    {
                        togglectrl("シーン＋背景名で優先登録 (共通)", ref cfg.CharEdits_PosOfsByScene_PlusBGName);
                        posYs += 5;

                        if (Free3pXtMS.IsSlave(ctrl))
                        {
                            stateNow = Free3pXtMS.GetXtSceneName;
                            stateNowNote = "(リンク中)";
                        }
                        else
                        {
                            stateNow = boneScales.GetCurrentBsSceneName(actScene, cfg.CharEdits_PosOfsByScene_PlusBGName);

                            switch (stateNow.Substring(0, Math.Min(stateNow.Length, 2)))
                            {
                                case "IC": stateNowNote = " (前戯)"; break;
                                case "UC": stateNowNote = " (特殊H)"; break;
                                case "FH": stateNowNote = " (フリーH)"; break;
                                case "ADV@BG3D": stateNowNote = " (イベント)"; break;
                            }
                            if (cfg.CharEdits_PosOfsByScene_PlusBGName)
                            {
                                var bgn = BGMGR.GetJpNameByNameBG(BGMGR.GetNameBG());
                                if (string.IsNullOrEmpty(bgn))
                                    bgn = "2D背景";

                                if (string.IsNullOrEmpty(stateNowNote))
                                    stateNowNote = $" ({bgn})";
                                else
                                    stateNowNote.Replace(")", $"+{bgn})");
                            }
                        }

                    }
                    else if (state == "モーション")
                    {
                        stateNow = ctrl.charaCtrl.getIOAnimeStateString();
                    }

                    labelctrl($"現在: {stateNow}{stateNowNote}", 5, 0);
                    labelctrl($"  Pos: {boneScales.edits.offsetData.girlOffset.dpos}  Rot: {boneScales.edits.offsetData.girlOffset.drot}", 5, 0);
                    labelctrl($"  位置補正基準:  {BoneScales.offsetCoordsData.First(x => x.coord == boneScales.edits.offsetData.girlOffset.coord).dispName}", 5, 0);

                    if (centerbtn($"{state}登録"))
                    {
                        if (offsetByStates.list.Any(x => x.stateName == stateNow))
                            offsetByStates.list.First(x => x.stateName == stateNow).offset = boneScales.edits.offsetData.girlOffset;
                        else
                            offsetByStates.list.Add(new BoneScales.OffsetByState(stateNow, boneScales.edits.offsetData.girlOffset));

                    }
                    posYs += 10;
                    labelctrl($"登録数: {offsetByStates.list.Count}", 5, 0);

                    const int SUBBTNW = (SLDW - LX - 20) / 4;
                    const int SUBLX = LX + 20;//LX + 30 + 5;

                    int num = 0;
                    foreach (var o in offsetByStates.list.ToArray())
                    {
                        num++;

                        using (new UiUtil.GuiColor(Color.green, o.stateName == stateNow))
                            labelctrl($"{state}: {o.stateName}", 5, 0);
                        labelctrl($"  Pos: {o.offset.dpos}  Rot: {o.offset.drot}", 5, 0);
                        if (ctrl.ini.isGirl())
                        {
                            labelctrl($"  位置補正基準:  {BoneScales.offsetCoordsData.First(x => x.coord == o.offset.coord).dispName}", 5, 0);
                        }
                        else
                        {
                            o.offset.coord = BoneScales.OffsetCoord.None;
                        }
                        posYs += 20;

                        if (GUI.Button(RectU.sclRect(SUBLX + SUBBTNW * 0, (posYs), SUBBTNW - 2, 20), "Load", gsButton))
                        {
                            boneScales.edits.offsetData.girlOffset = o.offset;
                        }
                        if (GUI.Button(RectU.sclRect(SUBLX + SUBBTNW * 1, (posYs), SUBBTNW - 2, 20), "Set", gsButton))
                        {
                            offsetByStates.list[num].offset = boneScales.edits.offsetData.girlOffset;
                        }

                        if (num > 1)
                        if (GUI.Button(RectU.sclRect(SUBLX + SUBBTNW * 2, (posYs), SUBBTNW - 2, 20), "Up", gsButton))
                        {
                            var i = offsetByStates.list.IndexOf(o);
                            if (i > 0)
                            {
                                var t = offsetByStates.list[i];
                                offsetByStates.list[i] = offsetByStates.list[i - 1];
                                offsetByStates.list[i - 1] = t;
                            }
                        }
                        if (GUI.Button(RectU.sclRect(SUBLX + SUBBTNW * 3, (posYs), SUBBTNW - 2, 20), "Del", gsButton))
                        {
                            offsetByStates.list.Remove(o);
                        }

                        posYs += 5;
                    }
                    posYs += 10;
                }
            }
        }
        #endregion



        #region 背景の位置変更
        void BGposCtrl()
        {
            var scnPos = BGMGR.scnPosMainBG;

            //using (new UiUtil.GuiEnable(BGMGR.mainBg))
            if (rootbtn2(nowEdit = "BG_ScenePosition", "背景位置変更", 0, 0, Color.cyan))
            {
                BGMGR.GetBG();
                var goBg = BGMGR.mainBG;

                Color clr = (scnPos.enable) ? Color.yellow : Color.white;
                //if (rootbtn2(nowEdit +  "_MainBG", "MAIN", 5, 0, clr))

                if (!goBg)
                {
                    labelctrl(" 背景が見つかりません", 5, 0);
                    labelctrl(" (タイトルや2D背景時)", 5, 0);
                    posYs += 5;
                }
                else //if (goBg)
                {
                    posYs += 5;

                    using (new UiUtil.GuiColor(scnPos.enable ? Color.yellow : colorDemiCyan))
                        labelctrl($"背景名: {goBg.name} ({BGMGR.GetJpNameByNameBG(goBg.name)})", 25, 0);
                    if (rootbtn(nowEdit + goBg.name, 5))
                    {
                        posYs += 5;
                        labelctrl(" ※一時用。プリセットのみに保存", 10, 0);
                        posYs += 5;

                        //if (scnPos.enable)
                        {
                            //posYs += 5;
                            ScenePos obj = null;
                            string spEditTag = nowEdit;
                            if (is_preset_save(spEditTag))
                                obj = scnPos;
                            if (presetctrl(ref obj, spEditTag))
                            {
                                obj.Load(ref BGMGR.scnPosMainBG);
                            }
                            posYs += 5;
                        }

                        var sp = scnPos;
                        if (sp.enable)
                        {
                            GUI.color = Color.yellow;
                        }

                        var onflg = false;
                        var bo = gettoggle("有効にする", sp.enable);
                        if (sp.enable != bo)
                        {
                            sp.enable = bo;
                            if (sp.enable)
                            {
                                onflg = true;
                            }
                            else
                            {
                                // カレンダーオフセットを戻す
                                BGMGR.BGCalendarOffset(Vector3.zero);
                                BGMGR.scnPosMainBG.ResetPosRot(goBg.transform);

                                // IOのBGは定期的に位置リセットされる
                                if (ScenePos.gizmoTgt == goBg)
                                    ScenePos.gizmoTgt = null;
                            }
                        }

                        if (!sp.enable || onflg)
                        {
                            GUI.enabled = false;
                            if (goBg)
                            {
                                var tr = goBg.transform;
                                sp.orgPos = sp.pos = tr.localPosition;
                                sp.orgRot = sp.rot = tr.localEulerAngles.angle180();
                                sp.scale = tr.localScale.x;
                            }
                        }

                        bool bo0 = ScenePos.gizmoTgt == goBg;
                        bool bo1 = gettoggle("Gizmo", bo0, 0, 5);
                        if (bo0 != bo1)
                        {
                            ScenePos.gizmoTgt = bo1 ? goBg : null;
                        }
                        posYs += 5;
                        sp.pos = v3ctrl2_test(" 背景座標", " ", sp.pos, sp.orgPos, -10f, 10f);
                        posYs += 5;
                        sp.rot = v3ctrl2(" 背景角度", " Rot-", sp.rot, sp.orgRot, -180f, 180f);
                        posYs += 5;
                        sp.scale = getfloatctrl(" 背景拡縮", sp.scale, 1f, 0.1f, 10f, 1f);

                        GUI.enabled = true;
                        GUI.color = Color.white;
                    }


                }
                posYs += 5;


                //if (rootbtn2(nowEdit + "_AutoMove", "自動変更設定(シーンID別)", 0, 0, colorDemiCyan))
                using (new UiUtil.GuiColor(colorDemiCyan))
                    labelctrl("シーン変更時自動設定", 25, 0);
                if (rootbtn(nowEdit + "_AutoBgPos", 5))
                {
                    posYs += 5;
                    togglectrl("有効", ref BGMGR.posByStates.enabled);

                    if (BGMGR.posByStates.enabled)
                    {
                        posYs += 5;
                        PositionByStates obj = null;
                        string spEditTag = "ScenePosAuto_BG";
                        if (is_preset_save(spEditTag))
                            obj = BGMGR.posByStates;
                        if (presetctrl(ref obj, spEditTag))
                        {
                            BGMGR.posByStates = obj;
                        }
                        posYs += 5;
                    }

                    BGposCtrlByState("BG", "シーンID", BGMGR.scnPosMainBG, BGMGR.posByStates);
                    posYs += 5;
                    posYs += 5;
                }
                posYs += 5;

                using (new UiUtil.GuiEnable(BGMGR.mainBG))
                {
                    using (new UiUtil.GuiColor(colorDemiCyan))
                        labelctrl("背景アイテム位置変更", 25, 0);
                    if (rootbtn(nowEdit + "_背景アイテム", 5))
                    {
                        posYs += 5;
                        labelctrl(" ※一時用。プリセットのみに保存", 10, 0);
                        posYs += 5;

                        BGMGR.BGItems obj = null;
                        string spEditTag = "BG_ItemPositions";
                        if (is_preset_save(spEditTag))
                            obj = BGMGR.bgItems;
                        if (presetctrl(ref obj, spEditTag))
                        {
                            BGMGR.bgItems.ResetAll();
                            BGMGR.bgItems = obj;
                            BGMGR.bgItems.LoadAll(BGMGR.mainBG);
                        }
                        posYs += 10;

                        ctrl_BGSubObjs(BGMGR.mainBG.transform);
                        posYs += 5;

                        labelctrl("背景内に存在しない登録", 10, 0);
                        foreach (var item in BGMGR.bgItems.listBgTrs)
                        {
                            if (item.IsLoaded()) continue;

                            if (GUI.Button(RectU.sclRect(LX + EDTW - 26, (posYs + ItemHeight), 54, 20), "削除", gsButton))
                            {
                                BGMGR.bgItems.listBgTrs.Remove(item);
                                GUI.color = Color.white;
                                return;
                            }
                            labelctrl("[X] " + item.name, 25, 0);
                        }
                        posYs += 5;
                    }
                }
                posYs += 5;

            }
            else
            {
                if (ScenePos.gizmoTgt == BGMGR.mainBG)
                    ScenePos.gizmoTgt = null;
            }
            posYs += 10;
        }

        void ctrl_BGSubObjs(Transform tr, int nest = 0)
        {
            if (!tr || nest > 2)
                return;

            if (tr.gameObject.GetComponent<MeshRenderer>()
                && (tr.gameObject.GetComponent<Rigidbody>() || tr.gameObject.GetComponent<MeshCollider>()))
            {
                var item = BGMGR.bgItems.listBgTrs.FirstOrDefault(x => x.name == tr.name);
                if (item == null)
                {
                    
                }
                else
                {
                    item.Save(tr.gameObject);

                    GUI.color = Color.yellow;
                    if (GUI.Button(RectU.sclRect(LX + EDTW - 26, (posYs + ItemHeight), 54, 20), "解除", gsButton))
                    {
                        BGMGR.bgItems.listBgTrs.Remove(item);
                        GUI.color = Color.white;

                        item.Reset(tr.gameObject);
                        return;
                    }
                }

                var bo = item != null && item.CheckGizmo();
                string flg = new string('+', nest);
                
                int dwx = 10;
                if (GUI.color == Color.white)
                    dwx += 54;
                var nb = GUI.Toggle(RectU.sclRect(LX, (posYs += ItemHeight), EDTW - 36 + dwx, ItemHeight), bo, "" + flg + tr.name, gsToggle);
                if (nb != bo)
                {
                    if (nb)
                    {
                        if (item == null)
                        {
                            item = new BGMGR.BGItemTransform(tr.gameObject, true);
                            BGMGR.bgItems.listBgTrs.Add(item);
                        }
                        
                        item.OnGizmo(tr.gameObject);
                    }
                    else if (item != null)
                    {
                        item.OffGizmo();
                    }
                }

                GUI.color = Color.white;
            }

            for (int i = 0; i < tr.childCount; i++)
            {
                var t = tr.GetChild(i);
                if (!t || string.IsNullOrEmpty(t.name))
                    continue;

                var n = t.name;
                if (!n.StartsWith("gizmo", StringComparison.Ordinal))
                {
                    ctrl_BGSubObjs(t, nest+1);
                }
            }
        }

        #endregion


        #region 背景位置調整・シーン割り当て
        void BGposCtrlByState(string name, string state, ScenePos scnPos, PositionByStates posByStates, int pos_dX = 10)
        {
            Color clr = (posByStates.list.Count > 0) ? Color.yellow : Color.white;
            if (rootbtn2($"PosOffset-{name}_{state}", $"{name}位置の{state}別登録", pos_dX, 0, clr))
            {
                posYs += 5;

                //togglectrl("有効", ref posByStates.enabled);
                //posYs += 5;

                string stateNow = "";
                string stateNowNote = "";
                if (state == "シーンID")
                {
                    stateNow = actScene.name;
                    switch (stateNow)
                    {
                        case "IC": stateNowNote = " (前戯)"; break;
                        case "UC": stateNowNote = " (特殊H)"; break;
                        case "FH": stateNowNote = " (フリーH)"; break;
                    }

                    // ID付与
                    stateNow = actScene.name + SaveLoad_Game.Data.savegamedata.SceneID;

                    if (SaveLoad_Game.Data.savegamedata.SceneID.Substring(2, 2) == "01")
                        stateNowNote += " ※猫";
                    else
                        stateNowNote += " ※兎";
                }

                labelctrl($"現在: {stateNow}{stateNowNote}", 5, 0);
                using (new UiUtil.GuiEnable(scnPos.enable))
                {
                    labelctrl($"  Pos: {scnPos.pos}  Rot: {scnPos.rot}", 5, 0);

                    if (centerbtn($"{state}登録"))
                    {
                        if (posByStates.list.Any(x => x.stateName == stateNow))
                            posByStates.list.First(x => x.stateName == stateNow).trs = Clone.Data(scnPos);
                        else
                            posByStates.list.Add(new PositionByState(stateNow, Clone.Data(scnPos)));
                    }
                }

                posYs += 10;
                labelctrl($"登録数: {posByStates.list.Count}", 5, 0);

                const int SUBBTNW = (SLDW - LX - 20) / 4;
                const int SUBLX = LX + 20;//LX + 30 + 5;

                int num = -1;
                foreach (var o in posByStates.list.ToArray())
                {
                    num++;
                    using (new UiUtil.GuiColor(Color.green, o.stateName == stateNow))
                        labelctrl($"{state}: {o.stateName}", 5, 0);
                    labelctrl($"  Pos: {o.trs.pos}  Rot: {o.trs.rot}", 5, 0);
                    posYs += 20;

                    if (GUI.Button(RectU.sclRect(SUBLX + SUBBTNW * 0, (posYs), SUBBTNW - 2, 20), "Load", gsButton))
                    {
                        scnPos.enable = true;
                        o.trs.LoadTrs(scnPos);
                    }
                    using(new UiUtil.GuiEnable(scnPos.enable))
                        if (GUI.Button(RectU.sclRect(SUBLX + SUBBTNW * 1, (posYs), SUBBTNW - 2, 20), "Set", gsButton))
                        {
                            posByStates.list[num].trs = Clone.Data(scnPos);
                        }

                    if (num > 0)
                    {
                        if (GUI.Button(RectU.sclRect(SUBLX + SUBBTNW * 2, (posYs), SUBBTNW - 2, 20), "Up", gsButton))
                        {
                            var i = posByStates.list.IndexOf(o);
                            if (i > 0)
                            {
                                var t = posByStates.list[i];
                                posByStates.list[i] = posByStates.list[i - 1];
                                posByStates.list[i - 1] = t;
                            }
                        }
                    }
                    
                    if (GUI.Button(RectU.sclRect(SUBLX + SUBBTNW * 3, (posYs), SUBBTNW - 2, 20), "Del", gsButton))
                    {
                        posByStates.list.Remove(o);
                    }
                    posYs += 5;
                }
                posYs += 5;
            }
        }
#endregion

#region 画面効果
        void EffectCtrl()
        {
            bool open = false;
            //using (new UiUtil.GuiEnable(EffectMgr.isActive()))
                open = (rootbtn2(nowEdit = "EffectMgr", "エフェクト設定", 0, 0, Color.cyan));
            if (open) 
            {
                //if (Camera.main)
                {
                    if (EffectMgr.Enabled)
                    {
                        if (EffectMgr.isActive())
                        {
                            if (GUI.Button(RectU.sclRect(LX + EDTW - 26, (posYs), 54, 20), "RESET", gsButton))
                            {
                                // バックアップからロード
                                EffectMgr.BkupOnLoad.Load();
                                EffectMgr.Users.UpdateEnabled = false;
                                return;
                            }
                        }
                        posYs += 5;

                        EffectMgr.Configs obj = null;
                        if (is_preset_save(nowEdit))
                            obj = EffectMgr.Users;
                        if (presetctrl<EffectMgr.Configs>(ref obj, nowEdit))
                        {
                            EffectMgr.Users = obj;
                            obj.Load();
                            return;
                        }
                        posYs += 5;
                    }

                    posYs += 5;
                    using (new UiUtil.GuiColor(colorDemiCyan))
                        labelctrl("効果設定", 0, 0);

                    using (new UiUtil.GuiColor(Color.yellow, EffectMgr.Enabled))
                        cfg.EffectMgr_Enabled = gettoggle(!EffectMgr.Enabled || EffectMgr.isActive() ? "有効 (取得&設定)" : "有効 (無効シーンのため停止中)", EffectMgr.Enabled);

                    using (new UiUtil.GuiColor(Color.grey, !EffectMgr.Enabled))
                        using (new UiUtil.GuiColor(Color.yellow, EffectMgr.Enabled && EffectMgr.Users.UpdateEnabled))
                            EffectMgr.Users.UpdateEnabled = gettoggle("設定値を維持 (自動上書)", EffectMgr.Users.UpdateEnabled);
                    posYs += 5;

                    EffectMgr.Configs getter;
                    if (EffectMgr.Users.UpdateEnabled || !EffectMgr.isActive() || !EffectMgr.Enabled)
                        getter = EffectMgr.Users;
                    else
                        getter = EffectMgr.Getval;

                    using(new UiUtil.GuiEnable(EffectMgr.Enabled))
                    {
                        foreach (var fld in typeof(EffectMgr.Configs).GetFields())
                        {
                            var label = EffectMgr.TranslateToDisp(fld.Name);
                            object o = fld.GetValue(getter); ;
                            object def = fld.GetValue(EffectMgr.BkupOnLoad);

                            bool nodata = false;
                            switch (o)
                            {
                                case float f:
                                    if (label == "ブルーム_強度")
                                        o = getfloatctrl(label, f, (float)def, 0f, 5f);
                                    else
                                        o = getfloatctrl(label, f, (float)def, 0f, 1f);
                                    break;

                                case int i:
                                    o = getintctrl(label, i, (int)def, 0, 5);
                                    break;

                                case Color c:
                                    using (new UiUtil.GuiColor(Color.grey, !EffectMgr.Users.UpdateEnabled && actScene.name != "Custom" && actScene.name != "ADV"))
                                        o = EffectMgr.Users.DirectionalLight_color = colorctrl(label, c, (Color)def);
                                    break;

                                default:
                                    nodata = true;
                                    break;
                            }

                            posYs += 5;

                            if (!nodata)
                                fld.SetValue(EffectMgr.Users, o);
                        }
                    }
                    

                    if (EffectMgr.Enabled && !EffectMgr.Users.UpdateEnabled)
                    {
                        if (EffectMgr.isActive())
                            EffectMgr.Users.Load();
                    }
                }
            }
            posYs += 10;
        }
        #endregion

        #region フェイスコントロール
        void faceCtrl(string name, GirlCtrl ctrl)
        {
            if (rootbtn(nowEdit = "FaceCtrl_" + name))
            {
                FaceCtrl faceCtrl = ctrl.faceCtrl;
                if (faceCtrl.enabled)
                {
                    posYs += 5;
                    using (new UiUtil.GuiColor(colorDemiCyan))
                        labelctrl("表情変更", 5, 0);
                    posYs += 5;
                    labelctrl(" ※撮影用。暫定版（保存無し）", 5, 0);
                    labelctrl(" ※場面によっては変更不可な項目あり", 5, 0);
                    posYs += 10;

                    stateCtrl(name, "目", faceCtrl.EyeNames, nameof(faceCtrl.Eye));
                    stateCtrl(name, "口元1", faceCtrl.MouthNames1, nameof(faceCtrl.Mouth1));
#if !DEBUG
                    GUI.enabled = false;
#endif
                    stateCtrl(name, "口元2 (1に連動)", faceCtrl.MouthNames2, nameof(faceCtrl.Mouth2));
                    GUI.enabled = true;


                    //if (faceCtrl.getAnimator)
                    //{
                    //    string[] layers = { "体", "目", "口1", "口2" };
                    //    for(int i = 0; i<=3; i++)
                    //        faceCtrl.getAnimator.SetLayerWeight(i, getfloatctrl("LayerWt " + layers[i], faceCtrl.getAnimator.GetLayerWeight(i), 0f, 0f, 2f));
                    //}
                    posYs += 10;

                    faceCtrl.Cheek = getfloatctrl("赤面", faceCtrl.Cheek, 0f, 0f, 2f);
                    posYs += 5;


                    Animator ani = ctrl.FindModel().GetComponent<Animator>();
                    if (ani)
                    {
                        float w = ani.GetLayerWeight(3);
                        if (floatctrl("口の開度", ref w, 0f, 0f, 1f, 1, 3, true))
                            ani.SetLayerWeight(3, w);

                        //posYs += 5;
                    }

                    faceCtrl.ToothOpen = gettoggle("歯", faceCtrl.ToothOpen);
                    posYs += 5;

                    var go_tear = ctrl.ini.rootObj.FindDeepSp("LA01_tear", true);
                    if (go_tear)
                    {
                        bool tear = go_tear.gameObject.activeSelf;
                        if (togglectrl("涙", ref tear))
                        {
                            go_tear.gameObject.SetActive(tear);
                        }
                        posYs += 5;
                    }

                    void stateCtrl(string girl, string label, string[] names, string property)
                    {
                        var pi = faceCtrl.GetType().GetProperty(property);
                        //Func<FaceCtrl, string> getter = (Func<FaceCtrl, string>)Delegate.CreateDelegate(typeof(Func<FaceCtrl, string>), pi.GetSetMethod());
                        //Action<FaceCtrl, string> setter = (Action<FaceCtrl, string>)Delegate.CreateDelegate(typeof(Action<FaceCtrl, string>), pi.GetGetMethod());

                        labelctrl(label + ": ", 5, 0);

                        Rect rect = new Rect(LX + SLDWHF, (posYs), SLDWHF, ItemHeight);
                        if (easyCombo.Show(nowEdit + label + "_" + name, rect, ItemHeight, ItemHeight * 5,
                            names, pi.GetValue(faceCtrl, null) as string, gsButton, gsTextField)
                        )
                        {
                            if (easyCombo.sIndex >= 0)
                            {
                                pi.SetValue(faceCtrl, names[easyCombo.sIndex], null);

                                if (property == nameof(faceCtrl.Mouth1))
                                {
                                    // セットで変更
                                    faceCtrl.Mouth2 = faceCtrl.MouthNames2[easyCombo.sIndex];
                                }
                            }
                        }
                        if (easyCombo.lastPop())
                        {
                            posYs += ItemHeight * 5;
                            posYs += 5;
                        }
                        posYs += 5;
                    }

                    if (true)
                    {
                        posYs += 5;
                        //if (rootbtn2(nowEdit + "_Face-Debug_" + name, "DEBUG", 5 + (SLDW * 2 / 3), 0))
                        {
                            if (centerbtn("テスト: 尿"))
                            {
                                int mouth = 8;
                                string[] eyelist = { "EY01_09", "EY01_13", "EY01_15", };
                                if (ctrl.ini.id == "Usa")
                                {
                                    eyelist = new string[] { "EY02_02", "EY02_09", "EY02_13", };
                                    mouth = faceCtrl.MouthNames1.Length - 1;
                                }

                                var psn = ctrl.FindBone().FindDeepSp("Particle System[Nyo](Clone)");
                                if (psn)
                                {
                                    faceCtrl.Eye = eyelist[UnityEngine.Random.Range(0, 3)];
                                    faceCtrl.Mouth1 = faceCtrl.MouthNames1[mouth];
                                    faceCtrl.Mouth2 = faceCtrl.MouthNames2[mouth];
                                    faceCtrl.Cheek = 1f;

                                    var comp = psn.GetComponent<NyouController>();
                                    comp.Nyou = true;

                                    faceCtrl.Cheek = 1f;

                                    if (actScene.name == "Custom")
                                    {
                                        HsysMgr.CustomPlayNyoSE.PlayNyoSEinCustom(ctrl, comp);
                                    }
                                }
                            }
#if DEBUG

                            foreach(var p in ctrl.fixParticles.GetParticles)
                            {
                                if (centerbtn($"{p.name} : " + (p.isPlaying ? "停止" : "再生")))
                                {
                                    if (!p.isPlaying)
                                        p.Play();
                                    else
                                        p.Stop();
                                }
                            }
#endif
                        }
                    }
                    //posYs += 5;

                    posYs += 5;
                    posYs += 10;


                    List<GameObject> objlist = null;
                    Action csetupAct = null; 
                    if (ctrl.ini.id == "Neko")
                    {
                        var cs = ctrl.FindModel().transform.root.gameObject.GetComponent<CostumeSetUp_CH01>();
                        objlist = cs.MeshObj;
                        csetupAct = cs.CharacterSetUp;
                    }
                    else if (ctrl.ini.id == "Usa")
                    {
                        var cs = ctrl.FindModel().transform.root.gameObject.GetComponent<CostumeSetUp_CH02>();
                        objlist = cs.MeshObj;
                        csetupAct = cs.CharacterSetUp;
                    }

                    if (objlist != null) 
                    {
                        using(new UiUtil.GuiColor(colorDemiCyan))
                            labelctrl("アイテム表示設定", 5 + 20, 0);
                        if (faceCtrl.activateItems.Count > 0 &&
                            GUI.Button(RectU.sclRect(LX + EDTW - 26, (posYs), 54, 20), "RESET", gsButton))
                        {
                            ctrl.faceCtrl.activateItems.Clear();
                            if (csetupAct != null)
                                csetupAct.Invoke();
                        }

                        if (rootbtn("アイテム装着 " + name, 5, 0))
                        {
                            posYs += 5;
                            
                            foreach (var o in objlist)
                            {
                                if (!o.name.StartsWith("IT", StringComparison.Ordinal)
                                    && !o.name.StartsWith("LA", StringComparison.Ordinal))
                                    continue;

                                //var old = faceCtrl.activateItems.Contains(o);
                                //using (new UiUtil.GuiColor(Color.yellow, old))

                                // 非アクティブもキープできるように変更 v0.92
                                bool old;
                                var hit = faceCtrl.activateItems.TryGetValue(o, out old);

                                using (new UiUtil.GuiColor(Color.yellow, hit))
                                {

                                    if (hit)
                                    {
                                        // 非アクティブもキープできるように変更 v0.92
                                        if (GUI.Button(RectU.sclRect(LX + EDTW - 26, (posYs + ItemHeight), 54, 20), "解除", gsButton))
                                        {
                                            faceCtrl.activateItems.Remove(o);
                                            if (csetupAct != null)
                                                csetupAct.Invoke();
                                            continue;
                                        }
                                    }

                                    old |= o.activeSelf;
                                    var val = gettoggle(o.name.Replace("Gyagbole", "Gagball").Replace("Brea", "Breast").Replace("WashSci", "LaundryPinch"), old, 5); //ありえーるとか面白いけど一応
                                    if (val != old)
                                    {
                                        // 非アクティブもキープできるように変更 v0.92
                                        faceCtrl.activateItems[o] = val;

                                        /*
                                        if (!val) o.SetActive(val);

                                        if (val) faceCtrl.activateItems.Add(o);
                                        else if (old)
                                        {
                                            faceCtrl.activateItems.Remove(o);
                                            if (csetupAct != null)
                                                csetupAct.Invoke();
                                        }*/
                                    }
                                }
                                posYs += 5;
                            }
                        }
                    }
                    posYs += 10;

                    if (ctrl.milkCtrl.milkData != null)
                    {
                        using (new UiUtil.GuiColor(colorDemiCyan))
                            labelctrl("精液付着", 5 + 20, 0);
                        if (GUI.Button(RectU.sclRect(LX + EDTW - 26, (posYs), 54, 20), "RESET", gsButton))
                        {
                            ctrl.milkCtrl.milkData.ResetAll();
                        }

                        if (rootbtn("精液付着 " + name, 5, 0))
                        {

                            if (actScene.name == "Custom")
                            {
                                posYs += 5;
                                using (new UiUtil.GuiColor(Color.yellow))
                                    labelctrl("  ※カスタム画面などでは無効になります", 10, 0);
                                posYs += 5;
                            }

                            ctrl.milkCtrl.enabled = gettoggle("設定値をキープする", ctrl.milkCtrl.enabled);
                            ctrl.milkCtrl.autoFlush = gettoggle("ゲーム内部値に自動書込 (低速)", ctrl.milkCtrl.autoFlush);

                            posYs += 5;
                            posYs += 5;

                            bool flagWrite = false;
                            bool flagWriteSave = false;

                            if (centerbtn("設定の再適用"))
                                flagWrite = true;
                            if (centerbtn("ゲーム内部値に書込"))
                                flagWriteSave = true;

                            posYs += 10;

                            foreach (var milk in ctrl.milkCtrl.milkData.milks)
                            {
                                float count = milk.Get();
                                using (new UiUtil.GuiColor(Color.yellow, milk.IsSet()))
                                {
                                    if (floatctrl(milk.fieldName.Replace("Mouse", "Mouth"), ref count, -1, 0, 4, 1, 0))
                                    {
                                        if (count >= 0)
                                            milk.Set((int)count);
                                        else
                                        {
                                            milk.Set(0);
                                            milk.Reset();
                                        }

                                        flagWrite = true;
                                    }
                                }
                            }

                            if (flagWrite || flagWriteSave)
                                ctrl.milkCtrl.WriteData(true, flagWriteSave);
                        }

                    }

#if DEBUG
                    posYs += 5;
                    if (Extentions.FindSp("PC00") != null)
                    {
                        LiquidCounter comp = Extentions.FindSp("PC00").GetComponent<LiquidCounter>();
                        using (var enm = ctrl.milkCtrl.getMilkNames2(ctrl.ini.id))
                        {
                            while (enm.MoveNext())
                            {
                                var s = enm.Current as string;
                                var fi = typeof(LiquidCounter).GetField(s, BindingFlags.Instance | BindingFlags.Public);
                                if (fi == null)
                                    continue;
                                float count = (int)fi.GetValue(comp);

                                if (floatctrl(s, ref count, 0, 0, 4, 1, 0))
                                {
                                    int val = (int)count;
                                    fi.SetValue(comp, val);

                                    comp.InvokeNonPublicMethod("Update", null);

                                    if (ctrl.ini.id == "Neko")
                                    {
                                        var setup = ctrl.ini.rootObj.GetComponent<CostumeSetUp_CH01>();
                                        setup.SeiekiActive();
                                    }
                                    else if (ctrl.ini.id == "Usa")
                                    {
                                        var setup = ctrl.ini.rootObj.GetComponent<CostumeSetUp_CH02>();
                                        setup.SeiekiActive();
                                    }
                                }
                            }
                        }
                    }
#endif

                    posYs += 5;
                }
                posYs += 5;
            }
            posYs += 10;
        }
#endregion

#region ボーンスケール
        void boneScCtrl(string name, GirlCtrl girlCtrl)
        {
            BoneScales boneScales = girlCtrl.boneScales;

            if (rootbtn(nowEdit = "BoneSlider_" + name))
            {
                if (GUI.Button(RectU.sclRect(LX + EDTW - 26, (posYs), 54, 20), "RESET", gsButton))
                {
                    boneScales.ResetBoneScales();
                }
                posYs += 5;
                BoneScales.Edits obj = null;
                if (is_preset_save(nowEdit))
                    obj = boneScales.edits;
                if (presetctrl<BoneScales.Edits>(ref obj, nowEdit))
                {
                    boneScales.LoadEdits(obj);
                }

                posYs += 5;
                //boneScales.edits.girl_useLossyBScale = gettoggle("スケール設定で親の影響を除去(髪&服以外)", boneScales.edits.girl_useLossyBScale);
                boneScales.edits.hideFingers = gettoggle("指のボーンを表示しない", boneScales.edits.hideFingers);
                //BoneScales.hideNoacts = gettoggle("非アクティブなら非表示", BoneScales.hideNoacts);
                posYs += 5;

                using (new UiUtil.GuiColor(colorDemiCyan))
                    labelctrl("ボーンスケール変更", 0, 0);
                boneSclCtrlSub(name, girlCtrl);
                posYs += 5;
                posYs += 2;

                // パーティクルスケール
                using (new UiUtil.GuiColor(Color.yellow, boneScales.edits.fixParticlesScale != 1))
                    boneScales.edits.fixParticlesScale = getfloatctrl("パーティクル拡大率", boneScales.edits.fixParticlesScale, 1f, 0.01f, 5f, 1);

                //posYs += 5;
                posYs += 5;
                using (new UiUtil.GuiColor(colorDemiCyan))
                    labelctrl("ボーン位置変更", 0, 0);
                bonePosCtrl(name, girlCtrl);

                posYs += 5;
                using (new UiUtil.GuiColor(colorDemiCyan)) 
                    labelctrl("キャラ位置補正", 0, 0);
                if (GUI.Button(RectU.sclRect(LX + SLDWHF, (posYs), 60, 20), "Copy", gsButton))
                {
                    _copyOffsetData = Clone.Data(girlCtrl.boneScales.edits.offsetData);
                }
                if (_copyOffsetData != null)
                    if (GUI.Button(RectU.sclRect(LX + SLDWHF + 60, (posYs), 60, 20), "Paste", gsButton))
                    {
                        girlCtrl.boneScales.edits.offsetData = Clone.Data(_copyOffsetData);
                    }
                posCtrlOffset(name, girlCtrl);
                posCtrlOffsetByState(name, girlCtrl, "シーン", girlCtrl.boneScales.edits.offsetData.offsetByScenes);
                posCtrlOffsetByState(name, girlCtrl, "モーション", girlCtrl.boneScales.edits.offsetData.offsetByMotions);
                togglectrl("近いモーション(後ろ1文字違い)も適用", ref girlCtrl.boneScales.edits.offsetData.useFuzzySearchMotions);
                togglectrl("登録ﾓｰｼｮﾝ＆ｼｰﾝがなければ原点復帰", ref girlCtrl.boneScales.edits.offsetData.useOffsetReseet);

                posYs += 5;


                using (new UiUtil.GuiColor(colorDemiCyan))
                    labelctrl("フィルタリング", 0, 0);
                var list = girlCtrl.boneScales.edits.disapplyMouthMotionNames;
                bool bom = false;
                using (new UiUtil.GuiColor(Color.yellow, list.Count > 0))
                    bom = rootbtn2(nowEdit + "_MFilter_Mouth", "口スケール不適用モーション", 5, 0, GUI.color);
                if (bom)
                {
                    var state = "モーション";
                    var stateNow = girlCtrl.charaCtrl.getIOAnimeStateString();

                    labelctrl($"相性の悪いモーションを登録可", 10, 0);
                    labelctrl($"(口サイズ変更時、フェラ等で歪む場合)", 10, 0);

                    labelctrl($"現モーション: {stateNow}", 10, 0);

                    // 除外リスト
                    motionsListSubCtrl(list, state, stateNow);

                    posYs += 10;
                }
                else
                {
                    posYs += 15;
                }
                posYs += 5;
            }
            posYs += 10;
        }

        void boneSclCtrlSub(string name, GirlCtrl girlCtrl)
        {
            BoneScales boneScales = girlCtrl.boneScales;

            if (rootbtn2(nowEdit + "+scale", "ボーンサイズ調整", 0, 0,
                boneScales.edits.girl_bScales.Any(x => !string.IsNullOrEmpty(x.Key))  ? Color.yellow : Color.white))
            {
                foreach (var t in girlCtrl.ini.dics.bscValidObjNamesSW)
                {
                    if (rootbtn2(nowEdit + t.Key, t.Key, 5))
                    {
                        partscale(t.Key, t.Value);
                    }
                }

                // パーツスケール
                void partscale(string dic_name, string[] filter)
                {
                    if (filter == null || filter.Length <= 0)
                        return;

                    for (int i = 0; i < boneScales.girl_bScales0.Count; i++)
                    {
                        if (string.IsNullOrEmpty(boneScales.girl_bScales0[i].Key))
                            continue;

                        string bone_name = boneScales.girl_bScales0[i].Key;

                        var disp_name = girlCtrl.ini.dics.getDispStrBSC(dic_name, bone_name);
                        if (string.IsNullOrEmpty(disp_name))
                            continue;

                        if (boneScales.edits.hideFingers && (bone_name.Contains("Finger") || bone_name.Contains("Toe")))
                        {
                            continue;
                        }

                        posYs += 5;

                        //if (!string.IsNullOrEmpty(bone_name))
                        {
                            var def = boneScales.girl_bScales0[i].Value;
                            if (boneScales.dicNScale.ContainsKey(bone_name))
                            {
                                def = boneScales.dicNScale[bone_name];
                            }

                            bool enabled = !(string.IsNullOrEmpty(boneScales.edits.girl_bScales[i].Key));
                            Vector3 _v = def;
                            if (enabled)
                                _v = boneScales.edits.girl_bScales[i].Value;


                            var bon = gettoggle(disp_name, enabled);
                            if (!bon)
                            {
                                if (enabled)
                                {
                                    BoneScales.setBoneLocsclRL(boneScales.transform, girlCtrl.ini.dics, boneScales.edits.girl_bScales[i].Key, def);
                                    boneScales.edits.girl_bScales[i] = new StrV3Pair(string.Empty, def);
                                }
                            }
                            else
                            {
                                if (!enabled)
                                {
                                    var tr = boneScales.transform.FindSp(bone_name);
                                    if (tr)
                                    {
                                        _v = tr.localScale;
                                        Debug.Log(_v.x + " localScale @" + bone_name);
                                    }
                                    boneScales.dicNScale[bone_name] = _v;
                                }

                                posYs += 10;
                                GUI.color = Color.yellow;
                                _v.x = getfloatctrl(" *X", _v.x, def.x, 0f, 2f);
                                _v.y = getfloatctrl(" *Y", _v.y, def.y, 0f, 2f);
                                _v.z = getfloatctrl(" *Z", _v.z, def.z, 0f, 2f);

                                boneScales.edits.girl_bScales[i] = new StrV3Pair(bone_name, _v);

                                var bo0 = boneScales.edits.useLossyBScalePaths.Contains(bone_name);

                                /*
                                var pname = PlgUtil.GetParentNameFromPath(bone_name);
                                if (string.IsNullOrEmpty(pname))
                                    pname = string.Empty;
                                else
                                    pname = $"({pname})";*/

                                const int BSC_SUB_DX = 10;

                                //if (bo0 != gettoggle("親の影響を除去", bo0, SLDW - 100))
                                if (bo0 != gettoggle("親の影響を除去", bo0, BSC_SUB_DX, 0, -(SLDW - 100)-10))
                                {
                                    if (!bo0)
                                        boneScales.edits.useLossyBScalePaths.Add(bone_name);
                                    else
                                        boneScales.edits.useLossyBScalePaths.Remove(bone_name);
                                }

                                bo0 = boneScales.edits.useLocalPosScalePaths.Contains(bone_name);
                                posYs -= 20;
                                if (bo0 != gettoggle("位置にも適用", bo0, SLDW - 100))
                                //if (bo0 != gettoggle("位置にも適用", bo0, BSC_SUB_DX, 0, - (SLDW - 100)))
                                {
                                    if (!bo0)
                                        boneScales.edits.useLocalPosScalePaths.Add(bone_name);
                                    else
                                        boneScales.edits.useLocalPosScalePaths.Remove(bone_name);
                                }

                                //if (GUI.Button(RectU.sclRect(LX + 20, (posYs += 20), SLDW - 30, 20), "子への影響を除去", gsButton))

                                if (rootbtn2(nowEdit + "_L4C_" + bone_name, "子への影響を除去", BSC_SUB_DX + 5, 0, GUI.color))
                                {
                                    var on = PlgUtil.GetObjNameFromPath(bone_name);
                                    var nest = bone_name.CountChar('/');
                                    var lst = boneScales.girl_bScales0.FindAll(x => x.Key.Length > bone_name.Length && x.Key.CountChar('/') == nest + 1 && x.Key.StartsWith(bone_name));

                                    int count = 0;
                                    foreach (var p in lst)
                                    {
                                        if (PlgUtil.GetParentNameFromPath(p.Key) != on)
                                            continue;

                                        var cidx = boneScales.girl_bScales0.IndexOf(p);
                                        if (cidx < 0)
                                            continue;

                                        count++;

                                        var cld_path = p.Key;
                                        var cld_disp = girlCtrl.ini.dics.getDispStrBSC(cld_path);
                                        var bo1 = boneScales.edits.useLossyBScalePaths.Contains(cld_path);
                                        //if (string.IsNullOrEmpty(cld_disp))
                                        //    continue;

                                        var bo1n = bo1;
                                        using (new UiUtil.GuiColor(Color.white, !bo1))
                                            bo1n = gettoggle(cld_disp, bo1, BSC_SUB_DX+5);
                                        if (bo1 != bo1n)
                                        {
                                            if (bo1n)
                                            {
                                                var tmp = boneScales.edits.girl_bScales[cidx];
                                                var ctr = boneScales.transform.FindSp(p.Key);

                                                if (ctr)
                                                {
                                                    if (!boneScales.dicNScale.ContainsKey(cld_path))
                                                        boneScales.dicNScale[cld_path] = ctr.localScale;

                                                    if (string.IsNullOrEmpty(tmp.Key))
                                                        boneScales.edits.girl_bScales[cidx] = new StrV3Pair(cld_path, boneScales.dicNScale[cld_path]);

                                                    boneScales.edits.useLossyBScalePaths.Add(cld_path);
                                                }
                                            }
                                            else
                                            {
                                                boneScales.edits.useLossyBScalePaths.Remove(cld_path);

                                                if (boneScales.dicNScale.ContainsKey(cld_path))
                                                {
                                                    def = boneScales.dicNScale[cld_path];
                                                    if (def == boneScales.edits.girl_bScales[cidx].Value)
                                                    {
                                                        // 復元＆キー削除
                                                        BoneScales.setBoneLocsclRL(boneScales.transform, girlCtrl.ini.dics, cld_path, def);
                                                        boneScales.edits.girl_bScales[cidx] = new StrV3Pair(string.Empty, def);
                                                        boneScales.dicNScale.Remove(cld_path);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    if (count > 0)
                                        labelctrl("※子に[親への影響を除去]をセットします", 20, 0);
                                    else
                                        labelctrl("※子ボーンが見つかりません", 20, 0);
                                }

                                GUI.color = Color.white;
                            }
                            posYs += 5;
                        }
                    }
                    posYs += 5;
                }
            }
        }
#endregion

#region ボーン位置調整パーツ
        void bonePosCtrl(string name, GirlCtrl girlCtrl)
        {
            BonePos bonePos = girlCtrl.boneScales.pPosCtrl;

            if (rootbtn2(nowEdit + "+pos", "ボーン位置調整", 0, 0,
                bonePos.edits.girl_bPos.Any(x => !string.IsNullOrEmpty(x.Key)) ? Color.yellow : Color.white))
            {
                //posYs += 5;

                foreach (var t in girlCtrl.ini.dics.bpsValidObjNamesSW)
                {
                    if (rootbtn2(nowEdit + "+pos" + t.Key, t.Key, 5))
                    {
                        partspos(t.Key, t.Value);
                    }
                }

                // パーツ位置
                void partspos(string dic_name, string[] filter)
                {
                    if (filter == null || filter.Length <= 0)
                        return;

                    for (int i = 0; i < bonePos.girl_bPositions0.Count; i++)
                    {
                        if (string.IsNullOrEmpty(bonePos.girl_bPositions0[i].Key))
                            continue;

                        string bone_name = bonePos.girl_bPositions0[i].Key;
                        
                        var disp_name = girlCtrl.ini.dics.getDispStrBPS(dic_name, bone_name);
                        if (string.IsNullOrEmpty(disp_name))
                            continue;

                        if (girlCtrl.boneScales.edits.hideFingers && (bone_name.Contains("Finger") || bone_name.Contains("Toe")))
                        {
                            continue;
                        }
                        posYs += 5;

                        //if (!string.IsNullOrEmpty(bone_name))
                        {
                            var def_ = bonePos.girl_bPositions0[i].Value;
                            var key_ = bonePos.girl_bPositions0[i].Key;
                            if (bonePos.dicNPositions.ContainsKey(key_ ))
                            {
                                def_ = bonePos.dicNPositions[key_ ];
                            }

                            bool enabled = !(string.IsNullOrEmpty(bonePos.edits.girl_bPos[i].Key));
                            Vector3 _v = def_;
                            if (enabled)
                                _v = bonePos.edits.girl_bPos[i].Value;

                            var bon = gettoggle(disp_name, enabled);
                            if (!bon)
                            {
                                if (enabled)
                                {
                                    BonePos.setBoneLocpos(bonePos.transform, bonePos.edits.girl_bPos[i].Key, def_);
                                    bonePos.edits.girl_bPos[i] = new StrV3Pair(string.Empty, def_);
                                }
                            }
                            else
                            {
                                if (!enabled)
                                {
                                    var tr = bonePos.transform.FindSp(key_ );
                                    if (tr)
                                    {
                                        _v = tr.localPosition;
                                        Debug.Log(_v.x + " localPos @" + key_ );
                                    }
                                    bonePos.dicNPositions[key_ ] = _v;
                                }

                                posYs += 10;
                                GUI.color = Color.yellow;

                                int cx = (int)def_.x;
                                int cy = (int)def_.y;
                                int cz = (int)def_.z;

                                // バグあり保留
                                //_v.x = getfloatctrl_test(" *pos.X", _v.x, def.x, cx - 5f, cx + 5f);
                                //_v.y = getfloatctrl_test(" *pos.Y", _v.y, def.y, cy - 5f, cy + 5f);
                                //_v.z = getfloatctrl_test(" *pos.Z", _v.z, def.z, cz - 5f, cz + 5f);
                                _v.x = getfloatctrl_test(" *pos.X", _v.x, def_.x, - 5f, 5f);
                                _v.y = getfloatctrl_test(" *pos.Y", _v.y, def_.y, - 5f, 5f);
                                _v.z = getfloatctrl_test(" *pos.Z", _v.z, def_.z, - 5f, 5f);
                                bonePos.edits.girl_bPos[i] = new StrV3Pair(key_ , _v);

                                GUI.color = Color.white;
                            }
                            posYs += 5;
                        }
                    }
                    posYs += 5;
                }
            }
            posYs += 5;
        }

#endregion

#region ボーン位置調整
        void posCtrlOffset(string name, GirlCtrl ctrl)
        {
            //if (rootbtn("BoneSlider_" + name))
            {
                BoneScales boneScales = ctrl.boneScales;
                posYs += 5;
                
                Color clr = (boneScales.edits.offsetData.girlOffset.dpos != Vector3.zero
                        || boneScales.edits.offsetData.girlOffset.drot != Vector3.zero
                        || boneScales.edits.offsetData.girlOffset.coord != BoneScales.OffsetCoord.None
                    ) ? Color.yellow : Color.white;

                if (rootbtn2("PosOffset-Offset_" + name, "キャラ基準位置・角度補正", 0, 0, clr))
                {
                    //labelctrl("位置・角度補正", 0, 0);
                    //labelctrl(" ※保存は体形データに付加(体形プリセット等)", 5, 0);
                    Vector3 _v = boneScales.edits.offsetData.girlOffset.dpos;

                    if (!BoneScales.gizmoGirlPos)
                        BoneScales.gizmoGirlPosInit();

                    bool boGizmo = BoneScales.gizmoGirlPos.visible && BoneScales.gizmoGirlPos.target == boneScales.transform;
                    if (boGizmo != gettoggle("Gizmo", boGizmo, 0, 0))
                    {
                        if (!boGizmo)
                            BoneScales.gizmoGirlPos.setTarget(boneScales.transform, ctrl.ini.id + "_gizmoGirlPos");
                        else
                            BoneScales.gizmoGirlPos.resetTarget();
                    }

                    posYs += 10;

                    if (ctrl.ini.isGirl())
                    {
                        labelctrl(" 位置補正基準: ", 5, 0);

                        Rect rect = new Rect(LX + SLDWHF, (posYs), SLDWHF, ItemHeight);
                        var comboProc = easyCombo.Show("posCtrlOffset_" + name, rect, ItemHeight, ItemHeight * 5,
                            BoneScales.offsetCoordsData.Select(x => x.dispName).ToArray(),
                            BoneScales.offsetCoordsData.First(x => x.coord == boneScales.edits.offsetData.girlOffset.coord).dispName,
                            gsButton, gsTextField);

                        if (comboProc)
                        {
                            if (easyCombo.sIndex >= 0)
                            {
                                boneScales.edits.offsetData.girlOffset.coord = BoneScales.offsetCoordsData[easyCombo.sIndex].coord;
                            }
                        }
                        if (easyCombo.lastPop())
                        {
                            posYs += ItemHeight * 5;
                        }
                        posYs += 10;
                    }
                    else
                    {
                        boneScales.edits.offsetData.girlOffset.coord = BoneScales.OffsetCoord.None;
                    }

                    boneScales.edits.offsetData.girlOffset.dpos = v3ctrl2_test(" 位置補正", " +", boneScales.edits.offsetData.girlOffset.dpos, Vector3.zero, -1f, 1f);

                    posYs += 10;

                    _v = boneScales.edits.offsetData.girlOffset.drot;
                    boneScales.edits.offsetData.girlOffset.drot = v3ctrl2(" 角度補正", " +Rot-", boneScales.edits.offsetData.girlOffset.drot, Vector3.zero, -180f, 180f);


                    //if (GUI.Button(RectU.sclRect(LX + 10, (posYs += 20), SLDW - 30, 20), "角度初期化", gsButton))
                    //{
                    //    var tr = boneScales.transform;
                    //    if (tr)
                    //        tr.localRotation = Quaternion.identity;
                    //    boneScales.transform.localPosition = Vector3.zero;
                    //    boneScales.transform.localRotation = Quaternion.identity;
                    //    boneScales.edits.girlOffset.drot = Vector3.zero;
                    //}
                    posYs += 10;
                }
                else
                {
                    if (BoneScales.gizmoGirlPos && BoneScales.gizmoGirlPos.visible && BoneScales.gizmoGirlPos.targetName != null
                            && (BoneScales.gizmoGirlPos.target == ctrl.FindBone() || BoneScales.gizmoGirlPos.targetName.StartsWith(ctrl.ini.id, StringComparison.Ordinal))
                        )
                    {
                        BoneScales.gizmoGirlPos.resetTarget();
                    }
                }
            }
            //posYs += 5;
        }
#endregion

#region 位置調整・シーン割り当て
        void posCtrlOffsetByState(string name, GirlCtrl ctrl, string state, BoneScales.OffsetByStates offsetByStates)
        {
            //if (rootbtn("BoneSlider_" + name))
            {
                BoneScales boneScales = ctrl.boneScales;

                //posYs += 5;

                Color clr = (offsetByStates.list.Count > 0) ? Color.yellow : Color.white;
                if (rootbtn2($"PosOffset-{state}_" + name, $"位置補正の{state}別登録", 0, 0, clr))
                {
                    posYs += 5;

                    togglectrl("有効", ref offsetByStates.enabled);
                    posYs += 5;

                    string stateNow = "";
                    string stateNowNote = "";
                    if (state == "シーン")
                    {
                        togglectrl("シーン＋背景名で優先登録 (共通)", ref cfg.CharEdits_PosOfsByScene_PlusBGName);
                        posYs += 5;

                        if (Free3pXtMS.IsSlave(ctrl))
                        {
                            stateNow = Free3pXtMS.GetXtSceneName;
                            stateNowNote = "(リンク中)";
                        }
                        else
                        {
                            stateNow = boneScales.GetCurrentBsSceneName(actScene, cfg.CharEdits_PosOfsByScene_PlusBGName);

                            switch (stateNow.Substring(0, Math.Min(stateNow.Length, 2)))
                            {
                                case "IC": stateNowNote = " (前戯)"; break;
                                case "UC": stateNowNote = " (特殊H)"; break;
                                case "FH": stateNowNote = " (フリーH)"; break;
                            }
                            if (stateNow == "ADV@Cinema") stateNowNote = " (イベント)";

                            if (cfg.CharEdits_PosOfsByScene_PlusBGName)
                            {
                                var bgn = BGMGR.GetJpNameByNameBG(BGMGR.GetNameBG());
                                if (string.IsNullOrEmpty(bgn))
                                    bgn = "2D背景";

                                if (string.IsNullOrEmpty(stateNowNote))
                                    stateNowNote = $" ({bgn})";
                                else
                                    stateNowNote.Replace(")", $"+{bgn})");
                            }
                        }
                        
                    }
                    else if (state == "モーション")
                    {
                        stateNow = ctrl.charaCtrl.getIOAnimeStateString();
                    }

                    labelctrl($"現在: {stateNow}{stateNowNote}", 5, 0);
                    labelctrl($"  Pos: {boneScales.edits.offsetData.girlOffset.dpos}  Rot: {boneScales.edits.offsetData.girlOffset.drot}", 5, 0);
                    labelctrl($"  位置補正基準:  {BoneScales.offsetCoordsData.First(x => x.coord == boneScales.edits.offsetData.girlOffset.coord).dispName}", 5, 0);

                    if (centerbtn($"{state}登録"))
                    {
                        if (offsetByStates.list.Any(x => x.stateName == stateNow))
                            offsetByStates.list.First(x => x.stateName == stateNow).offset = boneScales.edits.offsetData.girlOffset;
                        else
                            offsetByStates.list.Add(new BoneScales.OffsetByState(stateNow, boneScales.edits.offsetData.girlOffset));

                    }
                    posYs += 10;
                    labelctrl($"登録数: {offsetByStates.list.Count}", 5, 0);

                    const int SUBBTNW = (SLDW - LX - 20) / 4;
                    const int SUBLX = LX + 20;//LX + 30 + 5;

                    int num = 0;
                    foreach (var o in offsetByStates.list.ToArray())
                    {
                        num++;

                        using (new UiUtil.GuiColor(Color.green, o.stateName == stateNow))
                            labelctrl($"{state}: {o.stateName}" , 5, 0);
                        labelctrl($"  Pos: {o.offset.dpos}  Rot: {o.offset.drot}" , 5, 0);
                        if (ctrl.ini.isGirl())
                        {
                            labelctrl($"  位置補正基準:  {BoneScales.offsetCoordsData.First(x => x.coord == o.offset.coord).dispName}", 5, 0);
                        }
                        else
                        {
                            o.offset.coord = BoneScales.OffsetCoord.None;
                        }
                        posYs += 20;

                        if (GUI.Button(RectU.sclRect(SUBLX + SUBBTNW * 0, (posYs), SUBBTNW - 2, 20), "Load", gsButton))
                        {
                            boneScales.edits.offsetData.girlOffset = o.offset;
                        }
                        if (GUI.Button(RectU.sclRect(SUBLX + SUBBTNW * 1, (posYs), SUBBTNW - 2, 20), "Set", gsButton))
                        {
                            offsetByStates.list[num].offset = boneScales.edits.offsetData.girlOffset;
                        }

                        if (num > 1)
                        if (GUI.Button(RectU.sclRect(SUBLX + SUBBTNW * 2, (posYs), SUBBTNW - 2, 20), "Up", gsButton))
                        {
                            var i = offsetByStates.list.IndexOf(o);
                            if (i > 0)
                            {
                                var t = offsetByStates.list[i];
                                offsetByStates.list[i] = offsetByStates.list[i - 1];
                                offsetByStates.list[i - 1] = t;
                            }
                        }
                        if (GUI.Button(RectU.sclRect(SUBLX + SUBBTNW * 3, (posYs), SUBBTNW - 2, 20), "Del", gsButton))
                        {
                            offsetByStates.list.Remove(o);
                        }

                        posYs += 5;
                    }
                    posYs += 10;
                }
            }
        }
#endregion

#region シーン位置調整
        void posCtrl(string name, GirlCtrl ctrl)
        {
            if (rootbtn(nowEdit = "ScenePosition_" + name))
            {
                Color clr = (ctrl.scnPos.enable) ? Color.yellow : Color.white;
                //if (rootbtn2("PosOffset-ScenePos", "シーン位置設定 (※TEMP)", 5, 0, clr))
                if (true)
                {
                    posYs += 5;
                    //labelctrl("シーン位置設定 (※TEMP)", 0, 0);
                    labelctrl(" ※撮影用。プリセットのみに保存", 5, 0);

                    if (ctrl.scnPos.enable)
                    {
                        posYs += 5;
                        ScenePos obj = null;
                        string spEditTag = nowEdit;
                        if (is_preset_save(spEditTag))
                            obj = ctrl.scnPos;
                        if (presetctrl(ref obj, spEditTag))
                        {
                            if (ctrl.scnPos.enable && !obj.enable)
                                ctrl.scnPos.ResetPosRot(ctrl.ScnPosRoot().transform);
                            ScenePos.Load(ctrl, obj);
                        }
                        posYs += 5;
                    }

                    var sp = ctrl.scnPos;
                    //if (sp.enable)
                    //{
                    //    GUI.color = Color.yellow;
                    //}

                    using (new UiUtil.GuiColor(Color.yellow, sp.enable))
                    {
                        var onflg = false;
                        var bo = gettoggle("有効にする", sp.enable);
                        if (sp.enable != bo)
                        {
                            sp.enable = bo;
                            if (sp.enable)
                            {
                                onflg = true;
                            }
                            else
                            {
                                ctrl.scnPos.ResetPosRot(ctrl.ScnPosRoot().transform);

                                if (ScenePos.gizmoTgt == ctrl.ScnPosRoot())
                                    ScenePos.gizmoTgt = null;
                            }
                        }

                        if (!sp.enable || onflg)
                        {
                            GUI.enabled = false;
                            var go = ctrl.ScnPosRoot();
                            if (go)
                            {
                                var tr = go.transform;
                                sp.orgPos = sp.pos = ctrl.charaCtrl.OriginalLocPos;//tr.localPosition - ctrl.boneScales.edits.offsetData.girlOffset.dpos;
                                sp.orgRot = sp.rot = ctrl.charaCtrl.OriginalLocRot.angle180();//tr.localEulerAngles.angle180();

                                //if (ctrl.boneScales.edits.girl_bScales.Any(c => c.Key == tr.name))
                                //{
                                //    sp.scale = tr.localScale.x / ctrl.boneScales.edits.girl_bScales.First(c => c.Key == tr.name).Value.x;
                                //}
                                //else
                                if (ctrl.boneScales.edits.girl_bScales.Any(c => c.Key == BoneScales.KITEN_PATH) && ctrl.boneScales.dicNScale.Any(c => c.Key == BoneScales.KITEN_PATH))
                                {
                                    sp.scale = tr.localScale.x / ctrl.boneScales.edits.girl_bScales.First(c => c.Key == BoneScales.KITEN_PATH).Value.x
                                        * ctrl.boneScales.dicNScale.First(c => c.Key == BoneScales.KITEN_PATH).Value.x;
                                }
                                else
                                {
                                    sp.scale = tr.localScale.x;
                                }
                            }
                        }

                        bool bo0 = ScenePos.gizmoTgt == ctrl.ScnPosRoot();
                        bool bo1 = gettoggle("Gizmo", bo0, 0, 5);
                        if (bo0 != bo1)
                        {
                            ScenePos.gizmoTgt = bo1 ? ctrl.ScnPosRoot() : null;
                        }
                        posYs += 5;
                        sp.pos = v3ctrl2_test(" キャラクター座標", " ", sp.pos, sp.orgPos, -10f, 10f);
                        posYs += 5;
                        sp.rot = v3ctrl2(" キャラクター角度", " Rot-", sp.rot, sp.orgRot, -180f, 180f);
                        posYs += 5;

                        if (ctrl.ini.boneRoot.Contains("HS_kiten"))
                            sp.scale = getfloatctrl(" キャラクター拡縮", sp.scale, 0.1f, 0.1f, 10f, 10f);
                        else
                            sp.scale = getfloatctrl(" キャラクター拡縮", sp.scale, 1f, 0.1f, 10f, 1f);

                    }

                    GUI.enabled = true;
                    GUI.color = Color.white;
                }
                //else
                //{
                //    if (ScenePos.gizmoTgt == ctrl.FindBone())
                //        ScenePos.gizmoTgt = null;
                //}
                posYs += 10;
            }
            else
            {
                //if (BoneScales.gizmoGirlPos && BoneScales.gizmoGirlPos.visible
                //    && BoneScales.gizmoGirlPos.target == ctrl.ScnPosRoot())
                //    BoneScales.gizmoGirlPos.resetTarget();

                if (ScenePos.gizmoTgt == ctrl.ScnPosRoot())
                    ScenePos.gizmoTgt = null;
            }
            posYs += 10;
        }
#endregion

#region シェイプキー

        void shapeCtrl(string name, GirlCtrl ctrl)
        {
            ShapeKeys shape = ctrl.shapeKeys;
            if (rootbtn(nowEdit = "ShapeKeys_" + name))
            {
                if (GUI.Button(RectU.sclRect(LX + EDTW - 26, (posYs), 54, 20), "RESET", gsButton))
                {
                    shape.ResetAllDic();
                }
                posYs += 5;
                SaveShapeKeys obj = null;
                if (is_preset_save(nowEdit))
                    obj = new SaveShapeKeys(shape.edits);
                if (presetctrl<SaveShapeKeys>(ref obj, nowEdit))
                {
                    obj.Load(shape);
                }
                posYs += 5;

                shape.edits.useLegacyCtrl = gettoggle("オブジェクト別設定", shape.edits.useLegacyCtrl);
                posYs += 5;

                if (!shape.edits.useLegacyCtrl)
                    shapeCtrl_new2(name, ctrl);
                else
                    shapeCtrl_old(name, ctrl);

                shape.GuiOpenFlag = true;
            }
            else
            {
                shape.GuiOpenFlag = false;
            }

            posYs += 10;
        }

        bool hideShapekeyInactive = true;

        // 実体を操作しない版、低負荷＆応答悪め
        void shapeCtrl_new2(string name, GirlCtrl ctrl)
        {
            ShapeKeys shape = ctrl.shapeKeys;
            Dictionary<string, float> setBlends = new Dictionary<string, float>();

            var dicBlends = shape.edits.dicNameAndValues;
            //foreach (var s in shape.edits.dicNameAndValues)
            //    setBlends.Add(s.Key, s.Value);

            //labelctrl("※服装などを変更したら再設定が必要", 5, 0);
            //labelctrl(" (この画面を開くだけでもOK)", 5, 0);

            hideShapekeyInactive = gettoggle("無効オブジェクトのキーを非表示", hideShapekeyInactive);
            posYs += 10;

            var tr0 = ctrl.FindModel().transform;
            foreach (var keys in shape.dicShapeKeysMap)
            {
                try
                {
                    if (keys.Value.dicNameToIndex.Count <= 0)
                        continue;

                    GameObject go = null;
                    if (hideShapekeyInactive)
                    {
                        var tr = tr0.FindSp(keys.Key);
                        if (!tr || !tr.gameObject.activeInHierarchy)
                            continue;
                        go = tr.gameObject;
                    }

                    foreach (var keyValue in keys.Value.dicNameToIndex)
                    {
                        var shapename = keyValue.Key;
                        if (ctrl.ini.dics.ignoreShapeKeys.Contains(shapename))
                            continue;

                        var fullsname = keys.Key + "@" + keyValue.Value;
                        float neww = 0;

                        float oldw = 0;
                        bool isExist = dicBlends.TryGetValue(shapename, out oldw);

                        if (!setBlends.TryGetValue(shapename, out neww))
                        {
                            if (!isExist)
                            {
                                shape.dicShapeKeysDefval.TryGetValue(fullsname, out oldw);
                                //oldw = shape.dicShapeKeysDefval[fullsname];
                            }

                            posYs += 5;
                            using (new UiUtil.GuiColor(Color.yellow, isExist))
                                neww = floatctrl2(shapename, oldw, -999f, 0f, 100f);

                            if (isExist || oldw != neww)
                            {
                                setBlends[shapename] = neww;
                            }
                            else
                            {
                                setBlends[shapename] = -999;
                            }
                        }

                        if (isExist && neww < -900f)
                        {
                            if (!go)
                            {
                                var tr = tr0.FindSp(keys.Key).gameObject;
                                if (tr)
                                    go = tr.gameObject;
                            }

                            if (go)
                            {
                                var skm = go.GetComponent<SkinnedMeshRenderer>();
                                if (skm)
                                {
                                    if (shape.dicShapeKeysDefval.ContainsKey(fullsname))
                                    {
                                        skm.SetBlendShapeWeight(keyValue.Value, shape.dicShapeKeysDefval[fullsname]);
                                    }
                                    else
                                        skm.SetBlendShapeWeight(keyValue.Value, 0f);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                }
                //posYs += 5;
            }
            //posYs -= 5;
            posYs += 5;


            foreach (var s in setBlends)
            {
                if (s.Value >= -100)
                    shape.edits.dicNameAndValues[s.Key] = s.Value;
                else
                    shape.edits.dicNameAndValues.Remove(s.Key);
            }
        }


        void shapeCtrl_new(string name, GirlCtrl ctrl)
        {
            ShapeKeys shape = ctrl.shapeKeys;
            Dictionary<string, float> setBlends = new Dictionary<string, float>();

            var dicBlends = shape.edits.dicNameAndValues;
            //foreach (var s in shape.edits.dicNameAndValues)
            //    setBlends.Add(s.Key, s.Value);

            labelctrl("※服装などを変更したら再設定が必要", 5, 0);
            labelctrl(" (この画面を開くだけでもOK)", 5, 0);

            var tr0 = ctrl.FindModel().transform;
            foreach (var keys in shape.edits.dicShapeKeys)
            {
                try
                {
                    var go = tr0.FindSp(keys.Key).gameObject;
                    if (!go.activeInHierarchy)
                        continue;

                    var skm = go.GetComponent<SkinnedMeshRenderer>();
                    if (!skm || !skm.enabled)
                        continue;

                    var shm = skm.sharedMesh;
                    for (int i = 0; i < shm.blendShapeCount; i++)
                    {
                        var shapename = shm.GetBlendShapeName(i);
                        if (ctrl.ini.dics.ignoreShapeKeys.Contains(shapename))
                            continue;

                        var fullsname = keys.Key + "@" + i;
                        //bool isExist = keys.Value.ContainsKey(i);
                        bool isExist = dicBlends.ContainsKey(shapename);

                        float oldw = 0;
                        float neww = 0;

                        if (!setBlends.TryGetValue(shapename, out neww))
                        {
                            oldw = skm.GetBlendShapeWeight(i);

                            posYs += 5;
                            using (new UiUtil.GuiColor(Color.yellow, isExist))
                                neww = floatctrl2(shapename, oldw, -999f, 0f, 100f);

                            if (isExist || oldw != neww)
                            {
                                setBlends[shapename] = neww;
                                if (neww >= -900)
                                    keys.Value[i] = neww; // 登録
                            }
                            else
                            {
                                setBlends[shapename] = -999;
                            }
                        }
                        else if (neww >= -900)
                        {
                            //oldw = skm.GetBlendShapeWeight(i);
                            keys.Value[i] = neww; // 登録
                        }

                        if (neww < -900f)
                        {
                            if (isExist)
                            {
                                skm.SetBlendShapeWeight(i, 0f);
                                keys.Value.Remove(i);

                                if (shape.dicShapeKeysDefval.ContainsKey(fullsname))
                                {
                                    skm.SetBlendShapeWeight(i, shape.dicShapeKeysDefval[fullsname]);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                }
                //posYs += 5;
            }
            //posYs -= 5;
            posYs += 5;


            foreach (var s in setBlends)
            {
                if (s.Value >= -100)
                    shape.edits.dicNameAndValues[s.Key] = s.Value;
                else
                    shape.edits.dicNameAndValues.Remove(s.Key);
            }
        }

        void shapeCtrl_old(string name, GirlCtrl ctrl)
        {
            ShapeKeys shape = ctrl.shapeKeys;

            var tr0 = ctrl.FindModel().transform;
            foreach (var keys in shape.edits.dicShapeKeys)
            {
                try
                {
                    var tr = tr0.FindSp(keys.Key);
                    if (!tr)
                        continue;

                    var go = tr.gameObject;

                    using (new UiUtil.GuiEnable(go.activeInHierarchy))
                    {
                        Color clr = keys.Value.Count > 0 ? Color.yellow : Color.white;
                        // GetFileNameは遅い if (rootbtn2("ShapeKey_" + keys.Key, Path.GetFileName(keys.Key), 5, 0, clr))
                        if (rootbtn2("ShapeKey_" + keys.Key, PlgUtil.GetObjNameFromPath(keys.Key), 5, 0, clr))
                        {
                            var skm = go.GetComponent<SkinnedMeshRenderer>();
                            var shm = skm.sharedMesh;
                            for (int i = 0; i < shm.blendShapeCount; i++)
                            {
                                var oldw = skm.GetBlendShapeWeight(i);
                                if (keys.Value.ContainsKey(i))
                                    GUI.color = Color.yellow;

                                var shapename = shm.GetBlendShapeName(i);
                                var fullsname = keys.Key + "@" + i;

                                posYs += 5;
                                var neww = floatctrl2(shapename, oldw, -999f, 0f, 100f);
                                GUI.color = Color.white;

                                if (neww < -900f)
                                {
                                    skm.SetBlendShapeWeight(i, 0f);
                                    keys.Value.Remove(i);

                                    if (shape.dicShapeKeysDefval.ContainsKey(fullsname))
                                    {
                                        skm.SetBlendShapeWeight(i, shape.dicShapeKeysDefval[fullsname]);
                                    }
                                }
                                else if (oldw != neww)
                                {
                                    keys.Value[i] = neww; // 登録
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                }
                posYs += 5;
            }
            //posYs -= 5;
        }
#endregion



#region ボーン角度
        // ボーンコントロール用
        void prectrl_bones(GirlCtrl girlCtrl, RotctrlBone rcbCtrl, bool noGizmo = false)
        {
            if (GUI.Button(RectU.sclRect(LX + EDTW - 26, (posYs), 54, 20), "RESET", gsButton))
            {
                rcbCtrl.dicEulers.Clear();
                gizmoRot.tryOffTarget(gizmoRot.transform);
            }

            posYs += 5;
            labelctrl(" ※撮影用。プリセットのみに保存", 5, 0);
            if (FlagUsaFH)
            {
                posYs += 5;
                using (new UiUtil.GuiColor(Color.yellow))
                    labelctrl(" ※当シーンのキャプは別シーンで使用不能", 5, 0);
            }

            posYs += 5;

            SaveDicStrV3 obj = null;
            if (is_preset_save(nowEdit))
                obj = new SaveDicStrV3(rcbCtrl.dicEulers);
            if (presetctrl<SaveDicStrV3>(ref obj, nowEdit))
            {
                obj.Load(ref rcbCtrl.dicEulers);
                rcbCtrl.enable = true;
                rcbCtrl.blend = 1f;

                if (rcbCtrl == girlCtrl.rcbFullbody)
                {
                    // リグの再配置など
                }
            }
            posYs += 5;

            rcbCtrl.enable = gettoggle("有効", rcbCtrl.enable);
            posYs += 5;
            rcbCtrl.blend = getfloatctrl("ブレンド率", rcbCtrl.blend, 1f, 0f, 1f);
            if (!noGizmo)
                gizmoRot.sizeRot = getfloatctrl("ギズモサイズ", gizmoRot.sizeRot, 1.5f, 0.01f, 5f);
        }

        Dictionary<Transform, Dictionary<string, Transform>> dicCacheRCB = new Dictionary<Transform, Dictionary<string, Transform>>();

        void ctrl_bones(RotctrlBone rcbCtrl, Transform tr0, string s, string flg)
        {
            var tr = tr0.FindSp(s);
            if (!tr && flg == string.Empty && tr0.name == s)
                tr = tr0;

            if (!tr)
                return;

            if (!dicCacheRCB.ContainsKey(tr0))
            {
                // キャッシュ作成
                dicCacheRCB[tr0] = new Dictionary<string, Transform>();
            }

            //if (!Path.GetFileName(s).Contains("Carpal"))
            {
                Vector3 _v;//tr.localEulerAngles;
                if (!rcbCtrl.dicEulers.TryGetValue(s, out _v))
                {
                    _v = tr.localEulerAngles;
                }
                else
                {
                    GUI.color = Color.yellow;
                    if (GUI.Button(RectU.sclRect(LX + EDTW - 26, (posYs + ItemHeight), 54, 20), "解除", gsButton))
                    {
                        rcbCtrl.dicEulers.Remove(s);
                        GUI.color = Color.white;

                        gizmoRot.tryOffTarget(tr);
                        return;
                    }
                }

                var name = rcbCtrl.transBoneName(Path.GetFileName(s));
                var label = "" + flg + name;
                var bo = gizmoRot.target == tr && gizmoRot.visible;
                if (bo && GUI.color != Color.yellow)
                {
                    // 設定が変わったのに残ってるパターン
                    gizmoRot.tryOffTarget(tr);
                    return;
                }

                dicCacheRCB[tr0][s] = tr;

                int dwx = 10;
                if (GUI.color == Color.white)
                    dwx += 54;
                var nb = GUI.Toggle(RectU.sclRect(LX, (posYs += ItemHeight), EDTW - 36 + dwx, ItemHeight), bo, label, gsToggle);
                if (nb != bo)
                {
                    if (nb)
                    {
                        gizmoRot.setTarget(tr, s, true);
                        _NowGizmoRotTargetCtrl = rcbCtrl;

                        rcbCtrl.dicEulers[s] = _v;
                    }
                    else
                    {
                        gizmoRot.tryOffTarget(tr);
                    }
                }

                if (nb)
                {
                    var bkv = _v;
                    // to -180~180
                    bkv.x = Mathf.DeltaAngle(0f, _v.x);
                    bkv.y = Mathf.DeltaAngle(0f, _v.y);
                    bkv.z = Mathf.DeltaAngle(0f, _v.z);
                    _v = v3ctrl("+Rot-", bkv, Vector3.zero, -180f, 180f);

                    //if (_v != tr.localEulerAngles)
                    if (_v != bkv)
                    {
                        gizmoRot.setTarget(tr, s, true);
                        _NowGizmoRotTargetCtrl = rcbCtrl;

                        rcbCtrl.dicEulers[s] = _v;
#if DEBUG
                        //Debug.Log(s + " -> " + _v);
#endif
                    }
                }
                GUI.color = Color.white;

                flg += "+";
            }

            for (int i = 0; i < tr.childCount; i++)
            //foreach(Transform t in tr) 内部でGetChild呼んでるだけだし遅い
            {
                var t = tr.GetChild(i);
                if (!t || string.IsNullOrEmpty(t.name))
                    continue;

                var n = t.name;
                //if (n.StartsWith("Left", StringComparison.Ordinal) || n.StartsWith("Right", StringComparison.Ordinal))
                if (!n.StartsWith(gizmoRot.name, StringComparison.Ordinal))
                {
                    var ns = s + "/" + n;
                    ctrl_bones(rcbCtrl, tr0, ns, flg);
                }
            }
        }

        void ctrl_bones_CacheReset(Transform tr0)
        {
            if (tr0 == null)
            {
                foreach (var d in dicCacheRCB)
                    if (d.Value != null) d.Value.Clear();
                dicCacheRCB.Clear();
                return;
            }

            if (dicCacheRCB.ContainsKey(tr0))
            {
                // キャッシュクリア
                dicCacheRCB[tr0].Clear();
                dicCacheRCB.Remove(tr0);
                return;
            }
        }

        void ctrl_bones_withCache(RotctrlBone rcbCtrl, Transform tr0, string _s, string _flg)
        {
            var tr = tr0.FindSp(_s);
            if (!tr && tr0.name != _s)
                return;

            if (!dicCacheRCB.ContainsKey(tr0))
            {
                // キャッシュなし
                ctrl_bones(rcbCtrl, tr0, _s, _flg);
                return;
            }

            foreach(var s in dicCacheRCB[tr0])
            {
                Vector3 _v;//tr.localEulerAngles;
                if (!rcbCtrl.dicEulers.TryGetValue(s.Key, out _v))
                {
                    _v = s.Value.localEulerAngles;
                }
                else
                {
                    GUI.color = Color.yellow;
                    if (GUI.Button(RectU.sclRect(LX + EDTW - 26, (posYs + ItemHeight), 54, 20), "解除", gsButton))
                    {
                        rcbCtrl.dicEulers.Remove(s.Key);
                        GUI.color = Color.white;

                        gizmoRot.tryOffTarget(s.Value);
                        return;
                    }
                }

                var name = rcbCtrl.transBoneName(PlgUtil.GetObjNameFromPath(s.Key));
                var label = "" + new string('+', s.Key.CountChar('/')) + name;
                var bo = gizmoRot.target == s.Value && gizmoRot.visible;
                if (bo && GUI.color != Color.yellow)
                {
                    // 設定が変わったのに残ってるパターン
                    gizmoRot.tryOffTarget(s.Value);
                    return;
                }

                int dwx = 10;
                if (GUI.color == Color.white)
                    dwx += 54;
                var nb = GUI.Toggle(RectU.sclRect(LX, (posYs += ItemHeight), EDTW - 36 + dwx, ItemHeight), bo, label, gsToggle);
                if (nb != bo)
                {
                    if (nb)
                    {
                        gizmoRot.setTarget(s.Value, s.Key, true);
                        _NowGizmoRotTargetCtrl = rcbCtrl;

                        rcbCtrl.dicEulers[s.Key] = _v;
                    }
                    else
                    {
                        gizmoRot.tryOffTarget(s.Value);
                    }
                }

                if (nb)
                {
                    var bkv = _v;
                    // to -180~180
                    bkv.x = Mathf.DeltaAngle(0f, _v.x);
                    bkv.y = Mathf.DeltaAngle(0f, _v.y);
                    bkv.z = Mathf.DeltaAngle(0f, _v.z);
                    _v = v3ctrl("+Rot-", bkv, Vector3.zero, -180f, 180f);

                    //if (_v != tr.localEulerAngles)
                    if (_v != bkv)
                    {
                        gizmoRot.setTarget(s.Value, s.Key, true);
                        _NowGizmoRotTargetCtrl = rcbCtrl;

                        rcbCtrl.dicEulers[s.Key] = _v;
#if DEBUG
                        //Debug.Log(s + " -> " + _v);
#endif
                    }
                }
                GUI.color = Color.white;
            }
        }
        #endregion


        #region ポーズ
        void poseCtrl(string name, GirlCtrl ctrl)
        {
            //GUI.contentColor = Color.cyan;
            //GUI.Label(RectU.sclRect(ItemX + 20, (posYs += 20), LBL_WD, 20), $"ポーズキャプチャ ({ctrl.ini.name})", gsLabel);
            //GUI.contentColor = Color.white;
            if (rootbtn(nowEdit = "FullbodyPose_" + name))
            {
                RotctrlBone rcbCtrl = ctrl.rcbFullbody;

                prectrl_bones(ctrl, rcbCtrl);
                posYs += 5;

                if (ctrl.isActive())
                {
                    if (rcbCtrl.dicEulers.Count > 0)
                    {
                        GUI.color = Color.yellow;
                    }
                    labelctrl("全身ポーズ取得", 0, 0);
                    if (GUI.Button(RectU.sclRect(LX + SLDWHF + 30 - 60 + 20, (posYs), 50, 20), "Cap", gsButton))
                    {
                        //foreach (var s in rcbCtrl.boneroot)
                        //    rcbCtrl.capAll(rcbCtrl.dicEulers, ctrl.FindBone().transform, s);

                        rcbCtrl.capAll(ctrl.FindBone().transform);
                        rcbCtrl.enable = true;
                    }
                    if (rcbCtrl.dicEulers.Count > 0)
                    {
                        if (GUI.Button(RectU.sclRect(LX + SLDWHF + 30 + 20, (posYs), SLDWHF - 10 - 30, 20), "クリア", gsButton))
                        {
                            rcbCtrl.dicEulers.Clear();
                            gizmoRot.tryOffTarget(gizmoRot.transform);
                        }
                        GUI.color = Color.white;
                    }

                    //bool capRoot = !RotctrlBone.ignoreBN.Contains("kiten");
                    //if (capRoot != gettoggle("キャラクター角度も保存", capRoot))
                    //{
                    //    if (RotctrlBone.ignoreBN.Contains("HS_kiten"))
                    //        RotctrlBone.ignoreBN.Remove("HS_kiten");
                    //    else
                    //        RotctrlBone.ignoreBN.Add("HS_kiten");
                    //}
                    posYs += 10;

                    if (rcbCtrl.enable /*&& rcbCtrl.dicEulers.Count > 0*/)
                    {
                        if (rcbCtrl.enable)
                        {
                            if (rootbtn2(nowEdit + "FullbodyFK", "全身ボーン角度調整 (FK)", 0, 0, rcbCtrl.dicEulers.Count > 0 ? Color.yellow : colorDemiCyan))
                            {
                                foreach (var s in rcbCtrl.boneroots)
                                {
                                    //ctrl_bones(rcbCtrl, ctrl.FindBone().transform, s, "");
                                    ctrl_bones_withCache(rcbCtrl, ctrl.FindBone().transform, s, "");
                                }
                                posYs += 10;
                            }
                            else
                            {
                                ctrl_bones_CacheReset(ctrl.FindBone().transform);
                            }
                            posYs += 10;
                        }
                    }

                }
            }
            posYs += 10;
        }
#endregion


#region マテリアル
        void mateCtrl(string name, GirlCtrl ctrl)
        {
            //GUI.contentColor = Color.cyan;
            //GUI.Label(RectU.sclRect(ItemX + 20, (posYs += 20), LBL_WD, 20), $"マテリアル設定 ({ctrl.ini.name})", gsLabel);
            //GUI.contentColor = Color.white;
            if (rootbtn(nowEdit = "Materials_" + name))
            {
                var mateProp = ctrl.mateProp;

                if (GUI.Button(RectU.sclRect(LX + EDTW - 26, (posYs), 54, 20), "RESET", gsButton))
                {
                    mateProp.ResetMateAll();
                    //mateProp.GetMateAll();
                }
                posYs += 5;
                SaveMaterials obj = null;
                if (is_preset_save(nowEdit))
                    obj = new SaveMaterials(mateProp);
                if (presetctrl(ref obj, nowEdit))
                {
                    obj.Load(ref mateProp);
                }
                posYs += 5;

                using (new UiUtil.GuiColor(colorDemiCyan))
                    labelctrl("カスタムマテリアル", 0, 0);

                if (ctrl.ini.isGirl())
                {
                    mateProp.mSYNC_CHEEK_BLEND = gettoggle("赤面値(_CheekAlpha)は表情制御に連動", mateProp.mSYNC_CHEEK_BLEND);
                    posYs += 5;
                }
                posYs += 5;
                cfg.MatePropWriteFreq = getintctrl("書換え頻度(※共通)", cfg.MatePropWriteFreq, 10, 1, 10);
                //posYs += 5;

                if (ctrl.FindModel())
                {
                    posYs += 5;
                    if (GUI.Button(RectU.sclRect(LX + 10, (posYs += 20), SLDW - 30, 20), "マテリアル再取得", gsButton))
                    {
                        mateProp.GetMateAll();
#if DEBUG
                        mateProp.logMat();
#endif
                    }
                    else
                    {
                        mateProp.ReadMateAll();
                    }

                    if (mateProp.dicMate.Count > 0)
                    {
                        if (GUI.Button(RectU.sclRect(LX + 10, (posYs += 20), SLDW - 30, 20), "マテリアル再設定", gsButton))
                        {
                            mateProp.OnNewScene();
                            //mateProp.WriteMateAll();
                        }

                        string cate = "";
                        foreach (var v in mateProp.dicMate)
                        {
                            var s = Path.GetDirectoryName(v.Key);
                            var sa = s.Split('/');
                            if (sa.Length > 2)
                                s = $"{sa[0]}/{sa[1]}";
                            //if (sa.Length > 3)
                            //    s = $"{sa[0]}/{sa[1]}…{sa[sa.Length-1]}";
                            if (cate != s)
                            {
                                cate = s;
                                posYs += 10;
                                labelctrl(cate);
                            }
                            _matectrl(v.Value, v.Key);
                            posYs += 10;
                        }

                    }
                    posYs += 5;

                    GUI.color = Color.yellow;
                    labelctrl("※着せ替えを行う際はRESET推奨", 5, 0);
                    labelctrl("（設定済みの物は着せ替えでの色変え不可）", 5, 0);
                    GUI.color = Color.white;

                    posYs += 5;
                }
            }

            posYs += 10;
        }

        static Color _copyColor;

        void _matectrl(MateProp.MateData md, string key)
        {
            if (md.write)
                GUI.color = Color.yellow;

            md.write = gettoggle(md.name.Replace("Instance", "I"), md.write, 0,0, - 80);

            if (md.write)
            {
                if (GUI.Button(RectU.sclRect(LX + SLDWHF + 80, (posYs), 60, 20), "同名適用", gsButton))
                {
                    foreach (var v in md.getParent().dicMate)
                    {
                        var tgt = v.Value;
                        if (tgt == md)
                            continue;

                        if (tgt.name.Replace(" (Custom)", "") == md.name.Replace(" (Custom)", "")
                            && tgt.shaderName == md.shaderName
                            && tgt.defData != null)
                        {
                            tgt.write = true;
                            tgt.cProps = Clone.Data(md.cProps);
                            tgt.fProps = Clone.Data(md.fProps);
                            tgt.OnUpdate();
                        }
                    }
                    return;
                }

                GUI.color = Color.yellow;
                posYs += 5;

                labelctrl("数値プロパティ", 25);
                if (rootbtn(key + "+float", 5))
                {
                    posYs += 5;
                    for (int i = 0; i < md.fProps.Count; i++)
                    {
                        posYs += 5;
                        var p = md.fProps[i];
                        var val = getfloatctrl_test(p.Key, p.Value, md.getFProps(p.Key, true), -1f, 1f);
                        if (val != p.Value)
                        {
                            md.fProps[i].Value = val;
                            //MateProp.WriteMateAll();
                            md.OnUpdate();
                        }
                    }
                }

                posYs += 10;
                labelctrl("色プロパティ", 25);
                if (rootbtn(key + "+color", 5))
                {
                    posYs += 5;
                    for (int i = 0; i < md.cProps.Count; i++)
                    {
                        posYs += 5;
                        var posYcopy = posYs + ItemHeight - 5;

                        var p = md.cProps[i];
                        var val = colorctrl(p.Key, p.Value, md.getCProps(p.Key, true), -1f, 2f);

                        if (GUI.Button(RectU.sclRect(LX + SLDWHF, (posYcopy), 60, 20), "Copy", gsButton))
                        {
                            _copyColor = val;
                        }
                        if (!_copyColor.Equals(default(Color)))
                            if (GUI.Button(RectU.sclRect(LX + SLDWHF + 60, (posYcopy), 60, 20), "Paste", gsButton))
                            {
                                val = _copyColor;
                            }

                        if (val != p.Value)
                        {
                            md.cProps[i].Value = val;
                            //MateProp.WriteMateAll();
                            md.OnUpdate();
                        }
                    }
                }

                GUI.color = Color.white;
            }
            else if (GUI.color != Color.white)
            {
                // 戻す
                //md.defData.WriteMate(true);
                md.Restore2SharedMesh(); 
                md.ForceRestore();
                GUI.color = Color.white;
            }
        }
#endregion


#region クローン

        void cloneCtrl(string name, GirlCtrl ctrl)
        {
            //GUI.contentColor = Color.cyan;
            //GUI.Label(RectU.sclRect(ItemX + 20, (posYs += 20), LBL_WD, 20), "クローン (※TEMP)", gsLabel);
            //GUI.contentColor = Color.white;

            if (rootbtn(nowEdit = "CloneCtrl_" + name))
            {
                CloneCtrlMgr ctrlMgr;
                if (!CloneCtrlMgr.dicCloneMgr.TryGetValue(name, out ctrlMgr))
                    ctrlMgr = CloneCtrlMgr.dicCloneMgr[name] = new CloneCtrlMgr();

                if (GUI.Button(RectU.sclRect(LX + EDTW - 26, (posYs), 54, 20), "RESET", gsButton))
                {
                    ctrlMgr.Clear();
                }
                posYs += 5;
                CloneCtrlMgr.SaveClone obj = null;
                if (is_preset_save(nowEdit))
                    obj = new CloneCtrlMgr.SaveClone(ctrlMgr);
                if (presetctrl(ref obj, nowEdit))
                {
                    obj.Load(ctrlMgr);
                }
                posYs += 5;

                if (GUI.Button(RectU.sclRect(LX + 10, (posYs += 20), SLDW - 30, 20), "クローン追加", gsButton))
                {
                    var newclone = ctrlMgr.Add(ctrl);
                    ScenePos.gizmoTgt = newclone.getScnPosRoot();
                }
                posYs += 5;

                CloneCtrl.AnimCopy_ = gettoggle("アニメ状態でコピー", CloneCtrl.AnimCopy_);
                if (CloneCtrl.AnimCopy_)
                    CloneCtrl.AnimSync_ = gettoggle("アニメを同期", CloneCtrl.AnimSync_);
                else
                    CloneCtrl.LookAtCopy_ = gettoggle("視点追従をコピー", CloneCtrl.LookAtCopy_);
                posYs += 5;

                if (ctrlMgr.dicClone.Count > 0)
                {
                    posYs += 5;
                    GUI.color = Color.yellow;
                    labelctrl("クローン管理");
                    GUI.color = Color.white;
                    const int SUBBTNW = (SLDW - 20 - 15) / 4;

                    int index = -1;
                    foreach (var c in ctrlMgr.dicClone.ToArray())
                    {
                        index++;

                        if (c.isNull())
                        {
                            c.Destroy();
                            continue;
                        }

                        var act = c.isActive();
                        //if (act != gettoggle("", act, 5, 5))
                        if (act != GUI.Toggle(RectU.sclRect(LX, (posYs += (20 + 5)), 20, 20), act, "", gsToggle))
                        {
                            c.setActive(!act);
                        }

                        using(new UiUtil.GuiEnable(index > 0))
                            if (GUI.Button(RectU.sclRect(LX + 25, (posYs), SUBBTNW - 5, 20), "UP↑", gsButton))
                            {
                                var tmp = ctrlMgr.dicClone[index];
                                ctrlMgr.dicClone[index] = ctrlMgr.dicClone[index - 1];
                                ctrlMgr.dicClone[index - 1] = tmp;
                            }
                        if (GUI.Button(RectU.sclRect(LX + 25 + SUBBTNW, (posYs), SUBBTNW - 5, 20), "更新", gsButton))
                        {
                            c.UpdateCloneInst();
                        }
                        // 今回は置換なし
                        //if (GUI.Button(RectU.sclRect(LX + 25 + SUBBTNW, (posYs), SUBBTNW - 5, 20), "置換", gsButton))
                        //{
                        //    c.Replace(ctrl);
                        //}
                        if (GUI.Button(RectU.sclRect(LX + 25 + SUBBTNW * 2, (posYs), SUBBTNW - 5, 20), "削除", gsButton))
                        {
                            c.Destroy();
                        }

                        if (!c.animCopy)
                        {
                            if (!c.lookAtCopy)
                                labelctrl("[Pause]", 5 + 25 + SUBBTNW * 3, -20);
                            else
                                labelctrl("[PLook]", 5 + 25 + SUBBTNW * 3, -20);
                        }
                        else if (c.animSync)
                            labelctrl("[ASync]", 5 + 25 + SUBBTNW * 3, -20);
                        else if (c.animCopy)
                            labelctrl("[Anime]", 5 + 25 + SUBBTNW * 3, -20);

                        var go = c.getScnPosRoot();
                        if (go == ScenePos.gizmoTgt)
                        {
                            GUI.color = Color.yellow;
                        }
                        if (GUI.Button(RectU.sclRect(5 + 25 + SUBBTNW * 4, (posYs), 20, 20), "G", gsButton))
                        {
                            if (go == ScenePos.gizmoTgt)
                            {
                                ScenePos.gizmoTgt = null;
                            }
                            else if (go && go.activeSelf)
                            {
                                ScenePos.gizmoTgt = go;
                            }
                        }
                        GUI.color = Color.white;

                        posYs += 5;
                    }
                    posYs += 5;

                    CloneCtrl.UpdateShape = gettoggle("更新時、シェイプキーも含む", CloneCtrl.UpdateShape);
                    CloneCtrl.UpdateEdits = gettoggle("更新時、体形等も含む", CloneCtrl.UpdateEdits);
                    CloneCtrl.UpdateAnimState = gettoggle("更新時、アニメ・追従状態を変更", CloneCtrl.UpdateAnimState);

                    posYs += 10;
                    labelctrl("※更新: ｴﾃﾞｨｯﾄ情報反映(体形/ｼｪｲﾌﾟ/衣服等)");
                    //labelctrl("※置換: Anime系とではｼｰﾝ移動までｹﾞｰﾑ進行に");
                    //labelctrl("　　　支障が出ることがあるので注意");
                    //labelctrl("※置換: Pauseなら視点追従OFF/ﾎﾟｰｽﾞｷｬﾌﾟﾁｬ有");
                    //labelctrl("　　　尚、ｼｰﾝ位置設定は必ずONになります");
                    posYs += 5;
                }
                posYs += 5;
            }
            posYs += 10;
        }
#endregion

#region 共通コントロールセット
        void labelctrl(string str, ref Rect rect)
        {
            GUI.Label(RectU.sclRect(rect.x, (rect.y), rect.width, rect.height), str, gsLabel);
            rect.y += rect.height;
        }

        bool isPresetSave(string name)
        {
            return selectPresetMenu == "SAVE-" + name;
        }

        bool isPresetLoad(string name)
        {
            return selectPresetMenu == "LOAD-" + name;
        }

        bool isPresetOpen(string name)
        {
            return selectPresetMenu.EndsWith(name, StringComparison.Ordinal);
        }

        void openPreset(string name, bool save)
        {
            if (save)
                selectPresetMenu = "SAVE-" + name;
            else
                selectPresetMenu = "LOAD-" + name;

            presetTooltip = "";
            presetTooltipTemp = "";
            xmlNote.ClearNoteCache();
        }

        // ロードしたときだけtrueを返す
        bool presetCtrl<T>(ref T obj, string label, string name, ref Rect rect, int slotnum, string[] labelkv = null)
        {
            bool isLoaded = false;

            //labelctrl($"プリセット ({name})", ref rect);

            int LX = (int)rect.x;
            int ItemHeight = (int)rect.height;
            int posYs = (int)rect.y - ItemHeight;
            int NBW = ((int)rect.width - 10) / 6;
            int SLDW = (int)rect.width;

            if (selectPresetMenu.EndsWith(name, StringComparison.Ordinal))
            {
                // オープン時
                if (selectPresetMenu.StartsWith("SAVE", StringComparison.Ordinal))
                    GUI.color = Color.yellow;
                else
                    GUI.color = Color.green;
                labelctrl(label + "選択: " + selectPresetMenu, ref rect);
                posYs = (int)rect.y;

                if (labelkv == null)
                    labelkv = getNumstrArray(slotnum);

                string gui_tooltip_old = GUI.tooltip;
                try
                {
                    GUI.tooltip = "";
                    for (int i = 0; i <= slotnum; i++)
                    {
                        if (i == 0)
                        {
                            if (GUI.Button(RectU.sclRect(LX + 10 + NBW * i, (posYs), NBW, ItemHeight), "<戻", gsButton))
                            {
                                selectPresetMenu = "";
                                break;
                            }
                            continue;
                        }
                        else
                        {
                            string slotname = labelkv[i - 1];
                            int dx = i;
                            if (i >= 6)
                            {
                                dx = (i - 1) % 5 + 1;
                                if (dx == 1)
                                    posYs += ItemHeight;
                            }

                            xmlNote xnote = null;

                            var fname = $"{SaveFileName}-{name}-{i}.xml";

                            Color cbk = GUI.color;
                            if (!XML_Exists(fname))
                            {
                                if (selectPresetMenu.StartsWith("SAVE", StringComparison.Ordinal))
                                    GUI.color = Color.white;
                                else
                                    GUI.color = Color.gray;
                            }
                            else
                            {
                                xnote = xmlNote.ReadNote(fname);
                            }

                            if (xnote == null)
                            {
                                xnote = new xmlNote();
                                xnote.note = "\r";
                            }

                            if (GUI.Button(RectU.sclRect(LX + 10 + NBW * dx, (posYs), NBW, ItemHeight), new GUIContent(slotname, xnote.note), gsButton))
                            {
                                if (selectPresetMenu.StartsWith("SAVE", StringComparison.Ordinal))
                                {
                                    XML_Save<T>(obj, fname);

                                    if (!string.IsNullOrEmpty(presetTooltip))
                                    {
                                        xnote.note = presetTooltip;
                                        xmlNote.SaveNote(xnote, fname);
                                    }
                                }
                                else
                                {
                                    if (XML_Load<T>(out obj, fname))
                                        isLoaded = true;
                                }
                                selectPresetMenu = "";
                            }

                            GUI.color = cbk;
                        }
                    }
                    // tooltip試作
                    if (!string.IsNullOrEmpty(selectPresetMenu))
                    {
                        bool issave = selectPresetMenu.StartsWith("SAVE", StringComparison.Ordinal);
                        Color cbk = GUI.color;

                        GUI.Label(RectU.sclRect(LX + 10, (posYs += 20), NBW * 1.5f, ItemHeight),
                            new GUIContent("Memo:", issave ? "説明を記入できます\n" : "説明を表示/無ければ更新日"), gsTextField);

                        string note = presetTooltip;
                        if (!issave || string.IsNullOrEmpty(note))
                        {
                            note = GUI.tooltip;
                            if (issave)
                            {
                                GUI.color = Color.white;

                                // 更新日以外は再利用可能にする
                                if (note != presetTooltipTemp && !string.IsNullOrEmpty(note))
                                {
                                    if (note == "\r")
                                        note = "";
                                    presetTooltipTemp = note;
                                }
                                else if (!presetTooltipTemp.EndsWith("\n", StringComparison.Ordinal))
                                {
                                    note = presetTooltipTemp;
                                }
                            }

                            if (note.EndsWith("\n", StringComparison.Ordinal))
                                note = note.Replace("\r", "").Replace("\n", "");
                        }

                        string str = GUI.TextField(RectU.sclRect(LX + 10 + NBW * 1.5f, (posYs), NBW * 4.5f, ItemHeight), note, gsTextField);
                        if (issave && (note != str))
                            presetTooltip = str;

                        if (!presetTooltipTemp.EndsWith("\n", StringComparison.Ordinal))
                            presetTooltipTemp = str;

                        GUI.color = cbk;
                    }
                }
                finally
                {
                    GUI.tooltip = gui_tooltip_old;
                    GUI.color = Color.white;
                }
            }
            //else
            //{
            //    // クローズ時
            //    int SBW = SLDW / 2 - 20;
            //    if (GUI.Button(RectU.sclRect(LX + 10, (posYs += 20), SBW, 20), "SAVE選択", gsButton))
            //    {
            //        selectPresetMenu = "SAVE-" + name;

            //        presetTooltip = "";
            //        presetTooltipTemp = "";
            //        xmlNote.ClearNoteCache();
            //    }
            //    if (GUI.Button(RectU.sclRect(LX + SBW + 20, (posYs), SBW, 20), "LOAD選択", gsButton))
            //    {
            //        selectPresetMenu = "LOAD-" + name;

            //        presetTooltip = "";
            //        presetTooltipTemp = "";
            //        xmlNote.ClearNoteCache();
            //    }
            //}

            rect.y = posYs;

            return isLoaded;
        }



        float btnset_LR(Rect rc, int bw, float f, GUIStyle gsButton)
        {
            float fd = 0.001f;
            f = btnset_LR(rc, bw, f, fd, gsButton);

            return f;
        }
        float btnset_LR(Rect rc, int bw, float f, float fd, GUIStyle gsButton)
        {
            if (InputEx.CheckModifierKeys(InputEx.ModifierKey.Shift))
                fd *= 10f;

            if (InputEx.CheckModifierKeys(InputEx.ModifierKey.Control))
                fd *= 100f;

            if (InputEx.CheckModifierKeys(InputEx.ModifierKey.Alt))
                fd *= 10f;

            if (GUI.Button(rc, "<", gsButton))
            {
                f -= fd;
            }
            rc.x += bw;
            if (GUI.Button(rc, ">", gsButton))
            {
                f += fd;
            }

            return f;
        }
#endregion

        static string[] getNumstrArray(int count, int start = 1)
        {
            var a = new string[count];
            for (int i = 0; i < count; i++)
                a[i] = (i + start).ToString();
            return a;
        }


        static string[] getABCstrArray(int count, int start = 0)
        {
            var a = new string[count];
            char c = 'a';

            for (int i = 0; i < count; i++)
                a[i] = ((char)((int)c + i + start)).ToString();
            return a;
        }

    }


#region コンボボックス
    //超簡単コンボボックス、範囲外を選択されたときに消すなんて機能もできた
    class EasyCombo
    {
        const int ItemHeight = 20;
        //static EasyCombo openCombo = null;

        public Vector2 scrollPosition = Vector2.zero;
#if single
        private bool boPop_ = false;
        public bool boPop
        {
            get { return boPop_; }
            set
            {
                if (value)
                {   //一つに抑制する
                    if (openCombo != null)
                        openCombo.boPop = false;
                    openCombo = this;
                }
                boPop_ = value;
            }
        }
#else
        private HashSet<string> boPopOn_ = new HashSet<string>();
        public bool boPop(string uid)
        {
            return boPopOn_.Contains(uid);
        }

        public bool lastPop()
        {
            return boPopOn_.Contains(prevId);
        }

        public void setPop(string uid, bool set)
        {
            if (set)
            {
                boPopOn_.Clear(); //複数同時に開くのを許可しない
                boPopOn_.Add(uid);
            }
            else
            {
                boPopOn_.Remove(uid);
                boPopOn_.Clear(); //複数同時に開くのを許可しない
            }
        }

        public void Close()
        {
            boPopOn_.Clear();
        }

        public string defSelected = String.Empty;
        public int defIndex { get; set; }
#endif

        public string sSelected = String.Empty;
        public int sIndex { get; set; }
        public bool boChanged { get; private set; }

        //アウトカーソルクリッククローズ用
        private Vector2 scrollPosition_bk = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        private int closeCntdwn = 0;    //マウスのボタンUPの次フレームあたりにボタンイベントが起きるようなので

        /// <summary>
        ///     簡単コンボボックス
        /// </summary>
        /// <param name="s">初期表示文字列</param>
        /// <param name="i">初期選択インデックス</param>
        public EasyCombo(string s, int i = -1)
        {
            defSelected = sSelected = s;
            defIndex = sIndex = i;
        }

        /// <summary>
        ///     OnGUIで呼ぶこと
        /// </summary>
        /// <typeparam name="T">Enum型のタイプ</typeparam>
        /// <param name="uid">コントロール別ID</param>
        /// <param name="rect"></param>
        /// <param name="strSelected">選択中</param>
        /// <param name="gsBtn"></param>
        /// <param name="gsLst"></param>
        /// <param name="numLines">リスト行数</param>
        /// <param name="itemH">リストアイテム高さ</param>
        /// <param name="disables">無効にしたいアイテムインデックス配列</param>
        /// <returns>変更があればTrue</returns>
        public bool Show<T>(string uid, Rect rect, string strSelected, Dictionary<T, string> dicToDisp, GUIStyle gsBtn, GUIStyle gsLst, int numLines = 5, int itemH = ItemHeight, bool[] disables = null)
        {
            string[] e_names;

            if (dicToDisp == null)
                e_names = Enum.GetNames(typeof(T));
            else
            {
                e_names = dicToDisp.Select(x => x.Value).ToArray();
            }

            return Show(uid, rect, itemH, itemH * numLines, e_names, strSelected, gsBtn, gsLst, disables);
        }

        public bool Show(string uid, Rect rect_btn, int itemH, int maxH, string[] slist, string sBtn, GUIStyle gsBtn, GUIStyle gsLst, bool[] disables = null)
        {
            Rect rect_list = new Rect(rect_btn);
            rect_list.y += rect_btn.height;
            rect_list.height = maxH;

            return Show(uid, rect_btn, itemH, rect_list, slist, sBtn, gsBtn, gsLst, disables);
        }

        string prevId = "";

        public bool Show(string uid, Rect rect_btn, int itemH, Rect rect_list, string[] slist, string sBtn, GUIStyle gsBtn, GUIStyle gsLst, bool[] disables = null)
        {
            int maxlen = 0;

            if (prevId != uid)
            {
                sSelected = defSelected;
                sIndex = defIndex;
                prevId = uid;
            }

            boChanged = false;

            Color cbk = GUI.color;
            if (boPop(uid))
                GUI.color = Color.cyan;

            string sText = sBtn;
            if (sText == null)
                sText = sSelected;
            else
                sSelected = sText;

            if (GUI.Button(RectU.sclRect(rect_btn), sText, gsBtn))
            {
                setPop(uid, !boPop(uid));
            }
            GUI.color = cbk;

            if (boPop(uid))
            {
                rect_btn = rect_list;

                foreach (string s in slist)
                {
                    /*if (maxlen < s.Length)
                        maxlen = s.Length;*/
                    int len = (int)gsLst.CalcSize(new GUIContent(s)).x;

                    if (maxlen < len)
                        maxlen = len;
                }
                int iw = /*(gsLst.fontSize+2) * */maxlen;
                if (iw < (rect_btn.width - 16))
                    iw = (int)rect_btn.width - 16;//スクロールバー分幅を引く

                GUI.Box(RectU.sclRect(rect_btn.x, rect_btn.y, rect_btn.width - 15, rect_btn.height), "");
                scrollPosition = GUI.BeginScrollView(RectU.sclRect(rect_btn), scrollPosition, RectU.sclRect(0, 0, iw, itemH * slist.Length), false, true);

                try
                {
                    int pos_y = 0;
                    int i = 0;
                    foreach (string s in slist)
                    {
                        if (disables != null && disables[i])
                            GUI.enabled = false;

                        if (GUI.Button(RectU.sclRect(0, pos_y, iw, itemH), s, gsLst))
                        {
                            if (sSelected != s)
                                boChanged = true;

                            sSelected = s;
                            sIndex = i;
                            setPop(uid, false);
                        }
                        GUI.enabled = true;

                        i++;
                        pos_y += itemH;
                    }
                }
                finally
                {
                    GUI.enabled = true;
                    GUI.EndScrollView();
                }

                if (boPop(uid) && !boChanged)
                {
                    if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonUp(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonUp(1))
                    {
                        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
                            scrollPosition_bk = scrollPosition;
                        if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1))
                        {
                            if (scrollPosition_bk == scrollPosition)
                            {
                                //スクロールドラッグなし
                                //マウスのボタンUPの次フレームあたりにボタンイベントが起きるようなので
                                closeCntdwn = 3;
                            }
                        }
                    }
                    else if (closeCntdwn > 0)
                    {
                        closeCntdwn--;
                        if (closeCntdwn == 0)
                            setPop(uid, false);
                    }
                }
            }
            else if (boPopOn_.Count == 0)
            {
                if (scrollPosition_bk == scrollPosition)    //開くときのクリックで閉じないように
                    scrollPosition_bk = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                if (closeCntdwn > 0)
                    closeCntdwn = 0;
            }

            return boChanged;
        }

    }
#endregion
}

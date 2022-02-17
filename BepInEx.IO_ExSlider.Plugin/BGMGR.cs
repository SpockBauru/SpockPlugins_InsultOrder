using System.Linq;
using UnityEngine;
using nnPlugin;
using static BepInEx.IO_ExSlider.Plugin.IO_ExSlider;
using System.Reflection;
using System.Collections;
using OhMyGizmo2;
using System.Collections.Generic;
using System;

namespace BepInEx.IO_ExSlider.Plugin
{
    public static class BGMGR
    {
        static FieldInfo fiBG;

        public static OhMyGizmo gizmoPosSubobj;
        public static ScenePos scnPosMainBG = new ScenePos();
        public static GameObject mainBG;
        public static PositionByStates posByStates = new PositionByStates();

        public static BGItems bgItems = new BGItems();

        [Serializable]
        public class BGItems
        {
            public List<BGItemTransform> listBgTrs = new List<BGItemTransform>();
            public BGItems() { }

            public void ResetAll()
            {
                listBgTrs.ForEach(x => x.Reset());
            }

            public void LoadAll(GameObject root)
            {
                listBgTrs.ForEach(x => x.Load(root));
            }
        }

        [Serializable]
        public class BGItemTransform
        {
            public string name = string.Empty;
            public ScenePos trs = new ScenePos();
            GameObject _obj;

            public bool IsLoaded() { return (bool) _obj; }

            public BGItemTransform() { }

            public BGItemTransform(GameObject gameObj, bool gizmo) 
            {
                _obj = gameObj;
                this.name = gameObj.name;
                trs = new ScenePos(gameObj, false);

                if (gizmo)
                    OnGizmo(gameObj);
            }

            public void Save(GameObject obj)
            {
                if (obj.name == name)
                    _obj = obj;

                if (_obj)
                    trs.SaveTrs(_obj.transform);
            }
            public void Save()
            {
                if (_obj)
                    trs.SaveTrs(_obj.transform);
            }

            public void Load(GameObject root)
            {
                if (root.name == name)
                    _obj = root;
                else
                    _obj = root.FindDeep(this.name);

                if (_obj)
                    trs.WritePosRot(_obj, false);
            }

            public void Reset(GameObject root)
            {
                if (root.name == name)
                    _obj = root;
                else
                    _obj = root.FindDeep(this.name);

                Reset();
            }

            public void Reset()
            {
                OffGizmo();

                if (_obj)
                    trs.ResetPosRot(_obj.transform);
            }

            internal void OffGizmo()
            {
                if (_obj)
                    gizmoPosSubobj.tryOffTarget(_obj.transform);
            }

            internal void OnGizmo(GameObject go)
            {
                gizmoPosSubobj.setTarget(go.transform, name);
            }

            internal bool CheckGizmo()
            {
                return (_obj && gizmoPosSubobj.target == _obj.transform);
            }
        }

        public static void GizmoInit()
        {
            if (!gizmoPosSubobj)
            {
                gizmoPosSubobj = OhMyGizmo.AddGizmo(null, "gizmoPosSubobj");
                gizmoPosSubobj.modePos = true;
                gizmoPosSubobj.modeRot = true;
                gizmoPosSubobj.sizeHandle = 2f * 6;
                gizmoPosSubobj.threthold = 30;
                gizmoPosSubobj.procTargetCtrl = true;
            }

            gizmoPosSubobj.visible = false;
            gizmoPosSubobj.resetTarget();
        }

        public readonly static string[] BG_Names =
        {
            "BG01", "BG03", "BG04", "BG05", "BG08", "BG09", "BG10",
        };
        public readonly static string[] BG_NamesJpn =
        {
            "部屋", "道路", "路地", "牢獄", "玄関", "フロア", "便所",
        };

        public static string GetNameBG()
        {
            if (!mainBG)
                GetBG();

            if (mainBG)
                return mainBG.name;
            else
                return null;
        }
        public static string GetJpNameByNameBG(string bg)
        {
            if (string.IsNullOrEmpty(bg))
                return bg;

            for (int i=0; i<BG_Names.Length; i++)
            {
                if (BG_Names[i] == bg)
                    return BG_NamesJpn[i];
            }
            return bg;
        }

        public static void GetBG()
        {
            // 通常
            var gowin = Extentions.FindSp("UI Root(UI)/Wind_Conf");
            if (gowin)
            {
                var config = gowin.GetComponent<ConfigSetting>();

                if (fiBG == null)
                    fiBG = config.GetNonPublicFieldInfo("MainBG");
                mainBG = fiBG.GetValue(config) as GameObject;
                //var goBg = mainBg = config.GetNonPublicField<GameObject>("MainBG");
            }

            // 特例
            if (!mainBG)
            {
                // ADVはmainBGで取れないぽい
                if (actScene.name == "ADV")
                {
                    var ms = Extentions.FindSp("MainSystem");
                    if (ms)
                    {
                        var adv = ms.GetComponent<ADV_Loader>();
                        if (adv)
                        {
                            if (adv.BGround3D == false)
                                return;
                        }
                    }
                }

                for (int i = 0; i < BG_Names.Length; i++)
                {
                    var bg = Extentions.FindSp(BG_Names[i]);
                    if (bg)
                    {
                        mainBG = bg;
                        break;
                    }
                }
                return;
            }
        }

        public static void BGscnPosUpdate()
        {
            if (scnPosMainBG.enable)
            {
                if (!mainBG)
                    GetBG();

                if (mainBG)
                {
                    scnPosMainBG.WritePosRot(mainBG);

                    BGCalendarOffset(scnPosMainBG.pos - scnPosMainBG.orgPos);
                }
            }
        }

        public static void BGCalendarOffset(Vector3 offset)
        {
            if (mainBG.name == "BG01")
            {
                // カレンダー位置合わせ、角度は色々面倒になるので見送り（使う人がそんなに居るとも思えないし非表示にできるようにもしたので）
                if (actScene.name == "FH")
                {
                    var go = Extentions.FindSp("UI Root(screen)");
                    if (go)
                        go.transform.localPosition = new Vector3(5.956215E-07f, 5.956215E-07f, 22.24676f) + offset;
                }
                else if (actScene.name == "IC")
                {
                    var go = Extentions.FindSp("UI Root(screen)");
                    if (go)
                        go.transform.localPosition = new Vector3(6.17f, 6.071f, 8.821f) + offset;
                }
            }
        }

        public static void OnNewScene(MonoBehaviour inst, string scnName)
        {
            GizmoInit();

            // 卓上カレンダー位置修正
            if (cfg.FixTableCalendarPos)
            {
                inst.StartCoroutine(BGMGR.FixCalendarPos(scnName));
            }

            // クリア
            mainBG = null;
            scnPosMainBG.enable = false;

            // シーン別背景位置
            if (posByStates.enabled)
            {
                var stateNow = scnName + SaveLoad_Game.Data.savegamedata.SceneID;
                var set = posByStates.list.FirstOrDefault(x => x.stateName == stateNow);
                if (set != null)
                {
                    Debug.Log($"BG: {stateNow}用の位置データを読み込み");
                    set.trs.LoadTrs(scnPosMainBG);
                    scnPosMainBG.enable = true;
                }

            }
        }

        public static IEnumerator FixCalendarPos(string scnName)
        {
            if (scnName != "IC")
                yield break;

            var ui = GameObject.Find("UI Root(Menu)");
            if (!ui || !ui.transform.Find("Zengi"))
                yield break;

            var btnZengi = ui.transform.Find("Zengi"); // ボタン
            while (btnZengi && btnZengi.gameObject.activeSelf) // 非アクティブまで待つ
            {
                yield return new WaitForSeconds(0.5f);
            }
            if (!ui)
                yield break; // カスタム画面などに移動した場合

            var go = GameObject.Find("UI Root(screen)");
            if (!go)
                yield break;

            var bpos = go.GetComponent<BgPositon>();
            if (bpos && bpos.enabled)
            {
                // 修正が入るかもしれないので位置もチェック
                var label = GameObject.Find("UI Root(screen)/Label");
                if (label && label.transform.localPosition.x == 0)
                {
                    bpos.enabled = false; // シーン読み込み時は無効になってるので途中設定が必要(Zengiボタン押されたときにONになる)
                    go.transform.localPosition = new Vector3(6.17f, 6.071f, 8.821f);
                    go.transform.localEulerAngles = new Vector3(0.5f, -90f, -4.076924E-05f);
                }
            }
            yield break;
        }

    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using B83.Win32;
using System.Reflection;
using static BepInEx.IO_ExSlider.Plugin.IO_ExSlider;

namespace BepInEx.IO_ExSlider.Plugin
{
    public class CustomDragAndDrop : MonoBehaviour
    {
        List<string> mFiles;
        static FieldInfo _fiSlotNo = typeof(CustomSetting).GetField("SlotNo", BindingFlags.NonPublic | BindingFlags.Instance);
        static FieldInfo _fiImgFile = typeof(CustomSetting).GetField("imgFile", BindingFlags.NonPublic | BindingFlags.Instance); 
        static FieldInfo _fiCostumeID = typeof(CustomSetting).GetField("CostumeID", BindingFlags.NonPublic | BindingFlags.Instance);
        static CustomSetting customSetting;
        static CustomDragAndDrop _Instance;

        public static CustomDragAndDrop Start()
        {
            if (_Instance)
                return _Instance;

            return _Instance = new GameObject("PlugIn-CustomDragAndDrop").AddComponent<CustomDragAndDrop>();
        }

        private void Awake()
        {
            customSetting = GameObject.Find("UI Root (Custom)").GetComponent<CustomSetting>();
        }

        void OnEnable()
        {
            UnityDragAndDropHook.InstallHook();
            UnityDragAndDropHook.OnDroppedFiles += OnFiles;
        }
        void OnDisable()
        {
            UnityDragAndDropHook.UninstallHook();
        }

        void OnFiles(List<string> aFiles, POINT aPos)
        {
            Debug.Log("D&D count:" + aFiles.Count + " at:" + aPos + "\n files:" + string.Join(",", aFiles.ToArray()));
            mFiles = aFiles;
        }

        private void OnRenderObject()
        {
            if (mFiles != null && mFiles.Count == 1)
            {
                Debug.Log("カスタムデータのD&Dを検出 " + mFiles[0]);

                if (mFiles[0].EndsWith("png", StringComparison.OrdinalIgnoreCase) // System.IO.Path.GetExtension(mFiles[0]).ToLower() == "png"
                     && GameObject.Find("UI Root (Custom)/Check/CheckBox").GetComponent<UISprite>().alpha == 0)
                {
                    string cid = _fiCostumeID.GetValue(customSetting) as string;
                    if (!string.IsNullOrEmpty(cid))
                    {
                        // ゲーム本体側カスタムロード
                        _fiSlotNo.SetValue(customSetting, -1);
                        _fiImgFile.SetValue(customSetting, mFiles[0]);
                        customSetting.StartCoroutine("Load");

                        // 拡張プリセットロード
                        int cindex = 0; // neko
                        if (cid == "02")
                            cindex = 1; // usa

                        //var path = mFiles[0].Substring(0, mFiles[0].Length - ".png".Length);
                        //path += ".ExPreset.xml";
                        //if (MyXML.XML_Load(out IO_ExSlider.SaveGirlExPresets preset, path, true))
                        //{
                        //    preset.Load(IO_ExSlider.CtrlGirlsInst[cindex]);
                        //}

                        if (SaveDataUtil.ReadExPresetPNG(mFiles[0], out IO_ExSlider.SaveGirlExPresets preset, true))
                        {
                            preset.Load(IO_ExSlider.CtrlGirlsInst[cindex]);
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("セーブやロード処理中のためスキップ");
                }
                mFiles = null;
            }
        }
    }
}

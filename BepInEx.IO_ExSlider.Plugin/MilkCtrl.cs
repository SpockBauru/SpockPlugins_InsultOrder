using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using nnPlugin;
using static BepInEx.IO_ExSlider.Plugin.IO_ExSlider;

namespace BepInEx.IO_ExSlider.Plugin
{
    public class MilkCtrl
    {
        public MilkData milkData;

        static string[] _milkFieldNames = {
                "seieki_CH01_face",
                "seieki_CH01_back",
                "seieki_CH01_belly",
                "seieki_CH01_breast",
                "seieki_CH01_hip",
                "seieki_CH01_leg",
                "seieki_CH01_breaIN",
                "seieki_CH01_footL",
                "seieki_CH01_footR",
                "seieki_CH01_Mouse",
                "seieki_CH01_HandL",
                "seieki_CH01_HandR",
                "seieki_CH01_In",
                "seieki_CH01_Anal",
            };

        GirlCtrl girlCtrl;
        public bool enabled = true;
        public bool autoFlush = false; 

        public MilkCtrl(GirlCtrl girlCtrl)
        {
            this.girlCtrl = girlCtrl;

            if (girlCtrl.ini.isNotGirl())
            {
                enabled = false;
                return;
            }

            this.milkData = new MilkData();

            using (var ie = getMilkNames(girlCtrl.ini.id))
            {
                while (ie.MoveNext())
                {
                    var s = ie.Current;
                    var fi = MilkDatum.GetFI(s);
                    if (fi != null)
                    {
                        this.milkData.milks.Add(new MilkDatum(fi));
                    }
                }
            }
        }

        Dictionary<string, int> prevData = new Dictionary<string, int>();

        public void OnLateUpdate()
        {
            bool flag = false;

            // 低速
            //if (autoFlush)
            //{
            //    foreach(var m in milkData.milks)
            //    {
            //        int val = m.Get();

            //        if (prevData.TryGetValue(m.fieldName, out int prev))
            //        {
            //            if (prev != val)
            //                flag = true;
            //        }
            //        prevData[m.fieldName] = val;

            //        //if (m.setCount >= 0 && m.setCount < val)
            //        //    m.setCount = val;
            //    }
            //}

            if (milkData == null || !enabled)
                return;

            WriteData(flag);
        }

        public void WriteData(bool write, bool saveData = false)
        {
            milkData.UpdateData();

            if (write)
            {
                // 重いのでなるべく呼ばない
                if (girlCtrl.ini.id == "Neko")
                {
                    if (autoFlush || saveData)
                    {
                        // セーブファイルまで更新しちゃうので没
                        //var bkup = GameClass.BackSceneID;
                        //GameClass.BackSceneID = "0001"; // Neko id
                        //GameClass.SeiekiClassSave();
                        //GameClass.BackSceneID = bkup;

                        var pc00 = Extentions.FindSp("PC00");
                        if (pc00)
                        {
                            SaveLoad_CharaData01.Data.savegamedata.LA50_01_CH01 = GameClass.seieki_CH01_face;
                            SaveLoad_CharaData01.Data.savegamedata.LA50_06_CH01 = GameClass.seieki_CH01_Mouse;
                            SaveLoad_CharaData01.Data.savegamedata.LA51_02_CH01 = GameClass.seieki_CH01_breast;
                            SaveLoad_CharaData01.Data.savegamedata.LA51_04_CH01 = GameClass.seieki_CH01_belly;
                            SaveLoad_CharaData01.Data.savegamedata.LA51_05_CH01 = GameClass.seieki_CH01_breaIN;
                            SaveLoad_CharaData01.Data.savegamedata.LA52_01_CH01 = GameClass.seieki_CH01_back;
                            SaveLoad_CharaData01.Data.savegamedata.LA52_03_CH01 = GameClass.seieki_CH01_hip;
                            //ゲーム側で保持してない// SaveLoad_CharaData01.Data.savegamedata.LA52_05_CH01 = GameClass.seieki_CH01_Anal;
                            SaveLoad_CharaData01.Data.savegamedata.LA52_07_CH01 = GameClass.seieki_CH01_leg;

                            LiquidCounter comp = pc00.GetComponent<LiquidCounter>();
                            comp.LA50_01_CH01 = SaveLoad_CharaData01.Data.savegamedata.LA50_01_CH01;
                            comp.LA50_06_CH01 = SaveLoad_CharaData01.Data.savegamedata.LA50_06_CH01;
                            comp.LA51_02_CH01 = SaveLoad_CharaData01.Data.savegamedata.LA51_02_CH01;
                            comp.LA51_04_CH01 = SaveLoad_CharaData01.Data.savegamedata.LA51_04_CH01;
                            comp.LA51_05_CH01 = SaveLoad_CharaData01.Data.savegamedata.LA51_05_CH01;
                            comp.LA52_01_CH01 = SaveLoad_CharaData01.Data.savegamedata.LA52_01_CH01;
                            comp.LA52_03_CH01 = SaveLoad_CharaData01.Data.savegamedata.LA52_03_CH01;
                            comp.LA52_07_CH01 = SaveLoad_CharaData01.Data.savegamedata.LA52_07_CH01;
                            comp.LA52_05_CH01 = GameClass.seieki_CH01_Anal;
                        }
                    }

                    var setup = girlCtrl.ini.rootObj.GetComponent<CostumeSetUp_CH01>();
                    setup.SeiekiActive();
                }
                else if (girlCtrl.ini.id == "Usa")
                {
                    if (autoFlush || saveData)
                    {
                        // セーブファイルまで更新しちゃうので没
                        //var bkup = GameClass.BackSceneID;
                        //GameClass.BackSceneID = "0002"; // Usa id
                        //GameClass.SeiekiClassSave();
                        //GameClass.BackSceneID = bkup;

                        var pc00 = Extentions.FindSp("PC00");
                        if (pc00)
                        {
                            SaveLoad_CharaData02.Data.savegamedata.LA50_01_CH02 = GameClass.seieki_CH02_face;
                            SaveLoad_CharaData02.Data.savegamedata.LA50_06_CH02 = GameClass.seieki_CH02_Mouse;
                            SaveLoad_CharaData02.Data.savegamedata.LA51_02_CH02 = GameClass.seieki_CH02_breast;
                            SaveLoad_CharaData02.Data.savegamedata.LA51_04_CH02 = GameClass.seieki_CH02_belly;
                            SaveLoad_CharaData02.Data.savegamedata.LA51_05_CH02 = GameClass.seieki_CH02_breaIN;
                            SaveLoad_CharaData02.Data.savegamedata.LA52_01_CH02 = GameClass.seieki_CH02_back;
                            SaveLoad_CharaData02.Data.savegamedata.LA52_03_CH02 = GameClass.seieki_CH02_hip;
                            //ゲーム側で保持してない// SaveLoad_CharaData02.Data.savegamedata.LA52_05_CH02 = GameClass.seieki_CH02_Anal;
                            SaveLoad_CharaData02.Data.savegamedata.LA52_07_CH02 = GameClass.seieki_CH02_leg;

                            LiquidCounter comp = pc00.GetComponent<LiquidCounter>();
                            comp.LA50_01_CH02 = SaveLoad_CharaData02.Data.savegamedata.LA50_01_CH02;
                            comp.LA50_06_CH02 = SaveLoad_CharaData02.Data.savegamedata.LA50_06_CH02;
                            comp.LA51_02_CH02 = SaveLoad_CharaData02.Data.savegamedata.LA51_02_CH02;
                            comp.LA51_04_CH02 = SaveLoad_CharaData02.Data.savegamedata.LA51_04_CH02;
                            comp.LA51_05_CH02 = SaveLoad_CharaData02.Data.savegamedata.LA51_05_CH02;
                            comp.LA52_01_CH02 = SaveLoad_CharaData02.Data.savegamedata.LA52_01_CH02;
                            comp.LA52_03_CH02 = SaveLoad_CharaData02.Data.savegamedata.LA52_03_CH02;
                            comp.LA52_07_CH02 = SaveLoad_CharaData02.Data.savegamedata.LA52_07_CH02;
                            comp.LA52_05_CH02 = GameClass.seieki_CH02_Anal;
                        }
                    }

                    var setup = girlCtrl.ini.rootObj.GetComponent<CostumeSetUp_CH02>();
                    setup.SeiekiActive();
                }
            }
        }

        public IEnumerator<string> getMilkNames(string id)
        {
            for (int i = 0; i < _milkFieldNames.Length; i++)
            {
                var s = _milkFieldNames[i];
                if (id == "Usa")
                {
                    s = s.Replace("CH01", "CH02");
                }
                yield return s;
            }
        }

        public IEnumerator<string> getMilkNames2(string id)
        {
            for (int i = 0; i <= 2; i++)
            {
                for (int j = 0; j <= 9; j++)
                {
                    string s = $"LA5{i}_0{j}_CH01";
                    if (id == "Usa")
                    {
                        s = s.Replace("CH01", "CH02");
                    }

                    yield return s;
                }
            }
        }
    }

    [Serializable]
    public class MilkData
    {
        public List<MilkDatum> milks = new List<MilkDatum>();

        public void UpdateData()
        {
            foreach(var d in milks)
            {
                d.OnUpdate();
            }
        }

        public void ResetAll()
        {
            foreach (var d in milks)
            {
                d.Set(0);
                d.Reset();
            }
        }
    }

    [Serializable]
    public class MilkDatum
    {
        const int _MaxCount = 4;
        static readonly Dictionary<string, int> _MaxClamps = new Dictionary<string, int>
                {
                    {"seieki_CH01_breaIN", 1 }
                };

        public string fieldName = string.Empty;
        public int setCount = -1;
        FieldInfo fieldInfo;

        public int Get()
        {
            setFI();
            return (int)fieldInfo.GetValue(null);
        }

        public void Set(int count)
        {
            setFI();
            if (_MaxClamps.ContainsKey(fieldName) && _MaxClamps[fieldName] < count)
                count = _MaxClamps[fieldName];

            setCount = count;
            fieldInfo.SetValue(null, count);
        }

        public bool IsSet()
        {
            return this.setCount >= 0;
        }

        public void Reset()
        {
            setCount = -1;
        }

        public void OnUpdate()
        {
            if (this.setCount >= 0)
                Set(this.setCount);
        }

        void setFI()
        {
            if (this.fieldInfo == null)
                this.fieldInfo = GetFI(fieldName);
        }

        public static FieldInfo GetFI(string fieldName)
        {
            return typeof(GameClass).GetField(fieldName, BindingFlags.Static | BindingFlags.Public);
        }

        public MilkDatum()
        {
        }

        public MilkDatum(FieldInfo fi)
        {
            this.fieldInfo = fi;
            this.fieldName = fi.Name;
            Reset();
        }
    }
}

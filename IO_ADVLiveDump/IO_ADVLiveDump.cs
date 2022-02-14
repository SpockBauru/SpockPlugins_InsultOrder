using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using HarmonyLib;
using System.IO;

namespace IO_ADVLiveDump
{
    [BepInProcess("IO")]
    [BepInPlugin("spockbauru.insultorder.io_IO_ADVLiveDump", "IO_ADVLiveDump", Version)]
    public class IO_ADVLiveDump : BaseUnityPlugin
    {
        // Set version in BepInEx and in AssemblyInfo
        public const string Version = "0.2";

        //Log Debug
        static IO_ADVLiveDump LogInstace = new IO_ADVLiveDump();

        string rawText = "";

        public void Start()
        {
            string path = Application.dataPath + "/Data/masterscenario.unity3d";
            var TxtBundle = AssetBundle.LoadFromFile(path);
            string[] fileNames = TxtBundle.AllAssetNames();


            for (int i = 0; i < fileNames.Length; i++)
            {
                string file = fileNames[i];
                var textAsset = (TxtBundle.LoadAsset(file) as TextAsset);
                rawText = rawText + "\r\n\r\n//================================\r\n\r\n" + textAsset.text;
            }

            string output = "DumbDump/ADV_Dump.txt";
            string log = "Creating file: " + output;
            DirectoryInfo _ = Directory.CreateDirectory("DumbDump");
            LogInstace.Logger.LogDebug(log);
            File.WriteAllText(output, rawText);

            LogInstace.Logger.LogDebug("Before text split");
            List<string> text = rawText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
            LogInstace.Logger.LogDebug(text.Count);
            //List<string> text = new List<string>(textFile);

            //Cleaning lines that are not dialogues
            bool endOfList = false;
            int k = 0;
            while (!endOfList)
            {
                if (text.ElementAt(k).Contains("CH"))
                {
                    text.RemoveAt(k);
                    k--;
                }
                if (text.ElementAt(k).StartsWith("***"))
                {
                    text.RemoveAt(k);
                    k--;
                }
                if (text.ElementAt(k).Contains("//"))
                {
                    if (text.ElementAt(k + 1) != "")
                    {
                        text[k]="//";
                    }
                    text.RemoveAt(k);
                    k--;
                }

                if (text[k] == "主人公" ||
                    text[k] == "音瑚" ||
                    text[k] == "兎萌" ||
                    text[k] == "スタッフＡ" ||
                    text[k] == "スタッフＢ" ||
                    text[k] == "スタッフ") 
                {
                    text.RemoveAt(k);
                    k--;
                }

                if (text[k] == "" && text[k + 1] == "")
                {
                    text.RemoveAt(k);
                    //k--;
                }
                k++;
                endOfList = k >= text.Count - 1;
            }

            //Substitution made by miconisomi
            for (int i = 0; i < text.Count; i++)
            {
                text[i] = text[i].Replace("「", "『");
                text[i] = text[i].Replace("」", "』");
            }

            //Grouping with /n because miconisomi wants this way...
            endOfList = false;
            k = 0;
            while (!endOfList)
            {
                if (text[k] != "" && text[k + 1] != "")
                {
                    text[k] = text[k] + "\\n" + text[k + 1];
                    text.RemoveAt(k + 1);
                    k--;
                }

                if (text[k] == "" && text[k + 1] == "")
                {
                    text.RemoveAt(k);
                    //k--;
                }
                k++;
                endOfList = k >= text.Count - 1;
            }



            output = "DumbDump/Cleaned_ADV_Dump.txt";
            File.WriteAllLines(output, text.ToArray());

            LogInstace.Logger.LogDebug("ADV Dump End");
        }

    }
}

using BepInEx;
using UnityEngine;
using HarmonyLib;

namespace IO_ResizeText
{
    [BepInProcess("IO")]
    [BepInPlugin("spockbauru.insultorder.io_resiztext", "IO_ResizeText", Version)]
    public class IO_ResizeText : BaseUnityPlugin
    {
        // Set version in BepInEx and in AssemblyInfo
        public const string Version = "1.1.1";

        //Internal names of Keypad Keys
        static string[] keyNames = {"Key1", "Key2", "Key3", "Key4", "Key5", "Key6", "Key7", "Key8", "Key9",
                                    "KeyNum", "Key_Plus", "Key_Divide", "Key_Multiply", "Key_Period", "Key_Enter"};

        static float counter = 0;
        static string currentKey;
        static GameObject key = null;
        static Vector3 pos;
        static UILabel keyText = null;

        public IO_ResizeText()
        {
            // Patch Everything
            Harmony.CreateAndPatchAll(typeof(IO_ResizeText));
        }


        //======================================== Hooks =================================================

        //UI Setup at start of the scene
        [HarmonyPostfix, HarmonyPatch(typeof(ConfigSetting), "Start")]
        private static void ConfigStart()
        {
            //=========================== Side Menu ============================
            //X-Ray text label
            currentKey = "UI Root(UI)/HS_MainWind/HS_qs/HS_QS11";
            ResizeUILabel(currentKey, 15, 45, 20);

            //Clear Semen text label
            currentKey = "UI Root(UI)/HS_MainWind/HS_qs/HS_QS09";
            ResizeUILabel(currentKey, 15, 52, 20);

            //Obstructions text label
            currentKey = "UI Root(UI)/HS_MainWind/HS_qs/HS_QS02";
            ResizeUILabel(currentKey, 15, 60, 20);

            //Show FPS text label
            currentKey = "UI Root(UI)/HS_MainWind/HS_qs/HS_QS03";
            ResizeUILabel(currentKey, 15, 50, 20);

            // Change Position label
            currentKey = "UI Root(UI)/HS_MainWind/HS_FreeH/Name";
            ResizeUILabel(currentKey, 11, 56, 25);
        }

        //Keypad in H-Scenes
        [HarmonyPostfix, HarmonyPatch(typeof(TenKeyPad), "Update")]
        private static void RepeatResize()
        {
            //Repeating every half second because bugs, my bad...
            counter += Time.deltaTime;
            if (counter >= 0.5f)
            {
                counter = 0f;

                //Make generic key boxes the same size
                for (int i = 0; i < keyNames.Length; i++)
                {
                    currentKey = "UI Root(FH)/TenKey/TenKey_BG/In/Key/" + keyNames[i] + "/Label";
                    ResizeUILabel(currentKey, 30, 110, 60);
                }

                //key minus have a special box, because miconisomi
                currentKey = "UI Root(FH)/TenKey/TenKey_BG/In/Key/Key_Minus/Label";
                ResizeUILabel(currentKey, 30, 110, 40);

                //key 0 is bigger
                currentKey = "UI Root(FH)/TenKey/TenKey_BG/In/Key/Key0/Label";
                ResizeUILabel(currentKey, 30, 200, 60);

                //Resizing adv textbox
                currentKey = "UI Root(FH)/FH_StartUI/Text_Text";
                ResizeUILabel(currentKey, 30, 800, 200);
            }
        }

        //====================================== Methods ============================================
        //Game's own UILabel
        private static void ResizeUILabel(string currentKey, int fontSize, int boxWidth, int boxHeight)
        {
            key = GameObject.Find(currentKey);

            if (key != null)
            {
                //saving position, because bugs...
                pos = key.transform.position;

                //changing box size
                keyText = key.GetComponent<UILabel>();
                keyText.fontSize = fontSize;
                keyText.width = boxWidth;
                keyText.height = boxHeight;

                key.transform.position = pos;
            }
        }
    }
}

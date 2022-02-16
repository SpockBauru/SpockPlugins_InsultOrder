using BepInEx;
using UnityEngine;
using HarmonyLib;

namespace IO_ResizeKeypad
{
    [BepInProcess("IO")]
    [BepInPlugin("spockbauru.insultorder.io_resizekeypad", "IO_ResizeKeypad", Version)]
    public class IO_ResizeKeypad : BaseUnityPlugin
    {
        // Set version in BepInEx and in AssemblyInfo
        public const string Version = "1.0";

        //Internal names of Keypad Keys
        static string[] keyNames = {"Key1", "Key2", "Key3", "Key4", "Key5", "Key6", "Key7", "Key8", "Key9",
                                    "KeyNum", "Key_Plus", "Key_Divide", "Key_Multiply", "Key_Period", "Key_Enter"};

        static float counter = 0;
        static string currentKey;
        static GameObject key = null;
        static Vector3 pos;
        static UILabel keyText = null;

        public IO_ResizeKeypad()
        {
            // Patch Everything
            Harmony.CreateAndPatchAll(typeof(IO_ResizeKeypad));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(TenKeyPad), "Update")]
        private static void RepeatResize()
        {
            //Repeating every second because bugs, my bad...
            counter += Time.deltaTime;
            if (counter > 1f)
            {
                counter = 0f;

                //Make generic key boxes the same size
                for (int i = 0; i < keyNames.Length; i++)
                {
                    currentKey = "UI Root(FH)/TenKey/TenKey_BG/In/Key/" + keyNames[i] + "/Label";
                    ResizeKey(currentKey, 30, 110, 60);
                }

                //key minus have a special box, because miconisomi
                currentKey = "UI Root(FH)/TenKey/TenKey_BG/In/Key/Key_Minus/Label";
                ResizeKey(currentKey, 30, 110, 40);

                //key 0 is bigger
                currentKey = "UI Root(FH)/TenKey/TenKey_BG/In/Key/Key0/Label";
                ResizeKey(currentKey, 30, 200, 60);
            }
        }

        private static void ResizeKey(string currentKey, int fontSize, int boxWidth, int boxHeight)
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

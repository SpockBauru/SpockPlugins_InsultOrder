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
        public const string Version = "0.2";

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
                ResizeKeypad();
                counter = 0f;
            }
        }

        private static void ResizeKeypad()
        {
            //Make all boxes the same sixe
            for (int i = 0; i < keyNames.Length; i++)
            {
                currentKey = "UI Root(FH)/TenKey/TenKey_BG/In/Key/" + keyNames[i] + "/Label";
                key = GameObject.Find(currentKey);

                if (key != null)
                {
                    //saving position, because bugs...
                    pos = key.transform.position;

                    //changing box size
                    keyText = key.GetComponent<UILabel>();
                    keyText.fontSize = 30;
                    keyText.width = 110;
                    keyText.height = 60;

                    key.transform.position = pos;
                }
            }

            //key minus have a special box, because miconisomi
            currentKey = "UI Root(FH)/TenKey/TenKey_BG/In/Key/Key_Minus/Label";
            key = GameObject.Find(currentKey);
            if (key != null)
            {
                //saving position, because bugs...
                pos = key.transform.position;

                //changing box size
                keyText = key.GetComponent<UILabel>();
                keyText.fontSize = 30;
                keyText.width = 110;
                keyText.height = 40;

                key.transform.position = pos;
            }

            //key 0 is bigger
            currentKey = "UI Root(FH)/TenKey/TenKey_BG/In/Key/Key0/Label";
            key = GameObject.Find(currentKey);
            if (key != null)
            {
                //saving position, because bugs...
                pos = key.transform.position;

                //changing box size
                keyText = key.GetComponent<UILabel>();
                keyText.fontSize = 30;
                keyText.width = 200;
                keyText.height = 60;

                key.transform.position = pos;
            }
        }
    }
}

using BepInEx;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IO_ResizeKeypad
{
    [BepInProcess("IO")]
    [BepInPlugin("spockbauru.insultorder.io_resizekeypad", "IO_ResizeKeypad", Version)]
    public class IO_ResizeKeypad : BaseUnityPlugin
    {
        // Set version in BepInEx and in AssemblyInfo
        public const string Version = "0.1";

        //Internal names of Keypad Keys
        string[] keyNames = {"Key1", "Key2", "Key3", "Key4", "Key5", "Key6", "Key7", "Key8", "Key9",
                             "KeyNum", "Key_Plus", "Key_Divide", "Key_Multiply", "Key_Period", "Key_Enter"};
        string currentKey;

        Vector3 pos;

        //just run when scene is complete loaded
        public void Start()
        {
            SceneManager.sceneLoaded += OnLevelFinishedLoading;
        }

        public void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
        {
            ResizeKeypad();
            //Recalculate again after 5 seconds, because bugs. My bad...
            Invoke("ResizeKeypad", 5);
        }

        private void ResizeKeypad()
        {
            Logger.LogDebug("Keypad Resizer Called");
            //Make all boxes the same sixe
            for (int i = 0; i < keyNames.Length; i++)
            {
                currentKey = "UI Root(FH)/TenKey/TenKey_BG/In/Key/" + keyNames[i] + "/Label";
                GameObject key = GameObject.Find(currentKey);

                if (key != null)
                {
                    //saving position, because miconisomi...
                    pos = key.transform.position;

                    //changing box size
                    UILabel keytext = key.GetComponent<UILabel>();
                    keytext.fontSize = 30;
                    keytext.lineWidth = 110;
                    keytext.lineHeight = 60;

                    key.transform.position = pos;
                }
            }

            //key down have a special box, becayse miconisomi
            currentKey = "UI Root(FH)/TenKey/TenKey_BG/In/Key/Key_Minus/Label";
            GameObject keyMinus = GameObject.Find(currentKey);

            if (keyMinus != null)
            {
                UILabel keytext = keyMinus.GetComponent<UILabel>();
                keytext.fontSize = 30;
                keytext.lineWidth = 110;
                keytext.lineHeight = 40;
            }

            //key 0 is bigger
            currentKey = "UI Root(FH)/TenKey/TenKey_BG/In/Key/Key0/Label";
            GameObject keyEnter = GameObject.Find(currentKey);

            if (keyEnter != null)
            {
                UILabel keytext = keyEnter.GetComponent<UILabel>();
                keytext.fontSize = 30;
                keytext.lineWidth = 200;
                keytext.lineHeight = 60;
            }
        }
    }
}

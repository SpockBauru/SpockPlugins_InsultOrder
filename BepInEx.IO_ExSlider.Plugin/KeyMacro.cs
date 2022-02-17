using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using UnityEngine;
using nnPlugin;
using static BepInEx.IO_ExSlider.Plugin.IO_ExSlider;
using System.Windows.Forms;

namespace BepInEx.IO_ExSlider.Plugin
{
    public class KeyMacro
    {
        public class PtnData
        {
            public Keybd.KeypadCode KeypadCode;
            public float waitTime;

            public PtnData()
            {
                KeypadCode = 0;
                waitTime = 0;
            }

            public void set(Keybd.KeypadCode keypadCode, float waitTime)
            {
                KeypadCode = keypadCode;
                this.waitTime = waitTime;
            }

            public PtnData(Keybd.KeypadCode keypadCode, float waitTime)
            {
                KeypadCode = keypadCode;
                this.waitTime = waitTime;
            }
        }

        public List<PtnData> macros = new List<PtnData>();

        float waitTime;
        int macroNow;
        bool _play = false;
        public bool Play
        {
            get { return _play; }
            set
            {
                if (!_play && value)
                {
                    macroNow = -1;
                }
                _play = value;
            }
        }

        public int CurrentMacroId
        {
            get => macroNow;
            set { selectMacro(value); }
        }
        public PtnData CurrentMacro
        {
            get {
                if (this.macroNow < 0 || this.macroNow >= this.macros.Count)
                    return new PtnData();

                return this.macros[this.macroNow];
            }
        }

        public int MaxLevel => macros.Count;

        public void Disable()
        {
            this.Play = false;
            this.enabled = false;
        }

        public bool enabled = true;
        public bool Pause = false;
        public bool Loop = true;

        // 最大レベル後の行先、＝MAXならランダム
        //public int restartLevel = 0;
        // タイマーのランダム変化率
        public float waitTimeRandomRate = 0f;

        public class SaveData
        {
            public List<PtnData> macros;
            public float waitTimeRandomRate = 0f;
            public bool Loop = true;

            public SaveData() { }

            public SaveData(KeyMacro pp)
            {
                macros = pp.macros;
                waitTimeRandomRate = pp.waitTimeRandomRate;
                Loop = pp.Loop;
            }

            public void Load(ref KeyMacro pp)
            {
                pp.macros = macros;
                pp.waitTimeRandomRate = waitTimeRandomRate;
                pp.Reset();
                pp.Loop = Loop;
            }
        }

        public void Reset()
        {
            this.macroNow = -1;
        }

        public void addPattern(Keybd.KeypadCode keypadCode, float waitTime)
        {
            macros.Add(new PtnData(keypadCode, waitTime));
        }

        public float getTimeRemains()
        {
            return waitTime;
        }
        public void setTimeRemains(float time)
        {
            waitTime = time;
        }

        void nextMacro()
        {
            if (this.macroNow < 0)
                this.macroNow = 0;

            if (this.macroNow < macros.Count - 1)
            {
                selectMacro(this.macroNow + 1);
            }
            else 
            {
                if (!this.Loop)
                    this.Play = false;

                selectMacro(0);
            }

        }

        void selectMacro(int id)
        {
            if (id < 0 || id >= macros.Count)
                return;

            this.macroNow = id;

            if (this.Play)
                this.waitTime = macros[macroNow].waitTime
                    * UnityEngine.Random.Range(1f - this.waitTimeRandomRate, 1f + this.waitTimeRandomRate);
        }

        void procMacro()
        {
            if (this.macroNow < 0 || this.macroNow >= macros.Count)
                return;

            PushKeypad(this.macros[macroNow].KeypadCode);
        }
        public static void PushKeypad(Keybd.KeypadCode key)
        {
            var tkbg = Extentions.FindSp("UI Root(FH)/TenKey/TenKey_BG");
            if (!tkbg)
                return;

            var pad = tkbg.GetComponent<TenKeyPad>();

            switch (key)
            {
                case Keybd.KeypadCode.NumPad0:
                    if (pad.Key0_b) pad.Key0_Push();
                    break;

                case Keybd.KeypadCode.NumPad1:
                    if (pad.Key1_b) pad.Key1_Push();
                    break;

                case Keybd.KeypadCode.NumPad2:
                    if (pad.Key2_b) pad.Key2_Push();
                    break;

                case Keybd.KeypadCode.NumPad3:
                    if (pad.Key3_b) pad.Key3_Push();
                    break;

                case Keybd.KeypadCode.NumPad4:
                    if (pad.Key4_b) pad.Key4_Push();
                    break;

                case Keybd.KeypadCode.NumPad5:
                    if (pad.Key5_b) pad.Key5_Push();
                    break;

                case Keybd.KeypadCode.NumPad6:
                    if (pad.Key6_b) pad.Key6_Push();
                    break;

                case Keybd.KeypadCode.NumPad7:
                    if (pad.Key7_b) pad.Key7_Push();
                    break;

                case Keybd.KeypadCode.NumPad8:
                    if (pad.Key8_b) pad.Key6_Push();
                    break;

                case Keybd.KeypadCode.NumPad9:
                    if (pad.Key9_b) pad.Key9_Push();
                    break;

                case Keybd.KeypadCode.Decimal:
                    if (pad.Key_Period_b) pad.Key_Period_Push();
                    break;

                case Keybd.KeypadCode.Add:
                    if (pad.Key_Plus_b) pad.Key_Plus_Push();
                    break;

                case Keybd.KeypadCode.Subtract:
                    if (pad.Key_Minus_b) pad.Key_Minus_Push();
                    break;

                case Keybd.KeypadCode.Divide:
                    if (pad.Key_Divide_b) pad.Key_Divide_Push();
                    break;

                case Keybd.KeypadCode.Enter:
                    if (pad.Key_Enter_b) pad.Key_Enter_Push();
                    break;
            }

            return;

            //// テンキーのリターンは生成できない＆キーを押すタイミングにより不具合がでるので没
            //if (this.macros[macroNow].KeypadCode == Keybd.KeypadCode.Enter)
            //{
            //    var tkbg = Extentions.FindSp("UI Root(FH)/TenKey/TenKey_BG");
            //    if (tkbg)
            //        tkbg.GetComponent<TenKeyPad>().Key_Enter_Push();
            //}
            //else
            //    Keybd.Send(this.macros[macroNow].KeypadCode);
        }

        public static bool checkClimax()
        {
            bool padDisabled = false;
            var tkbg = Extentions.FindSp("UI Root(FH)/TenKey/TenKey_BG");
            if (tkbg)
            {
                var pad = tkbg.GetComponent<TenKeyPad>();
                padDisabled = !pad.MotionControl || !pad.MotionControl || !pad.MotionControlAll;
            }
            else
            {
                padDisabled = GameClass.Climax || !ConfigClass.MiniMenu;
            }

            return padDisabled;
        }

        public void Update()
        {
            if (!Play)
                return;

            if (!this.enabled || macros.Count == 0 || !ConfigClass.KeyPadScene)
            {
                Play = false;
                return;
            }

            if (macroNow < 0)
            {
                nextMacro();
            }

            if (!Pause && !checkClimax())
            {
                var dt = Time.deltaTime;
                waitTime -= dt;

                // タイマーチェック
                if (waitTime <= 0)
                {
                    procMacro();
                    nextMacro();
                }
            }

        }

        public void OnApplicationFocus(bool hasFocus)
        {
            //if (!hasFocus && this.Play && !this.Pause)
            //    this.Pause = true;
        }

    }

    public class Keybd
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern void keybd_event(byte bVirtualKey, byte bScan, uint dwFlags, IntPtr dwExtraInfo);
        private const uint KEYEVENTF_KEYDOWN = 0;
        private const uint KEYEVENTF_KEYUP = 2;

        public static Dictionary<KeypadCode, string> dicToDisp = new Dictionary<KeypadCode, string>
        {
            { KeypadCode.None, "None" },
            { KeypadCode.Divide, "NumPad [/]" },
            { KeypadCode.Multiply, "NumPad [*]" },
            { KeypadCode.Subtract, "NumPad [-]" },
            { KeypadCode.Add, "NumPad [+]" },
            { KeypadCode.NumPad0, "NumPad [0]" },
            { KeypadCode.NumPad1, "NumPad [1]" },
            { KeypadCode.NumPad2, "NumPad [2]" },
            { KeypadCode.NumPad3, "NumPad [3]" },
            { KeypadCode.NumPad4, "NumPad [4]" },
            { KeypadCode.NumPad5, "NumPad [5]" },
            { KeypadCode.NumPad6, "NumPad [6]" },
            { KeypadCode.NumPad7, "NumPad [7]" },
            { KeypadCode.NumPad8, "NumPad [8]" },
            { KeypadCode.NumPad9, "NumPad [9]" },
            //{ KeypadCode.Separator, "Separator" },
            { KeypadCode.Decimal, "NumPad [.]" },
            { KeypadCode.Enter, "Enter" },
        };

        public enum KeypadCode
        {
            None = 0,
            Enter = 13,
            NumPad0 = 96,
            NumPad1 = 97,
            NumPad2 = 98,
            NumPad3 = 99,
            NumPad4 = 100,
            NumPad5 = 101,
            NumPad6 = 102,
            NumPad7 = 103,
            NumPad8 = 104,
            NumPad9 = 105,
            Multiply = 106,
            Add = 107,
            //Separator = 108,
            Subtract = 109,
            Decimal = 110,
            Divide = 111,
        }

        public static void Send(KeypadCode key)
        {
            if (key == KeypadCode.None)

                return;
            
            keybd_event((byte)key, 0, KEYEVENTF_KEYDOWN, IntPtr.Zero);
            keybd_event((byte)key, 0, KEYEVENTF_KEYUP, IntPtr.Zero);
        }
    }
}

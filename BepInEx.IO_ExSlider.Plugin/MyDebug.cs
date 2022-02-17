using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace nnPlugin
{
    public static class MyDebug
    {
        [Conditional("DEBUG"), Conditional("DEVMODE")]
        public static void Write(string s)
        {
            Console.Write(s);
        }

        [Conditional("DEBUG"), Conditional("DEVMODE")]
        public static void WriteLine(string s)
        {
            Console.WriteLine(s);
        }

        [Conditional("DEBUG"), Conditional("DEVMODE")]
        public static void Log(string s)
        {
            UnityEngine.Debug.Log(s);
        }

        [Conditional("DEBUG"), Conditional("DEVMODE")]
        public static void LogWarning(string s)
        {
            UnityEngine.Debug.LogWarning(s);
        }
    }
}

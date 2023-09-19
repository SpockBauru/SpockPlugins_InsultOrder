using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;
using BepInEx;
using System.Reflection;
using static BepInEx.IO_ExSlider.Plugin.IO_ExSlider;
using System.Runtime.InteropServices;

namespace BepInEx.IO_ExSlider.Plugin
{
    public static class ScreenCap
    {
        [DllImport("User32.dll")]
        static extern bool MessageBeep(UInt32 uType);
        public enum MessageBeepType : UInt32
        {
            BEEP = 0xFFFFFFFF,
            MB_OK = 0x00,
            MB_ICONERROR = 0x10,
            MB_ICONQUESTION = 0x20,
            MB_ICONWARNING = 0x30,
            MB_ICONINFORMATION = 0x40,
        }

        public static string pngPath;
        public static void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.SysReq) || Input.GetKeyDown(KeyCode.Home))
            {
                int scale = 1;
                if (InputEx.CheckModifierKeys(InputEx.ModifierKey.Shift))
                    scale = 2;

                var sspath = Path.GetFullPath(Paths.GameRootPath + "/../ScreenShot");
                if (!Directory.Exists(sspath))
                {
                    Directory.CreateDirectory(sspath);
                }
                sspath = Path.Combine(sspath, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + "_ex.png");
                pngPath = sspath;

                if (InputEx.CheckModifierKeys(InputEx.ModifierKey.Alt) && Camera.main)
                {
                    // UI無し
                    Texture2D sstex = new Texture2D(Screen.width * scale, Screen.height * scale,
                    //Texture2D sstex = new Texture2D(Screen.width, Screen.height,
                                                TextureFormat.RGB24, false);
                    var savert = RenderTexture.GetTemporary(sstex.width, sstex.height, 24);
                    var orgtgt = Camera.main.targetTexture;
                    var orgrta = RenderTexture.active;
                    //Debug.Log(orgtgt);
                    //Debug.Log(orgrta);

                    try
                    {
                        Camera.main.targetTexture = savert;
                        Camera.main.Render();

                        RenderTexture.active = savert;
                        sstex.ReadPixels(new Rect(0, 0, sstex.width, sstex.height), 0, 0);
                        sstex.Apply();

                        sspath = sspath.Replace(".png", "(NoUI).png");

                        byte[] bytes = sstex.EncodeToPNG();
                        File.WriteAllBytes(sspath, bytes);
                        //System.Media.SystemSounds.Exclamation.Play();

                        MessageBeep((UInt32)MessageBeepType.MB_ICONINFORMATION);
                    }
                    finally
                    {
                        Camera.main.targetTexture = orgtgt;
                        RenderTexture.active = orgrta;
                        if (sstex)
                            UnityEngine.Object.DestroyImmediate(sstex);
                        if (savert)
                            RenderTexture.ReleaseTemporary(savert);
                    }
                }
                else
                {
                    // UI有り
                    Debug.Log($"ScreenCapture *{scale}:" + sspath);
                    //ScreenCapture.CaptureScreenshot(sspath, scale);
                    //Camera.main.gameObject.AddComponent<ScreenCapture>();
                    //System.Media.SystemSounds.Asterisk.Play();
                    Application.CaptureScreenshot(sspath, scale);

                    MessageBeep((UInt32)MessageBeepType.MB_ICONINFORMATION);
                }
            }
        }

        public class ScreenCapture : MonoBehaviour
        {
            void Awake()
            {
                StartCoroutine(captureScreen(pngPath));
            }

            IEnumerator captureScreen(string filename)
            {
                yield return new WaitForEndOfFrame();

                var captureTex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);

                // ReadPixel
                captureTex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                captureTex.Apply();

                // 保存
                byte[] pngData = captureTex.EncodeToPNG();
                File.WriteAllBytes(filename, pngData);

                Destroy(captureTex);
                Destroy(this);
            }
        }
    }

}
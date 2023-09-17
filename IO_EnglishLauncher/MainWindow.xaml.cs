using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using MessageBox = System.Windows.Forms.MessageBox;

namespace IO_EnglishLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public List<string> ScreenSizeList = new List<string>();
        public List<string> QualityList = new List<string>();
        public List<string> DisplayList = new List<string>();
        public int width;
        public int heigh;
        public int quality;
        public bool fullScreen;
        public int display;

        public MainWindow()
        {
            InitializeComponent();
            width = 1280;
            heigh = 720;
            quality = 0;
            fullScreen = false;
            display = 0;
            base.MouseLeftButtonDown += delegate (object sender, MouseButtonEventArgs e)
            {
                base.DragMove();
            };
            ScreenSizeList.Add("512x288");
            ScreenSizeList.Add("768x432");
            ScreenSizeList.Add("1024x576");
            ScreenSizeList.Add("1280x720");
            ScreenSizeList.Add("1334x750");
            ScreenSizeList.Add("1366x768");
            ScreenSizeList.Add("1536x864");
            ScreenSizeList.Add("1600x900");
            ScreenSizeList.Add("1920x1080");
            ScreenSizeList.Add("2048x1152");
            ScreenSizeList.Add("2560x1440");
            ScreenSizeList.Add("2880x1620");
            ScreenSizeList.Add("3200x1800");
            ScreenSizeList.Add("3840x2160");
            ScreenSizeList.Add("4096x2304");
            ScreenSizeList.Add("5120x2880");
            QualityList.Add("Quality (Heavy)");
            QualityList.Add("Normal (Balanced)");
            QualityList.Add("Performance (Fast)");
            for (int i = 1; i < Screen.AllScreens.Length + 1; i++)
            {
                DisplayList.Add("Screen " + i);
            }
            foreach (string newItem in ScreenSizeList)
            {
                ScreenSize.Items.Add(newItem);
            }
            foreach (string newItem2 in QualityList)
            {
                Quality.Items.Add(newItem2);
            }
            foreach (string newItem3 in DisplayList)
            {
                Display.Items.Add(newItem3);
            }
            RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software\\みこにそみ\\インサルトオーダー～生イキにゃん娘の快堕メニュー～");
            if (registryKey.GetValue("Screenmanager Is Fullscreen mode_h3981298716") != null)
            {
                if ((int)registryKey.GetValue("Screenmanager Is Fullscreen mode_h3981298716") == 0)
                {
                    fullScreen = false;
                }
                else if ((int)registryKey.GetValue("Screenmanager Is Fullscreen mode_h3981298716") == 1)
                {
                    fullScreen = true;
                }
            }
            if (registryKey.GetValue("Screenmanager Resolution Height_h2627697771") != null)
            {
                heigh = (int)registryKey.GetValue("Screenmanager Resolution Height_h2627697771");
            }
            if (registryKey.GetValue("Screenmanager Resolution Width_h182942802") != null)
            {
                width = (int)registryKey.GetValue("Screenmanager Resolution Width_h182942802");
            }
            if (registryKey.GetValue("UnitySelectMonitor_h17969598") != null)
            {
                display = (int)registryKey.GetValue("UnitySelectMonitor_h17969598");
            }
            registryKey.Close();
            int j = 0;
            int selectedIndex = 0;
            while (j < ScreenSizeList.Count)
            {
                if (ScreenSizeList[j].Contains(width + "x" + heigh))
                {
                    selectedIndex = j;
                }
                j++;
            }
            ScreenSize.SelectedIndex = selectedIndex;
            Quality.SelectedIndex = quality;
            Display.SelectedIndex = display;
            FullScreenCheckBox.IsChecked = new bool?(fullScreen);
            CommandLineBox.Width = 0.0;
        }

        private void comboBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string[] array = ScreenSize.SelectedItem.ToString().Split(new char[]
            {
                'x'
            });
            width = int.Parse(array[0]);
            heigh = int.Parse(array[1]);
        }

        private void Quality_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            quality = Quality.SelectedIndex;
        }

        private void Display_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            display = Display.SelectedIndex;
        }

        private void FullScreenCheckBox_Checked(object sender, RoutedEventArgs e)
        {
        }

        private void FullScreenCheckBox_Click(object sender, RoutedEventArgs e)
        {
            fullScreen = FullScreenCheckBox.IsChecked.Value;
        }

        private void GamePlay_Click(object sender, RoutedEventArgs e)
        {
            RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software\\みこにそみ\\インサルトオーダー～生イキにゃん娘の快堕メニュー～");
            int num;
            if (fullScreen)
            {
                num = 1;
            }
            else
            {
                num = 0;
            }
            registryKey.SetValue("Screenmanager Resolution Height_h2627697771", heigh);
            registryKey.SetValue("Screenmanager Resolution Width_h182942802", width);
            registryKey.SetValue("Screenmanager Is Fullscreen mode_h3981298716", num);
            registryKey.SetValue("UnityGraphicsQuality_h1669003810", quality);
            registryKey.SetValue("UnitySelectMonitor_h17969598", display);
            registryKey.Close();

            string text = Environment.CurrentDirectory + "\\GameData";
            WriteAutoTranslatorLangIni(text, "en");
            SetConfigVariable(Path.Combine(text, "BepInEx\\config\\IO_Tweaks.cfg"), "[General]", "Fix camera rotate button", "true");
            Process.Start(text + "\\io.exe", " " + this.CommandLineBox.Text);

            System.Windows.Application.Current.MainWindow.Close();
        }

        private void GameQuit_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.MainWindow.Close();
        }

        private void WebSite_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://miconisomi.xii.jp/");
        }

        private void ManualBt_Click(object sender, RoutedEventArgs e)
        {
            string manualPath = Path.Combine(Environment.CurrentDirectory, "manual\\manual_en.html");
            if (File.Exists(manualPath))
            {
                Process.Start(manualPath);
                return;
            }

            manualPath = Path.Combine(Environment.CurrentDirectory, "manual\\manual_ja.html");
            if (File.Exists(manualPath))
            {
                Process.Start(manualPath);
                return;
            }

            Process.Start("http://miconisomi.xii.jp/io/manual/index.html");
        }

        private void SystemBt_Click(object sender, RoutedEventArgs e)
        {
            string text = Environment.ExpandEnvironmentVariables("%windir%") + "/System32/dxdiag.exe";
            if (File.Exists(text))
            {
                Process.Start(text);
            }
        }

        private void SaveFolderBt_Click(object sender, RoutedEventArgs e)
        {
            string text = Environment.CurrentDirectory + "\\Save";
            if (!Directory.Exists(text))
            {
                Directory.CreateDirectory(text);
            }
            Process.Start("explorer.exe", text);
        }

        private void ScreenShotBt_Click(object sender, RoutedEventArgs e)
        {
            string text = Environment.CurrentDirectory + "\\ScreenShot";
            Directory.CreateDirectory(text);
            Process.Start("explorer.exe", text);
        }

        private void VideoFolderBt_Click(object sender, RoutedEventArgs e)
        {
            string arguments = Environment.CurrentDirectory + "\\GameData\\io_Data\\StreamingAssets\\Video";
            Process.Start("explorer.exe", arguments);
        }

        private void SupportForumBt_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://miconisomi.xii.jp/forum/index.html");
        }

        private void CommandLineBt_Click(object sender, RoutedEventArgs e)
        {
            if (CommandLineBox.Width > 0.0)
            {
                CommandLineBox.Width = 0.0;
                return;
            }
            CommandLineBox.Width = 790.0;
        }

        private void Read_Click(object sender, RoutedEventArgs e)
        {
            new Process
            {
                StartInfo = new ProcessStartInfo("はじめにお読みください.txt")
            }.Start();
        }

        private static void WriteAutoTranslatorLangIni(string gameDirectory, string language)
        {
            string text = Path.Combine(gameDirectory, "BepInEx/Config/AutoTranslatorConfig.ini");
            if (language != "zh-CN" && language != "zh-TW")
            {
                language = language.Split(new char[] { '-' })[0];
            }
            bool flag = language.Equals("ja", StringComparison.OrdinalIgnoreCase);
            bool flag2 = File.Exists(gameDirectory + "/BepInEx/Translation/" + language + "/DisableGoogle.txt");
            try
            {
                List<string> list = new List<string>(File.Exists(text) ? File.ReadAllLines(text) : new string[0]);
                string text2 = string.Empty;
                string text3 = string.Empty;
                string text4 = string.Empty;
                text2 = ((language == "ko") ? "PapagoTranslate" : "GoogleTranslateV2");
                if (language == "ru")
                {
                    text3 = "Times New Roman";
                }
                else if (language == "zh-CN" || language == "zh-TW")
                {
                    text3 = "MS Gothic";
                }
                else
                {
                    text3 = string.Empty;
                }
                if (File.Exists(gameDirectory + "/BepInEx/Translation/" + language + "/UseFont.txt"))
                {
                    string text5 = File.ReadAllText(gameDirectory + "/BepInEx/Translation/" + language + "/UseFont.txt");
                    text4 = (File.Exists(gameDirectory + "\\BepInEx\\Translation\\fonts\\" + text5) ? ("BepInEx\\Translation\\fonts\\" + text5) : string.Empty);
                }
                int num = list.FindIndex((string s) => s.ToLower().Contains("[General]".ToLower()));
                if (num >= 0)
                {
                    int num2 = list.FindIndex(num, (string s) => s.StartsWith("Language"));
                    if (num2 > num)
                    {
                        list[num2] = "Language=" + language;
                    }
                    else
                    {
                        list.Insert(num + 1, "Language=" + language);
                    }
                    int num3 = list.FindIndex(num, (string s) => s.StartsWith("FromLanguage"));
                    if (num3 > num)
                    {
                        list[num3] = "FromLanguage=" + (flag ? "en" : "ja");
                    }
                    else
                    {
                        list.Insert(num + 1, "FromLanguage=" + (flag ? "en" : "ja"));
                    }
                }
                else
                {
                    list.Add("");
                    list.Add("[General]");
                    list.Add("Language=" + language);
                }
                int num4 = list.FindIndex((string s) => s.ToLower().Contains("[Service]".ToLower()));
                if (num4 >= 0)
                {
                    int num5 = list.FindIndex(num4, (string s) => s.StartsWith("Endpoint"));
                    if (num5 > num4)
                    {
                        list[num5] = (flag2 ? "Endpoint=" : ("Endpoint=" + text2));
                    }
                    else
                    {
                        list.Insert(num4 + 1, flag2 ? "Endpoint=" : "Endpoint=GoogleTranslate");
                    }
                }
                else
                {
                    list.Add("");
                    list.Add("[Service]");
                    list.Add(flag2 ? "Endpoint=" : ("Endpoint=" + text2));
                }
                int num6 = list.FindIndex((string s) => s.ToLower().Contains("[Behaviour]".ToLower()));
                if (num6 >= 0)
                {
                    int num7 = list.FindIndex(num6, (string s) => s.StartsWith("OverrideFont"));
                    if (num7 > num6)
                    {
                        list[num7] = ((flag || flag2) ? "OverrideFont=" : ("OverrideFont=" + text3));
                    }
                }
                else
                {
                    list.Add("");
                    list.Add("[Behaviour]");
                    list.Add((flag || flag2) ? "OverrideFont=" : ("OverrideFont=" + text3));
                }
                int num8 = list.FindIndex((string s) => s.ToLower().Contains("[Behaviour]".ToLower()));
                if (num8 >= 0)
                {
                    int num9 = list.FindIndex(num8, (string s) => s.StartsWith("OverrideFontTextMeshPro"));
                    if (num9 > num8)
                    {
                        list[num9] = ((flag || flag2) ? "OverrideFontTextMeshPro=" : ("OverrideFontTextMeshPro=" + text4));
                    }
                }
                else
                {
                    list.Add("");
                    list.Add("[Behaviour]");
                    list.Add((flag || flag2) ? "OverrideFontTextMeshPro=" : ("OverrideFontTextMeshPro=" + text4));
                }
                Directory.CreateDirectory(Path.GetDirectoryName(text));
                File.WriteAllLines(text, list.ToArray());
            }
            catch (Exception ex)
            {
                string text6 = "Something went wrong when setting language: ";
                Exception ex2 = ex;
                MessageBox.Show(text6 + ((ex2 != null) ? ex2.ToString() : null));
            }
        }

        private static void SetConfigVariable(string configPath, string settingCategory, string settingName, string value)
        {
            if (!File.Exists(configPath))
            {
                File.WriteAllText(configPath, string.Concat(new string[] { settingCategory, "\n\n", settingName, " = ", value }));
                return;
            }
            string text = File.ReadAllText(configPath);
            string text2 = "^" + settingName + ".?=.+$";
            if (Regex.IsMatch(text, text2, RegexOptions.Multiline))
            {
                text = Regex.Replace(text, text2, settingName + " = " + value, RegexOptions.Multiline);
                File.WriteAllText(configPath, text);
                return;
            }
            File.WriteAllText(configPath, string.Concat(new string[] { settingCategory, "\n\n", settingName, " = ", value }));
        }
    }
}

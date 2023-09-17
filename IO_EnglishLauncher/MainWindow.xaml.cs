using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

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
			Process.Start(Environment.CurrentDirectory + "\\GameData\\io.exe", " " + CommandLineBox.Text);
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
	}
}

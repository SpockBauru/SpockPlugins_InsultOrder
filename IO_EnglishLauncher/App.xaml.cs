using System.Windows;
using System.Threading;

namespace IO_EnglishLauncher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
		private void Application_Startup(object sender, StartupEventArgs e)
		{
			if (!mutex.WaitOne(0, false))
			{
				MessageBox.Show("Miconisomi is already running.", "Miconisomi", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				mutex.Close();
				mutex = null;
				base.Shutdown();
			}
		}

		// Token: 0x06000009 RID: 9 RVA: 0x000020F4 File Offset: 0x000002F4
		private void Application_Exit(object sender, ExitEventArgs e)
		{
			if (mutex != null)
			{
				mutex.ReleaseMutex();
				mutex.Close();
			}
		}

		// Token: 0x04000004 RID: 4
		private Mutex mutex = new Mutex(false, "Miconisomi");
	}
}

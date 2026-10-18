using System;
using System.Windows;
using WinIconsPro.Core;
using WinIconsPro.UI;

namespace WinIconsPro
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Exception ex = e.ExceptionObject as Exception;
                MessageBox.Show("An unexpected error occurred: " + (ex != null ? ex.Message : "Unknown error"), "WinIcons Pro", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            if (args != null && args.Length > 0)
            {
                if (args[0].Equals("--restore", StringComparison.OrdinalIgnoreCase))
                {
                    IconPatcher.RestoreOriginals(null);
                    return;
                }
            }

            Application app = new Application();
            app.DispatcherUnhandledException += (s, e) =>
            {
                MessageBox.Show("Application error: " + e.Exception.Message, "WinIcons Pro", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true;
            };

            MainWindow mainWindow = new MainWindow();
            app.Run(mainWindow);
        }
    }
}

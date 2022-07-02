using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using DyviniaUtils;
using DyviniaUtils.Dialogs;
using DyviniaUtils.SettingsManager;

namespace BulkAudio {

    public class Config : SettingsManager<Config> {
        public bool UpdateChecker { get; set; } = true;

        public string InDir { get; set; } = App.BaseDir + "Input";
        public string OutDir { get; set; } = App.BaseDir + "Output";
        public string UtilsDir { get; set; } = App.BaseDir + "Utils";
    }

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {

        public static readonly string Version = "v" + Assembly.GetExecutingAssembly().GetName().Version.ToString()[..5];
        public static readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;
        public static readonly string AppName = Assembly.GetEntryAssembly().GetName().Name;

        public App() {
            Config.Load();

            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            Directory.CreateDirectory(Config.Settings.InDir);
            Directory.CreateDirectory(Config.Settings.OutDir);

            DispatcherUnhandledException += Application_DispatcherUnhandledException;
        }

        protected override async void OnStartup(StartupEventArgs e) {
            MainWindow = new MainWindow();
            if (!File.Exists(Config.Settings.UtilsDir + "\\ffmpeg.exe"))
                await ShowPopup(new DownloadWindow());
            MainWindow.Show();

            if (Config.Settings.UpdateChecker)
                GitHub.CheckVersion("Dyvinia", "BulkAudio");
        }

        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) {
            e.Handled = true;
            string title = AppName;
            ExceptionDialog.Show(e.Exception, title, true);
        }

        private Task ShowPopup<TPopup>(TPopup popup) where TPopup : Window {
            var task = new TaskCompletionSource<object>();
            popup.Closed += (s, a) => task.SetResult(null);
            popup.Show();
            popup.Focus();
            return task.Task;
        }

    }
}

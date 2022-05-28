using BulkAudio.Dialogs;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Xabe.FFmpeg.Downloader;

namespace BulkAudio {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {

        public static readonly string Version = "v" + Assembly.GetExecutingAssembly().GetName().Version.ToString().Substring(0, 5);
        public static readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;

        public static string InpWavDir = BaseDir + "Input";
        public static string OutWavDir = BaseDir + "Output";
        public static string FFmpegDir = BaseDir + "Utils\\ffmpeg.exe";

        public App() {
            DispatcherUnhandledException += Application_DispatcherUnhandledException;

            Directory.CreateDirectory(InpWavDir);
            Directory.CreateDirectory(OutWavDir);
        }

        protected override async void OnStartup(StartupEventArgs e) {
            MainWindow = new MainWindow();
            if (!File.Exists(FFmpegDir))
                await ShowPopup(new DownloadWindow());
            MainWindow.Show();
        }

        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) {
            e.Handled = true;
            string title = "BulkAudio";
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

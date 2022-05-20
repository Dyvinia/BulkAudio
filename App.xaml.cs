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

        public App() {
            //DispatcherUnhandledException += Application_DispatcherUnhandledException;
        }

        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) {
            string title = "Bulk Audio";
            if (e.Exception.InnerException != null)
                MessageBox.Show(e.Exception.Message + Environment.NewLine + Environment.NewLine + e.Exception.InnerException, title, MessageBoxButton.OK, MessageBoxImage.Error);
            else
                MessageBox.Show(e.Exception.Message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
            App.Current.Shutdown();
        }

        protected override async void OnStartup(StartupEventArgs e) {
            MainWindow = new MainWindow();

            if (!File.Exists(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\tools\\ffmpeg.exe")) {
                await ShowPopup(new DownloadWindow());
            }

            MainWindow.Show();
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

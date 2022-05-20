using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

namespace BulkAudio {
    /// <summary>
    /// Interaction logic for DownloadWindow.xaml
    /// </summary>
    public partial class DownloadWindow : Window {
        

        public DownloadWindow() {
            InitializeComponent();

            IProgress<ProgressInfo> progress = new Progress<ProgressInfo>(report => {
                pbStatus.Value = report.DownloadedBytes;
                pbStatus.Maximum = report.TotalBytes;
            });
            Download(progress);
        }

        public async void Download(IProgress<ProgressInfo> progress) {
            try {
                await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\tools\\", progress);
                File.Delete(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\tools\\ffprobe.exe");
                File.Delete(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\tools\\version.json");
            }
            catch (Exception e) {
                string message = "Unable to Download FFmpeg to \\tools\\ffmpeg.exe" + Environment.NewLine + e.Message;
                if (e.InnerException != null) message += Environment.NewLine + e.InnerException.Message;
                MessageBox.Show(message, "BulkAudio", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            this.Close();
        }
    }
}

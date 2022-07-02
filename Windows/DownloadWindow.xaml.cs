using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shell;
using DyviniaUtils;

namespace BulkAudio {
    /// <summary>
    /// Interaction logic for DownloadWindow.xaml
    /// </summary>
    public partial class DownloadWindow : Window {
        public static readonly string TempDir = Path.Combine(Config.Settings.UtilsDir, ".temp");

        public DownloadWindow() {
            InitializeComponent();
            Startup();
        }

        public async void Startup() {

            IProgress<double> progress = new Progress<double>(p => {
                pbStatus.Value = p;
                TaskbarItemInfo.ProgressValue = p;
            });

            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
            Directory.CreateDirectory(TempDir);
            await DownloadFFmpeg(progress);
            await Task.Delay(200);
            Directory.Delete(TempDir, true);
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;

            this.Close();
        }

        public async static Task DownloadFFmpeg(IProgress<double> progress) {
            string destination = $"{TempDir}\\ffmpeg.zip";
            string url = "https://github.com/yt-dlp/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip";

            await Downloader.Download(url, destination, progress);

            using ZipArchive archive = ZipFile.OpenRead(destination);
            foreach (ZipArchiveEntry entry in archive.Entries.Where(e => e.FullName.Contains("ffmpeg.exe"))) {
                entry.ExtractToFile(Path.Combine(Config.Settings.UtilsDir, entry.Name), true);
            }
        }
    }
}

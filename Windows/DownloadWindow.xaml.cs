using BulkAudio.Dialogs;
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

namespace BulkAudio {
    /// <summary>
    /// Interaction logic for DownloadWindow.xaml
    /// </summary>
    public partial class DownloadWindow : Window {

        public DownloadWindow() {
            InitializeComponent();
            this.Close();
            //IProgress<ProgressInfo> progress = new Progress<ProgressInfo>(report => {
            //    pbStatus.Value = report.DownloadedBytes;
            //    pbStatus.Maximum = report.TotalBytes;
            //});
            //DownloadFFmpeg(progress);
        }

        //public async void DownloadFFmpeg(IProgress<ProgressInfo> progress) {
        //    try {
        //        await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, App.BaseDir + "Utils\\", progress);
        //        File.Delete(App.BaseDir + "Utils\\ffprobe.exe");
        //        File.Delete(App.BaseDir + "Utils\\version.json");
        //    }
        //    catch (Exception e) {
        //        string message = "Unable to Download FFmpeg to Utils\\ffmpeg.exe:";
        //        ExceptionDialog.Show(e, "BulkAudio", true, message);
        //    }
        //    this.Close();
        //}
    }
}

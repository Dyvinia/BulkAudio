using BulkAudio.Dialogs;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shell;

namespace BulkAudio {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        public class FileListItem {
            public string Name { get; set; }
            public string Path { get; set; }
        }
        public ObservableCollection<FileListItem> FileList = new ObservableCollection<FileListItem>();


        public MainWindow() {
            InitializeComponent();

            MouseDown += (s, e) => FocusManager.SetFocusedElement(this, this);
            btn_refresh.Click += (s, e) => FillInputAudioList();
            btn_inputopen.Click += (s, e) => Process.Start(App.InpWavDir);
            btn_outputopen.Click += (s, e) => Process.Start(App.OutWavDir);
            creditButton.Click += (s, e) => Process.Start("https://dyy.vin/twitter");

            audioListBox.ItemsSource = FileList;
            txt_Version.Text = App.Version;

            FillInputAudioList();
        }

        public void FillInputAudioList() {
            Mouse.OverrideCursor = Cursors.Wait;

            FileList.Clear();
            txtb_inputpath.Text = App.InpWavDir;
            txtb_outputpath.Text = App.OutWavDir;

            List<string> ext = new List<string> { "wav", "mp3", "ogg", "flac", "m4a" };
            string[] files = Directory.EnumerateFiles(App.InpWavDir, "*.*", SearchOption.AllDirectories).Where(s => ext.Contains(Path.GetExtension(s).TrimStart('.').ToLowerInvariant())).ToArray();

            foreach (string file in files)
                FileList.Add(new FileListItem { Name = Path.GetFileNameWithoutExtension(App.InpWavDir) + file.Remove(0, App.InpWavDir.Length), Path = file });

            Mouse.OverrideCursor = null;
        }

        public void AnalyzeAudio(string filePath) {
            using (Process ffmpeg = new Process()) {
                Mouse.OverrideCursor = Cursors.Wait;

                ffmpeg.StartInfo.FileName = App.FFmpegDir;
                ffmpeg.StartInfo.CreateNoWindow = true;
                ffmpeg.StartInfo.Arguments = $"-y -i \"{filePath}\" -af \"adelay=3s:all=true\",loudnorm=print_format=json -f null -";
                ffmpeg.StartInfo.UseShellExecute = false;
                ffmpeg.StartInfo.RedirectStandardError = true;
                ffmpeg.Start();
                string output = ffmpeg.StandardError.ReadToEnd();
                ffmpeg.WaitForExit();

                output = output.Substring(output.IndexOf('{'));
                dynamic results = JsonConvert.DeserializeObject<dynamic>(output);
                string lufs = results.input_i;
                string truepeak = results.input_tp;

                Mouse.OverrideCursor = null;

                string title = "Loudness";
                string message = $"Loudness: {lufs}LUFS" + Environment.NewLine + $"True Peak: {truepeak}dB";
                MessageBoxDialog.Show(message, title, MessageBoxButton.OK, DialogSound.Notify);
            }
        }

        private void ProcessAudio(int extensionIndex, int remixIndex, int? inputLUFS, IProgress<int> progress) {
            int currentProgress = 1;
            string logString = null;

            foreach (FileListItem soundInput in FileList) {
                string outFile = soundInput.Path.Replace(App.InpWavDir, App.OutWavDir);

                if (extensionIndex == 1) outFile = Path.ChangeExtension(outFile, ".wav");
                if (extensionIndex == 2) outFile = Path.ChangeExtension(outFile, ".mp3");
                if (extensionIndex == 3) outFile = Path.ChangeExtension(outFile, ".flac");
                if (extensionIndex == 4) outFile = Path.ChangeExtension(outFile, ".ogg");

                string remix = "";
                if (remixIndex == 1) remix = "-ac 1 ";
                if (remixIndex == 2) remix = "-ac 2 ";

                Directory.CreateDirectory(Path.GetDirectoryName(outFile));

                //FFMpeg
                using (Process ffmpeg = new Process()) {
                    ffmpeg.StartInfo.FileName = App.FFmpegDir;
                    ffmpeg.StartInfo.CreateNoWindow = true;
                    ffmpeg.StartInfo.UseShellExecute = false;
                    ffmpeg.StartInfo.RedirectStandardError = true;
                    string segmentvol = "";

                    if (inputLUFS != null && (inputLUFS > -71 & inputLUFS < -4)) {
                        ffmpeg.StartInfo.Arguments = $"-y -i \"{soundInput.Path}\" -af \"adelay=3s:all=true\",loudnorm=print_format=json -f null -";
                        logString += $"> ffmpeg {ffmpeg.StartInfo.Arguments}\r\n";
                        ffmpeg.Start();
                        string output = ffmpeg.StandardError.ReadToEnd();
                        ffmpeg.WaitForExit();

                        output = output.Substring(output.IndexOf('{'));
                        dynamic results = JsonConvert.DeserializeObject<dynamic>(output);
                        float lufs = (float)results.input_i;
                        string volume = (Math.Pow(10, (-(lufs - (Convert.ToInt32(inputLUFS))) / 20))).ToString();
                        segmentvol = $"-af \"volume = {volume}\" ";
                    }

                    ffmpeg.StartInfo.Arguments = $"-y -i \"{soundInput.Path}\" {segmentvol}{remix}\"{outFile}\"";
                    logString += $"> ffmpeg {ffmpeg.StartInfo.Arguments}\r\n";
                    ffmpeg.Start();
                    ffmpeg.WaitForExit();

                    progress.Report(currentProgress++);
                }

                //Save output
                File.WriteAllText(Path.GetDirectoryName(App.FFmpegDir) + "\\log.txt", logString);
            }
        }

        private void playAudio_Click(object sender, RoutedEventArgs e) {
            FileListItem file = ((Button)sender).DataContext as FileListItem;
            Process.Start(file.Path);
        }

        private void openAudioFolder_Click(object sender, RoutedEventArgs e) {
            FileListItem file = ((Button)sender).DataContext as FileListItem;
            Process.Start(new ProcessStartInfo("explorer.exe", " /select, " + file.Path));
        }

        private void analyzeAudio_Click(object sender, RoutedEventArgs e) {
            FileListItem file = ((Button)sender).DataContext as FileListItem;
            AnalyzeAudio(file.Path);
        }

        private void btn_anal_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog openFileDlg = new OpenFileDialog();
            openFileDlg.Filter = "Audio (*.wav, *.mp3, *.ogg, *.flac) |*.wav;*.mp3;*.ogg;*.flac";
            openFileDlg.FilterIndex = 2;
            openFileDlg.RestoreDirectory = true;

            if (openFileDlg.ShowDialog() == true) {
                AnalyzeAudio(openFileDlg.FileName);
            }
        }

        private async void btn_run_Click(object sender, RoutedEventArgs e) {
            FillInputAudioList();

            if (App.FFmpegDir != null & App.InpWavDir != "" & App.OutWavDir != "") {
                Mouse.OverrideCursor = Cursors.Wait;

                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;

                IProgress<int> progress = new Progress<int>(p => {
                    AudioProgress.Value = p;
                    AudioProgress.Maximum = FileList.Count;
                    TaskbarItemInfo.ProgressValue = (double)p / FileList.Count;
                });

                int extensionIndex = combo_channels.SelectedIndex;
                int remixIndex = remix_channels.SelectedIndex;
                int? inputLUFS = null;
                if (txtb_loudness.Text != "")
                    inputLUFS = Convert.ToInt32(txtb_loudness.Text);

                await Task.Run(() => ProcessAudio(extensionIndex, remixIndex, inputLUFS, progress));

                Mouse.OverrideCursor = null;

                string message = "Conversion Complete. Open output folder?";
                MessageBoxResult result = MessageBoxDialog.Show(message, this.Title, MessageBoxButton.YesNo, DialogSound.Notify);
                if (result == MessageBoxResult.Yes)
                    Process.Start(App.OutWavDir);

                // Reset Progress Bar
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                AudioProgress.Value = 0;
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e) {
            Regex regex = new Regex("[^0-9-]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void KeyValidationTextBox(object sender, KeyEventArgs e) {
            if (e.Key == Key.Space)
                e.Handled = true;
        }

        private void btn_inputselect_Click(object sender, RoutedEventArgs e) {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.Title = "Select Input Folder";
            dialog.InitialDirectory = App.BaseDir;
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                App.InpWavDir = dialog.FileName;
            FillInputAudioList();
        }

        private void btn_outputselect_Click(object sender, RoutedEventArgs e) {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.Title = "Select Output Folder";
            dialog.InitialDirectory = App.BaseDir;
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                App.OutWavDir = dialog.FileName;
            FillInputAudioList();
        }

        private void btn_clearInput_Click(object sender, RoutedEventArgs e) {
            string message = "Delete Contents of Input Folder?" + Environment.NewLine + "This action is not reversible.";
            MessageBoxResult result = MessageBoxDialog.Show(message, this.Title, MessageBoxButton.YesNo, DialogSound.Notify);
            if (result == MessageBoxResult.Yes) {
                Directory.Delete(App.InpWavDir, true);
                Directory.CreateDirectory(App.InpWavDir);
                FillInputAudioList();
            }
        }

        private void btn_clearOutput_Click(object sender, RoutedEventArgs e) {
            string message = "Delete Contents of Output Folder?" + Environment.NewLine + "This action is not reversible.";
            MessageBoxResult result = MessageBoxDialog.Show(message, this.Title, MessageBoxButton.YesNo, DialogSound.Notify);
            if (result == MessageBoxResult.Yes) {
                Directory.Delete(App.OutWavDir, true);
                Directory.CreateDirectory(App.OutWavDir);
                FillInputAudioList();
            }
        }
    }
}

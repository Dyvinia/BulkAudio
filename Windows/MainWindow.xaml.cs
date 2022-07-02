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
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using DyviniaUtils.Dialogs;
using System.Text;
using System.Collections.Concurrent;

namespace BulkAudio {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        public class FileListItem {
            public string Name { get; set; }
            public string Path { get; set; }
        }
        public ObservableCollection<FileListItem> FileList = new();


        public MainWindow() {
            InitializeComponent();

            MouseDown += (s, e) => FocusManager.SetFocusedElement(this, this);
            RefreshButton.Click += (s, e) => FillAudioList();
            InputOpen.Click += (s, e) => Process.Start(new ProcessStartInfo(Config.Settings.InDir) { UseShellExecute = true });
            OutputOpen.Click += (s, e) => Process.Start(new ProcessStartInfo(Config.Settings.OutDir) { UseShellExecute = true });
            CreditButton.Click += (s, e) => Process.Start(new ProcessStartInfo("https://dyy.vin/twitter") { UseShellExecute = true });

            AudioListBox.ItemsSource = FileList;
            VersionText.Text = App.Version;

            DataContext = Config.Settings;

            FillAudioList();
        }

        public void FillAudioList() {
            Mouse.OverrideCursor = Cursors.Wait;

            FileList.Clear();

            List<string> ext = new() { "wav", "mp3", "ogg", "flac", "m4a" };
            string[] files = Directory.EnumerateFiles(Config.Settings.InDir, "*.*", SearchOption.AllDirectories).Where(s => ext.Contains(Path.GetExtension(s).TrimStart('.').ToLowerInvariant())).ToArray();

            foreach (string file in files)
                FileList.Add(new FileListItem { Name = Path.GetFileNameWithoutExtension(Config.Settings.InDir) + file.Remove(0, Config.Settings.InDir.Length), Path = file });

            Mouse.OverrideCursor = null;
        }

        public void AnalyzeAudio(string filePath) {
            using Process ffmpeg = new();
            Mouse.OverrideCursor = Cursors.Wait;

            ffmpeg.StartInfo.FileName = Config.Settings.UtilsDir + "\\ffmpeg.exe";
            ffmpeg.StartInfo.CreateNoWindow = true;
            ffmpeg.StartInfo.Arguments = $"-y -i \"{filePath}\" -af \"adelay=3s:all=true\",loudnorm=print_format=json -f null -";
            ffmpeg.StartInfo.UseShellExecute = false;
            ffmpeg.StartInfo.RedirectStandardError = true;
            ffmpeg.Start();
            string output = ffmpeg.StandardError.ReadToEnd();
            ffmpeg.WaitForExit();

            output = output[output.IndexOf('{')..];
            dynamic results = JsonConvert.DeserializeObject<dynamic>(output);
            string lufs = results.input_i;
            string truepeak = results.input_tp;

            Mouse.OverrideCursor = null;

            string title = "BulkAudio: Analyze Audio";
            string message = $"Loudness: {lufs}LUFS" + Environment.NewLine + $"True Peak: {truepeak}dB";
            MessageBoxDialog.Show(message, title, MessageBoxButton.OK, DialogSound.Notify);
        }

        private async Task ProcessAudio(int extensionIndex, int remixIndex, int? inputLUFS, IProgress<int> progress) {
            int currentProgress = 1;
            progress.Report(currentProgress);
            ConcurrentBag<string> log = new();

            await Task.Run(async () => {
                Parallel.ForEach(FileList, soundInput => {
                    string outFile = soundInput.Path.Replace(Config.Settings.InDir, Config.Settings.OutDir);

                    if (extensionIndex == 1) outFile = Path.ChangeExtension(outFile, ".wav");
                    if (extensionIndex == 2) outFile = Path.ChangeExtension(outFile, ".mp3");
                    if (extensionIndex == 3) outFile = Path.ChangeExtension(outFile, ".flac");
                    if (extensionIndex == 4) outFile = Path.ChangeExtension(outFile, ".ogg");

                    string remix = "";
                    if (remixIndex == 1) remix = "-ac 1 ";
                    if (remixIndex == 2) remix = "-ac 2 ";

                    Directory.CreateDirectory(Path.GetDirectoryName(outFile));

                    //FFMpeg
                    using Process ffmpeg = new();
                    ffmpeg.StartInfo.FileName = Config.Settings.UtilsDir + "\\ffmpeg.exe";
                    ffmpeg.StartInfo.CreateNoWindow = true;
                    ffmpeg.StartInfo.UseShellExecute = false;
                    ffmpeg.StartInfo.RedirectStandardError = true;
                    string segmentvol = "";

                    if (inputLUFS != null && (inputLUFS > -71 & inputLUFS < -4)) {
                        ffmpeg.StartInfo.Arguments = $"-y -i \"{soundInput.Path}\" -af \"adelay=3s:all=true\",loudnorm=print_format=json -f null -";
                        log.Add($"> ffmpeg {ffmpeg.StartInfo.Arguments}");
                        ffmpeg.Start();
                        string output = ffmpeg.StandardError.ReadToEnd();
                        ffmpeg.WaitForExit();

                        output = output[output.IndexOf('{')..];
                        dynamic results = JsonConvert.DeserializeObject<dynamic>(output);
                        float lufs = (float)results.input_i;
                        string volume = (Math.Pow(10, (-(lufs - (Convert.ToInt32(inputLUFS))) / 20))).ToString();
                        segmentvol = $"-af \"volume = {volume}\" ";
                    }

                    ffmpeg.StartInfo.Arguments = $"-y -i \"{soundInput.Path}\" {segmentvol}{remix}\"{outFile}\"";
                    log.Add($"> ffmpeg {ffmpeg.StartInfo.Arguments}");
                    ffmpeg.Start();
                    ffmpeg.WaitForExit();

                    progress.Report(currentProgress++);
                });
                await Task.Delay(200);
            });

            //Save output
            File.WriteAllText(Config.Settings.UtilsDir + "\\log.txt", String.Join(Environment.NewLine, log));
        }

        private async void ProcessAudio_Click(object sender, RoutedEventArgs e) {
            FillAudioList();

            if ((Config.Settings.UtilsDir + "\\ffmpeg.exe") != null & Config.Settings.InDir != "" & Config.Settings.OutDir != "") {
                Mouse.OverrideCursor = Cursors.Wait;

                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;

                IProgress<int> progress = new Progress<int>(p => {
                    AudioProgress.Value = p;
                    AudioProgress.Maximum = FileList.Count;
                    TaskbarItemInfo.ProgressValue = (double)p / FileList.Count;
                });

                int extensionIndex = ExtensionComboBox.SelectedIndex;
                int remixIndex = RemixComboBox.SelectedIndex;
                int? inputLUFS = null;
                if (LoudnessInput.Text != "")
                    inputLUFS = Convert.ToInt32(LoudnessInput.Text);

                await ProcessAudio(extensionIndex, remixIndex, inputLUFS, progress);

                Mouse.OverrideCursor = null;
                SystemSounds.Exclamation.Play();

                Process.Start(new ProcessStartInfo(Config.Settings.OutDir) { UseShellExecute = true });

                // Reset Progress Bar
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                AudioProgress.Value = 0;
            }
        }

        private void PlayAudio_Click(object sender, RoutedEventArgs e) {
            FileListItem file = ((Button)sender).DataContext as FileListItem;
            Process.Start(new ProcessStartInfo(file.Path) { UseShellExecute = true });
        }

        private void OpenAudioFolder_Click(object sender, RoutedEventArgs e) {
            FileListItem file = ((Button)sender).DataContext as FileListItem;
            Process.Start(new ProcessStartInfo("explorer.exe", " /select, " + file.Path) { UseShellExecute = true });
        }

        private void AnalyzeAudio_Click(object sender, RoutedEventArgs e) {
            FileListItem file = ((Button)sender).DataContext as FileListItem;
            AnalyzeAudio(file.Path);
        }

        private void AnalyzeButton_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog openFileDlg = new();
            openFileDlg.Filter = "Audio (*.wav, *.mp3, *.ogg, *.flac) |*.wav;*.mp3;*.ogg;*.flac";
            openFileDlg.FilterIndex = 2;
            openFileDlg.RestoreDirectory = true;

            if (openFileDlg.ShowDialog() == true) {
                AnalyzeAudio(openFileDlg.FileName);
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e) {
            Regex regex = new("[^0-9-]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void KeyValidationTextBox(object sender, KeyEventArgs e) {
            if (e.Key == Key.Space)
                e.Handled = true;
        }

        private void InputSelect_Click(object sender, RoutedEventArgs e) {
            CommonOpenFileDialog dialog = new() {
                Title = "Select Input Folder",
                InitialDirectory = App.BaseDir,
                IsFolderPicker = true
            };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                Config.Settings.InDir = dialog.FileName;
            FillAudioList();
        }

        private void OutputSelect_Click(object sender, RoutedEventArgs e) {
            CommonOpenFileDialog dialog = new() {
                Title = "Select Output Folder",
                InitialDirectory = App.BaseDir,
                IsFolderPicker = true
            };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                Config.Settings.OutDir = dialog.FileName;
            FillAudioList();
        }

        private void ClearInput_Click(object sender, RoutedEventArgs e) {
            string message = "Delete Contents of Input Folder?" + Environment.NewLine + "This action is not reversible.";
            MessageBoxResult result = MessageBoxDialog.Show(message, this.Title, MessageBoxButton.YesNo, DialogSound.Notify);
            if (result == MessageBoxResult.Yes) {
                Directory.Delete(Config.Settings.InDir, true);
                Directory.CreateDirectory(Config.Settings.InDir);
                FillAudioList();
            }
        }

        private void ClearOutput_Click(object sender, RoutedEventArgs e) {
            string message = "Delete Contents of Output Folder?" + Environment.NewLine + "This action is not reversible.";
            MessageBoxResult result = MessageBoxDialog.Show(message, this.Title, MessageBoxButton.YesNo, DialogSound.Notify);
            if (result == MessageBoxResult.Yes) {
                Directory.Delete(Config.Settings.OutDir, true);
                Directory.CreateDirectory(Config.Settings.OutDir);
                FillAudioList();
            }
        }
    }
}

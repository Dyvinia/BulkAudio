using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BulkAudio {

    public class FileListItem {
        public string Name { get; set; }
        public string Path { get; set; }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public static string FFmpegDir = null;
        public string inpWavDir;
        public string outWavDir;
        List<FileListItem> fileList = new List<FileListItem>();

        public MainWindow() {
            InitializeComponent();

            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString().Substring(0, 5);
            txt_Version.Text = "v" + version;

            audioListBox.ItemsSource = fileList;

            Directory.CreateDirectory(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\Input");
            inpWavDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\Input";
            Directory.CreateDirectory(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\Output");
            outWavDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\Output";

            FFmpegDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\tools\\ffmpeg.exe";

            fillInputAudioList();
        }

        public void fillInputAudioList() {
            Mouse.OverrideCursor = Cursors.Wait;

            fileList.Clear();
            txtb_inputpath.Text = inpWavDir;
            txtb_outputpath.Text = outWavDir;

            var ext = new List<string> { "wav", "mp3", "ogg", "flac", "m4a" };
            string[] files = Directory.EnumerateFiles(inpWavDir, "*.*", SearchOption.AllDirectories).Where(s => ext.Contains(Path.GetExtension(s).TrimStart('.').ToLowerInvariant())).ToArray();

            foreach (string file in files) {
                fileList.Add(new FileListItem { Name = Path.GetFileNameWithoutExtension(inpWavDir) + file.Remove(0, inpWavDir.Length), Path = file });
            }

            Mouse.OverrideCursor = null;
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e) {
            Regex regex = new Regex("[^0-9-]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void KeyValidationTextBox(object sender, KeyEventArgs e) {
            if (e.Key == Key.Space) e.Handled = true;
        }

        private void btn_inputselect_Click(object sender, RoutedEventArgs e) {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.Title = "Select Input Folder";
            dialog.InitialDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok) {
                inpWavDir = dialog.FileName;
            }
            fillInputAudioList();
        }

        private void btn_outputselect_Click(object sender, RoutedEventArgs e) {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.Title = "Select Output Folder";
            dialog.InitialDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok) {
                outWavDir = dialog.FileName;
            }
            fillInputAudioList();
        }

        private void btn_clearInput_Click (object sender, RoutedEventArgs e) {
            string message = "Delete Contents of Input Folder?" + Environment.NewLine + "This action is not reversible.";
            string title = "Delete Contents";
            MessageBoxButton buttons = MessageBoxButton.YesNo;
            MessageBoxImage icon = MessageBoxImage.Warning;
            MessageBoxResult result = MessageBox.Show(message, title, buttons, icon);
            switch (result) {
                case MessageBoxResult.Yes:
                    Directory.Delete(inpWavDir, true);
                    Directory.CreateDirectory(inpWavDir);
                    fillInputAudioList();
                    break;
            }
        }

        private void btn_clearOutput_Click(object sender, RoutedEventArgs e) {
            string message = "Delete Contents of Output Folder?" + Environment.NewLine + "This action is not reversible.";
            string title = "Delete Contents";
            MessageBoxButton buttons = MessageBoxButton.YesNo;
            MessageBoxImage icon = MessageBoxImage.Warning;
            MessageBoxResult result = MessageBox.Show(message, title, buttons, icon);
            switch (result) {
                case MessageBoxResult.Yes:
                    Directory.Delete(outWavDir, true);
                    Directory.CreateDirectory(outWavDir);
                    fillInputAudioList();
                    break;
            }
        }

        public void analyzeAudio(string filePath) {
            using (Process ffmpeg = new Process()) {
                Mouse.OverrideCursor = Cursors.Wait;

                ffmpeg.StartInfo.FileName = MainWindow.FFmpegDir;
                ffmpeg.StartInfo.CreateNoWindow = true;
                ffmpeg.StartInfo.Arguments = "-y -i \"" + filePath + "\" -af \"adelay=3s:all=true\",loudnorm=print_format=json -f null -";
                ffmpeg.StartInfo.UseShellExecute = false;
                ffmpeg.StartInfo.RedirectStandardError = true;
                ffmpeg.Start();
                string output = ffmpeg.StandardError.ReadToEnd();
                ffmpeg.WaitForExit();

                string ffmpegjson = output.Substring(output.IndexOf('{'));
                dynamic results = JsonConvert.DeserializeObject<dynamic>(ffmpegjson);
                string lufs = results.input_i;
                string truepeak = results.input_tp;

                Mouse.OverrideCursor = null;

                string title = "Loudness";
                string message = "Loudness:" + Environment.NewLine + lufs + "LUFS" + Environment.NewLine + Environment.NewLine + "True Peak:" + Environment.NewLine + truepeak + "dB";
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btn_anal_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog openFileDlg = new OpenFileDialog();
            {
                openFileDlg.Filter = "Audio (*.wav, *.mp3, *.ogg, *.flac) |*.wav;*.mp3;*.ogg;*.flac";
                openFileDlg.FilterIndex = 2;
                openFileDlg.RestoreDirectory = true;

                Nullable<bool> result = openFileDlg.ShowDialog();

                if (result == true) {
                    analyzeAudio(openFileDlg.FileName);
                }
            }
        }


        private void btn_run_Click(object sender, RoutedEventArgs e) {
            Directory.CreateDirectory(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\Output");
            fillInputAudioList();
            if (FFmpegDir != null & inpWavDir != "" & outWavDir != "") {
                Mouse.OverrideCursor = Cursors.Wait;
                string logString = null;
                foreach (FileListItem soundInput in fileList) {
                    string outFile = soundInput.Path.Replace(inpWavDir, outWavDir);
                    if (combo_channels.SelectedIndex == 1) outFile = Path.ChangeExtension(outFile, ".wav");
                    if (combo_channels.SelectedIndex == 2) outFile = Path.ChangeExtension(outFile, ".mp3");
                    if (combo_channels.SelectedIndex == 3) outFile = Path.ChangeExtension(outFile, ".flac");
                    if (combo_channels.SelectedIndex == 4) outFile = Path.ChangeExtension(outFile, ".ogg");

                    string remix = "";
                    if (remix_channels.SelectedIndex == 1) remix = "-ac 1 ";
                    if (remix_channels.SelectedIndex == 2) remix = "-ac 2 ";

                    Directory.CreateDirectory(Path.GetDirectoryName(outFile));

                    //FFMpeg
                    using (Process ffmpeg = new Process()) {
                        ffmpeg.StartInfo.FileName = FFmpegDir;
                        ffmpeg.StartInfo.CreateNoWindow = true;
                        ffmpeg.StartInfo.UseShellExecute = false;
                        ffmpeg.StartInfo.RedirectStandardError = true;
                        string segmentvol = "";

                        if (txtb_loudness.Text != "" && (Convert.ToInt32(txtb_loudness.Text) > -71 & Convert.ToInt32(txtb_loudness.Text) < -4)) {
                            ffmpeg.StartInfo.Arguments = "-y -i \"" + soundInput + "\" -af \"adelay=3s:all=true\",loudnorm=print_format=json -f null -";
                            logString += "> ffmpeg " + ffmpeg.StartInfo.Arguments + "\r\n";
                            ffmpeg.Start();
                            string output = ffmpeg.StandardError.ReadToEnd();
                            ffmpeg.WaitForExit();

                            string ffmpegjson = output.Substring(output.IndexOf('{'));
                            dynamic results = JsonConvert.DeserializeObject<dynamic>(ffmpegjson);
                            float lufs = (float)results.input_i;
                            string volume = (Math.Pow(10, (-(lufs - (Convert.ToInt32(txtb_loudness.Text))) / 20))).ToString();
                            segmentvol = "-af \"volume = " + volume + "\" ";
                        }

                        else if (txtb_loudness.Text != "" && !(Convert.ToInt32(txtb_loudness.Text) > -71 & Convert.ToInt32(txtb_loudness.Text) < -4)) {
                            SystemSounds.Hand.Play();
                            Mouse.OverrideCursor = null;
                            return;
                        }

                        ffmpeg.StartInfo.Arguments = "-y -i \"" + soundInput + "\" " + segmentvol + remix + "\"" + outFile + "\"";
                        logString += "> ffmpeg" + ffmpeg.StartInfo.Arguments + "\r\n";
                        ffmpeg.Start();
                        ffmpeg.WaitForExit();


                    }

                    //Save output
                    File.WriteAllText(Path.GetDirectoryName(FFmpegDir) + "\\log.txt", logString);
                }

                Mouse.OverrideCursor = null;

                string message = "Conversion Complete. Open output folder?";
                string title = "Conversion Complete";
                MessageBoxButton buttons = MessageBoxButton.YesNo;
                MessageBoxImage icon = MessageBoxImage.Information;
                MessageBoxResult result = MessageBox.Show(message, title, buttons, icon);
                if (result == MessageBoxResult.Yes) Process.Start(outWavDir);
            }
        }


        private void window_MouseDown(object sender, MouseButtonEventArgs e) {
            Keyboard.ClearFocus();
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
            analyzeAudio(file.Path);
        }

        private void btn_refresh_Click(object sender, RoutedEventArgs e) {
            fillInputAudioList();
        }

        private void btn_inputopen_Click(object sender, RoutedEventArgs e) {
            Process.Start(inpWavDir);
        }

        private void btn_outputopen_Click(object sender, RoutedEventArgs e) {
            Process.Start(outWavDir);
        }

        private void creditButton_Click(object sender, RoutedEventArgs e) {
            Process.Start("https://dyy.vin/twitter");
        }
    }
}

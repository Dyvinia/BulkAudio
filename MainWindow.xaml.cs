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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace BulkAudio {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public static string encoderDir = "";
        public static string FFmpegDir = "";
        public string inpWavDir;
        public string outWavDir;
        List<string> filesToConvert = new List<string>();

        public MainWindow() {
            InitializeComponent();

            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString().Substring(0, 5);
            //txt_Version.Text = "v" + version;

            Directory.CreateDirectory(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\Input");
            Directory.CreateDirectory(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\Output");
            if (File.Exists(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\tools\\ffmpeg.exe")) {
                FFmpegDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\tools\\ffmpeg.exe";
            }
            else {
                string title = "Missing Dependencies";
                string message = "FFmpeg not found in tools folder";
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
            if (Directory.Exists(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\Input")) {
                inpWavDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\Input";
            }
            if (Directory.Exists(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\Output")) {
                outWavDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\Output";
            }

            fillInputAudioList();
        }

        public void fillInputAudioList() {
            Mouse.OverrideCursor = Cursors.Wait;
            if (encoderDir == "") encoderDir = null;
            txtb_inputpath.Text = inpWavDir;
            txtb_outputpath.Text = outWavDir;
            var ext = new List<string> { "wav", "mp3", "ogg", "flac", "m4a" };
            filesToConvert = Directory.EnumerateFiles(inpWavDir, "*.*", SearchOption.AllDirectories).Where(s => ext.Contains(Path.GetExtension(s).TrimStart('.').ToLowerInvariant())).ToList();
            txtb_audiolist.Text = "";
            foreach (string file in filesToConvert) {
                txtb_audiolist.Text += Path.GetFileNameWithoutExtension(inpWavDir) + file.Remove(0, inpWavDir.Length) + "\r\n";
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
            dialog.Title = "Select Profile";
            dialog.InitialDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok) {
                inpWavDir = dialog.FileName;
            }
            fillInputAudioList();
        }

        private void btn_outputselect_Click(object sender, RoutedEventArgs e) {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.Title = "Select Profile";
            dialog.InitialDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok) {
                outWavDir = dialog.FileName;
            }
            fillInputAudioList();
        }

        private void btn_inputopen_Click(object sender, RoutedEventArgs e) {
            Process.Start(inpWavDir);
        }

        private void btn_outputopen_Click(object sender, RoutedEventArgs e) {
            Process.Start(outWavDir);
        }

        private void btn_refresh_Click(object sender, RoutedEventArgs e) {
            fillInputAudioList();
        }

        private void btn_anal_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog openFileDlg = new OpenFileDialog();
            {
                openFileDlg.Filter = "Audio (*.wav, *.mp3, *.ogg, *.flac) |*.wav;*.mp3;*.ogg;*.flac";
                openFileDlg.FilterIndex = 2;
                openFileDlg.RestoreDirectory = true;

                Nullable<bool> result = openFileDlg.ShowDialog();

                if (result == true) {
                    Mouse.OverrideCursor = Cursors.Wait;
                    using (Process ffmpeg = new Process()) {
                        ffmpeg.StartInfo.FileName = MainWindow.FFmpegDir;
                        ffmpeg.StartInfo.CreateNoWindow = true;
                        ffmpeg.StartInfo.Arguments = "-y -i \"" + openFileDlg.FileName + "\" -af \"adelay=3s:all=true\",loudnorm=print_format=json -f null -";
                        ffmpeg.StartInfo.UseShellExecute = false;
                        ffmpeg.StartInfo.RedirectStandardError = true;
                        ffmpeg.Start();
                        string output = ffmpeg.StandardError.ReadToEnd();
                        ffmpeg.WaitForExit();

                        //if (MainWindow.saveanaloutput == true) File.WriteAllText(Path.GetDirectoryName(MainWindow.FFmpegDir) + "\\analysis_output.txt", output);

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
            }
        }


        private void btn_run_Click(object sender, RoutedEventArgs e) {
            Directory.CreateDirectory(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\Output");
            fillInputAudioList();
            if (FFmpegDir != "" & inpWavDir != "" & outWavDir != "") {
                Mouse.OverrideCursor = Cursors.Wait;
                using (Process EncTool = new Process()) {
                    string saveArgs = null;
                    EncTool.StartInfo.FileName = encoderDir;
                    EncTool.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    foreach (string soundInput in filesToConvert) {
                        string outFile = null;
                        outFile = soundInput.Replace(inpWavDir, outWavDir);
                        if (combo_channels.SelectedIndex == 1) outFile = Path.ChangeExtension(outFile, ".wav");
                        if (combo_channels.SelectedIndex == 2) outFile = Path.ChangeExtension(outFile, ".mp3");
                        if (combo_channels.SelectedIndex == 3) outFile = Path.ChangeExtension(outFile, ".flac");
                        if (combo_channels.SelectedIndex == 4) outFile = Path.ChangeExtension(outFile, ".ogg");

                        Directory.CreateDirectory(Path.GetDirectoryName(outFile));

                        //Convert/copy and/or normalize to output directory
                        using (Process ffmpeg = new Process()) {
                            ffmpeg.StartInfo.FileName = FFmpegDir;
                            ffmpeg.StartInfo.CreateNoWindow = true;
                            ffmpeg.StartInfo.UseShellExecute = false;
                            ffmpeg.StartInfo.RedirectStandardError = true;

                            //Normalize and convert
                            string segmentvol = "";
                            if (txtb_loudness.Text != "" && (Convert.ToInt32(txtb_loudness.Text) > -71 & Convert.ToInt32(txtb_loudness.Text) < -4)) {
                                ffmpeg.StartInfo.Arguments = "-y -i \"" + soundInput + "\" -af \"adelay=3s:all=true\",loudnorm=print_format=json -f null -";
                                saveArgs += "> ffmpeg " + ffmpeg.StartInfo.Arguments + "\r\n";
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

                            ffmpeg.StartInfo.Arguments = "-y -i \"" + soundInput + "\" " + segmentvol + "\"" + outFile + "\"";
                            saveArgs += "> ffmpeg" + ffmpeg.StartInfo.Arguments + "\r\n";
                            ffmpeg.Start();
                            ffmpeg.WaitForExit();


                        }

                        //Save output
                        File.WriteAllText(Path.GetDirectoryName(FFmpegDir) + "\\log.txt", saveArgs);
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
        }


        private void window_MouseDown(object sender, MouseButtonEventArgs e) {
            Keyboard.ClearFocus();
        }





    }
}

using System;
using System.IO;
using System.Media;
using System.Windows.Forms;
using NAudio.Wave;
using NAudio.Lame;

namespace ExamRecorder
{
    class Program
    {
        private static NotifyIcon notifyIcon;
        private static bool isRecording = false;
        private static WaveInEvent waveIn;
        private static LameMP3FileWriter mp3Writer;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Create notify icon
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = ExamRecorder.Properties.Resources.Icon;
            notifyIcon.Text = "ExamRecorder";

            // Show notify icon
            notifyIcon.Visible = true;

            // Create context menu
            ContextMenu contextMenu = new ContextMenu();
            MenuItem exitMenuItem = new MenuItem();
            exitMenuItem.Index = 0;
            exitMenuItem.Text = "Exit";
            exitMenuItem.Click += ExitMenuItem_Click;
            contextMenu.MenuItems.AddRange(new MenuItem[] { exitMenuItem });
            notifyIcon.ContextMenu = contextMenu;

            // Create waveIn object
            waveIn = new WaveInEvent();
            waveIn.DataAvailable += WaveIn_DataAvailable;

            // Handle space key press
            KeyboardHook.Initialize();
            KeyboardHook.KeyDown += KeyboardHook_KeyDown;

            // Start message loop
            Application.Run();
        }

        private static void KeyboardHook_KeyDown(Keys key)
        {
            if (key == Keys.Space)
            {
                if (!isRecording)
                {
                    // Start recording
                    isRecording = true;
                    waveIn.StartRecording();
                    PlaySound(ExamRecorder.Properties.Resources.Start);

                    string documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    string fileName = Path.Combine(documentsFolder, "Voice.mp3");
                    int counter = 1;

                    while (File.Exists(fileName))
                    {
                        fileName = Path.Combine(documentsFolder, $"Voice{counter}.mp3");
                        counter++;
                    }

                    // Create mp3 file writer
                    mp3Writer = new LameMP3FileWriter(fileName, waveIn.WaveFormat, LAMEPreset.STANDARD);
                }
                else
                {
                    // Stop recording
                    isRecording = false;
                    waveIn.StopRecording();
                    PlaySound(ExamRecorder.Properties.Resources.End);

                    // Dispose mp3 file writer
                    mp3Writer.Dispose();
                    mp3Writer = null;
                }
            }
        }


        private static void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (mp3Writer != null)
            {
                mp3Writer.Write(e.Buffer, 0, e.BytesRecorded);
            }
        }
        private static void PlaySound(byte[] soundBytes)
        {
            using (var ms = new MemoryStream(soundBytes))
            {
                using (var mp3Reader = new Mp3FileReader(ms))
                {
                    using (var waveStream = WaveFormatConversionStream.CreatePcmStream(mp3Reader))
                    {
                        using (var waveOut = new WaveOutEvent())
                        {
                            waveOut.Init(waveStream);
                            waveOut.Play();
                            while (waveOut.PlaybackState == PlaybackState.Playing)
                            {
                                System.Threading.Thread.Sleep(100);
                            }
                        }
                    }
                }
            }
        }


        private static void ExitMenuItem_Click(object sender, EventArgs e)
        {
            // Dispose waveIn object
            waveIn.Dispose();

            // Remove notify icon
            notifyIcon.Visible = false;
            notifyIcon.Dispose();

            // Exit application
            Application.Exit();
        }
    }
}
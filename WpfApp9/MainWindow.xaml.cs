using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using XInputDotNetPure;
using NAudio;
using NAudio.Wave;
using LiveCharts;
using LiveCharts.Wpf;
using System.Threading;

namespace WpfApp9
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        static WaveInEvent recorder = new WaveInEvent();
        double[] FreqBand = new double[20];


        public MainWindow()
        {
            InitializeComponent();
            recorder.DataAvailable += Recorder_DataAvailable;
            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                string wic = WaveIn.GetCapabilities(i).ProductName;
                ComboBoxItem item = new ComboBoxItem();
                item.Content = wic;
                SoundCardSelect.Items.Add(item);
            }
            SoundCardSelect.SelectionChanged += SoundCardSelect_SelectionChanged;
            SoundCardSelect.SelectedIndex = 0;
            recorder.StartRecording();
            for(int i=0;i<20;i++)   //频谱显示
            {
                ProgressBar bar = new ProgressBar();
                bar.Maximum = 100000;
                bar.Height = 23;
                stackpanel.Children.Add(bar);
            }
            for (int i = 0; i < 20; i++) //权重调整
            {
                Slider bar = new Slider();
                bar.Maximum = 1;
                bar.Value = 0.5;
                bar.Height = 23;
                SliderStackpanel.Children.Add(bar);
            }
        }

        private void SoundCardSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            recorder.DeviceNumber = SoundCardSelect.SelectedIndex ;
        }

        bool Active = false;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Active = true;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Active = false;
            GamePad.SetVibration(PlayerIndex.One, 0.0f, 0.0f);
        }

        double MaxValue = 10;
        private void Recorder_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (!Active) { return; }
            double[] VoiceData = new double[400];
            int cnt = 0;
            byte[] buffer = e.Buffer;
            GamePadState state = GamePad.GetState(PlayerIndex.One);
            byte[] byteTemp = new byte[2];
            for (int i = 0; i < buffer.Length; i += 4)
            {
                for (int c = 1; c >= 0; c--)
                {
                    if (c + i >= buffer.Length)
                    {
                        byteTemp[1 - c] = 0;
                    }
                    byteTemp[c] = buffer[i + c];
                }
                try
                {
                    short a = BitConverter.ToInt16(byteTemp, 0);
                    VoiceData[cnt] = Convert.ToDouble(a);
                    cnt++;
                }
                catch
                {

                }
            }
            double[] fftResult = FFTHelp.fft(VoiceData);

            int lastIndex = 0;
            int SplitArea = 20;
            for (int n = 1; n <= SplitArea; n++)
            {
                int index = Convert.ToInt32(512 * Math.Log(n, SplitArea));
                double BandAver = 0;
                for (int i = lastIndex; i < index; i++)
                {
                    BandAver += fftResult[i];
                }
                BandAver /= ((index - lastIndex) == 0 ? 1 : (index - lastIndex));
                lastIndex = index;
                App.Current.Dispatcher.Invoke(() =>
                {
                    ((ProgressBar)stackpanel.Children[n - 1]).Value = BandAver;
                });
                FreqBand[n - 1] = BandAver;
            }


            double[] freqband =(double[]) FreqBand.Clone();
            App.Current.Dispatcher.Invoke(() =>
            {
                for(int i=0;i<20;i++)
                {
                    freqband[i] *= ((Slider)(SliderStackpanel.Children[i])).Value;
                }
            });
            double max = freqband.Max();
            if (max > MaxValue)
            {
                MaxValue = max;
                App.Current.Dispatcher.Invoke(() =>
                {
                    for (int i = 0; i < 20; i++)
                    {
                        ((ProgressBar)(stackpanel.Children[i])).Maximum= MaxValue;
                    }
                });
            }
            double aver = freqband.Average();


            float Vibration = Convert.ToSingle(aver / (MaxValue * 9 / 10));
            App.Current.Dispatcher.Invoke(() =>
            {
                Vibration *=Convert.ToSingle( MainSlider.Value);
            });
            GamePad.SetVibration(PlayerIndex.One, Vibration, Vibration);
            //DataShow.Content = "" + Convert.ToInt32(MaxValue) + " " + Convert.ToInt32(aver / (MaxValue / 2));
        }
    }
}

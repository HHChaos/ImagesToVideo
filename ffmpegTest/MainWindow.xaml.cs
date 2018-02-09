using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using MessageBox = System.Windows.Forms.MessageBox;

namespace ffmpegTest
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void FilePicker_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Multiselect = false };
            if (dialog.ShowDialog().Equals(System.Windows.Forms.DialogResult.OK))
            {
                TboxFilePath.Text = dialog.FileName;
                
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var audio=FFmpegHelper.ConstructAudio(TboxFFmpeg.Text,
                TboxFilePath.Text, TimeSpan.FromSeconds(int.Parse(TboxDuration.Text)),
                TboxFolderPath.Text + "\\output.mp3");
            if (audio)
            {
                if (FFmpegHelper.ConvertVideo(TboxFFmpeg.Text, TboxPictures.Text, TboxFolderPath.Text + "\\output.mp3",
                    TboxFolderPath.Text + "\\output.mp4"))
                {
                    MessageBox.Show("导出成功，路径：\n"+ TboxFolderPath.Text + "\\output.mp4");
                }
            }
            MessageBox.Show("导出失败！");
        }
    }
}

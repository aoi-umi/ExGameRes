using ExGameRes.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace ExGameRes
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private string FilePath
        {
            get { return PathBox.Text; }
            set
            {
                if (PathBox.Text != value)
                {
                    PathBox.Text = value;
                }
            }
        }
        private string DirPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(DestBox.Text))
                    return FilePath + "~";
                return DestBox.Text;
            }
        }
        private ObservableCollection<Object> FileList
        {
            get
            {
                return mainControl.FileList;
            }
        }

        private BackgroundWorker BgWorker
        {
            get { return mainControl.BgWorker; }
        }
        public MainWindow()
        {
            InitializeComponent();
            Title = $"{Config.Name} -By {Config.Author} -v{Config.Version}";
            ListView.ItemsSource = FileList;

            SetCharset();

            BgWorker.DoWork += DoWork_Handler;
            BgWorker.ProgressChanged += BgWorker_ProgressChanged;
        }

        #region event
        private void PathBox_PreviewDrop(object sender, DragEventArgs e)
        {
            FilePath = ((Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
        }

        private void PathBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.All;
            e.Handled = true;
        }

        private void ListView_KeyUp(object sender, KeyEventArgs e)
        {
            if (BgWorker.IsBusy || e.Key != Key.Delete || ListView.SelectedItems.Count == 0)
                return;

            for (int i = ListView.SelectedItems.Count - 1; i >= 0; i--)
            {
                FileList.RemoveAt(ListView.Items.IndexOf(ListView.SelectedItems[i]));
            }
        }

        private void AnalyseFile_Click(object sender, RoutedEventArgs e)
        {
            Helper.TryHandler(() =>
            {
                if (string.IsNullOrEmpty(FilePath))
                    throw new Exception("文件路径不能为空");
                FileList.Clear();
                mainControl.ProgressBar.Value = 0;
                Analyse();
            });
        }

        private void ExFile_Click(object sender, RoutedEventArgs e)
        {
            Helper.TryHandler(() =>
            {
                Directory.CreateDirectory(DirPath);
                if (!BgWorker.IsBusy)
                {
                    OperationBox.IsEnabled = false;
                    PathInfoModel pim = new PathInfoModel() { DestDirPath = DirPath };
                    BgWorker.RunWorkerAsync(pim);//开始运行DoWork_Handler里的功能
                }
            });
        }

        private void BgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (mainControl.ProgressBar.Value == mainControl.ProgressBar.Maximum)
            {
                OperationBox.IsEnabled = true;
            }
        }

        private void DoWork_Handler(object sender, DoWorkEventArgs args)
        {
            var pi = args.Argument as PathInfoModel;
            if (pi == null || FileList.Count == 0)
                return;

            var bgWorker = sender as BackgroundWorker;
            bgWorker.ReportProgress(0);
            var filePath = (FileList[0] as FileInfoModel).FilePath;
            using (FileStream fs = new FileStream(filePath, FileMode.Open))
            using (BinaryReader br = new BinaryReader(fs))
            {
                int i = 0;
                foreach (FileInfoModel x in FileList)
                {
                    fs.Seek(x.Offset, SeekOrigin.Begin);
                    Helper.WriteBytesToFile(Path.Combine(pi.DestDirPath, x.Filename), br.ReadBytes((int)x.Length));
                    ++i;
                    BgWorker.ReportProgress(i);
                }
            }
            GC.Collect();
        }


        #endregion

        private void SetCharset()
        {
            var list = new List<Object>();
            foreach (EncodingInfo ei in Encoding.GetEncodings())
            {
                Encoding e = ei.GetEncoding();
                list.Add(new MyEncoding(e));
            }
            CharsetBox.ItemsSource = list;
        }

        private void Analyse()
        {
            var fileType = "";
            var ext = Path.GetExtension(FilePath);
            if (string.Compare(ext, "." + Config.Signature.Ald, true) == 0)
            {
                fileType = Config.Signature.Ald;
            }
            else
            {
                using (FileStream fs = new FileStream(FilePath, FileMode.Open))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    var signature = Encoding.Default.GetString(br.ReadBytes(4));
                    if (signature == Config.Signature.AFAH)
                    {
                        fs.Seek(4, SeekOrigin.Current);
                        signature = Encoding.Default.GetString(br.ReadBytes(8));
                        if (signature == Config.Signature.AlicArch)
                        {
                            fileType = Config.Signature.AFA;
                        }
                    }

                }
            }

            var desc = DescView.Text = "";
            if (fileType == Config.Signature.AFA)
            {
                var x = AnalyseAfa();
                desc = string.Join("\r\n", new string[]
                {
                    $"Company: {Config.Signature.AliceSoft}",
                    $"FileType: {Config.Signature.AFA}",
                    $"Version: {x.Version}",
                });
            }
            else if (fileType == Config.Signature.Ald)
            {
                var x = AnalyseAld();
                desc = string.Join("\r\n", new string[]
                {
                    $"Company: {Config.Signature.AliceSoft}",
                    $"FileType: {Config.Signature.Ald}",
                });
            }
            else
            {
                throw new MyException("不支持的文件格式");
            }
            #region 编码
            Encoding encoding = null;
            var selectedItem = CharsetBox.SelectedItem as MyEncoding;
            if (selectedItem != null)
            {
                encoding = selectedItem.encoding;
            }
            else if (!String.IsNullOrWhiteSpace(CharsetBox.Text))
            {
                encoding = Encoding.GetEncoding(CharsetBox.Text.Trim());
            }

            if (encoding != null)
            {
                for (var i = 0; i < FileList.Count; i++)
                {
                    var fileInfo = FileList[i] as FileInfoModel;
                    fileInfo.Filename = Encode(fileInfo.Filename, encoding);
                }
            }
            #endregion
            DescView.Text = desc;
        }

        private AfaArch AnalyseAfa()
        {
            var afaArch = new AfaArch(FilePath);
            for (int i = 0; i < afaArch.EntryList.Count; i++)
            {
                var aliceArchEntryInfo = afaArch.EntryList[i];
                var fileInfo = new FileInfoModel()
                {
                    FilePath = FilePath,
                    Filename = aliceArchEntryInfo.Filename,
                    Offset = afaArch.DataOffset + aliceArchEntryInfo.Offset,
                    Length = aliceArchEntryInfo.Length
                };
                FileList.Add(fileInfo);
            }
            return afaArch;
        }

        private AldArch AnalyseAld()
        {
            var aldArch = new AldArch(FilePath);
            for (int i = 0; i < aldArch.EntryList.Count; i++)
            {
                var aliceArchEntryInfo = aldArch.EntryList[i];
                var fileInfo = new FileInfoModel()
                {
                    FilePath = FilePath,
                    Filename = aliceArchEntryInfo.Filename,
                    Offset = aliceArchEntryInfo.Offset,
                    Length = aliceArchEntryInfo.Length
                };
                FileList.Add(fileInfo);
            }
            return aldArch;
        }

        private string Encode(string str, Encoding encoding)
        {
            Encoding defaultEncoding = Encoding.Default;
            return encoding.GetString(defaultEncoding.GetBytes(str));
        }
    }

    public class FileInfoModel
    {
        public string FilePath;
        public string Filename { get; set; }

        public uint Offset { get; set; }

        public uint Length { get; set; }
    }

    public class PathInfoModel
    {
        public string ResFilePath { get; set; }

        public string DestDirPath { get; set; }
    }

    class MyEncoding
    {
        public Encoding encoding;
        public string Name
        {
            get
            {
                return encoding.EncodingName + "(" + encoding.WebName + ")";
            }
        }
        public MyEncoding(Encoding e)
        {
            encoding = e;
        }
    }
}
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
        public MainWindow()
        {
            InitializeComponent();
            Title = $"{Config.Name} -By {Config.Author} -v{Config.Version}";
            FileList = new ObservableCollection<FileInfoModel>();
            listView.ItemsSource = FileList;
            Binding b = new Binding() { Source = FileList, Path = new PropertyPath("Count") };
            BindingOperations.SetBinding(progressBar, ProgressBar.MaximumProperty, b);

            AddCharset();

            BgWorker.WorkerReportsProgress = true;
            BgWorker.DoWork += DoWork_Handler;
            BgWorker.ProgressChanged += ProgressChanged_Handler;
        }

        private string FilePath
        {
            get { return PathBox.Text; }
            set { if (PathBox.Text != value) { PathBox.Text = value; } }
        }
        private string DirPath
        {
            get { return FilePath + "~"; }
        }
        private ObservableCollection<FileInfoModel> FileList { get; set; }
        private BackgroundWorker BgWorker = new BackgroundWorker();

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

        private void listView_KeyUp(object sender, KeyEventArgs e)
        {
            if (!BgWorker.IsBusy && e.Key == Key.Delete)
            {
                if (listView.SelectedItems.Count > 0)
                {
                    for (int i = listView.SelectedItems.Count - 1; i >= 0; i--)
                    {
                        FileList.RemoveAt(listView.Items.IndexOf(listView.SelectedItems[i]));
                    }
                }
            }
        }

        private void AnalyseFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(FilePath)) throw new Exception("文件路径不能为空");
                FileList.Clear();
                progressBar.Value = 0;
                AnalyseAfa();
            }
            catch (Exception ex)
            {
                Helper.ShowMessage(ex.Message);
            }
        }

        private void ExFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(FilePath)) throw new Exception("文件路径不能为空");
                Directory.CreateDirectory(DirPath);
                if (!BgWorker.IsBusy)
                {
                    OperationBox.IsEnabled = false;
                    PathInfoModel pim = new PathInfoModel() { ResFilePath = FilePath, DestDirPath = DirPath };
                    BgWorker.RunWorkerAsync(pim);//开始运行DoWork_Handler里的功能
                }
            }
            catch (Exception ex)
            {
                Helper.ShowMessage(ex.Message);
            }

        }

        private void DoWork_Handler(object sender, DoWorkEventArgs args)
        {
            BackgroundWorker bgWorker = sender as BackgroundWorker;
            bgWorker.ReportProgress(0);
            PathInfoModel pi = args.Argument as PathInfoModel;
            if (pi != null)
            {
                using (FileStream fs = new FileStream(pi.ResFilePath, FileMode.Open))
                {
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
                }
                bgWorker = null;
                pi = null;
                GC.Collect();
            }
        }

        private void ProgressChanged_Handler(object sender, ProgressChangedEventArgs args)
        {
            //这里更新ui   
            progressBar.Value = args.ProgressPercentage;
            messageView.Text = progressBar.Value.ToString() + "/" + progressBar.Maximum.ToString();
            if (progressBar.Value == progressBar.Maximum)
            {
                OperationBox.IsEnabled = true;
                messageView.Text += " Finished Time:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }
        #endregion

        private void AddCharset()
        {
            var list = new List<Object>();
            foreach (EncodingInfo ei in Encoding.GetEncodings())
            {
                Encoding e = ei.GetEncoding();
                if (true)
                {
                    //Console.Write("{0,-18} ", ei.Name);
                    //Console.Write("{0,-9} ", e.CodePage);
                    //Console.Write("{0,-18} ", e.BodyName);
                    //Console.Write("{0,-18} ", e.HeaderName);
                    //Console.Write("{0,-18} ", e.WebName);
                    //Console.WriteLine("{0} ", e.EncodingName);
                    list.Add(new MyEncoding(e));
                }
            }
            CharsetBox.ItemsSource = list;
        }

        private void AnalyseAfa()
        {
            try
            {
                AliceArch aliceArch;
                using (FileStream fs = new FileStream(FilePath, FileMode.Open))
                {
                    aliceArch = new AliceArch(fs);
                }
                Byte[] outTocBuff = AliceArch.ExtracAliceArch(aliceArch);

                Encoding encoding = null;
                Encoding defaultEncoding = Encoding.Default;
                var selectedItem = CharsetBox.SelectedItem as MyEncoding;
                if (selectedItem != null)
                {
                    encoding = selectedItem.encoding;
                }
                else if (!String.IsNullOrWhiteSpace(CharsetBox.Text))
                {
                    encoding = Encoding.GetEncoding(CharsetBox.Text.Trim());
                }
                #region 获取文件信息
                using (MemoryStream ms = new MemoryStream(outTocBuff))
                {
                    using (BinaryReader br = new BinaryReader(ms))
                    {
                        for (int i = 0; i < aliceArch.EntryCount; i++)
                        {
                            AliceArchEntryInfo aliceArchEntryInfo = new AliceArchEntryInfo(aliceArch.Version, br);
                            FileInfoModel fileInfo = new FileInfoModel()
                            {
                                Filename = aliceArchEntryInfo.Filename,
                                Offset = aliceArch.DataOffset + aliceArchEntryInfo.Offset,
                                Length = aliceArchEntryInfo.Length
                            };

                            if (encoding != null)
                            {
                                fileInfo.Filename = encoding.GetString(defaultEncoding.GetBytes(fileInfo.Filename));
                            }
                            FileList.Add(fileInfo);
                        }
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                Helper.ShowMessage(ex.Message);
            }
        }
    }

    public class FileInfoModel
    {
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
        public MyEncoding(Encoding e) : base()
        {
            encoding = e;
        }
    }
}
using ExGameRes.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;
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

namespace ExGameRes
{
    /// <summary>
    /// Interaction logic for QNTView.xaml
    /// </summary>
    public partial class QNTView : UserControl
    {
        public QNTView()
        {
            InitializeComponent();
            DataContext = BgWorker;
            FileList = new ObservableCollection<QNTFileInfoModel>();
            listView.ItemsSource = FileList;
            Binding b = new Binding() { Source = FileList, Path = new PropertyPath("Count") };
            BindingOperations.SetBinding(progressBar, ProgressBar.MaximumProperty, b);
            BgWorker.WorkerReportsProgress = true;
            BgWorker.DoWork += DoWork_Handler;
            BgWorker.ProgressChanged += ProgressChanged_Handler;
        }

        private string FilePath
        {
            get { return PathBox.Text; }
            set { if (PathBox.Text != value) { PathBox.Text = value; } }
        }

        private bool IsMergeDCF
        {
            get { return (bool)IsMergeDCFBox.IsChecked; }
        }
        private ObservableCollection<QNTFileInfoModel> FileList { get; set; }
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
                    progressBar.Value = 0;
                    for (int i = listView.SelectedItems.Count - 1; i >= 0; i--)
                    {
                        FileList.RemoveAt(listView.Items.IndexOf(listView.SelectedItems[i]));
                    }
                }
            }
        }

        private void listView_Drop(object sender, DragEventArgs e)
        {
            if (isDragingItem)
            {
                isDragingItem = false;
                int index = GetCurrentIndex(listView, e.GetPosition);
                if (listView.SelectedItems.Count > 0 && index >= 0 && index != FileList.IndexOf(listView.SelectedItems[0] as QNTFileInfoModel))
                {
                    foreach (QNTFileInfoModel item in listView.SelectedItems)
                    {
                        FileList.Move(FileList.IndexOf(item), index);
                    }
                }
            }
            else
            {
                progressBar.Value = 0;
                Array list = e.Data.GetData(DataFormats.FileDrop) as Array;
                foreach (var l in list)
                {
                    string s = l as string;
                    if (!string.IsNullOrEmpty(s))
                    {
                        int index = s.LastIndexOf("\\");
                        string path = s.Substring(0, index + 1);
                        string filename = s.Substring(index + 1);
                        AddFileInfo(path, filename);
                    }
                }
            }
        }

        private bool isDragingItem = false;
        private void listView_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragingItem && e.LeftButton == MouseButtonState.Pressed)
            {
                isDragingItem = true;
                DragDrop.DoDragDrop(listView, listView.SelectedItems, DragDropEffects.Move);
            }
        }

        #region BackgroundWorker
        private void DoWork_Handler(object sender, DoWorkEventArgs args)
        {
            BackgroundWorker bgWorker = sender as BackgroundWorker;
            bgWorker.ReportProgress(0);
            bool isMergeDCF = (bool)args.Argument;
            int i = 0;
            Byte[] lastQNTData = null;
            foreach (QNTFileInfoModel qntInfo in FileList)
            {
                try
                {
                    QNT qnt = null;
                    string fullPath = Path.Combine(qntInfo.Path, qntInfo.Filename);
                    string header = Helper.GetHeader(fullPath);
                    Byte[] bmpBuff = null;
                    switch (header.Substring(0, 3))
                    {
                        case Config.Signature.QNT:
                            qnt = new QNT(fullPath);
                            bmpBuff = QNT.ExtractQNT(qnt);
                            if (isMergeDCF) lastQNTData = bmpBuff;
                            break;
                        case Config.Signature.DCF:
                            DCF dcf = new DCF(fullPath);
                            MemoryStream ms = new MemoryStream(dcf.DCGDData);
                            qnt = new QNT(ms);
                            bmpBuff = QNT.ExtractQNT(qnt);
                            if (isMergeDCF && qnt.AlphaTocLength == 0)
                            {
                                if (lastQNTData == null)
                                    throw new MyException("合并源为空");
                                Helper.MergeBMPData(lastQNTData, bmpBuff, dcf.MaskData);
                            }
                            break;
                        default:
                            throw new MyException($"不支持的文件格式:{header}");
                    }
                    Directory.CreateDirectory(qntInfo.NewPath);
                    if (qnt.AlphaTocLength > 0)
                    {
                        qntInfo.NewFilename = Path.Combine(qntInfo.NewPath, qntInfo.FilenameWithoutExt + ".png");
                    }
                    else
                    {
                        qntInfo.NewFilename = Path.Combine(qntInfo.NewPath, qntInfo.FilenameWithoutExt + ".bmp");
                    }
                    Helper.WriteBytesToFile(qntInfo.NewFilename, bmpBuff);
                    qntInfo.Desc = "转换完毕";
                }
                catch (Exception ex)
                {
                    qntInfo.Desc = ex.Message;
                }
                ++i;
                bgWorker.ReportProgress(i);
            }
        }

        private void ProgressChanged_Handler(object sender, ProgressChangedEventArgs args)
        {
            //这里更新ui   
            progressBar.Value = args.ProgressPercentage;
            messageView.Text = $"{progressBar.Value}/{progressBar.Maximum}";
            if (progressBar.Value == progressBar.Maximum)
            {
                OperationBox.IsEnabled = true;
                messageView.Text += " Finished Time:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }
        #endregion BackgroundWorker

        private void GetFile_Click(object sender, RoutedEventArgs e)
        {
            Helper.TryHandler(() =>
            {
                if (string.IsNullOrEmpty(FilePath.Trim()))
                {
                    PathBox.Focus();
                    throw new Exception("请输入路径");
                }
                DirectoryInfo folder = new DirectoryInfo(FilePath);

                foreach (FileInfo CurrFile in folder.GetFiles())
                {
                    string filename = CurrFile.Name;
                    AddFileInfo(FilePath, filename);
                }
            });
        }

        private void ConvertFile_Click(object sender, RoutedEventArgs e)
        {
            if (!BgWorker.IsBusy)
            {
                OperationBox.IsEnabled = false;
                BgWorker.RunWorkerAsync(IsMergeDCF);//开始运行DoWork_Handler里的功能
            }
        }
        #endregion

        private void AddFileInfo(string path, string filename)
        {
            int extIndex = filename.LastIndexOf(".");
            string filenameWithoutExt = extIndex >= 0 ? filename.Substring(0, extIndex) : filename;
            FileList.Add(new QNTFileInfoModel()
            {
                Path = path,
                NewPath = Path.Combine(path, "Output"),
                Filename = filename,
                FilenameWithoutExt = filenameWithoutExt
            });
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            FileList.Clear();
        }

        #region drag listview item
        private delegate Point GetPositionDelegate(IInputElement element);
        private int GetCurrentIndex(ListView lv, GetPositionDelegate getPosition)
        {
            int index = -1;
            for (int i = 0; i < lv.Items.Count; ++i)
            {
                ListViewItem item = lv.ItemContainerGenerator.ContainerFromIndex(i) as ListViewItem;
                if (IsMouseOverTarget(item, getPosition))
                {
                    index = i;
                    break;
                }
            }
            return index;
        }

        private bool IsMouseOverTarget(Visual target, GetPositionDelegate getPosition)
        {
            Rect bounds = VisualTreeHelper.GetDescendantBounds(target);
            Point mousePos = getPosition((IInputElement)target);
            return bounds.Contains(mousePos);
        }
        #endregion
    }

    public class QNTFileInfoModel : INotifyPropertyChanged
    {
        public QNTFileInfoModel()
        {
        }

        public string Path { get; set; }

        public string NewPath { get; set; }

        public string Filename { get; set; }

        public string FilenameWithoutExt { get; set; }

        public string NewFilename
        {
            get { return _NewFilename; }
            set { if (value != _NewFilename) { _NewFilename = value; MyPropertyChanged("NewFilename"); } }
        }

        public string Desc
        {
            get { return _Desc; }
            set { if (value != _Desc) { _Desc = value; MyPropertyChanged("Desc"); } }
        }

        private string _Desc { get; set; }
        private string _NewFilename { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void MyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

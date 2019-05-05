﻿using ExGameRes.Model.AliceSoft;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ExGameRes
{
    /// <summary>
    /// Interaction logic for QNTView.xaml
    /// </summary>
    public partial class QNTView : UserControl
    {
        private string FilePath
        {
            get { return PathBox.Text; }
            set { if (PathBox.Text != value) { PathBox.Text = value; } }
        }

        private bool IsMergeDCF
        {
            get { return (bool)IsMergeDCFBox.IsChecked; }
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
        public QNTView()
        {
            InitializeComponent();
            listView.ItemsSource = FileList;
            BgWorker.DoWork += DoWork_Handler;
            BgWorker.ProgressChanged += ProgressChanged_Handler;
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
            if (BgWorker.IsBusy || e.Key != Key.Delete || listView.SelectedItems.Count == 0)
                return;

            for (int i = listView.SelectedItems.Count - 1; i >= 0; i--)
            {
                FileList.RemoveAt(listView.Items.IndexOf(listView.SelectedItems[i]));
            }
        }

        private void ListView_Drop(object sender, DragEventArgs e)
        {
            if (BgWorker.IsBusy)
                return;
            var format = e.Data.GetFormats();
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
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
            else
            {
                int index = GetCurrentIndex(listView, e.GetPosition);
                if (listView.SelectedItems.Count > 0 && index >= 0 && index != FileList.IndexOf(listView.SelectedItems[0] as ImageFileInfoModel))
                {
                    foreach (ImageFileInfoModel item in listView.SelectedItems)
                    {
                        FileList.Move(FileList.IndexOf(item), index);
                    }
                    listView.SelectedItems.Clear();
                }
            }
        }

        private void ListView_MouseMove(object sender, MouseEventArgs e)
        {
            if (BgWorker.IsBusy || e.LeftButton != MouseButtonState.Pressed)
                return;
            int index = GetCurrentIndex(listView, e.GetPosition);
            if (index >= 0 && listView.SelectedItems.Count > 0)
                DragDrop.DoDragDrop(listView, listView.SelectedItems, DragDropEffects.Move);
        }

        #region BackgroundWorker
        private void DoWork_Handler(object sender, DoWorkEventArgs args)
        {
            BackgroundWorker bgWorker = sender as BackgroundWorker;
            bgWorker.ReportProgress(0);
            bool isMergeDdf = (bool)args.Argument;
            int i = 0;
            Byte[] lastQntData = null;
            foreach (ImageFileInfoModel imageInfo in FileList)
            {
                try
                {
                    string ext = "";
                    string fullPath = Path.Combine(imageInfo.Path, imageInfo.Filename);
                    string header = Helper.GetHeader(fullPath);
                    Byte[] bmpBuff = null;
                    if (header == Config.Signature.QNT)
                    {
                        var qnt = new Qnt(fullPath);
                        bmpBuff = Qnt.ExtractQnt(qnt);
                        if (isMergeDdf) lastQntData = bmpBuff;
                        ext = qnt.Ext;
                    }
                    else if (header == Config.Signature.DCF)
                    {
                        Dcf dcf = new Dcf(fullPath);
                        MemoryStream ms = new MemoryStream(dcf.DCGDData);
                        var qnt = new Qnt(ms);
                        bmpBuff = Qnt.ExtractQnt(qnt);
                        if (isMergeDdf && qnt.AlphaTocLength == 0)
                        {
                            if (lastQntData == null)
                                throw new MyException("合并源为空");
                            Helper.MergeBMPData(lastQntData, bmpBuff, dcf.MaskData);
                        }
                        ext = qnt.Ext;
                    }
                    else if (header == Config.Signature.AJP)
                    {
                        var ajp = new Ajp(fullPath);
                        bmpBuff = Ajp.ExtractAjp(ajp);
                        ext = ajp.Ext;
                    }
                    else
                    {
                        throw new MyException($"不支持的文件格式:{header}");
                    }

                    imageInfo.NewFilename = Path.Combine(imageInfo.NewPath, imageInfo.FilenameWithoutExt + "." + ext);                    
                    Directory.CreateDirectory(imageInfo.NewPath);
                    Helper.WriteBytesToFile(imageInfo.NewFilename, bmpBuff);
                    imageInfo.Desc = "转换完毕";
                }
                catch (Exception ex)
                {
                    imageInfo.Desc = ex.Message;
                }
                ++i;
                bgWorker.ReportProgress(i);
            }
        }

        private void ProgressChanged_Handler(object sender, ProgressChangedEventArgs args)
        {
            //这里更新ui   
            if (mainControl.ProgressBar.Value == mainControl.ProgressBar.Maximum)
            {
                OperationBox.IsEnabled = true;
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
            FileList.Add(new ImageFileInfoModel()
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
            if (target == null)
                return false;
            Rect bounds = VisualTreeHelper.GetDescendantBounds(target);
            Point mousePos = getPosition((IInputElement)target);
            return bounds.Contains(mousePos);
        }
        #endregion
    }

    public class ImageFileInfoModel : INotifyPropertyChanged
    {
        public ImageFileInfoModel()
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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
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

namespace ExGameRes
{
    /// <summary>
    /// 按照步骤 1a 或 1b 操作，然后执行步骤 2 以在 XAML 文件中使用此自定义控件。
    ///
    /// 步骤 1a) 在当前项目中存在的 XAML 文件中使用该自定义控件。
    /// 将此 XmlNamespace 特性添加到要使用该特性的标记文件的根 
    /// 元素中: 
    ///
    ///     xmlns:MyNamespace="clr-namespace:ExGameRes"
    ///
    ///
    /// 步骤 1b) 在其他项目中存在的 XAML 文件中使用该自定义控件。
    /// 将此 XmlNamespace 特性添加到要使用该特性的标记文件的根 
    /// 元素中: 
    ///
    ///     xmlns:MyNamespace="clr-namespace:ExGameRes;assembly=ExGameRes"
    ///
    /// 您还需要添加一个从 XAML 文件所在的项目到此项目的项目引用，
    /// 并重新生成以避免编译错误: 
    ///
    ///     在解决方案资源管理器中右击目标项目，然后依次单击
    ///     “添加引用”->“项目”->[浏览查找并选择此项目]
    ///
    ///
    /// 步骤 2)
    /// 继续操作并在 XAML 文件中使用控件。
    ///
    ///     <MyNamespace:MainControl/>
    ///
    /// </summary>
    public class MainControl : ContentControl
    {
        public BackgroundWorker BgWorker = new BackgroundWorker();
        public ObservableCollection<Object> FileList = new ObservableCollection<Object>();
        static MainControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MainControl), new FrameworkPropertyMetadata(typeof(MainControl)));
        }
        public MainControl()
        {
            BgWorker.WorkerReportsProgress = true;
            BgWorker.ProgressChanged += ProgressChanged_Handler;
        }

        private const string ProgressBarName = "ProgressBar";
        private const string MessageViewName = "MessageView";

        public ProgressBar ProgressBar;
        public TextBlock MessageView;
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            ProgressBar = (ProgressBar)GetTemplateChild(ProgressBarName);
            MessageView = (TextBlock)GetTemplateChild(MessageViewName);

            Binding b = new Binding() { Source = FileList, Path = new PropertyPath("Count") };
            BindingOperations.SetBinding(ProgressBar, ProgressBar.MaximumProperty, b);
        }

        private void ProgressChanged_Handler(object sender, ProgressChangedEventArgs args)
        {
            //这里更新ui   
            ProgressBar.Value = args.ProgressPercentage;
            MessageView.Text = $"{ProgressBar.Value}/{ProgressBar.Maximum}";
            if (ProgressBar.Value == ProgressBar.Maximum)
            {
                MessageView.Text += " Finished Time:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }
    }
}

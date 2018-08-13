using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;

namespace ExGameRes
{
    /// <summary>
    /// AboutView.xaml 的交互逻辑
    /// </summary>
    public partial class AboutView : Window
    {
        public AboutView()
        {
            InitializeComponent();
            VersionView.Text = Config.Version;
            SourceCodeView.Inlines.Add(Config.SourceCode);
            SourceCodeView.NavigateUri = new Uri(Config.SourceCode);
        }

        private void SourceCodeView_Click(object sender, RoutedEventArgs e)
        {
            var link = sender as Hyperlink;
            Process.Start(new ProcessStartInfo(link.NavigateUri.AbsoluteUri));
        }
    }
}

using System;
using System.Windows;

namespace Inmeta.Exception.ReportUI.WPF
{
    /// <summary>
    /// Interaction logic for ReportException.xaml
    /// </summary>
    internal partial class ReportFormUI
    {
        public ReportFormUI()
        {
            InitializeComponent();
            this.versionTb.Text = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
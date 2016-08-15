using System;
using System.Windows;
using Kongsberg.Nemo.ExceptionReporter;

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
            this.versionTb.Content = ExceptionRegistrator.Version;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
using System.Windows;
using System.Windows.Controls;
using Kongsberg.Nemo.ExceptionReporter;

namespace Inmeta.Exception.ReportUI.WPF
{
    /// <summary>
    /// Interaction logic for ReportException.xaml
    /// </summary>
    public partial class ReportFormUI
    {
        public ReportFormUI()
        {
            InitializeComponent();
            versionTb.Content = ExceptionRegistrator.Version;
            btnPost.IsEnabled = false;
            txtGuideLines.Content = GuideLines;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ChkNoDescription_OnClick(object sender, RoutedEventArgs e)
        {
            EnableDisableSend();
        }

        private void EnableDisableSend()
        {
            btnPost.IsEnabled = (chkNoDescription.IsChecked.HasValue && chkNoDescription.IsChecked.Value) || txtDescription.Text.Length > 0;
        }


        private const string GuideLines = "Please help the developers to reproduce and fix this error!\r\n\r\nThe following information is required:\r\n- which action caused this error\r\n- which vessel(s) are inserted into simulation\r\n- which pages or tabs are open now\r\nThank you!";

        private void DescriptionChanged(object sender, TextChangedEventArgs e)
        {
            EnableDisableSend();
            if (txtDescription.Text.Length > 0)
            {
                if (chkNoDescription.IsChecked.HasValue && chkNoDescription.IsChecked.Value)
                {
                    chkNoDescription.IsChecked = false;
                }
                chkNoDescription.IsEnabled = false;
            }
            else if (!chkNoDescription.IsEnabled)
                chkNoDescription.IsEnabled = true;
        }


        // For testing
        public Button BtnPost => btnPost;
        public TextBox TxtDescription => txtDescription;

        public CheckBox ChkOverride => chkNoDescription;

    }
}
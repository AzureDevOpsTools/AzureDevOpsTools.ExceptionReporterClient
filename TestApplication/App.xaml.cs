using System.Windows;
using AzureDevOpsTools.ExceptionReporter;

namespace TestApplication
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            ExceptionRegistrator.Register("TestingExceptionRegistrator");
            ExceptionRegistrator.DoNotSend = true;
        }
       
    }
}

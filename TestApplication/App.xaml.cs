using System.Windows;
using Kongsberg.Nemo.ExceptionReporter;

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

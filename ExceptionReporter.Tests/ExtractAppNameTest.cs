using AzureDevOpsTools.ExceptionReporter;
using NUnit.Framework;

namespace ExceptionReporter.Tests
{
    public class ExtractAppNameTest
    {
        [Test]
        public void ThatWeCanExtractAppNameFromStackTrace()
        {
            var st = @"Application: TestApplication
Exception message: Exception of type 'TestApplication.DeliberateException' was thrown.
Type: TestApplication.DeliberateException
   at TestApplication.MainWindow.Bang(Object sender, RoutedEventArgs e) in g:\Source\Repos\TestApp\ExceptionReporter\TestApplication\MainWindow.xaml.cs:line 22
   at System.Windows.RoutedEventHandlerInfo.InvokeHandler(Object target, RoutedEventArgs routedEventArgs)
   at System.Windows.EventRoute.InvokeHandlersImpl(Object source, RoutedEventArgs args, Boolean reRaised)";
            var sut = new ExtractAppName(st);
            var res = sut.Appname;
            Assert.That(res,Is.EqualTo("TestApplication"));
        }
    }
}

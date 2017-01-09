using Inmeta.Exception.ReportUI.WPF;
using Kongsberg.Nemo.ExceptionReporter;
using NUnit.Framework;

namespace ExceptionReporter.Tests
{
    public class ExceptionReporterTests
    {
        [Test]
        public void CheckVersion()
        {
            var version = ExceptionRegistrator.Version;
            Assert.That(version,Is.EqualTo("1.2.3.4"));
        }


        [Test]
        public void CheckVersionThroughOtherMethod()
        {
            var version = ExceptionRegistrator.FindVersion();
            Assert.That(version, Is.EqualTo("1.2.3.4"));
        }
    }

    [RequiresSTA]
    [Category("UI")]
    public class TestReportFormUI
    {

        [Test]
        public void ThatFormButtonsAreCorrect()
        {
            var form = new ReportFormUI();
            Assert.That(form.BtnPost.IsEnabled,Is.False,"default wrong");
            form.TxtDescription.Text = "SomeText";
            Assert.That(form.BtnPost.IsEnabled,Is.True,"Changing text should enable it");

        }

        [Test]
        public void ThatFormOverridesAreCorrect()
        {
            var form = new ReportFormUI();
            Assert.That(form.BtnPost.IsEnabled, Is.False, "default wrong");
            form.ChkOverride.IsChecked = true; 
            Assert.That(form.BtnPost.IsEnabled, Is.True, "Changing override  should enable it");

        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            var version = ExceptionRegistrator.FindVersion;
            Assert.That(version, Is.EqualTo("1.2.3.4"));
        }
    }
}

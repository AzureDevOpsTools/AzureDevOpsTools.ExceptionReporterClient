using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Kongsberg.Nemo.ExceptionReporter;
using Kongsberg.Nemo.ExceptionReporter.TFSExceptionService;
using log4net;
using log4net.Repository.Hierarchy;
using System.Xml;
using log4net.Config;
using Osiris.Exception.Reporter;
using System.Reflection;
using Kongsberg.Nemo.ExceptionReporter.Properties;

namespace Inmeta.Exception.Reporter
{
    internal static class ReportLogger
    {
        private const string XMLReporterLogger = "XMLExceptionReporter";
        private const string ReporterLogger = "ExceptionReporter";
        private const string ReportFailedLogger = "ExceptionReporterFailed";
        private const string MyRepoName = "MyRepo";

        private static readonly string XMLConfig = @"  <log4net>" + Environment.NewLine +
                                    @"    <appender name=""XMLReporterRollingFileAppender"" " +
                                    @"type=""log4net.Appender.RollingFileAppender"">" + Environment.NewLine +
                                    @"      <file type=""log4net.Util.PatternString"">" + Environment.NewLine +
                                    @"        <converter>" + Environment.NewLine +
                                    @"          <name value=""folder""/>" + Environment.NewLine +
                                    @"          <type value=""Kongsberg.Nemo.ExceptionReporter.FilePatternConverter,Kongsberg.Nemo.ExceptionReporter"" />" + Environment.NewLine +
                                    @"        </converter>" + Environment.NewLine +
                                    @"        <conversionPattern value=""%folder\XMLExceptionReporter.log"" />" + Environment.NewLine +
                                    @"      </file>" + Environment.NewLine +
                                    @"      <appendToFile value=""true"" />" + Environment.NewLine +
                                    @"      <rollingStyle value=""Size"" />" + Environment.NewLine +
                                    @"      <maximumFileSize value=""1000KB"" />" + Environment.NewLine +
                                    @"      <maxSizeRollBackups value=""5"" />" + Environment.NewLine +
                                    @"      <layout type=""log4net.Layout.PatternLayout"">" + Environment.NewLine +
                                    @"        <conversionPattern value=""%message%newline"" />        " + Environment.NewLine +
                                    @"      </layout>" + Environment.NewLine +
                                    @"    </appender>" + Environment.NewLine +
                                    @"    <appender name=""ExceptionReporterRollingFileAppender"" " +
                                    @"type=""log4net.Appender.RollingFileAppender"">" + Environment.NewLine +
                                    @"      <file type=""log4net.Util.PatternString"">" + Environment.NewLine +
                                    @"        <converter>" + Environment.NewLine +
                                    @"          <name value=""folder""/>" + Environment.NewLine +
                                    @"          <type value=""Kongsberg.Nemo.ExceptionReporter.FilePatternConverter,Kongsberg.Nemo.ExceptionReporter"" />" + Environment.NewLine +
                                    @"        </converter>" + Environment.NewLine +
                                    @"        <conversionPattern value=""%folder\ExceptionReporter.log"" />" + Environment.NewLine +
                                    @"      </file>" + Environment.NewLine +
                                    @"      <appendToFile value=""true"" />" + Environment.NewLine +
                                    @"      <rollingStyle value=""Size"" />" + Environment.NewLine +
                                    @"      <maximumFileSize value=""500KB"" />" + Environment.NewLine +
                                    @"      <maxSizeRollBackups value=""4"" />" + Environment.NewLine +
                                    @"      <layout type=""log4net.Layout.PatternLayout"">" + Environment.NewLine +
                                    @"        <conversionPattern value=""%date [%thread] %-5level %logger - %message%newline"" />        " + Environment.NewLine +
                                    @"      </layout>" + Environment.NewLine +
                                    @"    </appender>" + Environment.NewLine +
                                    @"    <appender name=""ExceptionReporterFailedReportRollingFileAppender"" " +
                                    @"type=""log4net.Appender.RollingFileAppender"">" + Environment.NewLine +
                                    @"      <file type=""log4net.Util.PatternString"">" + Environment.NewLine +
                                    @"        <converter>" + Environment.NewLine +
                                    @"          <name value=""folder""/>" + Environment.NewLine +
                                    @"          <type value=""Kongsberg.Nemo.ExceptionReporter.FilePatternConverter,Kongsberg.Nemo.ExceptionReporter"" />" + Environment.NewLine +
                                    @"        </converter>" + Environment.NewLine +
                                    @"        <conversionPattern value=""%folder\ExceptionReporterFailedDelivery.log"" />" + Environment.NewLine +
                                    @"      </file>" + Environment.NewLine +
                                    @"      <appendToFile value=""true"" />" + Environment.NewLine +
                                    @"      <rollingStyle value=""Size"" />" + Environment.NewLine +
                                    @"      <maximumFileSize value=""500KB"" />" + Environment.NewLine +
                                    @"      <maxSizeRollBackups value=""4"" />" + Environment.NewLine +
                                    @"      <layout type=""log4net.Layout.PatternLayout"">" + Environment.NewLine +
                                    @"        <conversionPattern value=""%date [%thread] %-5level %logger - %message%newline"" />" + Environment.NewLine +
                                    @"      </layout>" + Environment.NewLine +
                                    @"    </appender>" + Environment.NewLine +
                                    Environment.NewLine +
                                    @"    <logger name=""ExceptionReporter"">" + Environment.NewLine +
                                    @"      <appender-ref ref=""ExceptionReporterRollingFileAppender"" />" + Environment.NewLine +
                                    @"      <level value=""INFO"" />" + Environment.NewLine +
                                    @"    </logger>" + Environment.NewLine +
                                    Environment.NewLine +
                                    @"    <logger name=""ExceptionReporterFailed"">" + Environment.NewLine +
                                    @"      <appender-ref ref=""ExceptionReporterFailedReportRollingFileAppender"" />" + Environment.NewLine +
                                    @"      <level value=""INFO"" />" + Environment.NewLine +
                                    @"    </logger>" + Environment.NewLine +
                                    @"    " + Environment.NewLine +
                                    @"    <logger name=""XMLExceptionReporter"">" + Environment.NewLine +
                                    @"      <appender-ref ref=""XMLReporterRollingFileAppender"" />" + Environment.NewLine +
                                    @"      <level value=""INFO"" />" + Environment.NewLine +
                                    @"    </logger>" + Environment.NewLine +
                                    @"    " + Environment.NewLine +
                                    @"  </log4net>";

        private static readonly Hierarchy repo = InitRepository();

        private static Hierarchy InitRepository()
        {
            try
            {
                //create our own logging repository, to avoid conflict with other log4net configurations.
                var repository = LogManager.CreateRepository(MyRepoName, typeof(Hierarchy)) as Hierarchy;
                repository.Name = MyRepoName;

                var doc = new XmlDocument();
                doc.LoadXml(XMLConfig);
                XmlConfigurator.Configure(repository, doc.DocumentElement);
                return repository;
            }
            catch (XmlException)
            {
                //failed to init Log4Net logger...
            }

            return null;
        }

        /// <summary>
        /// use this method to log to file when an exception report failed to deliver. 
        /// </summary>
        /// <param name="ex">The exception occured during delivery failure.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public static void LogExceptionsDuringDelivery(System.Exception ex)
        {
            try
            {
                var _log = LogManager.GetLogger(repo.Name, ReportFailedLogger);
                if (_log != null)
                    _log.Error("Failed to deliver TFS Exception", ex);
            }
            catch
            {
                //No more fallback solutions
                //need to catch to avoid circular exception
            }
        }


        /// <summary>
        /// This method will log an TFSExceptionReport to local file.
        /// </summary>
        /// <param name="ex">The TFSExceptionReport being saved to file.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public static void LogToFile(TFSExceptionReport ex)
        {
            try
            {
                //Get exception logger
                var _log = LogManager.GetLogger(repo.Name, ReporterLogger);


                //get exceptionentity
                // ReSharper disable PossibleNullReferenceException
                object exEnt = ex.ExceptionEntity;

                // ReSharper restore PossibleNullReferenceException

                if (_log != null && Settings.Default.LogExceptionReports)
                    _log.Info(exEnt.GetType().
                              GetProperties().Select(
                                  (prop) =>
                                  prop.Name + " = " +
                                  (prop.GetValue(exEnt, BindingFlags.Default, null, null, null) ?? "NO VALUE").ToString())
                              .Aggregate((x, y) => x + System.Environment.NewLine + y));
            }
            catch
            {
                //No more falback solutions. 
                //need to catch to avoid circular exception
            }
    
            LogToFileAsXML(ex);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public static void LogToFileAsXML(TFSExceptionReport ex)
        {
            try
            {
                //Ge{t exception logger
                var _log = LogManager.GetLogger(repo.Name, XMLReporterLogger);

                var builder = new StringBuilder();
                using (var strWrt = new StringWriter(builder))
                {
                    var ser = new XmlSerializer(typeof (ExceptionEntity));
                    var outStr = new StringBuilder();
                    StringWriter mem = null;
                    try
                    {
                        mem = new StringWriter(outStr);
                        using (var writer = new XmlTextWriter(mem))
                        {
                            mem = null;
                            writer.Formatting = Formatting.Indented;
                            ser.Serialize(writer, ex.ExceptionEntity);
                        }
                    }
                    finally 
                    {
                        if (mem != null)
                            mem.Dispose();
                    }
                    
                    _log.Info(outStr.ToString());
                }
            }
            catch
            {
                //No more falback solutions. 
                //need to catch to avoid circular exception
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        internal static void LogInfo(string p)
        {
            try
            {
                //Get exception logger
                LogManager.GetLogger(repo.Name, ReportFailedLogger).Info(p);
            }
            catch
            {
                //No more falback solutions. 
                //need to catch to avoid circular exception
            }
            
        }
    }
}

using System;
using log4net.Util;
using System.IO;
using Microsoft.Win32;

namespace AzureDevOpsTools.ExceptionReporter
{
    /// <inheritdoc />
    /// <summary>
    /// Specialized converter for Log4Net
    /// This class is loaded by reflection in log4net, and stated in the xml config in ReportLogger
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public class FilePatternConverter : PatternConverter
    {
        /// <inheritdoc />
        protected override void Convert(TextWriter writer, object state)
        {
            writer.Write(Path);
        }
        /// <summary>
        /// Location for storage of log files
        /// </summary>
        public static string Path
        {
            get
            {
                var localMachine = Registry.LocalMachine;
                const string keypath = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Shell Folders";
             
                //default location to 
                var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                
                //override with Registry settings if available.
                var key = localMachine.OpenSubKey(keypath);

                if (key?.GetValue("Common AppData") != null)
                    path = key.GetValue("Common AppData").ToString();
                
                //append AzureDevOpsTools\ExceptionReporter to seperate from other logs.
                path = System.IO.Path.Combine(path, @"AzureDevOpsTools\ExceptionReporter\");
                return path;
            }
        }
    }
}

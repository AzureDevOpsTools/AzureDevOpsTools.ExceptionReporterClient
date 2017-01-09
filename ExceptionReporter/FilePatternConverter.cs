using System;
using log4net.Util;
using System.IO;
using Microsoft.Win32;

namespace Kongsberg.Nemo.ExceptionReporter
{
    public class FilePatternConverter : PatternConverter
    {
        protected override void Convert(TextWriter writer, object state)
        {
            writer.Write(Path);
        }

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
                
                //append Kongsberg\ExceptionReporter to seperate from other logs.
                path = System.IO.Path.Combine(path, @"Kongsberg\ExceptionReporter\");
                return path;
            }
        }
    }
}

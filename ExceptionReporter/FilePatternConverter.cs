using System;
using log4net.Util;
using System.IO;
using Microsoft.Win32;

namespace Kongsberg.Nemo.ExceptionReporter
{
    public class FilePatternConverter : PatternConverter
    {
        override protected void Convert(TextWriter writer, object state)
        {
            writer.Write(Path);
        }

        public static string Path
        {
            get
            {
                RegistryKey localMachine = Registry.LocalMachine;
                const string keypath = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Shell Folders";
             
                //default location to 
                var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                
                //override with Registry settings if available.
                RegistryKey key = localMachine.OpenSubKey(keypath);

                if (key != null && key.GetValue("Common AppData") != null)
                    path = key.GetValue("Common AppData").ToString();
                
                //append Kongsberg\ExceptionReporter to seperate from other logs.
                path = System.IO.Path.Combine(path, @"Kongsberg\ExceptionReporter\");
                return path;
            }
        }
    }
}

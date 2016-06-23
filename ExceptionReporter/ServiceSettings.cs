using System;

namespace Osiris.Exception.Reporter
{
    /// <summary>
    /// 
    /// This class contains setting for connecting to the exception service.
    /// This file is replaced with the appropriate file during build, when building for test/production.
    /// 
    /// This sort of thing is usually handled with separate app.config files, but it is preferred
    /// for this project to produce an independent DLL, with the correct settings compiled in.
    /// 
    /// </summary>
    public static class ServiceSettings
    {
        public static Uri ServiceUrl { get; set; } = new Uri("http://exceptions.km.kongsberg.com/web/service.asmx");
    }
}

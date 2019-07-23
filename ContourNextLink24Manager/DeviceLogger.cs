using Device.Net;
using log4net;
using System;
using System.Diagnostics;

namespace WindowsUploader.ContourNextLink24Manager
{
    public class DeviceLogger : ILogger
    {
        private static readonly ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public void Log(string message, string region, Exception ex, LogLevel logLevel)
        {
            try 
            { 
                if (string.IsNullOrEmpty(region))
                    region = "global";

                var msg = $"{region} - {message}";

                if (ex != null)
                {
                    _log.Error(msg, ex);
                    return;
                }

                switch(logLevel)
                {
                    case LogLevel.Debug : _log.Debug(msg); break;
                    case LogLevel.Information : _log.Info(msg); break;
                    case LogLevel.Warning : _log.Warn(msg); break;
                    case LogLevel.Error : _log.Error(msg); break;
                }
            } catch (Exception x)
            {
                // logging should not break the app.
                var msg = x.Message;
                Debugger.Break();
            }
        }
    }
}

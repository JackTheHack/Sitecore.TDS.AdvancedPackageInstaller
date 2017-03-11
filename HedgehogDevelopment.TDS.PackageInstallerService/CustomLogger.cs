using System;
using log4net;
using log4net.spi;
using Microsoft.AspNet.SignalR.Client;

namespace HedgehogDevelopment.TDS.PackageInstallerService
{
    public class CustomLogger : ILog
    {
        private readonly ILog _log;
        private readonly IHubProxy _proxy;

        public CustomLogger(ILog baseLogger, IHubProxy hubProxy)
        {
            _log = baseLogger;
            _proxy = hubProxy;
        }

        public ILogger Logger => _log.Logger;

        private void SendLog(object message, string level)
        {
            try
            {
                _proxy.Invoke("Send", message, level);
            }
            catch (Exception e)
            {
                // ignored
            }
        }

        public void Debug(object message)
        {
            _log.Debug(message);
            SendLog(message, "Debug");
        }

        public void Debug(object message, Exception t)
        {
            _log.Debug(message);
            SendLog(message, "Debug");
        }

        public void Info(object message)
        {
            _log.Info(message);
            SendLog(message, "Info");
        }

        public void Info(object message, Exception t)
        {
            _log.Info(message,t);
            SendLog(message, "Info");
        }

        public void Warn(object message)
        {
            _log.Warn(message);
            SendLog(message, "Warn");
        }

        public void Warn(object message, Exception t)
        {
            _log.Warn(message, t);
            SendLog(message, "Warn");
        }

        public void Error(object message)
        {
            _log.Error(message);
            SendLog(message, "Error");
        }

        public void Error(object message, Exception t)
        {
            _log.Error(message, t);
            SendLog(message, "Error");
        }

        public void Fatal(object message)
        {
            _log.Fatal(message);
            SendLog(message, "Fatal");
        }

        public void Fatal(object message, Exception t)
        {
            _log.Fatal(message, t);
            SendLog(message, "Fatal");
        }

        public bool IsDebugEnabled => _log.IsDebugEnabled;
        public bool IsInfoEnabled => _log.IsInfoEnabled;
        public bool IsWarnEnabled => _log.IsWarnEnabled;
        public bool IsErrorEnabled => _log.IsErrorEnabled;
        public bool IsFatalEnabled => _log.IsFatalEnabled;
    }
}

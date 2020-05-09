using Akka.Event;
using System;

namespace Signer.Application.Impl.Actors.Akka
{
    internal sealed class ToVoidLogger : ILoggingAdapter
    {
        public bool IsDebugEnabled => false;

        public bool IsInfoEnabled => false;

        public bool IsWarningEnabled => false;

        public bool IsErrorEnabled => false;

        public void Debug(string format, params object[] args)
        {
        }

        public void Debug(Exception cause, string format, params object[] args)
        {
        }

        public void Error(string format, params object[] args)
        {
        }

        public void Error(Exception cause, string format, params object[] args)
        {
        }

        public void Info(string format, params object[] args)
        {
        }

        public void Info(Exception cause, string format, params object[] args)
        {
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.DebugLevel => false,
                LogLevel.InfoLevel => false,
                LogLevel.WarningLevel => false,
                LogLevel.ErrorLevel => false,

                _ => false
            };
        }

        public void Log(LogLevel logLevel, string format, params object[] args)
        {
        }

        public void Log(LogLevel logLevel, Exception cause, string format, params object[] args)
        {
        }

        public void Warning(string format, params object[] args)
        {
        }

        public void Warning(Exception cause, string format, params object[] args)
        {
        }
    }
}

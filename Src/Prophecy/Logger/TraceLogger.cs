/* **********************************************************************************
 * Copyright (c) 2011 John Hughes
 *
 * Prophecy is licenced under the Microsoft Reciprocal License (Ms-RL).
 *
 * Project Website: http://prophecy.codeplex.com/
 * **********************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Prophecy.Logger
{
    [Serializable]
    public enum LoggerVerbosity
    {
        /// <summary>
        /// Fatal will be logged.
        /// </summary>
        Quiet = 5,
        /// <summary>
        /// Warning, Error and Fatal events will be logged.
        /// </summary>
        Minimal = 3,
        /// <summary>
        /// SystemMessage, Warning, Error, and Fatal events will be logged.
        /// </summary>
        Normal = 2,
        /// <summary>
        /// Information, SystemMessage, Warning, Error, and Fatal events will be logged.
        /// </summary>
        Detailed = 1,
        /// <summary>
        /// All events will be logged.
        /// </summary>
        Diagnostic = 0 // Fatal, Errors, Warnings, SystemMessage, Information, Debug
    }

    /// <summary>
    /// Logs to System.Diagnostics.Trace.
    /// </summary>
    public class TraceLogger : ILogger
    {
        LoggerVerbosity _loggingVerbosity;

        /// <summary>
        /// Instantiates an ILogger with the specified contect type, context and verbosity.
        /// </summary>
        /// <param name="contextType"></param>
        /// <param name="context"></param>
        /// <param name="loggingVerbosity"></param>
        public TraceLogger(LoggerVerbosity loggingVerbosity)
        {
            _loggingVerbosity = loggingVerbosity;
        }

        #region message
        /// <summary>
        /// Logs a message to the system log with the Debug severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        public void Debug(object message)
        {
            Log(message, LoggerSeverity.Debug);
        }
        /// <summary>
        /// Logs a message to the system log with the Information severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        public void Info(object message)
        {
            Log(message, LoggerSeverity.Information);
        }
        /// <summary>
        /// Logs a message to the system log with the SystemMessage severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        public void SystemMsg(object message)
        {
            Log(message, LoggerSeverity.SystemMessage);
        }
        /// <summary>
        /// Logs a message to the system log with the Warning severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        public void Warning(object message)
        {
            Log(message, LoggerSeverity.Warning);
        }
        /// <summary>
        /// Logs a message to the system log with the Error severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        public void Error(object message)
        {
            Log(message, LoggerSeverity.Error);
        }
        /// <summary>
        /// Logs a message to the system log with the Fatal severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        public void Fatal(object message)
        {
            Log(message, LoggerSeverity.Fatal);
        }
        #endregion

        #region message, byte[]
        /// <summary>
        /// Logs a message to the system log with the Debug severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        public void Debug(object message, byte[] data)
        {
            Log(message, data, LoggerSeverity.Debug);
        }
        /// <summary>
        /// Logs a message to the system log with the Information severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        public void Info(object message, byte[] data)
        {
            Log(message, data, LoggerSeverity.Information);
        }
        /// <summary>
        /// Logs a message to the system log with the SystemMessage severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        public void SystemMsg(object message, byte[] data)
        {
            Log(message, data, LoggerSeverity.SystemMessage);
        }
        /// <summary>
        /// Logs a message to the system log with the Warning severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        public void Warning(object message, byte[] data)
        {
            Log(message, data, LoggerSeverity.Warning);
        }
        /// <summary>
        /// Logs a message to the system log with the Error severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        public void Error(object message, byte[] data)
        {
            Log(message, data, LoggerSeverity.Error);
        }
        /// <summary>
        /// Logs a message to the system log with the Fatal severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        public void Fatal(object message, byte[] data)
        {
            Log(message, data, LoggerSeverity.Fatal);
        }
        #endregion

        #region message, byte[], startIndex, length
        /// <summary>
        /// Logs a message to the system log with the Debug severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        /// <param name="startIndex">The start index in data to be included in the log.</param>
        /// <param name="length">The length in data to be included in the log.</param>
        public void Debug(object message, byte[] data, int startIndex, int length)
        {
            Log(message, data, startIndex, length, LoggerSeverity.Debug);
        }
        /// <summary>
        /// Logs a message to the system log with the Information severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        /// <param name="startIndex">The start index in data to be included in the log.</param>
        /// <param name="length">The length in data to be included in the log.</param>
        public void Info(object message, byte[] data, int startIndex, int length)
        {
            Log(message, data, startIndex, length, LoggerSeverity.Information);
        }
        /// <summary>
        /// Logs a message to the system log with the SystemMessage severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        /// <param name="startIndex">The start index in data to be included in the log.</param>
        /// <param name="length">The length in data to be included in the log.</param>
        public void SystemMsg(object message, byte[] data, int startIndex, int length)
        {
            Log(message, data, startIndex, length, LoggerSeverity.SystemMessage);
        }
        /// <summary>
        /// Logs a message to the system log with the Warning severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        /// <param name="startIndex">The start index in data to be included in the log.</param>
        /// <param name="length">The length in data to be included in the log.</param>
        public void Warning(object message, byte[] data, int startIndex, int length)
        {
            Log(message, data, startIndex, length, LoggerSeverity.Warning);
        }
        /// <summary>
        /// Logs a message to the system log with the Error severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        /// <param name="startIndex">The start index in data to be included in the log.</param>
        /// <param name="length">The length in data to be included in the log.</param>
        public void Error(object message, byte[] data, int startIndex, int length)
        {
            Log(message, data, startIndex, length, LoggerSeverity.Error);
        }
        /// <summary>
        /// Logs a message to the system log with the Fatal severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        /// <param name="startIndex">The start index in data to be included in the log.</param>
        /// <param name="length">The length in data to be included in the log.</param>
        public void Fatal(object message, byte[] data, int startIndex, int length)
        {
            Log(message, data, startIndex, length, LoggerSeverity.Fatal);
        }
        #endregion

        #region message, exception
        /// <summary>
        /// Logs a message and exception information to the system log with the Debug severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="ex">The exception to log, including its stack trace.</param>
        public void Debug(object message, Exception ex)
        {
            Log(message, ex, LoggerSeverity.Debug);
        }
        /// <summary>
        /// Logs a message and exception information to the system log with the Information severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="ex">The exception to log, including its stack trace.</param>
        public void Info(object message, Exception ex)
        {
            Log(message, ex, LoggerSeverity.Information);
        }
        /// <summary>
        /// Logs a message and exception information to the system log with the SystemMessage severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="ex">The exception to log, including its stack trace.</param>
        public void SystemMsg(object message, Exception ex)
        {
            Log(message, ex, LoggerSeverity.SystemMessage);
        }
        /// <summary>
        /// Logs a message and exception information to the system log with the Warning severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="ex">The exception to log, including its stack trace.</param>
        public void Warning(object message, Exception ex)
        {
            Log(message, ex, LoggerSeverity.Warning);
        }
        /// <summary>
        /// Logs a message and exception information to the system log with the Error severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="ex">The exception to log, including its stack trace.</param>
        public void Error(object message, Exception ex)
        {
            Log(message, ex, LoggerSeverity.Error);
        }
        /// <summary>
        /// Logs a message and exception information to the system log with the Fatal severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="ex">The exception to log, including its stack trace.</param>
        public void Fatal(object message, Exception ex)
        {
            Log(message, ex, LoggerSeverity.Fatal);
        }
        #endregion

        #region message, byte[], exception
        /// <summary>
        /// Logs a message and exception information to the system log with the Debug severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        /// <param name="ex">The exception to log, including its stack trace.</param>
        public void Debug(object message, byte[] data, Exception ex)
        {
            Log(message, data, ex, LoggerSeverity.Debug);
        }
        /// <summary>
        /// Logs a message and exception information to the system log with the Information severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        /// <param name="ex">The exception to log, including its stack trace.</param>
        public void Info(object message, byte[] data, Exception ex)
        {
            Log(message, data, ex, LoggerSeverity.Information);
        }
        /// <summary>
        /// Logs a message and exception information to the system log with the SystemMessage severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        /// <param name="ex">The exception to log, including its stack trace.</param>
        public void SystemMsg(object message, byte[] data, Exception ex)
        {
            Log(message, data, ex, LoggerSeverity.SystemMessage);
        }
        /// <summary>
        /// Logs a message and exception information to the system log with the Warning severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        /// <param name="ex">The exception to log, including its stack trace.</param>
        public void Warning(object message, byte[] data, Exception ex)
        {
            Log(message, data, ex, LoggerSeverity.Warning);
        }
        /// <summary>
        /// Logs a message and exception information to the system log with the Error severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        /// <param name="ex">The exception to log, including its stack trace.</param>
        public void Error(object message, byte[] data, Exception ex)
        {
            Log(message, data, ex, LoggerSeverity.Error);
        }
        /// <summary>
        /// Logs a message and exception information to the system log with the Fatal severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        /// <param name="ex">The exception to log, including its stack trace.</param>
        public void Fatal(object message, byte[] data, Exception ex)
        {
            Log(message, data, ex, LoggerSeverity.Fatal);
        }
        #endregion

        #region format, params object[]
        /// <summary>
        /// Logs a formatted message string to the system log with the Debug severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="format">A String containing zero or more format items</param>
        /// <param name="args">Object array containing zero or more objects to format.</param>
        public void DebugFormat(string format, params object[] args)
        {
            Log(string.Format(format, args), LoggerSeverity.Debug);
        }
        /// <summary>
        /// Logs a formatted message string to the system log with the Information severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="format">A String containing zero or more format items</param>
        /// <param name="args">Object array containing zero or more objects to format.</param>
        public void InfoFormat(string format, params object[] args)
        {
            Log(string.Format(format, args), LoggerSeverity.Information);
        }
        /// <summary>
        /// Logs a formatted message string to the system log with the SystemMessage severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="format">A String containing zero or more format items</param>
        /// <param name="args">Object array containing zero or more objects to format.</param>
        public void SystemMsgFormat(string format, params object[] args)
        {
            Log(string.Format(format, args), LoggerSeverity.SystemMessage);
        }
        /// <summary>
        /// Logs a formatted message string to the system log with the Warning severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="format">A String containing zero or more format items</param>
        /// <param name="args">Object array containing zero or more objects to format.</param>
        public void WarningFormat(string format, params object[] args)
        {
            Log(string.Format(format, args), LoggerSeverity.Warning);
        }
        /// <summary>
        /// Logs a formatted message string to the system log with the Error severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="format">A String containing zero or more format items</param>
        /// <param name="args">Object array containing zero or more objects to format.</param>
        public void ErrorFormat(string format, params object[] args)
        {
            Log(string.Format(format, args), LoggerSeverity.Error);
        }
        /// <summary>
        /// Logs a formatted message string to the system log with the Fatal severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="format">A String containing zero or more format items</param>
        /// <param name="args">Object array containing zero or more objects to format.</param>
        public void FatalFormat(string format, params object[] args)
        {
            Log(string.Format(format, args), LoggerSeverity.Fatal);
        }
        #endregion

        ///* Log a message string using the System.String.Format syntax */
        //void DebugFormat(IFormatProvider provider, string format, params object[] args);
        //void InfoFormat(IFormatProvider provider, string format, params object[] args);
        //void WarnFormat(IFormatProvider provider, string format, params object[] args);
        //void ErrorFormat(IFormatProvider provider, string format, params object[] args);
        //void FatalFormat(IFormatProvider provider, string format, params object[] args);

        #region Log
        /// <summary>
        /// Logs a message to the system log.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The text to log.</param>
        /// <param name="severity">The severity of the event.</param>
        protected virtual void Log(object message, LoggerSeverity severity)
        {
            Log(message, null, null, severity);
        }

        /// <summary>
        /// Logs a message to the system log.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The text to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        /// <param name="startIndex">The start index in data to be included in the log.</param>
        /// <param name="length">The length in data to be included in the log.</param>
        /// <param name="severity">The severity of the event.</param>
        protected virtual void Log(object message, byte[] data, int startIndex, int length, LoggerSeverity severity)
        {
            Log(message, data, startIndex, length, null, severity);
        }

        /// <summary>
        /// Logs a message to the system log.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The text to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        /// <param name="severity">The severity of the event.</param
        protected virtual void Log(object message, byte[] data, LoggerSeverity severity)
        {
            Log(message, data, null, severity);
        }

        /// <summary>
        /// Logs a message and exception information to the system log.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The text to log.</param>
        /// <param name="ex">The exception to log, including its stack trace.</param>
        /// <param name="severity">The severity of the event.</param>
        protected virtual void Log(object message, Exception ex, LoggerSeverity severity)
        {
            Log(message, null, ex, severity);
        }

        /// <summary>
        /// Logs a message and exception information to the system log.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The text to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        /// <param name="ex">The exception to log, including its stack trace.</param>
        /// <param name="severity">The severity of the event.</param>
        protected virtual void Log(object message, byte[] data, Exception ex, LoggerSeverity severity)
        {
            int length = 0;
            if (data != null)
                length = data.Length;

            Log(message, data, 0, length, ex, severity);
        }

        /// <summary>
        /// Logs a message and exception information to the system log.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The text to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        /// <param name="startIndex">The start index in data to be included in the log.</param>
        /// <param name="length">The length in data to be included in the log.</param>
        /// <param name="ex">The exception to log, including its stack trace.</param>
        /// <param name="severity">The severity of the event.</param>
        protected virtual void Log(object message, byte[] data, int startIndex, int length, Exception ex, LoggerSeverity severity)
        {
            // I added this function so that the driver could have it's own verbosity.

            // Compare Driver's Verbosity
            if ((int)severity >= (int)_loggingVerbosity)
                Trace.WriteLine(DateTime.Now.ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss'.'fff") + "  " + message);
        }
        #endregion
    }
}

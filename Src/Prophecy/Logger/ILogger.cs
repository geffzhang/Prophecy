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

namespace Prophecy.Logger
{
    public interface ILogger
    {
        /// <summary>
        /// Logs a message to the system log with the Debug severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        void Debug(object message);
        /// <summary>
        /// Logs a message to the system log with the Information severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        void Info(object message);
        /// <summary>
        /// Logs a message to the system log with the SystemMessage severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        void SystemMsg(object message);
        /// <summary>
        /// Logs a message to the system log with the Warning severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        void Warning(object message);
        /// <summary>
        /// Logs a message to the system log with the Error severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        void Error(object message);
        /// <summary>
        /// Logs a message to the system log with the Fatal severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        void Fatal(object message);



        /// <summary>
        /// Logs a message to the system log with the Debug severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        void Debug(object message, byte[] data);
        /// <summary>
        /// Logs a message to the system log with the Information severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        void Info(object message, byte[] data);
        /// <summary>
        /// Logs a message to the system log with the SystemMessage severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        void SystemMsg(object message, byte[] data);
        /// <summary>
        /// Logs a message to the system log with the Warning severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        void Warning(object message, byte[] data);
        /// <summary>
        /// Logs a message to the system log with the Error severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        void Error(object message, byte[] data);
        /// <summary>
        /// Logs a message to the system log with the Fatal severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        void Fatal(object message, byte[] data);


        /// <summary>
        /// Logs a message to the system log with the Debug severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        /// <param name="startIndex">The start index in data to be included in the log.</param>
        /// <param name="length">The length in data to be included in the log.</param>
        void Debug(object message, byte[] data, int startIndex, int length);
        /// <summary>
        /// Logs a message to the system log with the Information severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        /// <param name="startIndex">The start index in data to be included in the log.</param>
        /// <param name="length">The length in data to be included in the log.</param>
        void Info(object message, byte[] data, int startIndex, int length);
        /// <summary>
        /// Logs a message to the system log with the SystemMessage severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        /// <param name="startIndex">The start index in data to be included in the log.</param>
        /// <param name="length">The length in data to be included in the log.</param>
        void SystemMsg(object message, byte[] data, int startIndex, int length);
        /// <summary>
        /// Logs a message to the system log with the Warning severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        /// <param name="startIndex">The start index in data to be included in the log.</param>
        /// <param name="length">The length in data to be included in the log.</param>
        void Warning(object message, byte[] data, int startIndex, int length);
        /// <summary>
        /// Logs a message to the system log with the Error severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        /// <param name="startIndex">The start index in data to be included in the log.</param>
        /// <param name="length">The length in data to be included in the log.</param>
        void Error(object message, byte[] data, int startIndex, int length);
        /// <summary>
        /// Logs a message to the system log with the Fatal severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        /// <param name="startIndex">The start index in data to be included in the log.</param>
        /// <param name="length">The length in data to be included in the log.</param>
        void Fatal(object message, byte[] data, int startIndex, int length);


        /// <summary>
        /// Logs a message and exception information to the system log with the Debug severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="ex">The exception to log, including its stack trace.</param>
        void Debug(object message, Exception ex);
        /// <summary>
        /// Logs a message and exception information to the system log with the Information severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="ex">The exception to log, including its stack trace.</param>
        void Info(object message, Exception ex);
        /// <summary>
        /// Logs a message and exception information to the system log with the SystemMessage severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="ex">The exception to log, including its stack trace.</param>
        void SystemMsg(object message, Exception ex);
        /// <summary>
        /// Logs a message and exception information to the system log with the Warning severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="ex">The exception to log, including its stack trace.</param>
        void Warning(object message, Exception ex);
        /// <summary>
        /// Logs a message and exception information to the system log with the Error severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="ex">The exception to log, including its stack trace.</param>
        void Error(object message, Exception ex);
        /// <summary>
        /// Logs a message and exception information to the system log with the Fatal severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="ex">The exception to log, including its stack trace.</param>
        void Fatal(object message, Exception ex);

        /// <summary>
        /// Logs a message and exception information to the system log with the Debug severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        /// <param name="ex">The exception to log, including its stack trace.</param>
        void Debug(object message, byte[] data, Exception ex);
        /// <summary>
        /// Logs a message and exception information to the system log with the Information severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        /// <param name="ex">The exception to log, including its stack trace.</param>
        void Info(object message, byte[] data, Exception ex);
        /// <summary>
        /// Logs a message and exception information to the system log with the SystemMessage severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        /// <param name="ex">The exception to log, including its stack trace.</param>
        void SystemMsg(object message, byte[] data, Exception ex);
        /// <summary>
        /// Logs a message and exception information to the system log with the Warning severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        /// <param name="ex">The exception to log, including its stack trace.</param>
        void Warning(object message, byte[] data, Exception ex);
        /// <summary>
        /// Logs a message and exception information to the system log with the Error severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        /// <param name="ex">The exception to log, including its stack trace.</param>
        void Error(object message, byte[] data, Exception ex);
        /// <summary>
        /// Logs a message and exception information to the system log with the Fatal severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="message">The message object to log.</param>
        /// <param name="data">The byte data will be logged hexadecimal notation.</param>
        /// <param name="ex">The exception to log, including its stack trace.</param>
        void Fatal(object message, byte[] data, Exception ex);

        ///// <summary>
        ///// Logs a message to the system log with the Debug severity.  This method will NOT throw an exception.
        ///// </summary>
        ///// <param name="message">The message object to log.</param>
        ///// <param name="data">The byte data will be logged hexadecimal notation.</param>
        ///// <param name="startIndex">The start index in data to be included in the log.</param>
        ///// <param name="length">The length in data to be included in the log.</param>
        ///// <param name="ex">The exception to log, including its stack trace.</param>
        //void Debug(object message, byte[] data, int startIndex, int length, Exception ex);
        ///// <summary>
        ///// Logs a message to the system log with the Information severity.  This method will NOT throw an exception.
        ///// </summary>
        ///// <param name="message">The message object to log.</param>
        ///// <param name="data">The byte data will be logged hexadecimal notation.</param>
        ///// <param name="startIndex">The start index in data to be included in the log.</param>
        ///// <param name="length">The length in data to be included in the log.</param>
        ///// <param name="ex">The exception to log, including its stack trace.</param>
        //void Info(object message, byte[] data, int startIndex, int length, Exception ex);
        ///// <summary>
        ///// Logs a message to the system log with the SystemMessage severity.  This method will NOT throw an exception.
        ///// </summary>
        ///// <param name="message">The message object to log.</param>
        ///// <param name="data">The byte data will be logged hexadecimal notation.</param>
        ///// <param name="startIndex">The start index in data to be included in the log.</param>
        ///// <param name="length">The length in data to be included in the log.</param>
        ///// <param name="ex">The exception to log, including its stack trace.</param>
        //void SystemMsg(object message, byte[] data, int startIndex, int length, Exception ex);
        ///// <summary>
        ///// Logs a message to the system log with the Warning severity.  This method will NOT throw an exception.
        ///// </summary>
        ///// <param name="message">The message object to log.</param>
        ///// <param name="data">The byte data will be logged hexadecimal notation.</param>
        ///// <param name="startIndex">The start index in data to be included in the log.</param>
        ///// <param name="length">The length in data to be included in the log.</param>
        ///// <param name="ex">The exception to log, including its stack trace.</param>
        //void Warning(object message, byte[] data, int startIndex, int length, Exception ex);
        ///// <summary>
        ///// Logs a message to the system log with the Error severity.  This method will NOT throw an exception.
        ///// </summary>
        ///// <param name="message">The message object to log.</param>
        ///// <param name="data">The byte data will be logged hexadecimal notation.</param>
        ///// <param name="startIndex">The start index in data to be included in the log.</param>
        ///// <param name="length">The length in data to be included in the log.</param>
        ///// <param name="ex">The exception to log, including its stack trace.</param>
        //void Error(object message, byte[] data, int startIndex, int length, Exception ex);
        ///// <summary>
        ///// Logs a message to the system log with the Fatal severity.  This method will NOT throw an exception.
        ///// </summary>
        ///// <param name="message">The message object to log.</param>
        ///// <param name="data">The byte data will be logged hexadecimal notation.</param>
        ///// <param name="startIndex">The start index in data to be included in the log.</param>
        ///// <param name="length">The length in data to be included in the log.</param>
        ///// <param name="ex">The exception to log, including its stack trace.</param>
        //void Fatal(object message, byte[] data, int startIndex, int length, Exception ex);


        /// <summary>
        /// Logs a formatted message string to the system log with the Debug severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="format">A String containing zero or more format items</param>
        /// <param name="args">Object array containing zero or more objects to format.</param>
        void DebugFormat(string format, params object[] args);
        /// <summary>
        /// Logs a formatted message string to the system log with the Information severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="format">A String containing zero or more format items</param>
        /// <param name="args">Object array containing zero or more objects to format.</param>
        void InfoFormat(string format, params object[] args);
        /// <summary>
        /// Logs a formatted message string to the system log with the SystemMessage severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="format">A String containing zero or more format items</param>
        /// <param name="args">Object array containing zero or more objects to format.</param>
        void SystemMsgFormat(string format, params object[] args);
        /// <summary>
        /// Logs a formatted message string to the system log with the Warning severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="format">A String containing zero or more format items</param>
        /// <param name="args">Object array containing zero or more objects to format.</param>
        void WarningFormat(string format, params object[] args);
        /// <summary>
        /// Logs a formatted message string to the system log with the Error severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="format">A String containing zero or more format items</param>
        /// <param name="args">Object array containing zero or more objects to format.</param>
        void ErrorFormat(string format, params object[] args);
        /// <summary>
        /// Logs a formatted message string to the system log with the Fatal severity.  This method will NOT throw an exception.
        /// </summary>
        /// <param name="format">A String containing zero or more format items</param>
        /// <param name="args">Object array containing zero or more objects to format.</param>
        void FatalFormat(string format, params object[] args);
    }
}

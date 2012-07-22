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
using System.IO;
using System.Net.Sockets;

namespace Prophecy.Extensions
{
    public static class ExceptionExtensions
    {
        public static string ToString(this Exception ex, bool includeExtendedInformation)
        {
            //// The following formats the text just like Exception.ToString() without the stack trace.
            //string messagetext = "";
            //for (Exception ex2 = ex; ex2 != null; ex2 = ex2.InnerException)
            //{
            //    if (messagetext.Length > 0)
            //        messagetext += " ---> ";
            //    messagetext += ex2.GetType().FullName + ": " + ex2.Message;
            //}
            //return messagetext;

            if (includeExtendedInformation == false)
                return ex.ToString();

            string messagetext = "";

            for (Exception ex2 = ex; ex2 != null; ex2 = ex2.InnerException)
            {
                if (messagetext.Length > 0)
                    messagetext += " ---> ";
                messagetext += ex2.GetType().FullName + ": " + ex2.Message;

                if (includeExtendedInformation)
                {
                    if (ex2 is FileNotFoundException)
                        if (((FileNotFoundException)ex2).FusionLog != null) // Fusion log can be null.
                            messagetext += "\r\nFileNotFoundException FusionLog: " + ((FileNotFoundException)ex2).FusionLog;

                    if (ex2 is SocketException)
                        messagetext += "\r\nSocketException SocketErrorCode: " + ((SocketException)ex2).SocketErrorCode;

                    if (ex2 is System.Reflection.ReflectionTypeLoadException)
                    {
                        System.Reflection.ReflectionTypeLoadException rtlex = ex2 as System.Reflection.ReflectionTypeLoadException;
                        for (int i = 0; i < rtlex.LoaderExceptions.Length; i++)
                            if (rtlex.LoaderExceptions[i] != null) // I doubt it is ever null.
                                messagetext += "\r\nReflectionTypeLoadException LoaderExceptions[" + i + "] : " + rtlex.LoaderExceptions[i].ToString(includeExtendedInformation);
                    }

                    if (ex2 is ArgumentNullException)
                        messagetext += "\r\nArgumentNullException ParamName: " + ((ArgumentNullException)ex2).ParamName;

                    if (ex2 is ObjectDisposedException)
                        messagetext += "\r\nObjectDisposedException ObjectName: " + ((ObjectDisposedException)ex2).ObjectName;
                }
            }
            return messagetext;
        }
    }
}

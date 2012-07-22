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
    [Serializable]
    public enum LoggerSeverity
    {
        /// <summary>
        /// Logged with Verbosity of Diagnostic.
        /// </summary>
        Debug = 0,
        /// <summary>
        /// Logged with Verbosity of Detailed, and Diagnostic.
        /// </summary>
        Information = 1,
        /// <summary>
        /// Logged with Verbosity of Normal, Detailed, and Diagnostic.
        /// </summary>
        SystemMessage = 2,
        /// <summary>
        /// Logged with Verbosity of Normal, Detailed, and Diagnostic.
        /// </summary>
        Warning = 3,
        /// <summary>
        /// Logged with Verbosity of Minimal, Normal, Detailed, and Diagnostic.
        /// </summary>
        Error = 4,
        /// <summary>
        /// Logged with Verbosity of Minimal, Normal, Detailed, and Diagnostic.
        /// </summary>
        Fatal = 5
    }
}

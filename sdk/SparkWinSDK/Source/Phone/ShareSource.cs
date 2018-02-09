﻿#region License
// Copyright (c) 2016-2017 Cisco Systems, Inc.

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkSDK
{
    /// <summary>
    /// share source types.
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public enum ShareSourceType
    {
        /// <summary>
        /// unknown type.
        /// </summary>
        /// <remarks>Since: 0.1.0</remarks>
        Unknown = 0,
        /// <summary>
        /// share the whole desktop.
        /// </summary>
        /// <remarks>Since: 0.1.0</remarks>
        Desktop = 1,
        /// <summary>
        /// share an application.
        /// </summary>
        /// <remarks>Since: 0.1.0</remarks>
        Application = 2,
        /// <summary>
        /// share a scope of content.
        /// </summary>
        /// <remarks>Since: 0.1.0</remarks>
        Content = 3
    }

    /// <summary>
    /// Share source.
    /// </summary>
    /// <remarks>Since: 0.1.0</remarks>
    public class ShareSource
    {
        /// <summary>
        /// the source ID.
        /// </summary>
        /// <remarks>Since: 0.1.0</remarks>
        public string SourceId;
        /// <summary>
        /// the name of the shared source.
        /// </summary>
        /// <remarks>Since: 0.1.0</remarks>
        public string Name;
        /// <summary>
        /// if this source has sharrd.
        /// </summary>
        /// <remarks>Since: 0.1.0</remarks>
        public bool IsShared;
        /// <summary>
        /// the width of the scope of the content
        /// </summary>
        /// <remarks>Since: 0.1.0</remarks>
        public int Width;
        /// <summary>
        /// the height of the scope of the content.
        /// </summary>
        /// <remarks>Since: 0.1.0</remarks>
        public int Height;
        /// <summary>
        /// the position of the start point.
        /// </summary>
        /// <remarks>Since: 0.1.0</remarks>
        public int X;
        /// <summary>
        /// the position of the start point.
        /// </summary>
        /// <remarks>Since: 0.1.0</remarks>
        public int Y;
    }
}

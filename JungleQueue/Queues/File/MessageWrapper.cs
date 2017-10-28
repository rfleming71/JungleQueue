// <copyright file="MessageWrapper.cs">
//     The MIT License (MIT)
//
// Copyright(c) 2017 Ryan Fleming
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// </copyright>
using System;
using System.Collections.Generic;

namespace JungleQueue.Queues.File
{
    /// <summary>
    /// Message wrapper for the file bus
    /// </summary>
    internal class MessageWrapper
    {
        /// <summary>
        /// Gett or sets the message id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the message body
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// Gets or sets the metadata for the message
        /// </summary>
        public IEnumerable<KeyValuePair<string, string>> Metadata { get; set; }

        /// <summary>
        /// Gets or sets the sent message time
        /// </summary>
        public DateTime SentTime { get; set; }

        /// <summary>
        /// NUmber of attempts made for this message
        /// </summary>
        public int Attempts { get; set; }

        /// <summary>
        /// Gets the next attempt time for this message
        /// </summary>
        public DateTime? NextAttemptTime { get; set; }
    }
}

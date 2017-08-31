// <copyright file="TestQueue.cs">
//     The MIT License (MIT)
//
// Copyright(c) 2016 Ryan Fleming
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
using System.Linq;
using JungleQueue.Interfaces;

namespace JungleQueue.Testing
{
    /// <summary>
    /// Implementation of the queue for unit test purposes
    /// </summary>
    public class TestQueue : IQueue
    {
        /// <summary>
        /// Collection of sent messages
        /// </summary>
        private Dictionary<Type, List<object>> _sentMessages = new Dictionary<Type, List<object>>();

        /// <summary>
        /// Resets the sent messages
        /// </summary>
        public void Reset()
        {
            _sentMessages.Clear();
        }

        /// <summary>
        /// Send a message to the input queue
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="messageBuilder">Function to initialize the message</param>
        public void Send<T>(Action<T> messageBuilder) where T : new()
        {
            T message = new T();
            messageBuilder?.Invoke(message);

            Send(message);
        }

        /// <summary>
        /// Send a message to the input queue
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="message">Message to send</param>
        public void Send<T>(T message)
        {
            Type messageType = typeof(T);
            if (!_sentMessages.ContainsKey(messageType))
            {
                _sentMessages[messageType] = new List<object>();
            }

            _sentMessages[messageType].Add(message);
        }

        /// <summary>
        /// Verifies that a message of the given type and data was not sent
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="verificationMethod">Method used to check the message</param>
        public void VerifyNotSent<T>(Func<T, bool> verificationMethod)
            where T : class
        {
            VerifySent<T>(verificationMethod, 0);
        }

        /// <summary>
        /// Verifies that a message of the given type and data was sent
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="verificationMethod">Method used to check the message</param>
        /// <param name="expectedNumberOfTimes">Number of times the message should have been sent</param>
        public void VerifySent<T>(Func<T, bool> verificationMethod, int expectedNumberOfTimes)
            where T : class
        {
            Type messageType = typeof(T);
            int sendCount = 0;
            if (_sentMessages.ContainsKey(messageType))
            {
                sendCount = _sentMessages[messageType].Count(x => verificationMethod(x as T));
            }

            if (sendCount != expectedNumberOfTimes)
            {
                throw new Exception(string.Format("Message of type {0} was expected to be sent {1} times but was {2}", messageType, expectedNumberOfTimes, sendCount));
            }
        }
    }
}

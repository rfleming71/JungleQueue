// <copyright file="SimpleObjectBuilder.cs">
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
using JungleQueue.Interfaces.IoC;

namespace JungleQueue.IoC
{
    /// <summary>
    /// Default object builder. 
    /// Requires all constructed objects to have a default constructor
    /// </summary>
    internal class SimpleObjectBuilder : IObjectBuilder
    {
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting
        /// unmanaged resources.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Creates a child builder
        /// </summary>
        /// <returns>Nested builder</returns>
        public IObjectBuilder GetNestedBuilder()
        {
            return this;
        }

        /// <summary>
        /// Gets an instance of type T
        /// </summary>
        /// <typeparam name="T">Type to get</typeparam>
        /// <returns>Instance of type T</returns>
        public T GetValue<T>()
        {
            return Activator.CreateInstance<T>();
        }

        /// <summary>
        /// Gets an instance of type type
        /// </summary>
        /// <param name="type">Type to get</param>
        /// <returns>Instance of type</returns>
        public object GetValue(Type type)
        {
            return Activator.CreateInstance(type);
        }

        /// <summary>
        /// Gets all instances of type T
        /// </summary>
        /// <typeparam name="T">Type to get</typeparam>
        /// <returns>Instances of type T</returns>
        public IEnumerable<T> GetValues<T>()
        {
            return null;
        }

        /// <summary>
        /// Registers an instance of type T with the builder
        /// </summary>
        /// <typeparam name="T">Type to register</typeparam>
        /// <param name="value">Instance of T to register</param>
        public void RegisterInstance<T>(T value) where T : class
        {
        }

        /// <summary>
        /// Register a concrete type to a given base type
        /// </summary>
        /// <param name="baseType">Base type</param>
        /// <param name="concreteType">Concrete Type</param>
        public void RegisterType(Type baseType, Type concreteType)
        {
        }
    }
}

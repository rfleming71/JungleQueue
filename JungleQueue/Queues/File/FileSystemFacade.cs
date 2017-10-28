// <copyright file="FileSystemFacade.cs">
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
using System.IO;

namespace JungleQueue.Queues.File
{
    using File = System.IO.File;

    /// <summary>
    /// Wraps file system calls
    /// </summary>
    public class FileSystemFacade : IFileSystemFacade
    {
        /// <summary>
        /// Reads all the text out of a file
        /// </summary>
        /// <param name="filepath">File path</param>
        /// <returns>File text</returns>
        public string ReadAllText(string filepath)
        {
            return File.ReadAllText(filepath);
        }

        /// <summary>
        /// Write text to a file
        /// </summary>
        /// <param name="filepath">Path to the file</param>
        /// <param name="text">Text to write</param>
        public void WriteAllText(string filepath, string text)
        {
            File.WriteAllText(filepath, text);
        }

        /// <summary>
        /// Deletes a file
        /// </summary>
        /// <param name="filepath">File path</param>
        public void DeleteFile(string filepath)
        {
            if (File.Exists(filepath))
            {
                File.Delete(filepath);
            }
        }

        /// <summary>
        /// Gets the files in a given directory
        /// </summary>
        /// <param name="directoryPath">Directory Path</param>
        /// <returns>List of files</returns>
        public string[] GetFilesInDirectory(string directoryPath)
        {
            return Directory.GetFiles(directoryPath, "*", SearchOption.TopDirectoryOnly);
        }

        /// <summary>
        /// Creates the directory if it doesn't exist
        /// </summary>
        /// <param name="directoryPath">Directory path</param>
        public void CreateDiretory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }
    }
}

namespace JungleQueue.Queues.File
{
    /// <summary>
    /// Wraps file system calls
    /// </summary>
    public interface IFileSystemFacade
    {
        /// <summary>
        /// Deletes a file
        /// </summary>
        /// <param name="filepath">File path</param>
        void DeleteFile(string filepath);

        /// <summary>
        /// Gets the files in a given directory
        /// </summary>
        /// <param name="directoryPath">Directory Path</param>
        /// <returns>List of files</returns>
        string[] GetFilesInDirectory(string directoryPath);

        /// <summary>
        /// Reads all the text out of a file
        /// </summary>
        /// <param name="filepath">File path</param>
        /// <returns>File text</returns>
        string ReadAllText(string filepath);

        /// <summary>
        /// Write text to a file
        /// </summary>
        /// <param name="filepath">Path to the file</param>
        /// <param name="text">Text to write</param>
        void WriteAllText(string filepath, string text);

        /// <summary>
        /// Creates the directory if it doesn't exist
        /// </summary>
        /// <param name="directoryPath">Directory path</param>
        void CreateDiretory(string directoryPath);
    }
}
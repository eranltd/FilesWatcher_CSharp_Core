using System;
namespace FilesWatcherService.Models
{
    public class FileChangeEventArgs
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }

        public double TimeBetweenUpdates { get; set; }
        public FileChangeEventArgs(string fileName, string fullPath)
        {
            FileName = fileName;
            FullPath = fullPath;
        }
    }
}

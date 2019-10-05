using System;
using System.Collections.Generic;
using FilesWatcherService;
using FilesWatcherService.BLL;
using FilesWatcherService.Models;

namespace FilesWatcher_CSharp_Core
{
    class Program
    {
        static void Main(string[] args)
        {
            Run();
            Console.WriteLine("File System Watcher has been started, press any key to exit...");
            Console.ReadKey();
        }


        public static void Run()
        {
            //get folder to watch path from appsettings.json

            var FSWSource = ConfigValueProvider.Get("FSW:FSWSource");

            //get file types from appsettings.json

            List<string> fileTypes = ConfigValueProvider.GetArray("FSW:FileTypes");

            //register our events

            FilesWatcher.OnFileReady += OnFileReady;
            FilesWatcher.OnNewMessage += OnNewMessage;
            FilesWatcher.Instance.Watch(FSWSource, fileTypes);
        }


        private static void OnNewMessage(object sender, string str)
        {
            Console.WriteLine(str); //or write to a logger...
        }


        private static void OnFileReady(object sender, FileChangeEventArgs e)
        {
			//case 1 : you can log file ready event
            //case 2: you can call web api to report
            //case 3 : you can write to the screen        
    }
}
}


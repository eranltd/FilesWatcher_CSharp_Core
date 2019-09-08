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
            Console.WriteLine("File System Wathcer has been started, press any key to exit...");
            Console.ReadKey();
        }


        public static void Run()
        {
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
            Console.WriteLine(str);
        }


        private static void OnFileReady(object sender, FileChangeEventArgs e)
        {
            //log file ready event

            //call web api to report

            //write to screen about file ready.




        }
    }
}

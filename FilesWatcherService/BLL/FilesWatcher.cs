using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FilesWatcherService.Models;

namespace FilesWatcherService.BLL
{
    public sealed class FilesWatcher
    {

        #region config

        static int initialTimerInterval = 5;
        static int delayedTimerIntervalAddition = 1;
        static int permittedIntervalBetweenFiles = 60;

        static bool LogFSWEvents = false;
        static bool LogFileReadyEvents = false;
        static bool FSWUseRegex = false;

        static string FSWRegex = null;

        #endregion

        #region private vars

        private List<string> _filteredFileTypes;
        private FileSystemWatcher _watcher;

        #endregion

        #region Singletone

        private static readonly FilesWatcher instance = new FilesWatcher();

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static FilesWatcher()
        {
        }

        private FilesWatcher()
        {
        }

        public static FilesWatcher Instance
        {
            get
            {
                return instance;
            }
        }

        #endregion


        #region events

        public static event EventHandler<FileChangeEventArgs> OnFileReady;
        public static event EventHandler<string> OnNewMessage;

        #endregion


        public static ConcurrentDictionary<string, FileChangeItem> filesEvents = new ConcurrentDictionary<string, FileChangeItem>();


        public void Watch(string FSWSource, List<string> fileTypes)
        {
            Instance._filteredFileTypes = fileTypes;
            Instance._Watch(FSWSource);
        }


        private void _Watch(string FSWSource)
        {
            //read all settings from app.config
            ReadAllSettings();

            // If a directory is not specified, exit program.
            if (!(FSWSource.Length > 0) || _filteredFileTypes == null)
            {
                Console.Error.WriteLine("Cannot Proceed without FSWSource || fileTypes");
                return;
            }


            // Create a new FileSystemWatcher and set its properties.
            _watcher = new FileSystemWatcher();

            //watcher.Path = folder;
            _watcher.EnableRaisingEvents = false;
            _watcher.IncludeSubdirectories = true;
            _watcher.InternalBufferSize = 32768; //32KB

            _watcher.Path = FSWSource;

            Console.WriteLine($"Watching Folder: {_watcher.Path}");


            // Watch for changes in LastAccess and LastWrite times, and
            // the renaming of files or directories.

            _watcher.NotifyFilter = NotifyFilters.Size | NotifyFilters.CreationTime |
                                                    NotifyFilters.LastWrite | NotifyFilters.LastAccess;


            _watcher.Filter = "*.*";

            // Add event handlers.
            _watcher.Changed += OnFileChanged;
            _watcher.Created += OnFileChanged;

            // Begin watching.
            _watcher.EnableRaisingEvents = true;
            Console.WriteLine("FileSystemWatcher Ready.");

        }


        private void OnFileChanged(object source, FileSystemEventArgs e)
        {

            if (FSWUseRegex && !Regex.IsMatch(e.Name, FSWRegex))
                return;

            try
            {
   
                    FileInfo f = new FileInfo(e.FullPath);

                    //discard event to block other file extentions...

                    if (_filteredFileTypes.Any(str => f.Extension.Equals(str)))
                    {
                        DateTime eventTime = DateTime.Now;
                        string fileName = e.Name;

                        if (LogFSWEvents)
                            Console.WriteLine($"Time: {eventTime.TimeOfDay}\t ChangeType: {e.ChangeType,-14} FileName: {fileName,-50} Path: {e.FullPath} ");

                        if (filesEvents.TryGetValue(fileName, out FileChangeItem r2NetWatchItem))
                        {
                            // in update process
                            if (r2NetWatchItem.State == FileChangeItem.WatchItemState.Updating)
                            {
                                r2NetWatchItem.ResetTimer(e, eventTime);
                            }
                            // new / already reported file ready.
                            else if (r2NetWatchItem.State == FileChangeItem.WatchItemState.Idle)
                            {
                                if (!r2NetWatchItem.WaitingForNextFile(eventTime))
                                {
                                    // increase timer
                                    r2NetWatchItem.UpdateTimeForFileToBeReady(eventTime); //reset + interval
                                }
                                else
                                {
                                    Console.WriteLine($"FileName: {fileName} restarting count again.");
                                    r2NetWatchItem.ResetTimer(e, eventTime);
                                }
                            }
                        }
                        else // new supplier file
                        {
                            var watchItem = new FileChangeItem(e, initialTimerInterval);

                            watchItem.OnFileReadyEvent += WatchItem_OnFileReady;
                            watchItem.OnNewMessageEvent += WatchItem_OnNewMessage;
                            watchItem.permittedIntervalBetweenFiles = permittedIntervalBetweenFiles;
                            filesEvents.TryAdd(watchItem.FileName, watchItem);
                        }
                    }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }


        private void WatchItem_OnNewMessage(object sender, string msg)
        {
            OnNewMessage?.Invoke(this, msg);
        }

        private void WatchItem_OnFileReady(object sender, FileChangeEventArgs e)
        { 
            if (LogFileReadyEvents)
                Console.WriteLine($"Time: {DateTime.Now.TimeOfDay}\t File: {e.FileName,-50} ready.");

            OnFileReady?.Invoke(this, e);
        }


        void ReadAllSettings()
        {
            try
            {
                initialTimerInterval = int.Parse(ConfigValueProvider.Get("FSW:initialTimerInterval"));
                delayedTimerIntervalAddition = int.Parse(ConfigValueProvider.Get("FSW:delayedTimerAddition"));
                permittedIntervalBetweenFiles = int.Parse(ConfigValueProvider.Get("FSW:permittedSecondsBetweenReadyEvents"));
                LogFileReadyEvents = bool.Parse(ConfigValueProvider.Get("FSW:LogFileReadyEvents"));
                LogFSWEvents = bool.Parse(ConfigValueProvider.Get("FSW:LogFSWEvents"));
                FSWUseRegex = bool.Parse(ConfigValueProvider.Get("FSW:FSWUseRegex"));
                FSWRegex = ConfigValueProvider.Get("FSW:FSWRegex");


                Console.WriteLine($"initialTimerInterval:[{initialTimerInterval}], delayedTimerIntervalAddition:[{delayedTimerIntervalAddition}], permittedIntervalBetweenEvents:[{permittedIntervalBetweenFiles}]");
                Console.WriteLine($"LogFileReadyEvents:[{LogFileReadyEvents}], LogFSWEvents:[{LogFSWEvents}]");

            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error reading R2NETFSWSettings settings(setting defaults...), Message: {e.Message}", true);
            }
        }


    }
}


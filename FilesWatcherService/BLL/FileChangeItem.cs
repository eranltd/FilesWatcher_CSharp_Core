using System;
using System.IO;
using System.Timers;
using FilesWatcherService.Models;

namespace FilesWatcherService.BLL
{
    public class FileChangeItem : IDisposable
    {
        #region private vars
        private System.Timers.Timer Timer;
        private static readonly Object timerLocker = new Object();
        internal enum WatchItemState { Idle, Updating }
        private int TimeForFileToBeReady { get; set; }
        private static readonly Object objectLocker = new Object();

        #endregion

        #region public / protected vars

        public string FileName { get; }
        public string FullPath { get; }
        public DateTime LastEventTime { get; private set; } = DateTime.Now;
        public Guid ID { get; set; }

        #endregion

        #region events

        public event EventHandler<FileChangeEventArgs> OnFileReadyEvent;
        public event EventHandler<string> OnNewMessageEvent;

        #endregion


        public int permittedIntervalBetweenFiles = 60;


        #region Getters


        public double TimeBetweenUpdates(DateTime eventTime) => (eventTime - LastEventTime).TotalSeconds;
        public bool WaitingForNextFile(DateTime eventTime) => State == WatchItemState.Idle && TimeBetweenUpdates(eventTime) > permittedIntervalBetweenFiles;

        internal WatchItemState State { get; private set; } = WatchItemState.Idle;

        internal double GetIntervalSeconds => TimeSpan.FromMilliseconds(Timer.Interval).TotalSeconds;

        #endregion


        public FileChangeItem()
        {
            ID = Guid.NewGuid();
        }

        public FileChangeItem(int initTimerInterval = 5) : this()
        {
            TimeForFileToBeReady = initTimerInterval;
            Timer = new System.Timers.Timer(TimeSpan.FromSeconds(TimeForFileToBeReady).TotalMilliseconds);
            Timer.Elapsed += OnTimedEvent;
            Timer.AutoReset = false;
            Timer.Enabled = true;
            State = WatchItemState.Updating;
        }

        public FileChangeItem(FileSystemEventArgs args, int initialTimerInterval) :
            this(initialTimerInterval)
        {
            FileName = args.Name;
            FullPath = args.FullPath;
        }


        //if instance exists, reset timer and overwrite event.
        public void ResetTimer(FileSystemEventArgs args, DateTime eventTime)
        {
            lock (objectLocker)
            {
                LastEventTime = eventTime;
                ResetTimer();
            }
        }

        public void UpdateTimeForFileToBeReady(DateTime eventTime)
        {
            lock (objectLocker)
            {
                TimeForFileToBeReady = TimeForFileToBeReady + (int)(eventTime - LastEventTime).TotalSeconds + 3;

                //Console.WriteLine($"New TimeForFileToBeReady: {TimeForFileToBeReady}, FileName: {FileName}");

                //LogMessage($"New TimeForFileToBeReady: {TimeForFileToBeReady}, FileName: {FileName}");

                Timer.Interval = TimeSpan.FromSeconds(TimeForFileToBeReady).TotalMilliseconds;
            }
        }


        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            try
            {
                lock (objectLocker)
                {
                    State = WatchItemState.Idle;
                    FileChangeEventArgs args = new FileChangeEventArgs(FileName, FullPath);
                    args.TimeBetweenUpdates = this.TimeBetweenUpdates(DateTime.Now);
                    OnFileReady(args);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
            }
        }

        protected virtual void OnFileReady(FileChangeEventArgs e)
        {
            OnFileReadyEvent?.Invoke(this, e);
        }

        protected virtual void OnNewMessage(string str)
        {
            OnNewMessageEvent?.Invoke(this, str);
        }

        private void ResetTimer()
        {
            Timer.Stop();
            Timer.Interval = TimeSpan.FromSeconds(TimeForFileToBeReady).TotalMilliseconds;
            State = WatchItemState.Updating;
            Timer.Start();
        }



        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    lock (timerLocker)
                    {
                        Timer?.Dispose();
                    }
                }
                disposedValue = true;
            }
        }



        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);

        }
        #endregion
    }
}

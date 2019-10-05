
# Watching File(s) Changes in .NET 4.5+ & .NET Core 2+

Note: the .NET Core 2.2 code for this article is available from the following GitHub repository.
> *Note: this code is based on [*FileSystemWatcher](https://docs.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher?view=netframework-4.8) Class.

![(Figure 1) program output](https://cdn-images-1.medium.com/max/4344/1*kZ5w3ethozWQlFeG5ct96g.png)*(Figure 1) program output*

Microsoft is awesome when it comes to a handy libraries. in this article we will review the **FileSystemWatcher** Library and how to use it to **detect** file changes in a real life scenarios.

![(Figure 2) Client Uploading file to FTP](https://cdn-images-1.medium.com/max/2000/1*Aw4A1-eIsEDqhRLSBYAsbQ.png)*(Figure 2) Client Uploading file to FTP*

## FileSystemWatcher class :

The FileSystemWatcher class in the System.IO namespace can be used to monitor changes to the file system. It watches a file or a directory in your system for changes and triggers events when changes occur.

In order for the FileSystemWatcher to work, you should specify a directory that needs to be monitored. The **FileSystemWatcher** raises the following **events** when changes occur to a directory that it is monitoring.

* **Changed**: This event is triggered when a file or a directory in the path being monitored is changed

* **Created**: This event is triggered when a file or a directory in the path being monitored is created

* **Deleted**: This event is triggered when a file or a directory in the path being monitored is deleted

* **Error**: This event is triggered there is an error due to changes made in the path being monitored

* **Renamed**: This event is triggered when a file or a directory in the path being monitored is renamed

## This library is not so perfect and you can find yourself getting multiple events on the same file, causing you ‘losing your seas’.

![(Figure 2.1) multiple events per file(red)… we want only the fileReady events(blue)..](https://cdn-images-1.medium.com/max/6362/1*rAqZN1GPuwnHMepa7boBww.jpeg)*(Figure 2.1) multiple events per file(red)… we want only the fileReady events(blue)..*

![Figure 3 (Remark from Microsoft team)](https://cdn-images-1.medium.com/max/3052/1*tnwuxiekuTng8C5rzUZkpg.png)*Figure 3 (Remark from Microsoft team)*

## Algorithm — Basic Principle:

Build a smart ‘wrapper’ program around [FileSystemWatcher](https://docs.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher?view=netframework-4.8) Class, with additional features like regex and file extensions filters, and ensure we’ll get only 1 event per file, and not multiple events.

![( Figure 4 ) main logic](https://cdn-images-1.medium.com/max/2000/1*2JTi9KksdIxxajpYDGJ9ug.png)*( Figure 4 ) main logic*

I’ll try to explain it simple as possible, and it goes like this :

Let’s consider the next scenario :

* You have a public FTP folder for your suppliers

* Every-time a supplier uploads a file, you want to parse that file and insert the data to your database.

So you’ll have 2 options :

1. Every-time a supplier is finish uploading a file we want to notify some service and parse it. **(that’s what we’ll do in this article…)**

1. Every x-time(say like 10 minutes) we will sample the FTP folder and take the supplier file.

The 2nd approach has downsides:

* We can accidentally take “in-complete” file, meaning by the time we sampled the supplier files he hasn’t finished uploading it yet.

* If we are sampling the supplier files every 60 minutes, the file will take a long time to be parsed.

## Algorithm — Explanation:

We are constantly getting FSW updates from [FileSystemWatcher](https://docs.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher?view=netframework-4.8) Class (FileCreated, FileModified, FileDeleted…) by subscribing to FSWEvent

![(Figure 4.1) subscribe to FSWEvents](https://cdn-images-1.medium.com/max/2000/1*8x0NTx2y0hD2k142QsgFmg.png)*(Figure 4.1) subscribe to FSWEvents*

![](https://cdn-images-1.medium.com/max/2000/1*mxbAxNbiM5dGRpMZA9PJWw.png)

**Every-time** we’ll get an update, we will start a ***unique*** timer for each file.

(for example, “/Users/eran/Desktop/TestFSW/1.txt” with timer set to 5 seconds)

Those unique entires will be saved in a special dictionary.

The purpose of that special dictionary is to **log** the **last time** a specific file has fired a FSW event(it is crucial).

When the timer is ***elapsed***(for exmaple, after 5 seconds) FileChangeItem will fire an “FileReady” event.

If the timer has ***not been elapsed ***and we’ve got a ***New*** FSW event, we will restart the timer, that’s will ensure that enough time has elapsed between the last FSW Event, from that we can deduce that the supplier has **finished** uploading his file.

Our ‘wrapper’ program has 2 types of events:

![(Figure 4.2) Types of Events](https://cdn-images-1.medium.com/max/2000/1*fNE-AEhqyQYdu719qAerAQ.png)*(Figure 4.2) Types of Events*

You‘ll need to subscribe to the OnFileReady Event, and do whatever you like with it.

    FilesWatcher.OnFileReady += OnFileReady;

    *private static void **OnFileReady**(object sender, FileChangeEventArgs e)
    {
      //case 1 : you can log when file ready event
      //case 2: you can call web api 
      //case 3 : you can write to the screen
    }*

**Optional : **you can subscribe to the OnNewMessage Event to log/print FSW / FileReady events(you can toggle these options from appsettings.json)

    FilesWatcher.OnNewMessage += OnNewMessage;

    private static void OnNewMessage(object sender, string str)
    {
      Console.WriteLine(str); //or write to a logger...
    }

The source code has additional features like filter specific **filetypes** and filter specific **folders** using **regex**, you can grab a working demo of this code from my [*GitHub repository](https://github.com/eranltd/FilesWatcher_CSharp_Core)* and check it out.

## C# Recap : Events, Regex, Timers and Settings

### [Events](https://docs.microsoft.com/en-us/dotnet/standard/events/)
> An event is a message sent by an object to signal the occurrence of an action.

C# and .NET supports event driven programming via delegates. Delegates and events provide notifications to client applications when some state changes of an object. It is an encapsulation of idea that “Something happened”. Events and Delegates are tightly coupled concept because event handling requires delegate implementation to dispatch events.

The class that sends or raises an event is called a Publisher and class that receives or handle the event is called “Subscriber”.

### [C# Timers](https://docs.microsoft.com/en-us/dotnet/api/system.timers.timer?view=netframework-4.8)
> Generates an event after a set interval, with an option to generate recurring events.

### [C# Regex](https://docs.microsoft.com/en-us/dotnet/api/system.text.regularexpressions.regex?view=netframework-4.8)
> Regular expressions are patterns used to match character combinations in strings.

![( Figure 5) project structure](https://cdn-images-1.medium.com/max/4040/1*HCUNkzIzjEY3_QLKbhDNoA.png)*( Figure 5) project structure*

## FilesWatcher, FileChangeItem, FileChangeEventArgs, ConfigValueProvider classes :

## FilesWatcher Class:

This class is our main ‘service’ responsible for everything , and you’ll need to subscribe to their events.

## FileChangeItem Class:

* Has a state (Idle / Updating) based on timer ***elapsed / not elapsed***

* Has a **unique** timer

* Remember the last time FSW event has been fired

## FileChangeEventArgs Class:

Custom event data class named FileChangeEventArgs that derives from the [EventArgs](https://docs.microsoft.com/en-us/dotnet/api/system.eventargs?view=netframework-4.8) class. An instance of the event data class is passed to the event handler for the OnFileReady event.

## ConfigValueProvider Class:

* Provides a clean way to access settings from appsettings.json

appsettings.json :

![](https://cdn-images-1.medium.com/max/2384/1*kBldW9Bok3RXzB_rxQs4Mg.png)

* initialTimerInterval : set the default timer interval

* permittedSecondsBetweenReadyEvents :

* LogFSWEvents : a flag, ON ->a new OnNewMessage event will be fired

* LogFileReadyEvents : a flag, ON ->a new OnNewMessage event will be fired

* FSWUseRegex : a flag, ON ->filter file path using regex

* FileTypes : a flag, ON ->filter specific fileTypes(*.csv,*.txt …)

![(Figure 6) part from appsettings.json](https://cdn-images-1.medium.com/max/2000/1*IcVC2SHTZcWV5ZhG_LMDaw.png)*(Figure 6) part from appsettings.json*

Let the magic begins…

![(Figure 7) program output when LogFSWEvents flag is off](https://cdn-images-1.medium.com/max/2980/1*voxC4bbxudy8NItvi5p6KA.png)*(Figure 7) program output when LogFSWEvents flag is off*
> *source code Link : [GitHub repository](https://github.com/eranltd/FilesWatcher_CSharp_Core).*

## Wrap-up

Thanks for reading this article, don’t forget to clap if you got something out of it!

You can grab a working demo of this code from my [*GitHub repository](https://github.com/eranltd/FilesWatcher_CSharp_Core)*.

Don’t hesitate to ping me if you have any questions about the article or ideas for how to improve it.

Thank you.

# Serilog.Enrichers.Thread [![Build status](https://ci.appveyor.com/api/projects/status/2vgxdy3swg6eaj3f?svg=true)](https://ci.appveyor.com/project/serilog/serilog-enrichers-thread) [![NuGet Version](http://img.shields.io/nuget/v/Serilog.Enrichers.Thread.svg?style=flat)](https://www.nuget.org/packages/Serilog.Enrichers.Thread/)

Enrich Serilog events with properties from the current thread.

### Getting started

Install the package from NuGet:

```powershell
Install-Package Serilog.Enrichers.Thread
```

In your logger configuration, apply `Enrich.WithThreadId()` and `Enrich.WithThreadName()`:

```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.WithThreadId()
    .Enrich.WithThreadName()
    .CreateLogger();
```

Many sinks simply include all properties without further action required, so the thread id will be logged automatically.
However, some sinks, such as the File and Console sinks use an output template and the new `ThreadId` may not be automatically output in your sink. In this case, in order for the `ThreadId` or `ThreadName` to show up in the logging, you will need to create or modify your output template. 

```csharp
w.File(...., outputTemplate:
  "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties}{NewLine}{Exception}")
```
Here, \{Properties} can include not only `ThreadId` and `ThreadName`, but any other enrichment which is applied. Alternatively, \{ThreadId} could be used instead, if you want to only add the thread id enrichment and \{ThreadName}, if you want to only add the thread name enrichment.

An example, which also uses the Serilogs.Sinks.Async Nuget package, is below:

```csharp
            Thread.CurrentThread.Name = "MyWorker";
              
            var logger = Log.Logger = new LoggerConfiguration()
                 .MinimumLevel.Debug()
                 .WriteTo.Console(restrictedToMinimumLevel:Serilog.Events.LogEventLevel.Information)
                 .WriteTo.Async(w=>w.File("..\\..\\..\\..\\logs\\SerilogLogFile.json", rollingInterval: RollingInterval.Day, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} <{ThreadId}><{ThreadName}>{NewLine}{Exception}"))
                 .Enrich.WithThreadId()
                 .CreateLogger();
  ```
  Which would produce an output in the log file as follows:
  ```
2018-04-06 13:12:45.684 +02:00 [ERR] The file file_name.svg does not exist <4><MyWorker>
  ```
Where, <4> is an example thread id and \<MyWorker> is an example thread name.

To use the enricher, first install the NuGet package:

```powershell
Install-Package Serilog.Enrichers.Thread
```

Note:
The \{ThreadName} property will only be attached when it is not null. Otherwise it will be omitted.
If you want to get this property always attached you can use the following:
```csharp
using Serilog.Enrichers;

Log.Logger = new LoggerConfiguration()
    .Enrich.WithThreadName()
    .Enrich.WithProperty(ThreadNameEnricher.ThreadNamePropertyName, "MyDefault")
    .CreateLogger();
```
The enrichment order is important. Otherwise "MyDefault" would always win.

Copyright &copy; 2016 Serilog Contributors - Provided under the [Apache License, Version 2.0](http://apache.org/licenses/LICENSE-2.0.html).

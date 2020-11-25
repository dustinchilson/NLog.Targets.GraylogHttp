# NLog.Targets.GraylogHttp
[![Codacy Badge](https://api.codacy.com/project/badge/Grade/d4e1107092654f41a7a513fc8954308e)](https://app.codacy.com/app/dustinchilson/NLog.Targets.GraylogHttp?utm_source=github.com&utm_medium=referral&utm_content=dustinchilson/NLog.Targets.GraylogHttp&utm_campaign=Badge_Grade_Dashboard)
[![Apache 2.0 licensed](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](https://github.com/dustinchilson/NLog.Targets.GraylogHttp/blob/master/LICENSE)
[![NuGet](https://img.shields.io/nuget/v/NLog.Targets.GraylogHttp.svg)](https://www.nuget.org/packages/NLog.Targets.GraylogHttp)
[![Dependabot Status](https://api.dependabot.com/badges/status?host=github&repo=dustinchilson/NLog.Targets.GraylogHttp)](https://dependabot.com)

This is a custom target for NLog that pushes log messages to [Graylog](https://www.graylog.org/) using the Http input.

This library was influenced by [EasyGelf](https://github.com/Pliner/EasyGelf)

## Installation

This library is packaged as a nuget package available [here](https://www.nuget.org/packages/NLog.Targets.GraylogHttp/)

```
Install-Package NLog.Targets.GraylogHttp
```

## NetStandard, .Net Core 2.1-5.0, .Net Framework 4.5

This library runs under netstandard 1.3 and fully supports .Net Core 2.1-3.1, .Net 5.0, and .Net Framework 4.5+

## Usage

Add or modify your NLog Configuration to add the new target and Extension Assembly.

```
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <extensions>
    <add assembly="NLog.Targets.GraylogHttp"/>
  </extensions>
  <targets>
    <target name="graylog"
              xsi:type="GraylogHttp"
              facility="[FACILITY]"
              graylogServer="[SERVER]"
              graylogPort="[PORT]">
        <!-- Location information. -->
        <parameter name="source_method" layout="${callsite}" />
        <parameter name="source_line" layout="${callsite-linenumber}" />

        <parameter name="test_prop" layout="${event-context:item=test_prop}" />
      </target>
  </targets>
  <rules>
    <logger name="*" minlevel="Trace" appendTo="graylog"/>
  </rules>
</nlog>
```

- *graylogServer* - REQUIRED - URI formatted address of your server (e.g. http://192.168.1.2, http://example.com, https://graylog.example.com:3030)
- *graylogPort* - OPTIONAL - server port, normally specified by Input in Graylog, Can also be specified in the server
- *facility* - OPTIONAL - variable could be used to identify your application, library, etc.

### Simple Logging

```
var logger = LogManager.GetCurrentClassLogger();
logger.Trace("String");
logger.Debug("String");
logger.Warn("String");
logger.Error("String");
logger.Fatal("String");
```

### Advanced Properties

```
var logger = LogManager.GetCurrentClassLogger();
var e = new LogEventInfo(LogLevel.Fatal, "Test", "Test Message");
e.Properties["test_prop"] = "test property";
logger.Log(e);
```

## Note

In order to receive logs in your Graylog server, make sure you create **Input** for your application mentioned in [Getting Started](http://docs.graylog.org/en/3.0/pages/getting_started.html) guide under Collect Messages link.

## References

[Gelf Spec](http://docs.graylog.org/en/latest/pages/gelf.html)

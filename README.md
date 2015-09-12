# NLog.Targets.GraylogHttp [![Build status](https://ci.appveyor.com/api/projects/status/4g5uup3i6p4kx5tr/branch/master?svg=true)](https://ci.appveyor.com/project/dustinchilson/nlog-targets-grayloghttp/branch/master)


This is a custom target for NLog that pushes log messages to Graylog using the Http input. 

This library is heavily influenced by [EasyGelf](https://github.com/Pliner/EasyGelf)

## Usage

Add or modify your NLog Configuration to add the new target and Extension Assembily.

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

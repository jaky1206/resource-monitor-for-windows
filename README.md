# Windows Resource Monitor

This project, developed around 2017, addresses the requirements of managing Windows-based devices handed down to users. The goal was to monitor system resource usage, browser history (Internet Explorer, Google Chrome, and Mozilla Firefox), and top-running processes. Data was reported to specific URLs at predefined time intervals. The management used this information to prevent unintended device usage.

## App Settings (from app.config)

```xml
<appSettings>
    <add key="entrepreneurId" value="" />
    <add key="timeInterval" value="15000" />
    <add key="notificationTimeInterval" value="1800000" />
    <add key="resourceUrl" value="http://example.com/data.php" />
    <add key="processUrl" value="http://example.com/process.php" />
    <add key="historyUrl" value="http://example.com/history.php" />
    <add key="notificationUrl" value="http://example.com/notification.php" />
    <add key="machineNameParam" value="machine" />
    <add key="deviceFingerprintParam" value="devicefingerprint" />
    <add key="cpuParam" value="cpu" />
    <add key="ramUsageParam" value="ramusage" />
    <add key="bandwidthParam" value="bandwidth" />
    <add key="processNameParam" value="processname" />
    <add key="browserNameParam" value="browsername" />
    <add key="historyTitleParam" value="historytitle" />
    <add key="historyUrlParam" value="historyurl" />
    <add key="historyVisitedTimeParam" value="historyvisitedtime" />
    <add key="entrepreneurIdParam" value="entrepreneurid" />
    <add key="ClientSettingsProvider.ServiceUri" value="" />
    <add key="totalRamParam" value="totalram" />
    <add key="browserNameAndVersionParam" value="browsernameandversion" />
    <add key="opSysNameAndVersionParam" value="osnameandversion" />
    <add key="processorArchitectureParam" value="processorarchitecture" />
    <add key="dbPrefix" value="Ji_5hsY69_" />
  </appSettings>
```

# Key Settings

- **deviceFingerprint**: Device fingerprint was generated to uniquely identify them.
- **entrepreneurId**: Identifier for the entrepreneur (if applicable)
- **timeInterval**: Time interval for data reporting
- **resourceUrl**, **processUrl**, **historyUrl**, **notificationUrl**: URLs for reporting data
- Parameters for various data types (e.g., `cpuParam`, `ramUsageParam`, `historyTitleParam`)

## Technologies Used

- .NET Framework 3.5
- SQLite

## Packages (from packages.config)

```xml
<packages>
  <package id="log4net" version="2.0.8" targetFramework="net35" />
  <package id="Newtonsoft.Json" version="10.0.3" targetFramework="net35" />
  <package id="System.Data.SQLite" version="1.0.105.2" targetFramework="net35" />
  <package id="System.Data.SQLite.Core" version="1.0.105.2" targetFramework="net35" />
  <package id="System.Data.SQLite.Linq" version="1.0.105.2" targetFramework="net35" />
</packages>
```





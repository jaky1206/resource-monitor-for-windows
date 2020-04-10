using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Timers;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using System.Threading;
using Microsoft.Win32;
using System.Runtime.InteropServices;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace WindowsResourceMonitor
{
    static class Program
    {

        static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static long bytesReceivedPrev = long.MinValue;
        static string machineName = getMachineName();
        static string deviceFingerprint = Fingerprint.Value();
        static float cpuPercent = float.MinValue;
        static double ramUsage = double.MinValue;
        static double totalRam = double.MinValue;
        static string browserNameAndVersion = String.Empty;
        static string opSysNameAndVersion = String.Empty;
        static string processorArchitecture = String.Empty;
        // will always return 0
        static double mBytesUsed = checkBandwidthUsage();

        //config
        static string entrepreneurId = GetEntrepreneurId();
        static int interval = int.TryParse(Convert.ToInt32(ConfigurationManager.AppSettings["timeInterval"]).ToString(), out interval) ? interval : 60000;
        static int notificationTimeInterval = int.TryParse(Convert.ToInt32(ConfigurationManager.AppSettings["notificationTimeInterval"]).ToString(), out notificationTimeInterval) ? notificationTimeInterval : 1800000;
        static string resourceUrl = ConfigurationManager.AppSettings["resourceUrl"].ToString();
        static string processUrl = ConfigurationManager.AppSettings["processUrl"].ToString();
        static string historyUrl = ConfigurationManager.AppSettings["historyUrl"].ToString();
        static string notificationUrl = ConfigurationManager.AppSettings["notificationUrl"].ToString();
        static string machineNameParam = ConfigurationManager.AppSettings["machineNameParam"].ToString();
        static string deviceFingerprintParam = ConfigurationManager.AppSettings["deviceFingerprintParam"].ToString();
        static string cpuPercName = ConfigurationManager.AppSettings["cpuParam"].ToString();
        static string ramUsageParam = ConfigurationManager.AppSettings["ramUsageParam"].ToString();
        static string bandwidthParam = ConfigurationManager.AppSettings["bandwidthParam"].ToString();
        static string processNameParam = ConfigurationManager.AppSettings["processNameParam"].ToString();
        static string browserNameParam = ConfigurationManager.AppSettings["browserNameParam"].ToString();
        static string historyTitleParam = ConfigurationManager.AppSettings["historyTitleParam"].ToString();
        static string historyUrlParam = ConfigurationManager.AppSettings["historyUrlParam"].ToString();
        static string historyVisitedTimeParam = ConfigurationManager.AppSettings["historyVisitedTimeParam"].ToString();
        static string entrepreneurIdParam = ConfigurationManager.AppSettings["entrepreneurIdParam"].ToString();
        static string totalRamParam = ConfigurationManager.AppSettings["totalRamParam"].ToString();
        static string browserNameAndVersionParam = ConfigurationManager.AppSettings["browserNameAndVersionParam"].ToString();
        static string opSysNameAndVersionParam = ConfigurationManager.AppSettings["opSysNameAndVersionParam"].ToString();
        static string processorArchitectureParam = ConfigurationManager.AppSettings["processorArchitectureParam"].ToString();
        static string dbPrefix = ConfigurationManager.AppSettings["dbPrefix"].ToString();

        //db
        static readonly string _appDirectoryName = "Entertech";
        static readonly string _dbFile = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\" + _appDirectoryName + @"\" + dbPrefix + "windows_resource_monitor.sqlite";
        static readonly string _dbConnectionString = "Data Source=" + _dbFile + ";Version=3;New=False;Compress=True;datetimeformat=CurrentCulture;";

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (ProcessIcon pi = new ProcessIcon())
            {
                pi.Display();

                CreateAndSeedDatabase();

                var worker = new BackgroundWorker();
                worker.WorkerReportsProgress = false;
                worker.WorkerSupportsCancellation = false;
                worker.DoWork += worker_DoWork;
                //worker.RunWorkerCompleted += worker_RunWorkerCompleted;
                worker.RunWorkerAsync();

                var notificationWorker = new BackgroundWorker();
                notificationWorker.WorkerReportsProgress = false;
                notificationWorker.WorkerSupportsCancellation = false;
                notificationWorker.DoWork += notificationWorker_DoWork;
                //notificationWorker.RunWorkerCompleted += notificationWorker_RunWorkerCompleted;
                notificationWorker.RunWorkerAsync();

                // Make sure the application runs!
                while (!IsEntrepreneurIdValid())
                {
                    EntrepreneurIdInsertForm entrepreneurIdInsertForm = new EntrepreneurIdInsertForm();
                    entrepreneurIdInsertForm.ShowDialog();
                }
                Application.Run();
            }
        }
        static void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            System.Timers.Timer aTimer;
            aTimer = new System.Timers.Timer();
            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            aTimer.Interval = interval;
            aTimer.Enabled = true;
        }
        static void notificationWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            System.Timers.Timer aTimer;
            aTimer = new System.Timers.Timer();
            aTimer.Elapsed += new ElapsedEventHandler(NotificationOnTimedEvent);
            aTimer.Interval = notificationTimeInterval;
            aTimer.Enabled = true;
        }
        static void NotificationOnTimedEvent(object source, ElapsedEventArgs e)
        {
            if (CheckForInternetConnection(notificationUrl))
            {
                GetNotification(notificationUrl);
            }
        }
        static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            PhysicalMemoryInfo physicalMemoryInfo = getPhysicalMemoryInfo();
            ramUsage = Math.Round(physicalMemoryInfo.physicalMemoryUsagePercentage, 2);
            mBytesUsed = checkBandwidthUsage();
            cpuPercent = getTotalCPUUsage();
            totalRam = Math.Round((physicalMemoryInfo.totalVisibleMemorySize / 1024) / 1024, 0);
            browserNameAndVersion = GetInstalledBrowserList();
            opSysNameAndVersion = FriendlyNameOfOperatinfSystem();
            processorArchitecture = String.Format("{0} {1}", getPrcossorInformation(), (IsOS64Bit() ? "64 bit" : "32 bit"));

            if (CheckForInternetConnection(resourceUrl))
            {
                List<ResourceUsageLog> resourceUsageLogs = GetResourceUsageLogs();
                if (resourceUsageLogs.Count > 0)
                {
                    foreach (ResourceUsageLog log in resourceUsageLogs)
                    {
                        sendResultThroughURL(resourceUrl, machineNameParam + "=" + log.MachineName + "&" + entrepreneurIdParam + "=" + entrepreneurId + "&" + deviceFingerprintParam + "=" + log.PhysicalAddress + "&" + cpuPercName + "=" + log.CpuUsage + "&" + ramUsageParam + "=" + log.RamUsage + "&" + bandwidthParam + "=" + log.IntBandWidthUsage + "&" + totalRamParam + "=" + log.TotalRam + "&" + browserNameAndVersionParam + "=" + log.BrowserNameAndVersion + "&" + opSysNameAndVersionParam + "=" + log.OpSysNameAndVersion + "&" + processorArchitectureParam + "=" + log.ProcessorArchitecture);
                        RemoveResourceUsageLog(log.Id);
                    }
                }
                sendResultThroughURL(resourceUrl, machineNameParam + "=" + machineName + "&" + entrepreneurIdParam + "=" + entrepreneurId + "&" + deviceFingerprintParam + "=" + deviceFingerprint + "&" + cpuPercName + "=" + cpuPercent + "&" + ramUsageParam + "=" + ramUsage + "&" + bandwidthParam + "=" + mBytesUsed + "&" + totalRamParam + "=" + totalRam + "&" + browserNameAndVersionParam + "=" + browserNameAndVersion + "&" + opSysNameAndVersionParam + "=" + opSysNameAndVersion + "&" + processorArchitectureParam + "=" + processorArchitecture);
            }
            else
            {
                InsertResourceUsageLog(cpuPercent, ramUsage, mBytesUsed, totalRam, browserNameAndVersion, opSysNameAndVersion, processorArchitecture);
            }
            #region Functional Code for future
            /*
            List<string> processNameList = GetRunningProcessList();
            if (processNameList.Count > 0)
            {
                if (CheckForInternetConnection(processUrl))
                {
                    Dictionary<string, string> storedProcessNameList = GetRunningProcessLogs();
                    if (storedProcessNameList.Count > 0)
                    {
                        foreach (KeyValuePair<string, string> process in storedProcessNameList)
                        {
                            sendResultThroughURL(processUrl, machineNameParam + "=" + machineName + "&" + entrepreneurIdParam + "=" + entrepreneurId + "&" + deviceFingerprintParam + "=" + deviceFingerprint + "&" + processNameParam + "=" + process.Value);
                            RemoveRunningProcessLog(process.Key);
                        }
                    }
                    foreach (string processName in processNameList)
                    {
                        sendResultThroughURL(processUrl, machineNameParam + "=" + machineName + "&" + entrepreneurIdParam + "=" + entrepreneurId + "&" + deviceFingerprintParam + "=" + deviceFingerprint + "&" + processNameParam + "=" + processName);                    }
                }
                else
                {
                    foreach (string processName in processNameList)
                    {
                        InsertRunningProcessLog(processName);
                    }
                }
            }

            RemovePreviousBrowsingHistoryLogs();
            if (GetBrowsingHistoryLogCount() > 0)
            {
                List<HistoryItem> internetBrowsingHistoryLogs = GetBrowsingHistoryLogs();
                if (internetBrowsingHistoryLogs.Count > 0)
                {
                    foreach (HistoryItem log in internetBrowsingHistoryLogs)
                    {
                        if (CheckForInternetConnection(historyUrl))
                        {
                            sendResultThroughURL(historyUrl, machineNameParam + "=" + log.MachineName + "&" + entrepreneurIdParam + "=" + entrepreneurId + "&" + browserNameParam + "=" + log.BrowserName + "&" + historyTitleParam + "=" + log.Title + "&" + historyUrlParam + "=" + log.URL + "&" + historyVisitedTimeParam + "=" + log.VisitedTime.ToString());
                            UpdateBrowsingHistoryLog(log.Id);
                        }
                    }
                }
            }
            else
            {
                List<HistoryItem> chromeHistoryItemList = ChromeHistory();
                if (chromeHistoryItemList.Count > 0)
                {
                    foreach (HistoryItem item in chromeHistoryItemList)
                    {
                        InsertBrowsingHistoryLog(item);
                    }
                }
                List<HistoryItem> ieHistoryItemList = GetIeHistory();
                if (ieHistoryItemList.Count > 0)
                {
                    foreach (HistoryItem item in ieHistoryItemList)
                    {
                        InsertBrowsingHistoryLog(item);
                    }
                }
                List<HistoryItem> firefoxHistoryItemList = GetFirefoxHistory();
                if (firefoxHistoryItemList.Count > 0)
                {
                    foreach (HistoryItem item in firefoxHistoryItemList)
                    {
                        InsertBrowsingHistoryLog(item);
                    }
                }
            }
            */
            #endregion

        }
        static string getMachineName()
        {
            return System.Environment.MachineName;
        }
        static bool IsEntrepreneurIdValid()
        {
            entrepreneurId = GetEntrepreneurId();
            return (entrepreneurId.Trim() == "") ? false : true;
        }
        static string GetPhysicalAddress()
        {
            ManagementObjectSearcher objMOS = new ManagementObjectSearcher("Select * FROM Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection objMOC = objMOS.Get();
            string macAddress = String.Empty;
            foreach (ManagementObject objMO in objMOC)
            {
                object tempMacAddrObj = objMO["MacAddress"];

                if (tempMacAddrObj == null) //Skip objects without a MACAddress
                {
                    continue;
                }
                if (macAddress == String.Empty) // only return MAC Address from first card that has a MAC Address
                {
                    macAddress = tempMacAddrObj.ToString();
                }
                objMO.Dispose();
            }
            macAddress = macAddress.Replace(":", "");
            return macAddress;
        }
        static double checkBandwidthUsage()
        {
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            long bytesReceived = 0;
            foreach (NetworkInterface inf in interfaces)
            {
                if (inf.OperationalStatus == OperationalStatus.Up &&
                    inf.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    inf.NetworkInterfaceType != NetworkInterfaceType.Tunnel &&
                    inf.NetworkInterfaceType != NetworkInterfaceType.Unknown && !inf.IsReceiveOnly)
                {
                    bytesReceived += inf.GetIPv4Statistics().BytesReceived;
                }
            }

            if (bytesReceivedPrev == 0)
            {
                bytesReceivedPrev = bytesReceived;
            }
            long bytesUsed = bytesReceived - bytesReceivedPrev;
            double kBytesUsed = bytesUsed / 1024;
            double mBytesUsed = kBytesUsed / 1024;
            bytesReceivedPrev = bytesReceived;
            return mBytesUsed;
        }
        static float getTotalCPUUsage()
        {
            PerformanceCounter cpuCounter = new PerformanceCounter();
            cpuCounter.CategoryName = "Processor";
            cpuCounter.CounterName = "% Processor Time";
            cpuCounter.InstanceName = "_Total";
            cpuCounter.NextValue();
            System.Threading.Thread.Sleep(1000);
            return cpuCounter.NextValue();
        }
        static PhysicalMemoryInfo getPhysicalMemoryInfo()
        {
            PhysicalMemoryInfo memoryUsage = new PhysicalMemoryInfo();
            var wmiObject = new ManagementObjectSearcher("select * from Win32_OperatingSystem");

            var memoryValues = wmiObject.Get().Cast<ManagementObject>().Select(mo => new
            {
                FreePhysicalMemory = Double.Parse(mo["FreePhysicalMemory"].ToString()),
                TotalVisibleMemorySize = Double.Parse(mo["TotalVisibleMemorySize"].ToString())
            }).FirstOrDefault();

            if (memoryValues != null)
            {
                memoryUsage.freePhysicalMemory = memoryValues.FreePhysicalMemory;
                memoryUsage.totalVisibleMemorySize = memoryValues.TotalVisibleMemorySize;
                memoryUsage.physicalMemoryUsagePercentage = ((memoryValues.TotalVisibleMemorySize - memoryValues.FreePhysicalMemory) / memoryValues.TotalVisibleMemorySize) * 100;
            }
            return memoryUsage;
        }
        static HttpWebResponse sendResultThroughURL(string url, string parameters)

        {
            url += "?";
            parameters = Uri.EscapeUriString(parameters);
            HttpWebRequest request = WebRequest.Create(url + parameters) as HttpWebRequest;
            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                return response;
            }
        }
        static string postResultToURL(string url, Dictionary<string, string> parameters)
        {
            using (WebClient client = new WebClient())
            {
                var reqparm = new System.Collections.Specialized.NameValueCollection();
                foreach (KeyValuePair<string, string> pair in parameters)
                {
                    reqparm.Add(pair.Key, Uri.EscapeUriString(pair.Value));
                }
                reqparm.Add("param1", "<any> kinds & of = ? strings");
                reqparm.Add("param2", "escaping is already handled");
                byte[] responsebytes = client.UploadValues(url, "POST", reqparm);
                string responsebody = Encoding.UTF8.GetString(responsebytes);
                return responsebody;
            }
        }
        static bool CheckForInternetConnection(string url)
        {
            try
            {
                using (var client = new WebClient())
                using (var stream = client.OpenRead(url))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
        static void GetNotification(string url)
        {
            url += "?" + entrepreneurIdParam + "=" + entrepreneurId;
            using (var webClient = new System.Net.WebClient())
            {
                JArray result = JArray.Parse(webClient.DownloadString(url));
                if (result.Count > 0)
                {
                    IList<Notification> notificationList = result.Select(p => new Notification
                    {
                        id = (string)p["id"],
                        entrepreneur_id = (string)p["entrepreneur_id"],
                        status = (string)p["status"],
                        notificationtime = (DateTime)p["notificationtime"],
                        notification = (string)p["notification"]
                    }).ToList();

                    foreach (Notification notification in notificationList)
                    {
                        ShowNotification("Notification", notification.notification);
                    }
                }
            }
        }
        static void ShowNotification(string title, string body)
        {
            var notification = new System.Windows.Forms.NotifyIcon()
            {
                Visible = true,
                Icon = System.Drawing.SystemIcons.Information,
                // optional - BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info,
                BalloonTipTitle = title,
                BalloonTipText = body,
            };

            notification.ShowBalloonTip(notificationTimeInterval);

            // This will let the balloon close after it's 5 second timeout
            // for demonstration purposes. Comment this out to see what happens
            // when dispose is called while a balloon is still visible.
            //Thread.Sleep(notificationTimeInterval);

            // The notification should be disposed when you don't need it anymore,
            // but doing so will immediately close the balloon if it's visible.
            notification.Dispose();
        }
        static void ExecuteNonQuery(string statement)
        {
            try
            {
                using (SQLiteConnection _dbConnection = new SQLiteConnection(_dbConnectionString))
                {
                    _dbConnection.Open();

                    using (var cmd = new SQLiteCommand(_dbConnection))
                    {
                        cmd.CommandText = statement;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
            }
        }
        static void CreateAndSeedDatabase()
        {
            try
            {
                Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\" + _appDirectoryName);
                if (!System.IO.File.Exists(_dbFile))
                {
                    SQLiteConnection.CreateFile(_dbFile);
                }
                string sql = @"CREATE TABLE IF NOT EXISTS ResourceUsageLogs (
                            Id VARCHAR(250) NOT NULL PRIMARY KEY,
                            MachineName VARCHAR(150) NOT NULL,
                            DeviceFingerprint VARCHAR(50) NOT NULL,
                            CpuUsage FLOAT NOT NULL,
                            RamUsage DOUBLE NOT NULL,
                            IntBandWidthUsage DOUBLE NOT NULL,
                            TotalRam DOUBLE NOT NULL,
                            BrowserNameAndVersion VARCHAR(400) NOT NULL,
                            OpSysNameAndVersion VARCHAR(150) NULL,
                            ProcessorArchitecture VARCHAR(50) NULL,
                            DateCreated TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                        )";
                ExecuteNonQuery(sql);
                sql = @"CREATE TABLE IF NOT EXISTS BrowsingHistoryLogs (
                            Id VARCHAR(250) NOT NULL PRIMARY KEY,
                            MachineName VARCHAR(150) NOT NULL,
                            DeviceFingerprint VARCHAR(50) NOT NULL,
                            BrowserName VARCHAR(150) NOT NULL,
                            Url TEXT NOT NULL,
                            Title TEXT NOT NULL,
                            VisitedTime TEXT NOT NULL,
                            IsSent INT DEFAULT 0
                        )";
                ExecuteNonQuery(sql);
                sql = @"CREATE TABLE IF NOT EXISTS RunningProcessLogs (
                            Id VARCHAR(250) NOT NULL PRIMARY KEY,
                            MachineName VARCHAR(150) NOT NULL,
                            DeviceFingerprint VARCHAR(50) NOT NULL,
                            ProcessName TEXT NOT NULL,
                            DateCreated TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                        )";
                ExecuteNonQuery(sql);
                sql = @"CREATE TABLE IF NOT EXISTS EntrepreneurIdStore (
                            Id VARCHAR(250) NOT NULL PRIMARY KEY,
                            EntrepreneurId VARCHAR(150) NOT NULL
                        )";
                ExecuteNonQuery(sql);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
            }
        }
        static string GetEntrepreneurId()
        {
            string entrepreneurId = String.Empty;
            try
            {
                using (SQLiteConnection con = new SQLiteConnection(_dbConnectionString))
                {
                    con.Open();
                    string stm = "SELECT EntrepreneurId FROM EntrepreneurIdStore LIMIT 1";
                    using (SQLiteCommand cmd = new SQLiteCommand(stm, con))
                    {
                        entrepreneurId = Convert.ToString(cmd.ExecuteScalar());
                    }

                    con.Close();
                }
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
            }
            return entrepreneurId;
        }
        static void InsertResourceUsageLog(float cpuPercent, double ramUsage, double mBytesUsed, double totalRam, string browserNameAndVersion, string opSysNameAndVersion, string processorArchitecture)
        {
            Guid Id = Guid.NewGuid();
            string statement = "INSERT INTO ResourceUsageLogs (Id,MachineName,DeviceFingerprint,CpuUsage,RamUsage,IntBandWidthUsage,TotalRam,BrowserNameAndVersion,OpSysNameAndVersion,ProcessorArchitecture) VALUES ('" + Id.ToString() + "','" + machineName + "','" + deviceFingerprint + "'," + cpuPercent + "," + ramUsage + "," + mBytesUsed + "," + totalRam + ",'" + browserNameAndVersion + "','" + opSysNameAndVersion + "','" + processorArchitecture + "')";
            ExecuteNonQuery(statement);
        }
        static List<ResourceUsageLog> GetResourceUsageLogs()
        {
            List<ResourceUsageLog> result = new List<ResourceUsageLog>();
            try
            {
                using (SQLiteConnection con = new SQLiteConnection(_dbConnectionString))
                {
                    con.Open();
                    string stm = "SELECT * FROM ResourceUsageLogs";
                    using (SQLiteCommand cmd = new SQLiteCommand(stm, con))
                    {
                        using (SQLiteDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                ResourceUsageLog log = new ResourceUsageLog();
                                log.Id = rdr["Id"].ToString();
                                log.MachineName = rdr["MachineName"].ToString();
                                log.PhysicalAddress = rdr["DeviceFingerprint"].ToString();
                                log.CpuUsage = rdr["CpuUsage"].ToString();
                                log.RamUsage = rdr["RamUsage"].ToString();
                                log.IntBandWidthUsage = rdr["IntBandWidthUsage"].ToString();
                                log.TotalRam = rdr["TotalRam"].ToString();
                                log.BrowserNameAndVersion = rdr["BrowserNameAndVersion"].ToString();
                                log.CpuUsage = rdr["OpSysNameAndVersion"].ToString();
                                log.ProcessorArchitecture = rdr["ProcessorArchitecture"].ToString();
                                log.DateCreated = Convert.ToDateTime(rdr["DateCreated"]);
                                result.Add(log);
                            }
                        }
                    }

                    con.Close();
                }
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
            }
            return result;
        }
        static void RemoveResourceUsageLog(string resourceUsageLogId)
        {
            ExecuteNonQuery("DELETE FROM ResourceUsageLogs WHERE Id='" + resourceUsageLogId + "'");
        }
        static void InsertBrowsingHistoryLog(HistoryItem item)
        {
            Guid Id = Guid.NewGuid();
            var statement = "INSERT INTO BrowsingHistoryLogs (Id,MachineName,DeviceFingerprint,BrowserName,Url,Title,VisitedTime) VALUES ('" + Id.ToString() + "', '" + item.MachineName + "','" + item.DeviceFingerprint + "','" + item.BrowserName + "','" + item.URL.Replace("'", "''") + "','" + item.Title.Replace("'", "''") + "','" + String.Format("{0:yyyy-MM-dd HH:mm:ss}", item.VisitedTime) + "')";
            ExecuteNonQuery(statement);
        }
        static int GetBrowsingHistoryLogCount()
        {
            int count = 0;
            try
            {
                using (SQLiteConnection con = new SQLiteConnection(_dbConnectionString))
                {
                    con.Open();
                    string stm = "SELECT COUNT(*) FROM BrowsingHistoryLogs";
                    using (SQLiteCommand cmd = new SQLiteCommand(stm, con))
                    {
                        count = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    con.Close();
                }
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
            }
            return count;
        }
        static List<HistoryItem> GetBrowsingHistoryLogs()
        {
            List<HistoryItem> result = new List<HistoryItem>();
            try
            {
                using (SQLiteConnection con = new SQLiteConnection(_dbConnectionString))
                {
                    con.Open();
                    string stm = "SELECT * FROM BrowsingHistoryLogs WHERE IsSent = 0";
                    using (SQLiteCommand cmd = new SQLiteCommand(stm, con))
                    {
                        using (SQLiteDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                HistoryItem item = new HistoryItem();
                                item.Id = rdr["Id"].ToString();
                                item.MachineName = rdr["MachineName"].ToString();
                                item.DeviceFingerprint = rdr["DeviceFingerprint"].ToString();
                                item.BrowserName = rdr["BrowserName"].ToString();
                                item.Title = rdr["Title"].ToString();
                                item.URL = rdr["URL"].ToString();
                                item.VisitedTime = Convert.ToDateTime(rdr["VisitedTime"]);
                                var y = rdr["IsSent"];
                                item.IsSent = Convert.ToInt32(rdr["IsSent"]) == 0 ? false : true;
                                result.Add(item);
                            }
                        }
                    }

                    con.Close();
                }
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
            }
            return result;
        }
        static void UpdateBrowsingHistoryLog(string Id)
        {
            var statement = "UPDATE BrowsingHistoryLogs SET IsSent = 1 WHERE Id='" + Id + "'";
            ExecuteNonQuery(statement);
        }
        static void RemovePreviousBrowsingHistoryLogs()
        {
            var statement = "DELETE FROM BrowsingHistoryLogs WHERE strftime('%Y-%m-%d',VisitedTime)=date('now','localtime','-2 day')";
            ExecuteNonQuery(statement);
        }
        static void InsertRunningProcessLog(string processName)
        {
            Guid Id = Guid.NewGuid();
            var statement = "INSERT INTO RunningProcessLogs (Id,MachineName,DeviceFingerprint,ProcessName) VALUES ('" + Id.ToString() + "', '" + machineName + "','" + deviceFingerprint + "','" + processName + "')";
            ExecuteNonQuery(statement);
        }
        static Dictionary<string, string> GetRunningProcessLogs()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            try
            {
                using (SQLiteConnection con = new SQLiteConnection(_dbConnectionString))
                {
                    con.Open();
                    string stm = "SELECT * FROM RunningProcessLogs";
                    using (SQLiteCommand cmd = new SQLiteCommand(stm, con))
                    {
                        using (SQLiteDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                result.Add(rdr["Id"].ToString(), rdr["ProcessName"].ToString());
                            }
                        }
                    }

                    con.Close();
                }
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
            }
            return result;
        }
        static void RemoveRunningProcessLog(string runningProcessLogId)
        {
            ExecuteNonQuery("DELETE FROM RunningProcessLogs WHERE Id='" + runningProcessLogId + "'");
        }
        static List<HistoryItem> ChromeHistory()
        {
            string chromeHistoryFile = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Google\Chrome\User Data\Default\History";
            //log.Info(chromeHistoryFile);
            var allHistoryItems = new List<HistoryItem>();
            if (File.Exists(chromeHistoryFile))
            {
                try
                {
                    SQLiteConnection connection = new SQLiteConnection
                ("Data Source=" + chromeHistoryFile + ";Version=3;New=False;Compress=True;");

                    connection.Open();

                    DataSet dataset = new DataSet();

                    SQLiteDataAdapter adapter = new SQLiteDataAdapter
                    ("select * from urls where date((last_visit_time/1000000)-11644473600, 'unixepoch', 'localtime') = date('now','localtime','-1 day')", connection);
                    adapter.Fill(dataset);
                    if (dataset != null && dataset.Tables.Count > 0 & dataset.Tables[0] != null)
                    {
                        DataTable dt = dataset.Tables[0];
                        foreach (DataRow historyRow in dt.Rows)
                        {
                            HistoryItem historyItem = new HistoryItem();
                            historyItem.URL = Convert.ToString(historyRow["url"]);
                            historyItem.Title = Convert.ToString(historyRow["title"]);
                            // Chrome stores time elapsed since Jan 1, 1601 (UTC format) in microseconds
                            long utcMicroSeconds = Convert.ToInt64(historyRow["last_visit_time"]);
                            // Windows file time UTC is in nanoseconds, so multiplying by 10
                            DateTime gmtTime = DateTime.FromFileTimeUtc(10 * utcMicroSeconds);
                            // Converting to local time
                            DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(gmtTime, TimeZoneInfo.Local);
                            historyItem.VisitedTime = localTime;
                            historyItem.MachineName = machineName;
                            historyItem.DeviceFingerprint = deviceFingerprint;
                            historyItem.BrowserName = "chrome";
                            historyItem.IsSent = false;
                            allHistoryItems.Add(historyItem);
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex.Message);
                }
            }
            return allHistoryItems;
        }
        static List<string> GetRunningProcessList()
        {
            List<string> processList = new List<string>();
            Process[] processes = Process.GetProcesses();
            foreach (Process p in processes)
            {
                if (!String.IsNullOrEmpty(p.MainWindowTitle))
                {
                    processList.Add(p.ProcessName);
                }
            }
            return processList;
        }
        static List<HistoryItem> GetIeHistory()
        {
            List<HistoryItem> allHistoryItems = new List<HistoryItem>();
            Dictionary<string, IeHistoryEntry> historyList = IeHistory.GetURLCache();
            foreach (KeyValuePair<string, IeHistoryEntry> history in historyList)
            {
                if (history.Value.Type.ToString().Contains(EntryType.UrlHistory.ToString()) && (Convert.ToDateTime(history.Value.LastAccessDate).Date == DateTime.Now.AddDays(-1).Date))
                {
                    HistoryItem historyItem = new HistoryItem();
                    historyItem.URL = history.Key;
                    historyItem.Title = history.Key;
                    historyItem.VisitedTime = Convert.ToDateTime(history.Value.LastAccessDate);
                    historyItem.BrowserName = "ie";
                    historyItem.IsSent = false;
                    historyItem.MachineName = machineName;
                    historyItem.DeviceFingerprint = deviceFingerprint;
                    allHistoryItems.Add(historyItem);
                }

            }
            return allHistoryItems;
        }
        static List<HistoryItem> GetFirefoxHistory()
        {
            string documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Mozilla\Firefox\Profiles\";
            var allHistoryItems = new List<HistoryItem>();
            if (Directory.Exists(documentsFolder))
            {
                foreach (string folder in Directory.GetDirectories(documentsFolder))
                {
                    string firefoxHistoryFile = folder + @"\places.sqlite";
                    if (File.Exists(firefoxHistoryFile))
                    {
                        try
                        {
                            SQLiteConnection connection = new SQLiteConnection
                        ("Data Source=" + firefoxHistoryFile + ";Version=3;New=False;Compress=True;");

                            connection.Open();

                            DataSet dataset = new DataSet();

                            SQLiteDataAdapter adapter = new SQLiteDataAdapter
                            ("SELECT * FROM moz_places WHERE date(last_visit_date/1000000,'unixepoch','localtime') = date('now','localtime','-1 day')", connection);
                            adapter.Fill(dataset);
                            if (dataset != null && dataset.Tables.Count > 0 & dataset.Tables[0] != null)
                            {
                                DataTable dt = dataset.Tables[0];
                                foreach (DataRow historyRow in dt.Rows)
                                {
                                    HistoryItem historyItem = new HistoryItem();
                                    historyItem.URL = Convert.ToString(historyRow["url"]);
                                    historyItem.Title = Convert.ToString(historyRow["title"]);
                                    long utcMicroSeconds = Convert.ToInt64(historyRow["last_visit_date"]);
                                    DateTime gmtTime = DateTime.FromFileTimeUtc(10 * utcMicroSeconds);
                                    DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(gmtTime, TimeZoneInfo.Local);
                                    historyItem.VisitedTime = localTime;
                                    historyItem.MachineName = machineName;
                                    historyItem.DeviceFingerprint = deviceFingerprint;
                                    historyItem.BrowserName = "firefox";
                                    historyItem.IsSent = false;
                                    allHistoryItems.Add(historyItem);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            log.Error(ex.Message);
                        }
                    }
                }
            }
            return allHistoryItems;
        }
        #region FriendlyNameOfOperatinfSystem
        static string HKLM_GetString(string path, string key)
        {
            try
            {
                RegistryKey rk = Registry.LocalMachine.OpenSubKey(path);
                if (rk == null) return "";
                return (string)rk.GetValue(key);
            }
            catch { return ""; }
        }

        static string FriendlyNameOfOperatinfSystem()
        {
            string ProductName = HKLM_GetString(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName");
            string CSDVersion = HKLM_GetString(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CSDVersion");
            if (ProductName != "")
            {
                return (ProductName.StartsWith("Microsoft") ? "" : "Microsoft ") + ProductName +
                            (CSDVersion != "" ? " " + CSDVersion : "");
            }
            return "";
        }
        #endregion
        #region Installed Browser List
        ///
        /// if string begins and ends with quotes, they are removed
        ///
        internal static String StripQuotes(this String s)
        {
            if (s.EndsWith("\"") && s.StartsWith("\""))
            {
                return s.Substring(1, s.Length - 2);
            }
            else
            {
                return s;
            }
        }
        static List<Browser> GetBrowsers()
        {
            RegistryKey browserKeys;
            //on 64bit the browsers are in a different location
            browserKeys = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Clients\StartMenuInternet");
            if (browserKeys == null)
                browserKeys = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Clients\StartMenuInternet");
            string[] browserNames = browserKeys.GetSubKeyNames();
            var browsers = new List<Browser>();
            for (int i = 0; i < browserNames.Length; i++)
            {
                Browser browser = new Browser();
                RegistryKey browserKey = browserKeys.OpenSubKey(browserNames[i]);
                browser.Name = (string)browserKey.GetValue(null);
                RegistryKey browserKeyPath = browserKey.OpenSubKey(@"shell\open\command");
                browser.Path = (string)browserKeyPath.GetValue(null).ToString().StripQuotes();
                RegistryKey browserIconPath = browserKey.OpenSubKey(@"DefaultIcon");
                browser.IconPath = (string)browserIconPath.GetValue(null).ToString().StripQuotes();
                browsers.Add(browser);
                if (browser.Path != null)
                    browser.Version = FileVersionInfo.GetVersionInfo(browser.Path).FileVersion;
                else
                    browser.Version = "unknown";
            }
            return browsers;
        }
        static string GetInstalledBrowserList()
        {
            StringBuilder installedBrowserList = new StringBuilder();
            installedBrowserList.Append("|");
            foreach (Browser browser in GetBrowsers())
            {
                installedBrowserList.Append(browser.Name);
                installedBrowserList.Append(" ");
                installedBrowserList.Append(browser.Version);
                installedBrowserList.Append("|");
            }
            return installedBrowserList.ToString();
        }
        #endregion
        #region Processor Architecture
        [DllImport("kernel32", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        public extern static IntPtr LoadLibrary(string libraryName);

        [DllImport("kernel32", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        public extern static IntPtr GetProcAddress(IntPtr hwnd, string procedureName);

        private delegate bool IsWow64ProcessDelegate([In] IntPtr handle, [Out] out bool isWow64Process);
        public static bool IsOS64Bit()
        {
            if (IntPtr.Size == 8 || (IntPtr.Size == 4 && Is32BitProcessOn64BitProcessor()))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private static IsWow64ProcessDelegate GetIsWow64ProcessDelegate()
        {
            IntPtr handle = LoadLibrary("kernel32");

            if (handle != IntPtr.Zero)
            {
                IntPtr fnPtr = GetProcAddress(handle, "IsWow64Process");

                if (fnPtr != IntPtr.Zero)
                {
                    return (IsWow64ProcessDelegate)Marshal.GetDelegateForFunctionPointer((IntPtr)fnPtr, typeof(IsWow64ProcessDelegate));
                }
            }

            return null;
        }
        private static bool Is32BitProcessOn64BitProcessor()
        {
            IsWow64ProcessDelegate fnDelegate = GetIsWow64ProcessDelegate();

            if (fnDelegate == null)
            {
                return false;
            }

            bool isWow64;
            bool retVal = fnDelegate.Invoke(Process.GetCurrentProcess().Handle, out isWow64);

            if (retVal == false)
            {
                return false;
            }

            return isWow64;
        }
        #endregion
        #region Processor Information
        static string getPrcossorInformation()
        {
            StringBuilder processorInformation = new StringBuilder();
            using (ManagementObjectSearcher win32Proc = new ManagementObjectSearcher("select * from Win32_Processor"),
                win32CompSys = new ManagementObjectSearcher("select * from Win32_ComputerSystem"),
                win32Memory = new ManagementObjectSearcher("select * from Win32_PhysicalMemory"))
            {
                foreach (ManagementObject obj in win32Proc.Get())
                {
                    //processorInformation.Append(obj["CurrentClockSpeed"].ToString());
                    //processorInformation.Append(" ");
                    processorInformation.Append(obj["Name"].ToString());
                    //processorInformation.Append(" ");
                    //processorInformation.Append(obj["Manufacturer"].ToString());
                    //processorInformation.Append(" ");
                    //processorInformation.Append(obj["Version"].ToString());
                    //processorInformation.Append(" ");
                }
            }
            return processorInformation.ToString(); ;
        }
        #endregion
    }
}
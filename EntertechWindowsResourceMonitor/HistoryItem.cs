using System;


namespace WindowsResourceMonitor
{
    public class HistoryItem
    {
        public string Id { get; set; }
        public string MachineName { get; set; }
        public string DeviceFingerprint { get; set; }
        public string URL { get; set; }
        public string Title { get; set; }
        public DateTime VisitedTime { get; set; }
        public string BrowserName { get; set; }
        public bool IsSent { get; set; }
        
    }
}

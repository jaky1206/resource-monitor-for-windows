using System;

namespace WindowsResourceMonitor
{
    public class ResourceUsageLog
    {
        public string Id { get; set; }
        public string MachineName { get; set; }
        public string PhysicalAddress { get; set; }
        public string CpuUsage { get; set; }
        public string RamUsage { get; set; }
        public string IntBandWidthUsage { get; set; }
        public string TotalRam { get; set; }
        public string BrowserNameAndVersion { get; set; }
        public string OpSysNameAndVersion { get; set; }
        public string ProcessorArchitecture { get; set; }
        public DateTime DateCreated { get; set; }
    }
}

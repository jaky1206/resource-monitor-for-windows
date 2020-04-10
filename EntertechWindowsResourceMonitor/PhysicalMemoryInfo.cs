using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsResourceMonitor
{
    class PhysicalMemoryInfo
    {
        public double freePhysicalMemory { get; set; }
        public double totalVisibleMemorySize { get; set; }
        public double physicalMemoryUsagePercentage { get; set; }
    }
}

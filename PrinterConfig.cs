using System;

namespace WMSApp.PrintManagement
{
    /// <summary>
    /// Configuration model for printer settings
    /// </summary>
    public class PrinterConfig
    {
        public string PrinterName { get; set; }
        public string PaperSize { get; set; }
        public string Orientation { get; set; }
        public string FusionInstance { get; set; }
        public string FusionUsername { get; set; }
        public string FusionPassword { get; set; }
        public bool AutoDownload { get; set; }
        public bool AutoPrint { get; set; }
        public int Copies { get; set; } = 1;
        public bool Collate { get; set; } = true;
        public bool Duplex { get; set; } = false;
    }
}

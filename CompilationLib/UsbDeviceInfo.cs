namespace CompilationLib
{
    /// <summary>
    /// Complete USB device information
    /// </summary>
    public class UsbDeviceInfo
    {
        public string VendorName { get; set; }
        public string ProductName { get; set; }
        public int? MaxBaudrate { get; set; }

        public UsbDeviceInfo(string vendorName, string productName = null, int? maxBaudrate = null)
        {
            VendorName = vendorName;
            ProductName = productName;
            MaxBaudrate = maxBaudrate;
        }
    }
}

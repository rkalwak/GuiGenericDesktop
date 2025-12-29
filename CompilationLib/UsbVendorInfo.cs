namespace CompilationLib
{
    /// <summary>
    /// USB Vendor information with product catalog
    /// </summary>
    public class UsbVendorInfo
    {
        public string VendorName { get; set; }
        public Dictionary<int, UsbBridgeInfo> Products { get; set; }

        public UsbVendorInfo(string vendorName)
        {
            VendorName = vendorName;
            Products = new Dictionary<int, UsbBridgeInfo>();
        }
    }
}

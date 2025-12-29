namespace CompilationLib
{
    /// <summary>
    /// USB Bridge chip information
    /// </summary>
    public class UsbBridgeInfo
    {
        public string Name { get; set; }
        public int MaxBaudrate { get; set; }

        public UsbBridgeInfo(string name, int maxBaudrate)
        {
            Name = name;
            MaxBaudrate = maxBaudrate;
        }
    }
}

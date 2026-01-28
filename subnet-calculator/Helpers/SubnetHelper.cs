using System.Net;
using System.Net.Sockets;

namespace subnet_calculator.Helpers
{
    public class SubnetResult
    {
        public string IP { get; set; } = "";
        public string Mask { get; set; } = "";
        public string Network { get; set; } = "";
        public string Broadcast { get; set; } = "";
        public string FirstHost { get; set; } = "";
        public string LastHost { get; set; } = "";
        public long TotalHosts { get; set; }
        public long HostNumber { get; set; }
        public string Cidr { get; set; } = "";
    }

    public static class SubnetHelper
    {
        public static SubnetResult Calculate(string ipString, int cidr)
        {
            if (!IPAddress.TryParse(ipString, out var ip))
            {
                throw new ArgumentException("Invalid IP Address");
            }

            if (ip.AddressFamily != AddressFamily.InterNetwork)
            {
                throw new ArgumentException("Only IPv4 is supported currently");
            }

            if (cidr < 0 || cidr > 32)
            {
                throw new ArgumentException("CIDR must be between 0 and 32");
            }

            byte[] ipBytes = ip.GetAddressBytes();
            uint ipUint = BytesToUint(ipBytes);

            uint maskUint = 0xffffffff << (32 - cidr);
            byte[] maskBytes = UintToBytes(maskUint);
            
            uint networkUint = ipUint & maskUint;
            uint broadcastUint = networkUint | ~maskUint;

            // Handle edge cases like /31 or /32 where hosts calculation differs conceptually,
            // but standard logic usually applies until specifically handled. 
            // For /31 and /32, usable hosts are 0 usually in strict sense, but let's stick to standard math.
            
            uint firstHostUint = networkUint + 1;
            uint lastHostUint = broadcastUint - 1;
            
            // Total hosts = 2^(32-cidr) - 2
            long totalHosts = (long)Math.Pow(2, 32 - cidr) - 2;
            if (totalHosts < 0) totalHosts = 0; // for /32 and /31

            long hostNumber = ipUint - networkUint;

            return new SubnetResult
            {
                IP = ipString,
                Cidr = "/" + cidr,
                Mask = new IPAddress(maskBytes).ToString(),
                Network = new IPAddress(UintToBytes(networkUint)).ToString(),
                Broadcast = new IPAddress(UintToBytes(broadcastUint)).ToString(),
                FirstHost = totalHosts > 0 ? new IPAddress(UintToBytes(firstHostUint)).ToString() : "N/A",
                LastHost = totalHosts > 0 ? new IPAddress(UintToBytes(lastHostUint)).ToString() : "N/A",
                TotalHosts = totalHosts,
                HostNumber = hostNumber
            };
        }

        private static uint BytesToUint(byte[] bytes)
        {
            // IPAddress.GetAddressBytes returns bytes in network byte order (big-endian) usually?
            // Actually it just returns logic bytes: 192.168.1.1 -> [192, 168, 1, 1]
            // We need to be careful with Endianness if we used BitConverter, 
            // but doing manual shift is safer for consistency.
            return (uint)(bytes[0] << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3]);
        }

        private static byte[] UintToBytes(uint value)
        {
            return new byte[]
            {
                (byte)((value >> 24) & 0xFF),
                (byte)((value >> 16) & 0xFF),
                (byte)((value >> 8) & 0xFF),
                (byte)(value & 0xFF)
            };
        }
    }
}

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
        public int? TargetHostIndex { get; set; }
        public string? TargetHostIP { get; set; }
        public string NetworkClass { get; set; } = "";
        public string? SubnetNumber { get; set; }
        public string Cidr { get; set; } = "";
    }

    public static class SubnetHelper
    {
        public static SubnetResult Calculate(string ipString, int cidr, int? targetHostIndex = null)
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

            uint firstHostUint = networkUint + 1;
            uint lastHostUint = broadcastUint - 1;
            
            // Total hosts = 2^(32-cidr) - 3 (subtracting Network, Broadcast, and one reserved address)
            long totalHosts = (long)Math.Pow(2, 32 - cidr) - 3;
            if (totalHosts < 0) totalHosts = 0; 

            long hostNumber = ipUint - networkUint;

            string networkClass = "Unknown";
            int majorCidr = 0;
            byte firstByte = ipBytes[0];

            if (firstByte >= 1 && firstByte <= 126) { networkClass = "A"; majorCidr = 8; }
            else if (firstByte >= 128 && firstByte <= 191) { networkClass = "B"; majorCidr = 16; }
            else if (firstByte >= 192 && firstByte <= 223) { networkClass = "C"; majorCidr = 24; }
            else if (firstByte >= 224 && firstByte <= 239) { networkClass = "D (Multicast)"; }
            else if (firstByte >= 240) { networkClass = "E (Reserved)"; }

            string? subnetNumberText = null;
            if (majorCidr > 0 && cidr > majorCidr)
            {
                uint majorMask = 0xffffffff << (32 - majorCidr);
                // Subnet index is the value of the bits between majorCidr and cidr
                uint subnetBits = (ipUint & ~majorMask) >> (32 - cidr);
                subnetNumberText = (subnetBits + 1).ToString();
            }

            string? targetHostIP = null;
            if (targetHostIndex.HasValue)
            {
                uint targetUint = networkUint + (uint)targetHostIndex.Value;
                if (targetUint <= broadcastUint)
                {
                    targetHostIP = new IPAddress(UintToBytes(targetUint)).ToString();
                }
                else
                {
                    targetHostIP = "Fuori range";
                }
            }

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
                HostNumber = hostNumber,
                TargetHostIndex = targetHostIndex,
                TargetHostIP = targetHostIP,
                NetworkClass = networkClass,
                SubnetNumber = subnetNumberText
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

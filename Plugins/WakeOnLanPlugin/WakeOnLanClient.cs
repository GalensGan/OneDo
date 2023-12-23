using System.Net.Sockets;
using System.Net;

namespace OneDo.WakeOnLanPlugin
{
    /// <summary>
    /// 发送幻数据包唤醒电脑
    /// </summary>
    public class WakeOnLanClient
    {
        private readonly UdpClient client;
        private readonly IPAddress iPAddress;
        private readonly int port;

        /// <summary>
        /// 指定端口号初始化 UDP 客户端
        /// </summary>
        /// <param name="port"></param>
        public WakeOnLanClient(string ipString, int port = 9)
        {
            if (!IPAddress.TryParse(ipString, out iPAddress))
                throw new Exception($"{ipString} 不是有效的 IP 地址");
            this.client = new UdpClient();
            this.port = port;
        }

        /// <summary>
        /// 发送幻数据包唤醒电脑
        /// </summary>
        /// <param name="macAddress"></param>
        public void SendMagicPacket(string macAddress)
        {
            byte[] mac = ParseMacAddress(macAddress);
            byte[] packet = CreateMagicPacket(mac);

            client.Send(packet, packet.Length, new IPEndPoint(iPAddress, port));
        }

        /// <summary>
        /// 分隔 MAC 地址
        /// </summary>
        /// <param name="macAddress"></param>
        /// <returns></returns>
        private byte[] ParseMacAddress(string macAddress)
        {
            // mac 地址可能是以 : 或 - 分隔,也可能没有分隔符
            macAddress = macAddress.Replace(":", "").Replace("-", "");
            // 判断 mac 地址是否合法
            if (macAddress.Length != 12)
                throw new Exception("MAC 地址不合法，长度应是 12 位");
            // 将 mac 地址转换为 byte 数组
            var macBytes = new byte[6];
            for (int i = 0; i < 6; i++)
                macBytes[i] = Convert.ToByte(macAddress.Substring(i * 2, 2), 16);
            return macBytes;
        }

        private byte[] CreateMagicPacket(byte[] mac)
        {
            byte[] packet = new byte[6 + 16 * 6];
            for (int i = 0; i < 6; i++)
                packet[i] = 0xFF;
            for (int i = 6; i < packet.Length; i++)
                packet[i] = mac[(i - 6) % mac.Length];

            return packet;
        }
    }
}
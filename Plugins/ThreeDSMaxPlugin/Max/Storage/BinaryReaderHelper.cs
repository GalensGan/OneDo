using OpenMcdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDo.ThreeDSMaxPlugin.Max.Storage
{
    public class BinaryReaderHelper
    {
        public static int INT_S = sizeof(int);
        public static int SHORT_S = sizeof(short);

        /// <summary>
        /// Read an identifier of a chunk.
        /// An identifier is an unsigned short integer.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static string ReadIdn(BinaryReader stream)
        {
            byte[] bytes = stream.ReadBytes(2);
            return "0x" + BitConverter.ToString(bytes).Replace("-", "");
        }

        /// <summary>
        /// 读取整数
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static int ReadInt(BinaryReader stream)
        {
            byte[] b = stream.ReadBytes(4);
            return BitConverter.ToInt32(b, 0);
        }

        /// <summary>
        /// 读取 Header
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        /// <exception cref="StorageException"></exception>
        public static Header ReadHeader(BinaryReader stream)
        {
            string idn = ReadIdn(stream);
            int length = ReadInt(stream);
            if (length == 0)
            {
                throw new Exception("Extended header length is not yet supported: " + idn);
            }
            // 整数+短整数长度
            length -= 4 + 2;
            // the msb is a flag that helpfully lets us know if the chunk itself
            // contains more chunks, i.e. is a container
            int signBit = 1 << 31;
            StorageType storageType;
            if ((length & signBit) != 0)
            {
                storageType = StorageType.CONTAINER;
                int byteMask = (1 << 32) - 1;
                length &= ~signBit & byteMask;
            }
            else
            {
                storageType = StorageType.VALUE;
            }

            if (KnownTypes.Instance.ContainsKey(idn))
            {
                storageType = KnownTypes.Instance[idn].StorageType;
            }
            return new Header { Idn = idn, Length = length, StorageType = storageType, StorageTypeName = storageType.ToString() };
        }

        /// <summary>
        /// 将字节转成字符串
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string ReadValue(BinaryReader stream, int length)
        {
            byte[] val = stream.ReadBytes(length);
            return BitConverter.ToString(val).Replace("-", "");
        }

        /// <summary>
        /// 读取 Container
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<StorageContainer> ReadContainer(BinaryReader stream, int length)
        {
            List<StorageContainer> childs = new List<StorageContainer>();
            long start = stream.BaseStream.Position;
            long consumed = 0;
            while (consumed < length)
            {
                Header header = ReadHeader(stream);
                StorageContainer child;
                if (header.StorageType == StorageType.CONTAINER)
                {
                    List<StorageContainer> nodes = ReadContainer(stream, header.Length);
                    child = new StorageContainer { Header = header, Childs = nodes };
                }
                else if (header.StorageType == StorageType.VALUE)
                {
                    string val = ReadValue(stream, header.Length);
                    child = new StorageValue()
                    {
                        Header = header,
                        Value = val
                    };
                }
                else
                {
                    throw new Exception("Unknown header type: " + header.StorageType);
                }
                childs.Add(child);
                consumed = stream.BaseStream.Position - start;
            }
            return childs;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="maxFname"></param>
        /// <param name="streamName"></param>
        /// <returns></returns>
        public static byte[] ReadStream(string maxFname, string streamName)
        {
            using CompoundFile cf = new CompoundFile(maxFname);
            CFStream stream = cf.RootStorage.GetStream(streamName);
            return stream.GetData();
        }

        public static List<StorageContainer> StorageParser(string maxFname, string streamName)
        {
            var bytes = ReadStream(maxFname, streamName);
            // 字节转换成 stream
            MemoryStream stream = new MemoryStream(bytes);
            BinaryReader binaryReader = new BinaryReader(stream);
            return ReadContainer(binaryReader, bytes.Length);
        }

        public static List<StorageContainer> StorageParser(CompoundFile cf, string streamName)
        {
            if (!cf.RootStorage.TryGetStream(streamName, out var cfStream)) return new List<StorageContainer>();
            MemoryStream memoryStream = new MemoryStream(cfStream.GetData());
            BinaryReader binaryReader = new BinaryReader(memoryStream);
            return ReadContainer(binaryReader, (int)memoryStream.Length);
        }

        public static List<StorageContainer> ExtractVpq(string maxFname)
        {
            return StorageParser(maxFname, "VideoPostQueue");
        }
    }
}

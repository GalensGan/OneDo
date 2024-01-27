using OneDo.ThreeDSMaxPlugin.Max.Decoders;
using OneDo.ThreeDSMaxPlugin.Max.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDo.ThreeDSMaxPlugin.Max
{
    /// <summary>
    /// 已知类型
    /// </summary>
    public class KnownTypes : Dictionary<string, KnownType>
    {
        private static KnownTypes _instance;
        public static KnownTypes Instance
        {
            get
            {
                if (_instance == null) _instance = new KnownTypes();
                return _instance;
            }
        }
        /// <summary>
        /// 初始化
        /// </summary>
        private KnownTypes()
        {
            List<KnownType> knownTypes =
            [
                new KnownType()
                {
                    Name = "0x2037",
                    StorageType = StorageType.DLL_NAME,
                    Decoder = new Utf16Decoder()
                },
                new KnownType()
                {
                    Name = "0x2038",
                    StorageType = StorageType.DLL_ENTRY,
                    Decoder = new NullDecoder(),
                },
                new KnownType()
                {
                    Name = "0x2039",
                    StorageType = StorageType.DLL_DESCRIPTION,
                    Decoder = new Utf16Decoder(),
                },
                new KnownType()
                {
                    Name = "0x2042",
                    StorageType = StorageType.CLASS_DESCRIPTION,
                    Decoder = new Utf16Decoder(),
                },
                new KnownType()
                {
                    Name = "0x2060",
                    StorageType = StorageType.CLASS_HEADER,
                    Decoder = new ClassHeaderDecoder(),
                },
                new KnownType()
                {
                    Name = "0x962",
                    StorageType = StorageType.SCENE_OBJECT_NAME,
                    Decoder = new Utf16Decoder(),
                },
            ];

            foreach (var knownType in knownTypes)
            {
                Add(knownType.Name, knownType);
            }
        }
    }
}

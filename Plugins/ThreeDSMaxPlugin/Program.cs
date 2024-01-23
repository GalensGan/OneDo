// See https://aka.ms/new-console-template for more information
using OneDo.ThreeDSMaxPlugin;
using OpenMcdf;

Console.WriteLine("Hello, World!");


string filename = "C:\\Users\\galens\\Desktop\\test.max"; // 替换为实际的文件路径

CompoundFile cf = new(filename);
cf.RootStorage.VisitEntries(item =>
{
    var name = item.Name;
    Console.WriteLine($"entry:{name}-{item.Size}-{item.IsStream}-{item.IsStorage}-{item.CLSID}");
}, true);
var sceneStrem = cf.RootStorage.GetStream("FileAssetMetaData2");

var file = new FileStream("C:\\Users\\galens\\Desktop\\test.bin", FileMode.Create);
var bytes =new byte[sceneStrem.Size];
sceneStrem.Read(bytes, 0, (int)sceneStrem.Size);
file.Write(bytes);
file.Close();

cf.Close();

var relative = Path.GetRelativePath(Environment.CurrentDirectory, filename);
Console.WriteLine($"{relative}");

List<string> names = EverythingSDK.SearchFiles("test.max");
names.ForEach(x=>Console.WriteLine(x));

Console.ReadKey();
// See https://aka.ms/new-console-template for more information
using OneDo.ThreeDSMaxPlugin;
using OpenMcdf;
using System;

Console.WriteLine("Hello, World!");


string filename = "C:\\Users\\galens\\Desktop\\新建文件夹2\\test1.max"; // 替换为实际的文件路径

CompoundFile cf = new(filename);
cf.RootStorage.VisitEntries(item =>
{
    var name = item.Name;
    Console.WriteLine($"entry:{name}-{item.Size}-{item.IsStream}-{item.IsStorage}-{item.CLSID}");
}, true);

var dirName = Path.GetDirectoryName(filename);

void SaveStream(CompoundFile cf, string name)
{
    var path = Path.Combine(dirName, name+".bin");
    var stream = cf.RootStorage.GetStream(name);
    byte[] bytes = new byte[stream.Size];
    stream.Read(bytes, 0, bytes.Length);
    File.WriteAllBytes(path, bytes);
}

// var info= cf.RootStorage.GetStream("Config");

SaveStream(cf, "Config");
SaveStream(cf, "Scene");

cf.Close();
Console.ReadKey();
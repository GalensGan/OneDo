// See https://aka.ms/new-console-template for more information
using OneDo.ThreeDSMaxPlugin;
using OpenMcdf;

Console.WriteLine("Hello, World!");


string filename = "C:\\Users\\galens\\Desktop\\新建文件夹\\max\\金女贞_01_01.jpg"; // 替换为实际的文件路径

bool IsSymbolic(string path)
{
    FileInfo pathInfo = new FileInfo(path);
   

    Console.WriteLine(pathInfo.FullName);

    return pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);    
}

Console.WriteLine(IsSymbolic(filename));

Console.ReadKey();
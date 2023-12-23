param(
    $fileName
)

if ($fileName -eq $null)
{
    Write-Host "Missing argument fileName"
    exit
}

Add-Type -TypeDefinition @"
using System;
using System.IO;
using System.Windows.Forms;
using System.Drawing.Imaging;

public class ClipboardUtil
{
    public static bool SaveImageFromClipboard(string path)
    {
	    if(string.IsNullOrEmpty(path))
	    {
	        return false;
	    }

        // 如果目录不存在，则新增目录
        if(!Directory.Exists(Path.GetDirectoryName(path)))
        {
			Directory.CreateDirectory(Path.GetDirectoryName(path));
		}
	
        if (Clipboard.ContainsImage())
        {
            var image = Clipboard.GetImage();
            image.Save(path, ImageFormat.Png);
	    return true;
        }
	return false;
    }
}
"@ -IgnoreWarnings -ReferencedAssemblies "System.Windows.Forms","System.Drawing"

$tempPath = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), $fileName)
$saveResult = [ClipboardUtil]::SaveImageFromClipboard($tempPath)

if ($saveResult)
{
    Write-Host "Image saved to $tempPath"
}
else
{
    Write-Host "Not save any image"
}
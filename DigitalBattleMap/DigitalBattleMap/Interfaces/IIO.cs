using System.Drawing;
using System.IO;

namespace DigitalBattleMap.Interfaces;

public interface IDirectory
{
    public string[] GetFiles(string path);
    public bool Exists(string? path);
    public DirectoryInfo CreateDirectory(string path);
    public void Delete(string path, bool recursive);
    public void Copy(string sourceDir, string destinationDir, bool recursive);
}

public interface IFile
{
    public bool Exists(string? path);
    public void Delete(string path);
    public string ReadAllText(string path);
    public void WriteAllText(string path, string? contents);
    public Bitmap LoadBitmap(string path);
    public Bitmap LoadBitmap(Stream stream);
    public Bitmap? LoadBitmapFromClipboard();
    public void Copy(string sourceFileName, string destFileName);
}

public interface IZipFile
{
    public void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName);
    public void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName);
}
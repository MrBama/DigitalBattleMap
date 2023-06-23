using DigitalBattleMap.Interfaces;
using System.Drawing;
using System.IO;

namespace DigitalBattleMap.Utilities;

public static class IO
{
    public static IDirectory Directory { get; private set; }
    public static IFile File { get; private set; }
    public static IZipFile ZipFile { get; private set; }

    public static void Initialize(IDirectory directory, IFile file, IZipFile zipFile)
    {
        Directory = directory;
        File = file;
        ZipFile = zipFile;
    }
}

public class Directory : IDirectory
{
    public string[] GetFiles(string path)
    {
        return System.IO.Directory.GetFiles(path);
    }

    public bool Exists(string? path)
    {
        return System.IO.Directory.Exists(path);
    }

    public DirectoryInfo CreateDirectory(string path)
    {
        return System.IO.Directory.CreateDirectory(path);
    }

    public void Delete(string path, bool recursive)
    {
        System.IO.Directory.Delete(path, recursive);
    }
}

public class File : IFile
{
    public bool Exists(string? path)
    {
        return System.IO.File.Exists(path);
    }

    public void Delete(string path)
    {
        System.IO.File.Delete(path);
    }

    public string ReadAllText(string path)
    {
        return System.IO.File.ReadAllText(path);
    }

    public void WriteAllText(string path, string? contents)
    {
        System.IO.File.WriteAllText(path, contents);
    }

    public Bitmap LoadBitmap(string path)
    {
        using var tempBitmap = new Bitmap(path);
        return new(tempBitmap);
    }

    public void Copy(string sourceFileName, string destFileName)
    {
        System.IO.File.Copy(sourceFileName, destFileName);
    }
}

public class ZipFile : IZipFile
{
    public void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName)
    {
        System.IO.Compression.ZipFile.CreateFromDirectory(sourceDirectoryName, destinationArchiveFileName);
    }

    public void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName)
    {
        System.IO.Compression.ZipFile.ExtractToDirectory(sourceArchiveFileName, destinationDirectoryName);
    }
}


using DigitalBattleMap.DataClasses;
using System;

namespace DigitalBattleMap.Utilities;

public class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = $"{Constants.TempDirectoryPath}_{Guid.NewGuid()}";
        if (IO.Directory.Exists(Path))
        {
            IO.Directory.Delete(Path, true);
        }

        IO.Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose()
    {
        if (IO.Directory.Exists(Path))
        {
            IO.Directory.Delete(Path, true);
        }
    }
}

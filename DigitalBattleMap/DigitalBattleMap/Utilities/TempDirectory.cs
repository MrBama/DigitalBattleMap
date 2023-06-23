using System;

namespace DigitalBattleMap.Utilities;

public class TempDirectory : IDisposable
{
    private string _path;

    public TempDirectory(string path)
    {
        _path = path;
        if (IO.Directory.Exists(_path))
        {
            IO.Directory.Delete(_path, true);
        }

        IO.Directory.CreateDirectory(_path);
    }

    public void Dispose()
    {
        if (IO.Directory.Exists(_path))
        {
            IO.Directory.Delete(_path, true);
        }
    }
}

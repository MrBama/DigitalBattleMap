using DigitalBattleMap.ViewModels;
using System.Reflection;
using System;
using System.IO;

namespace DigitalBattleMap.Utilities;

public class DebugOptions
{
    // DO NOT EDIT THE VALUES OF THE PROPERTIES!!!
    // Instead edit the "DebugOptions.json" file generated in the same location as this executable

    public bool ConnectToServer { get; set; } = false;
    public string OpenSaveFile { get; set; } = ""; // Leave empty to not open anything
    public int SelectedTabIndex { get; set; } = 0;

    public static void Load(MainWindowViewModel viewModel)
    {
        var path = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location), "DebugOptions.json");

        if (!FileManager.OpenFile(path, out DebugOptions debugOptions))
        {
            debugOptions = new DebugOptions();
            FileManager.SaveFile(debugOptions, path);
        }

        debugOptions!.Execute(viewModel);
    }

    private void Execute(MainWindowViewModel viewModel)
    {
        if (ConnectToServer)
        {
            var method = viewModel.GetType().GetMethod("ServerConnectionButton", BindingFlags.NonPublic | BindingFlags.Instance);
            method!.Invoke(viewModel, new object[] { });
        }

        if (OpenSaveFile != "")
        {
            var method = viewModel.GetType().GetMethod("OpenMap", BindingFlags.NonPublic | BindingFlags.Instance, new Type[] { typeof(string) });
            method!.Invoke(viewModel, new object[] { OpenSaveFile });
        }

        if(SelectedTabIndex > 0)
        {
            viewModel.SelectedTabIndex = SelectedTabIndex;
        }
    }
}

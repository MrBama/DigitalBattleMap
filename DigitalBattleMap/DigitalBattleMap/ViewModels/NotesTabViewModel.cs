using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SysIO = System.IO;

namespace DigitalBattleMap.ViewModels;

public class NotesTabViewModel : ViewModelBase
{
    private readonly IWindowService _windowService;

    public NotesTabViewModel(IWindowService windowService)
    {
        _windowService = windowService;
    }

    public NotesTabViewModel() { }

    protected override void InitializeCommands()
    {
        AddFolderCommand    = new RelayCommand(_ => AddFolder());
        RemoveFolderCommand = new RelayCommand(_ => RemoveFolder(), _ => SelectedNode != null && RootNodes.Contains(SelectedNode));
        SaveFileCommand     = new RelayCommand(_ => SaveCurrentFile(), _ => SelectedNode != null && !SelectedNode.IsFolder);
        NodeSelectedCommand = new RelayCommand(p => OnNodeSelected(p));
    }

    public ObservableCollection<NotesNodeViewModel> RootNodes { get; } = new();

    public NotesNodeViewModel SelectedNode
    {
        get => Get<NotesNodeViewModel>();
        set => Set(value, OnSelectedNodeChanged);
    }

    public string FileContent
    {
        get => Get<string>();
        set => Set(value);
    }

    public string SelectedFileName => SelectedNode?.IsFolder == false ? SelectedNode.Name : string.Empty;

    public ICommand AddFolderCommand    { get; private set; }
    public ICommand RemoveFolderCommand { get; private set; }
    public ICommand SaveFileCommand     { get; private set; }
    public ICommand NodeSelectedCommand { get; private set; }

    public NotesConfig GetConfig() => new()
    {
        FolderPaths     = RootNodes.Select(n => n.FullPath).ToList(),
        SelectedFilePath = SelectedNode?.IsFolder == false ? SelectedNode.FullPath : null
    };

    public void LoadConfig(NotesConfig config)
    {
        RootNodes.Clear();
        foreach (var path in config.FolderPaths)
        {
            if (SysIO.Directory.Exists(path))
                RootNodes.Add(BuildNode(path));
        }

        if (!string.IsNullOrEmpty(config.SelectedFilePath))
            SelectNodeByPath(config.SelectedFilePath);
    }

    private void AddFolder()
    {
        if (_windowService == null) return;
        if (_windowService.ShowFolderBrowserDialog(out string path))
        {
            if (RootNodes.Any(n => n.FullPath == path)) return;
            RootNodes.Add(BuildNode(path));
        }
    }

    private void RemoveFolder()
    {
        if (SelectedNode == null) return;
        var root = RootNodes.FirstOrDefault(n => n == SelectedNode);
        if (root != null)
        {
            RootNodes.Remove(root);
            SelectedNode = null;
            FileContent = string.Empty;
        }
    }

    private void SaveCurrentFile()
    {
        if (SelectedNode == null || SelectedNode.IsFolder) return;
        SysIO.File.WriteAllText(SelectedNode.FullPath, FileContent ?? string.Empty);
    }

    private void OnNodeSelected(object parameter)
    {
        if (parameter is System.Windows.RoutedPropertyChangedEventArgs<object> args
            && args.NewValue is NotesNodeViewModel node)
        {
            SelectedNode = node;
        }
    }

    private void OnSelectedNodeChanged()
    {
        NotifyPropertyChange(nameof(SelectedFileName));
        if (SelectedNode == null || SelectedNode.IsFolder)
        {
            FileContent = string.Empty;
            return;
        }
        try { FileContent = SysIO.File.ReadAllText(SelectedNode.FullPath); }
        catch { FileContent = string.Empty; }
    }

    private void SelectNodeByPath(string path)
    {
        foreach (var root in RootNodes)
        {
            var found = FindNode(root, path);
            if (found != null) { SelectedNode = found; return; }
        }
    }

    private static NotesNodeViewModel FindNode(NotesNodeViewModel node, string path)
    {
        if (node.FullPath == path) return node;
        foreach (var child in node.Children)
        {
            var found = FindNode(child, path);
            if (found != null) return found;
        }
        return null;
    }

    private static NotesNodeViewModel BuildNode(string path)
    {
        var isDir = SysIO.Directory.Exists(path);
        var name = SysIO.Path.GetFileName(path);
        if (string.IsNullOrEmpty(name)) name = path;

        var node = new NotesNodeViewModel
        {
            Name     = name,
            FullPath = path,
            IsFolder = isDir
        };

        if (isDir)
        {
            foreach (var dir in SysIO.Directory.GetDirectories(path).OrderBy(d => d))
                node.Children.Add(BuildNode(dir));
            foreach (var file in SysIO.Directory.GetFiles(path).OrderBy(f => f))
                node.Children.Add(BuildNode(file));
        }

        return node;
    }
}

using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using DigitalBattleMap.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Windows.Input;

namespace DigitalBattleMap.ViewModels;

public class DownloadWindowViewModel : ViewModelBase
{
    private const int _numberOfThreads = 4;
    private List<Thread> _threadPool = new();
    private bool _isTerminated = false;
    private object _lock = "";
    private IWindowService _windowService;

    public DownloadWindowViewModel()
    {
        Initialize();
    }

    public DownloadWindowViewModel(IWindowService windowService)
    {
        _windowService = windowService;
        Initialize();
    }

    private void Initialize()
    {
        ProgressBarMaximum = 100;
    }

    protected override void InitializeCommands()
    {
        CancelCommand = new RelayCommand(p => Cancel());
        RedownloadCommand = new RelayCommand(p => Redownload());
    }

    public double ProgressBarValue { get => Get<double>(); set => Set(value, () => NotifyPropertyChange(nameof(IsOkButtonEnabled))); }
    public double ProgressBarMinimum { get => Get<double>(); set => Set(value); }
    public double ProgressBarMaximum { get => Get<double>(); set => Set(value); }

    public ICommand CancelCommand { get; set; }
    public ICommand RedownloadCommand { get; set; }

    public bool IsOkButtonEnabled
    {
        get => ProgressBarValue == ProgressBarMaximum;
    }

    public void StartDownload()
    {
        var data = MonsterTokens.GetRawData();

        ProgressBarMinimum = 0;
        ProgressBarMaximum = data.Tokens.Count;

        var tokenLists = new List<List<MonsterToken>>();

        for (int i = 0; i < _numberOfThreads; i++)
        {
            tokenLists.Add(new List<MonsterToken>());
        }

        int index = 0;
        foreach (var token in data.Tokens)
        {
            if (index == _numberOfThreads)
            {
                index = 0;
            }

            tokenLists[index].Add(token);
            index++;
        }

        for (int i = 0; i < _numberOfThreads; i++)
        {
            var tokenList = new List<MonsterToken>(tokenLists[i]);
            var thread = new Thread(() => DownloadTokens(tokenList));
            thread.Start();
            _threadPool.Add(thread);
        }
    }

    private void DownloadTokens(List<MonsterToken> tokens)
    {
        using var client = new HttpClient();
        for (int i = 0; i < tokens.Count; i++)
        {
            if (_isTerminated)
            {
                return;
            }

            var imagePath = Path.Combine(Constants.MonsterTokensPath, $"{tokens[i].Name}.png");
            if (!IO.File.Exists(imagePath))
            {
                try
                {
                    using var stream = client.GetStreamAsync(tokens[i].TokenUrl).Result;
                    using var fileStream = new FileStream(imagePath, FileMode.OpenOrCreate);
                    stream.CopyTo(fileStream);
                }
                catch (Exception e)
                {
                    // If the exception is caused by too many request, wait a bit and retry
                    if (e.Message.Contains("429"))
                    {
                        Thread.Sleep(500);
                        i--;
                        lock (_lock)
                        {
                            ProgressBarValue--;
                        }
                    }
                }
            }

            lock (_lock)
            {
                ProgressBarValue++;
            }
        }
    }

    private void Cancel()
    {
        _isTerminated = true;
        foreach (var thread in _threadPool)
        {
            thread.Join();
        }
    }

    private void Redownload()
    {
        var confirmed = false;
        var confirmationWindowViewModel = new ConfirmationWindowViewModel
        {
            Content = "Are you sure you want to\nredownload all tokens?",
            LeftButtonAction = () => { confirmed = true; }
        };
        _windowService.ShowWindowDialog<ConfirmationWindow>(confirmationWindowViewModel);

        if (confirmed)
        {
            Cancel();
            IO.Directory.Delete(Constants.MonsterTokensPath, true);
            IO.Directory.CreateDirectory(Constants.MonsterTokensPath);
            _threadPool.Clear();
            ProgressBarValue = 0;
            _isTerminated = false;
            StartDownload();
        }
    }
}

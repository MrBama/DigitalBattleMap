using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Input;

namespace DigitalBattleMap
{
    public class DownloadWindowViewModel : INotifyPropertyChanged
    {
        private const int _numberOfThreads = 4;

        private List<Thread> _threadPool = new List<Thread>();
        private bool _isTerminated = false;

        private double _progressBarValue = 0;
        private double _progressBarMinimum = 0;
        private double _progressBarMaximum = 100;
        private object _lock = "";

        public DownloadWindowViewModel()
        {
            CancelCommand = new RelayCommand(p => Cancel());
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ICommand CancelCommand { get; set; }

        public double ProgressBarValue
        {
            get => _progressBarValue;
            set
            {
                _progressBarValue = value;
                NotifyPropertyChange();
                NotifyPropertyChange(nameof(IsOkButtonEnabled));
            }
        }
        public double ProgressBarMinimum
        {
            get => _progressBarMinimum;
            set
            {
                _progressBarMinimum = value;
                NotifyPropertyChange();
            }
        }
        public double ProgressBarMaximum
        {
            get => _progressBarMaximum;
            set
            {
                _progressBarMaximum = value;
                NotifyPropertyChange();
            }
        }

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
            using (var client = new HttpClient())
            {
                for (int i = 0; i < tokens.Count; i++)
                {
                    if (_isTerminated)
                    {
                        return;
                    }


                    var imagePath = Path.Combine(Constants.MonsterTokensPath, $"{tokens[i].Name}.png");
                    if (!File.Exists(imagePath))
                    {
                        try
                        {
                            using (var stream = client.GetStreamAsync(tokens[i].TokenUrl).Result)
                            {
                                using (var fileStream = new FileStream(imagePath, FileMode.OpenOrCreate))
                                {
                                    stream.CopyTo(fileStream);
                                }
                            }
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
        }

        public void Cancel()
        {
            _isTerminated = true;
            foreach (var thread in _threadPool)
            {
                thread.Join();
            }
        }

        private void NotifyPropertyChange([CallerMemberName] string propertyname = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        }
    }
}

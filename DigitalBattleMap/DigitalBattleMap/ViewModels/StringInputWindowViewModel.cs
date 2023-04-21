using DigitalBattleMap.Utilities;
using System;
using System.Windows.Input;

namespace DigitalBattleMap.ViewModels
{
    public class StringInputWindowViewModel : ViewModelBase
    {
        private Func<string, bool> _validate = (p) => true;

        public StringInputWindowViewModel() : this("", "", (p) => true)
        {
        }

        public StringInputWindowViewModel(string header, Func<string, bool> validate) : this(header, "", validate)
        {
        }

        public StringInputWindowViewModel(string header, string input, Func<string, bool> validate)
        {
            _validate = validate;
            Header = header;            
            Input = input;
        }

        protected override void InitializeCommands()
        {
            OkCommand = new RelayCommand(p => OkButton());
        }

        public string Header { get => Get<string>(); set => Set(value); }
        public string Input { get => Get<string>(); set => Set(value, OnInputChange); }
        public bool Success { get; set; }
        public bool IsOkEnabled { get => Get<bool>(); set => Set(value); }

        public ICommand OkCommand { get; set; }

        private void OkButton()
        {
            Success = true;
        }

        private void OnInputChange()
        {
            IsOkEnabled = _validate(Input);
        }
    }
}


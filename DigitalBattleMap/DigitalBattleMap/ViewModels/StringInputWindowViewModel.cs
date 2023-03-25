using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Navigation;

namespace DigitalBattleMap
{
    public class StringInputWindowViewModel : PropertyHandler
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
            Header = header;
            _validate = validate;
            Input = input;
            OkCommand = new RelayCommand(p => OkButton());
        }

        public string Header { get => Get<string>(); set => Set(value); }
        public string Input { get => Get<string>(); set => Set(value, OnInputChange); }
        public ICommand OkCommand { get; set; }
        public bool Success { get; set; }
        public bool IsOkEnabled { get => Get<bool>(); set => Set(value); }

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


using DigitalBattleMap.Utilities;
using System.Windows.Input;

namespace DigitalBattleMap.ViewModels;

public delegate bool ValidateStringInputDelegate(string input, out string errorMessage);

public class StringInputWindowViewModel : ViewModelBase
{
    private ValidateStringInputDelegate _validator;

    public StringInputWindowViewModel() : this("", "", null)
    {
    }

    public StringInputWindowViewModel(string header, ValidateStringInputDelegate validate) : this(header, "", validate)
    {
    }

    public StringInputWindowViewModel(string header, string input, ValidateStringInputDelegate validator)
    {
        _validator = validator;
        Header = header;            
        Input = input;
        ErrorMessage = "";
    }

    protected override void InitializeCommands()
    {
        OkCommand = new RelayCommand(p => OkButton());
    }

    public string Header { get => Get<string>(); set => Set(value); }
    public string Input { get => Get<string>(); set => Set(value, OnInputChange); }
    public string ErrorMessage { get => Get<string>(); set => Set(value, () => NotifyPropertyChange(nameof(ShowErrorMessage))); }
    public bool ShowErrorMessage { get => ErrorMessage != null && ErrorMessage != ""; }
    public bool Success { get; set; }
    public bool IsOkEnabled { get => Get<bool>(); set => Set(value); }

    public ICommand OkCommand { get; set; }

    private void OkButton()
    {
        Success = true;
    }

    private void OnInputChange()
    {
        if(_validator != null)
        {
            IsOkEnabled = _validator(Input, out var errorMessage);
            if (IsOkEnabled)
            {
                ErrorMessage = "";
            }
            else
            {
                ErrorMessage = errorMessage;
            }
        }

    }
}


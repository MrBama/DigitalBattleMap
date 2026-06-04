using DigitalBattleMap.Utilities;
using System;
using System.Windows.Input;

namespace DigitalBattleMap.ViewModels;

public class ConfirmationWindowViewModel : ViewModelBase
{
    protected override void InitializeCommands()
    {
        LeftButtonCommand = new RelayCommand(p => LeftButtonClicked());
        MiddleButtonCommand = new RelayCommand(p => MiddleButtonClicked());
        RightButtonCommand = new RelayCommand(p => RightButtonClicked());
    }

    public string Content { get; set; } = "Are you sure?";
    public string LeftButtonContent { get; set; } = "Yes";
    public string MiddleButtonContent { get; set; } = "Ok";
    public string RightButtonContent { get; set; } = "No";
    public bool IsLeftButtonVisible { get; set; } = true;
    public bool IsMiddleButtonVisible { get; set; } = false;
    public bool IsRightButtonVisible { get; set; } = true;
    public Action LeftButtonAction { get; set; } = () => { };
    public Action MiddleButtonAction { get; set; } = () => { };
    public Action RightButtonAction { get; set; } = () => { };
    public ICommand LeftButtonCommand { get; set; }
    public ICommand MiddleButtonCommand { get; set; }
    public ICommand RightButtonCommand { get; set; }

    private void LeftButtonClicked()
    {
        LeftButtonAction();
    }

    private void MiddleButtonClicked()
    {
        MiddleButtonAction();
    }

    private void RightButtonClicked()
    {
        RightButtonAction();
    }
}

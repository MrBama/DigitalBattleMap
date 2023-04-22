using DigitalBattleMap.Utilities;
using System.Windows.Input;

namespace DigitalBattleMap.ViewModels;

public class ConfirmationWindowViewModel : ViewModelBase
{
    protected override void InitializeCommands()
    {
        YesCommand = new RelayCommand(p => YesButtonClicked());
    }

    public string Content { get; set; } = "Are you sure?";
    public bool Confirmed { get; set; } = false;
    public ICommand YesCommand { get; set; }

    private void YesButtonClicked()
    {
        Confirmed = true;
    }
}

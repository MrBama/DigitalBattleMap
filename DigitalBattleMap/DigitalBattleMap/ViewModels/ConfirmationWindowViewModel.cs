using DigitalBattleMap.Utilities;
using System.Windows.Input;

namespace DigitalBattleMap
{
    public class ConfirmationWindowViewModel
    {
        public ConfirmationWindowViewModel()
        {
            YesCommand = new RelayCommand(p => YesButtonClicked());
        }

        public string Content { get; set; } = "Are you sure?";
        public ICommand YesCommand { get; set; }
        public bool Confirmed { get; set; } = false;

        private void YesButtonClicked()
        {
            Confirmed = true;
        }
    }
}

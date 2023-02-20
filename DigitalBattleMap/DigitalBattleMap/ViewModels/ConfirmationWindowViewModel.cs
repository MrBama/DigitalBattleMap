using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DigitalBattleMap
{
    public class ConfirmationWindowViewModel
    {
        private ICommand _yesCommand;

        public ConfirmationWindowViewModel()
        {
            _yesCommand = new RelayCommand(p => YesButtonClicked());
        }

        public string Content { get; set; } = "Are you sure?";
        public ICommand YesCommand { get => _yesCommand; }
        public bool Confirmed { get; set; } = false;

        private void YesButtonClicked()
        {
            Confirmed = true;
        }
    }
}

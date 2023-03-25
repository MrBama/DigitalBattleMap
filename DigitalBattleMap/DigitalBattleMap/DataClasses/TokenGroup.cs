using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DigitalBattleMap
{
    public class TokenGroup : INotifyPropertyChanged
    {
        private string _name = "";

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Name 
        { 
            get => _name; 
            set
            {
                if (value != _name)
                {
                    _name = value;
                    NotifyPropertyChange();
                }
            }
        }
        public List<string> TokenNames { get; set; } = new List<string>();

        protected void NotifyPropertyChange([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

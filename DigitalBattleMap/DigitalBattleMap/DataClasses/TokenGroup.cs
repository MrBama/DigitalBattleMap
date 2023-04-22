using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DigitalBattleMap.DataClasses;

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

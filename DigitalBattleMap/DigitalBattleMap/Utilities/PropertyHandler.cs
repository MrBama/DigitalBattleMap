using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DigitalBattleMap.Utilities;

public class PropertyHandler : INotifyPropertyChanged
{
    private Dictionary<string, object> _properties = new Dictionary<string, object>();

    public event PropertyChangedEventHandler? PropertyChanged;

    protected T Get<T>([CallerMemberName] string propertyName = "")
    {
        if (!_properties.ContainsKey(propertyName))
        {
            _properties[propertyName] = default(T);
        }

        return (T)_properties[propertyName];
    }

    protected void Set<T>(T value, [CallerMemberName] string propertyName = "")
    {
        _properties[propertyName] = value;
        NotifyPropertyChange(propertyName);
    }

    protected void Set<T>(T value, Action action, [CallerMemberName] string propertyName = "")
    {
        _properties[propertyName] = value;
        NotifyPropertyChange(propertyName);
        action();
    }

    protected void NotifyPropertyChange([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DigitalBattleMap.Utilities;

public class PropertyHandler : INotifyPropertyChanged
{
    private Dictionary<string, object> _properties = new();
    private List<PropertyChangedWatcher> _propertyChangedWatchers = new();

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
        NotifyPropertyChangedWatchers(propertyName);
    }

    protected void Set<T>(T value, Action action, [CallerMemberName] string propertyName = "")
    {
        _properties[propertyName] = value;
        NotifyPropertyChange(propertyName);
        action();
        NotifyPropertyChangedWatchers(propertyName);
    }

    protected void SetWhenChanged<T>(T value, Action action, [CallerMemberName] string propertyName = "") where T : IEquatable<T>
    {
        if (!_properties.ContainsKey(propertyName) || !_properties[propertyName].Equals(value))
        {
            _properties[propertyName] = value;
            NotifyPropertyChange(propertyName);
            action();
            NotifyPropertyChangedWatchers(propertyName);
        }
    }

    protected void NotifyPropertyChange([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected void RegisterPropertyChangedWatcher(string propertyName, List<string> watchedProperties)
    {
        _propertyChangedWatchers.Add(new PropertyChangedWatcher { PropertyName = propertyName, WatchedProperties = watchedProperties });
    }

    private void NotifyPropertyChangedWatchers(string propertyName)
    {
        foreach (var watcher in _propertyChangedWatchers)
        {
            if (watcher.WatchedProperties.Contains(propertyName))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(watcher.PropertyName));
            }
        }
    }

    private class PropertyChangedWatcher
    {
        public string PropertyName { get; set; }
        public List<string> WatchedProperties { get; set; } = new();
    }
}

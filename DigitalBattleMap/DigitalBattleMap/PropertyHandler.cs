using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DigitalBattleMap
{
    public class PropertyHandler
    {
        private Action<string> _notifyPropertyChanged;
        private Dictionary<string, object> _properties = new Dictionary<string, object>();

        protected void SetNotifyPropertyChangedAction(Action<string> notifyPropertyChanged)
        {
            _notifyPropertyChanged = notifyPropertyChanged;
        }

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
            _notifyPropertyChanged(propertyName);
        }

        protected void Set<T>(T value, Action action, [CallerMemberName] string propertyName = "")
        {
            _properties[propertyName] = value;
            _notifyPropertyChanged(propertyName);
            action();
        }
    }
}

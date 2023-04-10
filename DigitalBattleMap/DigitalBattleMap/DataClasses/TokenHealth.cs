using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DigitalBattleMap
{
    public class TokenHealth : PropertyHandler
    {
        private string _hp;
        private string _maxHp;
        private string _editorHp;
        private string _editorMaxHp;
        private Regex _integerRegex = new Regex(@"^[0-9]*$", RegexOptions.IgnoreCase);
        private Regex _hpRegex = new Regex(@"^([-+])?([0-9]+)*$", RegexOptions.IgnoreCase);

        public TokenHealth()
        {
            HpEnterCommand = new RelayCommand(p => ApplyHp());
            MaxHpEnterCommand = new RelayCommand(p => ApplyMaxHp());
            HpEscCommand = new RelayCommand(p => DiscardHp());
            MaxHpEscCommand = new RelayCommand(p => DiscardMaxHp());
        }

        [JsonIgnore]
        public ICommand HpEnterCommand { get; set; }
        [JsonIgnore]
        public ICommand MaxHpEnterCommand { get; set; }
        [JsonIgnore]
        public ICommand HpEscCommand { get; set; }
        [JsonIgnore]
        public ICommand MaxHpEscCommand { get; set; }

        public string Hp
        {
            get => _editorHp;
            set
            {
                if (value != _editorHp)
                {
                    _editorHp = value;
                    NotifyPropertyChange();
                }
            }
        }

        public string MaxHp
        {
            get => _editorMaxHp;
            set
            {
                if (value != _editorMaxHp)
                {
                    _editorMaxHp = value;
                    NotifyPropertyChange();
                }
            }
        }

        public void ApplyEditorHp()
        {
            _hp = _editorHp;
            _maxHp = _editorMaxHp;
        }

        private void SetMaxHp(string value)
        {
            if (value == "")
            {
                _maxHp = null;
                _hp = null;
            }

            if (_integerRegex.IsMatch(value))
            {
                if (_hp == null)
                {
                    _hp = value;
                }
                _maxHp = value;
            }
        }

        private void SetHp(string value)
        {
            if (value == null)
            {
                _hp = null;
                return;
            }

            if (value == "")
            {
                if (_maxHp == null)
                {
                    _hp = null;
                }
                return;
            }

            if (_hpRegex.IsMatch(value))
            {
                var groups = _hpRegex.Match(value).Groups;
                var newHp = int.Parse(groups[2].Value);

                if (groups[1].Value == "+")
                {
                    newHp = int.Parse(_hp) + int.Parse(groups[2].Value);
                }
                else if (groups[1].Value == "-")
                {
                    newHp = int.Parse(_hp) - int.Parse(groups[2].Value);
                }

                if (_maxHp != null && _maxHp != "" && newHp > int.Parse(_maxHp))
                {
                    _hp = _maxHp;
                }
                else if (newHp < 0)
                {
                    _hp = "0";
                }
                else
                {
                    _hp = newHp.ToString();
                }
            }
        }

        private void ApplyHp()
        {
            SetHp(_editorHp);
            Hp = _hp;
        }

        private void ApplyMaxHp()
        {
            SetMaxHp(_editorMaxHp);
            Hp = _hp;
            MaxHp = _maxHp;
        }

        private void DiscardHp()
        {
            Hp = _hp;
        }

        private void DiscardMaxHp()
        {
            MaxHp = _maxHp;
        }
    }
}

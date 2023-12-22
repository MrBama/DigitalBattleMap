using DigitalBattleMap.Utilities;
using Newtonsoft.Json;
using System;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace DigitalBattleMap.DataClasses;

public class TokenHealth : PropertyHandler
{
    private string _editorHp;
    private string _editorMaxHp;
    private Brush _color = Brushes.White;
    private Regex _integerRegex = new(@"^[0-9]*$", RegexOptions.IgnoreCase);
    private Regex _hpRegex = new(@"^([-+])?([0-9]+)*$", RegexOptions.IgnoreCase);

    public TokenHealth()
    {
        HpEnterCommand = new RelayCommand(p => ApplyHp());
        MaxHpEnterCommand = new RelayCommand(p => ApplyMaxHp());
        HpEscCommand = new RelayCommand(p => DiscardHp());
        MaxHpEscCommand = new RelayCommand(p => DiscardMaxHp());
    }

    public event EventHandler OnHpChanged;
    public event EventHandler OnMaxHpChanged;

    [JsonIgnore]
    public ICommand HpEnterCommand { get; set; }
    [JsonIgnore]
    public ICommand MaxHpEnterCommand { get; set; }
    [JsonIgnore]
    public ICommand HpEscCommand { get; set; }
    [JsonIgnore]
    public ICommand MaxHpEscCommand { get; set; }

    [JsonIgnore]
    public string EditorHp
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

    [JsonIgnore]
    public string EditorMaxHp
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

    public string Hp { get; set; }

    public string MaxHp { get; set; }

    public Brush Color
    {
        get => _color;
        set
        {
            if (value != _color)
            {
                _color = value;
                NotifyPropertyChange();
            }
        }
    }

    public void InitializeEditorHp()
    {
        EditorHp = Hp;
        EditorMaxHp = MaxHp;
        SetHpColor();
    }

    public void InitializeEditorHp(int hp)
    {
        Hp = hp.ToString();
        MaxHp = hp.ToString();
        InitializeEditorHp();
    }

    public void ApplyHp()
    {
        SetHp(EditorHp);
        EditorHp = Hp;
        SetHpColor();
        NotifyHpChanged();
    }

    public void ApplyMaxHp()
    {
        SetMaxHp(EditorMaxHp);
        EditorHp = Hp;
        EditorMaxHp = MaxHp;
        SetHpColor();
        NotifyMaxHpChanged();
    }

    private void SetMaxHp(string value)
    {
        if (value == "")
        {
            MaxHp = null;
            Hp = null;
            return;
        }

        if (_integerRegex.IsMatch(value))
        {
            if (Hp == null)
            {
                Hp = value;
            }
            MaxHp = value;
        }
    }

    private void SetHp(string value)
    {
        if (value == null)
        {
            Hp = null;
            return;
        }

        if (value == "")
        {
            if (MaxHp == null)
            {
                Hp = null;
            }
            return;
        }

        if (_hpRegex.IsMatch(value))
        {
            var groups = _hpRegex.Match(value).Groups;
            var newHp = int.Parse(groups[2].Value);

            if (groups[1].Value == "+")
            {
                newHp = int.Parse(Hp) + int.Parse(groups[2].Value);
            }
            else if (groups[1].Value == "-")
            {
                newHp = int.Parse(Hp) - int.Parse(groups[2].Value);
            }

            if (MaxHp != null && MaxHp != "" && newHp > int.Parse(MaxHp))
            {
                Hp = MaxHp;
            }
            else if (newHp < 0)
            {
                Hp = "0";
            }
            else
            {
                Hp = newHp.ToString();
            }
        }
    }

    private void DiscardHp()
    {
        EditorHp = Hp;
    }

    private void DiscardMaxHp()
    {
        EditorMaxHp = MaxHp;
    }

    private void SetHpColor()
    {
        if (Hp != null && MaxHp != null && Hp != "" && MaxHp != "")
        {
            var brush = Brushes.LimeGreen;
            double percentage = double.Parse(Hp) / double.Parse(MaxHp) * 100;

            if (percentage <= 75)
            {
                brush = Brushes.LightGreen;
            }

            if (percentage <= 50)
            {
                brush = Brushes.Khaki;
            }

            if (percentage <= 25)
            {
                brush = Brushes.SandyBrown;
            }

            if (percentage <= 10)
            {
                brush = Brushes.Tomato;
            }

            if (percentage == 0)
            {
                brush = Brushes.LightGray;
            }

            Color = brush;
        }
        else
        {
            Color = Brushes.White;
        }
    }

    private void NotifyHpChanged()
    {
        OnHpChanged?.Invoke(this, new EventArgs());
    }

    private void NotifyMaxHpChanged()
    {
        OnMaxHpChanged?.Invoke(this, new EventArgs());
    }
}

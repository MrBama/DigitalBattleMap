using DigitalBattleMap.Common;
using System.Drawing;
using System.Reflection;

namespace DigitalBattleMap.DataClasses
{
    public class ConditionIcons
    {
        private const string _iconResourcePath = "DigitalBattleMap.Resources.ConditionIcons";

        private Bitmap _baned;
        private Bitmap _blessed;
        private Bitmap _blinded;
        private Bitmap _charmed;
        private Bitmap _concentration;
        private Bitmap _deafened;
        private Bitmap _death;
        private Bitmap _exhausted;
        private Bitmap _flying;
        private Bitmap _frightened;
        private Bitmap _grappled;
        private Bitmap _hasted;
        private Bitmap _hex;
        private Bitmap _highlighted;
        private Bitmap _incapacitated;
        private Bitmap _invisible;
        private Bitmap _mark;
        private Bitmap _paralyzed;
        private Bitmap _petrified;
        private Bitmap _poisoned;
        private Bitmap _prone;
        private Bitmap _restrained;
        private Bitmap _stabilized;
        private Bitmap _stunned;
        private Bitmap _unconcious;

        public ConditionIcons()
        {
            using (var bitmap = new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Baned.png")))
            {
                _baned = new Bitmap(bitmap);
            }
            using (var bitmap = new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Blessed.png")))
            {
                _blessed = new Bitmap(bitmap);
            }
            using (var bitmap = new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Blinded.png")))
            {
                _blinded = new Bitmap(bitmap);
            }
            using (var bitmap = new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Charmed.png")))
            {
                _charmed = new Bitmap(bitmap);
            }
            using (var bitmap = new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Concentration.png")))
            {
                _concentration = new Bitmap(bitmap);
            }
            using (var bitmap = new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Deafened.png")))
            {
                _deafened = new Bitmap(bitmap);
            }
            using (var bitmap = new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Death.png")))
            {
                _death = new Bitmap(bitmap);
            }
            using (var bitmap = new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Exhausted.png")))
            {
                _exhausted = new Bitmap(bitmap);
            }
            using (var bitmap = new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Flying.png")))
            {
                _flying = new Bitmap(bitmap);
            }
            using (var bitmap = new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Frightened.png")))
            {
                _frightened = new Bitmap(bitmap);
            }
            using (var bitmap = new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Grappled.png")))
            {
                _grappled = new Bitmap(bitmap);
            }
            using (var bitmap = new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Hasted.png")))
            {
                _hasted = new Bitmap(bitmap);
            }
            using (var bitmap = new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Hex.png")))
            {
                _hex = new Bitmap(bitmap);
            }
            using (var bitmap = new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Highlighted.png")))
            {
                _highlighted = new Bitmap(bitmap);
            }
            using (var bitmap = new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Incapacitated.png")))
            {
                _incapacitated = new Bitmap(bitmap);
            }
            using (var bitmap = new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Invisible.png")))
            {
                _invisible = new Bitmap(bitmap);
            }
            using (var bitmap = new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Mark.png")))
            {
                _mark = new Bitmap(bitmap);
            }
            using (var bitmap = new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Paralyzed.png")))
            {
                _paralyzed = new Bitmap(bitmap);
            }
            using (var bitmap = new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Petrified.png")))
            {
                _petrified = new Bitmap(bitmap);
            }
            using (var bitmap = new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Poisoned.png")))
            {
                _poisoned = new Bitmap(bitmap);
            }
            using (var bitmap = new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Prone.png")))
            {
                _prone = new Bitmap(bitmap);
            }
            using (var bitmap = new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Restrained.png")))
            {
                _restrained = new Bitmap(bitmap);
            }
            using (var bitmap = new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Stabilized.png")))
            {
                _stabilized = new Bitmap(bitmap);
            }
            using (var bitmap = new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Stunned.png")))
            {
                _stunned = new Bitmap(bitmap);
            }
            using (var bitmap = new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{_iconResourcePath}.Unconcious.png")))
            {
                _unconcious = new Bitmap(bitmap);
            }
        }

        public Bitmap GetConditionIcon(Condition condition)
        {
            switch (condition)
            {
                case Condition.Baned:
                    return _baned;
                case Condition.Blessed:
                    return _blessed;
                case Condition.Blinded:
                    return _blinded;
                case Condition.Charmed:
                    return _charmed;
                case Condition.Concentration:
                    return _concentration;
                case Condition.Deafened:
                    return _deafened;
                case Condition.Death:
                    return _death;
                case Condition.Exhausted:
                    return _exhausted;
                case Condition.Flying:
                    return _flying;
                case Condition.Frightened:
                    return _frightened;
                case Condition.Grappled:
                    return _grappled;
                case Condition.Hasted:
                    return _hasted;
                case Condition.Hex:
                    return _hex;
                case Condition.Highlighted:
                    return _highlighted;
                case Condition.Incapacitated:
                    return _incapacitated;
                case Condition.Invisible:
                    return _invisible;
                case Condition.Mark:
                    return _mark;
                case Condition.Paralyzed:
                    return _paralyzed;
                case Condition.Petrified:
                    return _petrified;
                case Condition.Poisoned:
                    return _poisoned;
                case Condition.Prone:
                    return _prone;
                case Condition.Restrained:
                    return _restrained;
                case Condition.Stabilized:
                    return _stabilized;
                case Condition.Stunned:
                    return _stunned;
                case Condition.Unconcious:
                    return _unconcious;
                default:
                    return new Bitmap(100, 100);
            }
        }
    }
}

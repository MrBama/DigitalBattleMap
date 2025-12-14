using DigitalBattleMap.Utilities;
using System.Collections.Generic;

namespace DigitalBattleMap.DataClasses;

public class CommandHistory<T> : PropertyHandler
{
    private List<T> _commands = new();
    private int _index = -1;

    public CommandHistory(int size)
    {
        Size = size;
    }

    public int Size { get; private set; }
    public bool IsPreviousCommandAvailable { get => Index > 0 && Index <= _commands.Count; }
    public bool IsNextCommandAvailable { get => Index >= 0 && Index < _commands.Count; }
    public bool PauseEnqueueing { get; set; }

    private int Index
    {
        get => _index;
        set
        {
            if (value != _index)
            {
                _index = value;
                NotifyPropertyChange(nameof(IsPreviousCommandAvailable));
                NotifyPropertyChange(nameof(IsNextCommandAvailable));
            }
        }
    }

    public void Enqueue(T command)
    {
        if (PauseEnqueueing)
            return;

        while (_commands.Count != 0 && Index < _commands.Count)
        {
            _commands.RemoveAt(Index);
        }

        _commands.Add(command);

        while (_commands.Count > Size)
        {
            _commands.RemoveAt(0);
        }

        Index = _commands.Count;
    }

    public bool TryDequeuePreviousCommand(out T command)
    {
        if (IsPreviousCommandAvailable)
        {
            Index--;
            command = _commands[_index];
            return true;
        }
        else
        {
            command = default;
            return false;
        }
    }

    public bool TryDequeueNextCommand(out T command)
    {
        if (IsNextCommandAvailable)
        {
            command = _commands[_index];
            Index++;
            return true;
        }
        else
        {
            command = default;
            return false;
        }
    }

    public void Clear()
    {
        _commands.Clear();
        Index = -1;
    }
}


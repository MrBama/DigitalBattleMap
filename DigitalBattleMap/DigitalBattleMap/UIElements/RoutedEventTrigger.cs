using Microsoft.Xaml.Behaviors;
using System;
using System.Windows;

namespace DigitalBattleMap.UIElements;

public class RoutedEventTrigger : EventTriggerBase<DependencyObject>
{
    private RoutedEvent _routedEvent;

    public RoutedEvent RoutedEvent
    {
        get => _routedEvent;
        set 
        { 
            _routedEvent = value; 
        }
    }

    protected override void OnAttached()
    {
        var behavior = AssociatedObject as Behavior;
        var associatedElement = AssociatedObject as FrameworkElement;

        if (behavior != null)
        {
            associatedElement = ((IAttachedObject)behavior).AssociatedObject as FrameworkElement;
        }

        if (associatedElement == null)
        {
            throw new ArgumentException("Routed Event trigger can only be associated to framework elements");
        }

        if (RoutedEvent != null)
        {
            associatedElement.AddHandler(RoutedEvent, new RoutedEventHandler(OnRoutedEvent));
        }
    }

    void OnRoutedEvent(object sender, RoutedEventArgs args)
    {
        base.OnEvent(args);
    }

    protected override string GetEventName()
    {
        return RoutedEvent.Name;
    }
}


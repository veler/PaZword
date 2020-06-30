using Microsoft.Xaml.Interactivity;
using System;
using System.Globalization;
using System.Reflection;
using System.Windows.Input;
using Windows.UI.Xaml;

namespace PaZword.Core.UI
{
    /// <summary>
    /// Behavior that will connect an UI event to a viewmodel Command, allowing the event arguments to be passed as the CommandParameter.
    /// </summary>
    public sealed class EventToCommandBehavior : Behavior<DependencyObject>
    {
        private Delegate _handler;
        private object _oldEvent;
        private Action<object> _detachEvent;

        public static readonly DependencyProperty EventProperty = DependencyProperty.Register(
            nameof(Event),
            typeof(string),
            typeof(EventToCommandBehavior),
            new PropertyMetadata(null, OnEventChanged));

        public string Event
        {
            get => (string)GetValue(EventProperty);
            set => SetValue(EventProperty, value);
        }

        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            nameof(Command),
            typeof(ICommand),
            typeof(EventToCommandBehavior),
            new PropertyMetadata(null));

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public static readonly DependencyProperty PassArgumentsProperty = DependencyProperty.Register(
            nameof(PassArguments),
            typeof(bool),
            typeof(EventToCommandBehavior),
            new PropertyMetadata(false));

        public bool PassArguments
        {
            get => (bool)GetValue(PassArgumentsProperty);
            set => SetValue(PassArgumentsProperty, value);
        }


        private static void OnEventChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (EventToCommandBehavior)d;

            if (behavior.AssociatedObject != null) // is not yet attached at initial load
            {
                behavior.AttachHandler((string)e.NewValue);
            }
        }

        protected override void OnAttached()
        {
            AttachHandler(Event); // initial set
        }

        /// <summary>
        /// Attaches the handler to the event
        /// </summary>
        private void AttachHandler(string eventName)
        {
            if (_oldEvent != null && _detachEvent != null)
            {
                // detach old event
                _detachEvent(_oldEvent);
            }

            if (!string.IsNullOrEmpty(eventName))
            {
                EventInfo eventInfo = AssociatedObject.GetType().GetEvent(eventName);
                if (eventInfo != null)
                {
                    MethodInfo addMethod = eventInfo.GetAddMethod();
                    MethodInfo removeMethod = eventInfo.GetRemoveMethod();
                    ParameterInfo[] addParameters = addMethod.GetParameters();
                    Type delegateType = addParameters[0].ParameterType;

                    Action<object, object> handler = (object sender, object eventArgs) => OnEvent(eventArgs);
                    MethodInfo handlerInvoke = handler.GetType().GetMethod(nameof(handler.Invoke));

                    _handler = handlerInvoke.CreateDelegate(delegateType, handler);

                    Func<object> attachEvent = () =>
                    {
                        object result = addMethod.Invoke(AssociatedObject, new object[] { _handler });
                        if (result != null)
                        {
                            return result;
                        }
                        return _handler;
                    };

                    _detachEvent = t => removeMethod.Invoke(AssociatedObject, new object[] { t });

                    // attach new event
                    _oldEvent = attachEvent();
                }
                else
                {
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "The event '{0}' was not found on type '{1}'", eventName, AssociatedObject.GetType().Name));
                }
            }
        }

        /// <summary>
        /// Executes the Command
        /// </summary>
        private void OnEvent(object eventArgs)
        {
            object parameter = PassArguments ? eventArgs : null;
            if (Command != null && Command.CanExecute(parameter))
            {
                Command.Execute(parameter);
            }
        }
    }
}

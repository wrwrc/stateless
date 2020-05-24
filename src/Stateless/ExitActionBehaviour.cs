using System;
using System.Threading.Tasks;

namespace Stateless
{
    internal abstract class ExitActionBehavior<TState, TTrigger>
    {
        protected ExitActionBehavior(Reflection.InvocationInfo actionDescription)
        {
            Description = actionDescription ?? throw new ArgumentNullException(nameof(actionDescription));
        }

        internal Reflection.InvocationInfo Description { get; }
    }

    internal class SyncExitActionBehavior<TState, TTrigger> : ExitActionBehavior<TState, TTrigger>
    {
        readonly Action<Transition<TState, TTrigger>> _action;

        public SyncExitActionBehavior(Action<Transition<TState, TTrigger>> action, Reflection.InvocationInfo actionDescription) : base(actionDescription)
        {
            _action = action;
        }

        public void Execute(Transition<TState, TTrigger> transition)
        {
            _action(transition);
        }
    }

    internal class AsyncExitActionBehavior<TState, TTrigger> : ExitActionBehavior<TState, TTrigger>
    {
        readonly Func<Transition<TState, TTrigger>, Task> _action;

        public AsyncExitActionBehavior(Func<Transition<TState, TTrigger>, Task> action, Reflection.InvocationInfo actionDescription) : base(actionDescription)
        {
            _action = action;
        }

        public Task ExecuteAsync(Transition<TState, TTrigger> transition)
        {
            return _action(transition);
        }
    }
}

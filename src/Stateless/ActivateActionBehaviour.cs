using System;
using System.Threading.Tasks;

namespace Stateless
{
    internal abstract class ActivateActionBehaviour<TState>
    {
        protected readonly TState _state;

        protected ActivateActionBehaviour(TState state, Reflection.InvocationInfo actionDescription)
        {
            _state = state;
            Description = actionDescription ?? throw new ArgumentNullException(nameof(actionDescription));
        }

        internal Reflection.InvocationInfo Description { get; }
    }

    internal class SyncActivateActionBehaviour<TState> : ActivateActionBehaviour<TState>
    {
        readonly Action _action;

        public SyncActivateActionBehaviour(TState state, Action action, Reflection.InvocationInfo actionDescription)
            : base(state, actionDescription)
        {
            _action = action;
        }

        public void Execute()
        {
            _action();
        }
    }

    internal class AsyncActivateActionBehaviour<TState> : ActivateActionBehaviour<TState>
    {
        readonly Func<Task> _action;

        public AsyncActivateActionBehaviour(TState state, Func<Task> action, Reflection.InvocationInfo actionDescription)
            : base(state, actionDescription)
        {
            _action = action;
        }

        public Task ExecuteAsync()
        {
            return _action();
        }
    }
}

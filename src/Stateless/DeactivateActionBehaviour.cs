using System;
using System.Threading.Tasks;

namespace Stateless
{
    internal abstract class DeactivateActionBehaviour<TState>
    {
        protected readonly TState _state;

        protected DeactivateActionBehaviour(TState state, Reflection.InvocationInfo actionDescription)
        {
            _state = state;
            Description = actionDescription ?? throw new ArgumentNullException(nameof(actionDescription));
        }

        internal Reflection.InvocationInfo Description { get; }
    }

    internal class SyncDeactivateActionBehaviour<TState> : DeactivateActionBehaviour<TState>
    {
        readonly Action _action;

        public SyncDeactivateActionBehaviour(TState state, Action action, Reflection.InvocationInfo actionDescription)
            : base(state, actionDescription)
        {
            _action = action;
        }

        public void Execute()
        {
            _action();
        }
    }

    internal class AsyncDeactivateActionBehaviour<TState> : DeactivateActionBehaviour<TState>
    {
        readonly Func<Task> _action;

        public AsyncDeactivateActionBehaviour(TState state, Func<Task> action, Reflection.InvocationInfo actionDescription)
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

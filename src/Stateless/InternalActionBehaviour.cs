using System;
using System.Threading.Tasks;

namespace Stateless
{
    internal class SyncInternalActionBehaviour<TState, TTrigger>
    {
        readonly Action<Transition<TState, TTrigger>, object[]> _action;

        public SyncInternalActionBehaviour(Action<Transition<TState, TTrigger>, object[]> action)
        {
            _action = action;
        }

        public void Execute(Transition<TState, TTrigger> transition, object[] args)
        {
            _action(transition, args);
        }
    }

    internal class AsyncInternalActionBehaviour<TState, TTrigger>
    {
        readonly Func<Transition<TState, TTrigger>, object[], Task> _action;

        public AsyncInternalActionBehaviour(Func<Transition<TState, TTrigger>, object[], Task> action)
        {
            _action = action;
        }

        public Task ExecuteAsync(Transition<TState, TTrigger> transition, object[] args)
        {
            return _action(transition, args);
        }
    }
}

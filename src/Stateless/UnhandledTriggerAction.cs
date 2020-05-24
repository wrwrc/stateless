using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stateless
{
    internal abstract class UnhandledTriggerAction<TState, TTrigger>
    {
        public abstract void Execute(TState state, TTrigger trigger, ICollection<string> unmetGuards);
        public abstract Task ExecuteAsync(TState state, TTrigger trigger, ICollection<string> unmetGuards);
    }

    internal class SyncUnhandledTriggerAction<TState, TTrigger> : UnhandledTriggerAction<TState, TTrigger>
    {
        readonly Action<TState, TTrigger, ICollection<string>> _action;

        internal SyncUnhandledTriggerAction(Action<TState, TTrigger, ICollection<string>> action = null)
        {
            _action = action;
        }

        public override void Execute(TState state, TTrigger trigger, ICollection<string> unmetGuards)
        {
            _action(state, trigger, unmetGuards);
        }

        public override Task ExecuteAsync(TState state, TTrigger trigger, ICollection<string> unmetGuards)
        {
            Execute(state, trigger, unmetGuards);
            return TaskResult.Done;
        }
    }

    internal class AsyncUnhandledTriggerAction<TState, TTrigger> : UnhandledTriggerAction<TState, TTrigger>
    {
        readonly Func<TState, TTrigger, ICollection<string>, Task> _action;

        internal AsyncUnhandledTriggerAction(Func<TState, TTrigger, ICollection<string>, Task> action)
        {
            _action = action;
        }

        public override void Execute(TState state, TTrigger trigger, ICollection<string> unmetGuards)
        {
            throw new InvalidOperationException(
                "Cannot execute asynchronous action specified in OnUnhandledTrigger. " +
                "Use asynchronous version of Fire [FireAsync]");
        }

        public override Task ExecuteAsync(TState state, TTrigger trigger, ICollection<string> unmetGuards)
        {
            return _action(state, trigger, unmetGuards);
        }
    }
}

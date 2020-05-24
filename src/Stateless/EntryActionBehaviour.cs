using System;
using System.Threading.Tasks;

namespace Stateless
{
    internal abstract class EntryActionBehavior<TState, TTrigger>
    {
        protected EntryActionBehavior(Reflection.InvocationInfo description)
        {
            Description = description;
        }

        public Reflection.InvocationInfo Description { get; }
    }

    internal class SyncEntryActionBehavior<TState, TTrigger> : EntryActionBehavior<TState, TTrigger>
    {
        readonly Action<Transition<TState, TTrigger>, object[]> _action;

        public SyncEntryActionBehavior(Action<Transition<TState, TTrigger>, object[]> action, Reflection.InvocationInfo description) : base(description)
        {
            _action = action;
        }

        public virtual void Execute(Transition<TState, TTrigger> transition, object[] args)
        {
            _action(transition, args);
        }
    }

    internal class SyncFromEntryActionBehavior<TState, TTrigger1, TTrigger2> : SyncEntryActionBehavior<TState, TTrigger1>
    {
        internal TTrigger2 Trigger { get; private set; }

        public SyncFromEntryActionBehavior(TTrigger2 trigger, Action<Transition<TState, TTrigger1>, object[]> action, Reflection.InvocationInfo description)
            : base(action, description)
        {
            Trigger = trigger;
        }

        public override void Execute(Transition<TState, TTrigger1> transition, object[] args)
        {
            if (transition.Trigger.Equals(Trigger))
                base.Execute(transition, args);
        }
    }

    internal class AsyncEntryActionBehavior<TState, TTrigger> : EntryActionBehavior<TState, TTrigger>
    {
        readonly Func<Transition<TState, TTrigger>, object[], Task> _action;

        public AsyncEntryActionBehavior(Func<Transition<TState, TTrigger>, object[], Task> action, Reflection.InvocationInfo description) : base(description)
        {
            _action = action;
        }

        public Task ExecuteAsync(Transition<TState, TTrigger> transition, object[] args)
        {
            return _action(transition, args);
        }
    }
}

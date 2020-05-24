using System;
using System.Threading.Tasks;

namespace Stateless
{
    internal abstract class InternalTriggerBehaviour<TState, TTrigger> : TriggerBehaviour<TState, TTrigger>
    {
        protected InternalTriggerBehaviour(TTrigger trigger, TransitionGuard guard) : base(trigger, guard)
        {
        }

        public override bool ResultsInTransitionFrom(TState source, object[] args, out TState destination)
        {
            destination = source;
            return false;
        }
    }

    internal class SyncInternalTriggerBehaviour<TState, TTrigger> : InternalTriggerBehaviour<TState, TTrigger>
    {
        public Action<Transition<TState, TTrigger>, object[]> InternalAction { get; }

        public SyncInternalTriggerBehaviour(TTrigger trigger, Func<object[], bool> guard, Action<Transition<TState, TTrigger>, object[]> internalAction, string guardDescription = null) : base(trigger, new TransitionGuard(guard, guardDescription))
        {
            InternalAction = internalAction;
        }
        public void Execute(Transition<TState, TTrigger> transition, object[] args)
        {
            InternalAction(transition, args);
        }
    }

    internal class AsyncInternalTriggerBehaviour<TState, TTrigger> : InternalTriggerBehaviour<TState, TTrigger>
    {
        readonly Func<Transition<TState, TTrigger>, object[], Task> InternalAction;

        public AsyncInternalTriggerBehaviour(TTrigger trigger, Func<bool> guard,Func<Transition<TState, TTrigger>, object[], Task> internalAction, string guardDescription = null) : base(trigger, new TransitionGuard(guard, guardDescription))
        {
            InternalAction = internalAction;
        }

        public Task ExecuteAsync(Transition<TState, TTrigger> transition, object[] args)
        {
            return InternalAction(transition, args);
        }
    }
}
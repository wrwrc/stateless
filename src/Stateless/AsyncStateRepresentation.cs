#if TASKS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Stateless
{
    internal class AsyncStateRepresentation<TState, TTrigger>
    {
        protected readonly TState _state;

        internal IDictionary<TTrigger, ICollection<TriggerBehaviour<TState, TTrigger>>> TriggerBehaviours { get; } = new Dictionary<TTrigger, ICollection<TriggerBehaviour<TState, TTrigger>>>();
        internal ICollection<AsyncEntryActionBehavior<TState, TTrigger>> EntryActions { get; } = new List<AsyncEntryActionBehavior<TState, TTrigger>>();
        internal ICollection<AsyncExitActionBehavior<TState, TTrigger>> ExitActions { get; } = new List<AsyncExitActionBehavior<TState, TTrigger>>();
        internal ICollection<AsyncActivateActionBehaviour<TState>> ActivateActions { get; } = new List<AsyncActivateActionBehaviour<TState>>();
        internal ICollection<AsyncDeactivateActionBehaviour<TState>> DeactivateActions { get; } = new List<AsyncDeactivateActionBehaviour<TState>>();

        AsyncStateRepresentation<TState, TTrigger> _superstate; // null

        readonly ICollection<AsyncStateRepresentation<TState, TTrigger>> _substates = new List<AsyncStateRepresentation<TState, TTrigger>>();
        public TState InitialTransitionTarget { get; private set; } = default(TState);

        public AsyncStateRepresentation(TState state)
        {
            _state = state;
        }

        public AsyncStateRepresentation<TState, TTrigger> Superstate
        {
            get
            {
                return _superstate;
            }
            set
            {
                _superstate = value;
            }
        }

        public TState UnderlyingState
        {
            get
            {
                return _state;
            }
        }

        public bool HasInitialTransition { get; private set; }

        internal ICollection<AsyncStateRepresentation<TState, TTrigger>> GetSubstates()
        {
            return _substates;
        }

        public void AddActivateAction(Func<Task> action, Reflection.InvocationInfo activateActionDescription)
        {
            ActivateActions.Add(new AsyncActivateActionBehaviour<TState>(_state, action, activateActionDescription));
        }

        public void AddDeactivateAction(Func<Task> action, Reflection.InvocationInfo deactivateActionDescription)
        {
            DeactivateActions.Add(new AsyncDeactivateActionBehaviour<TState>(_state, action, deactivateActionDescription));
        }

        public void AddEntryAction(TTrigger trigger, Func<Transition<TState, TTrigger>, object[], Task> action, Reflection.InvocationInfo entryActionDescription)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            EntryActions.Add(
                new AsyncEntryActionBehavior<TState, TTrigger>((t, args) =>
                {
                    if (t.Trigger.Equals(trigger))
                        return action(t, args);

                    return TaskResult.Done;
                },
                entryActionDescription));
        }

        public void AddEntryAction(Func<Transition<TState, TTrigger>, object[], Task> action, Reflection.InvocationInfo entryActionDescription)
        {
            EntryActions.Add(
                new AsyncEntryActionBehavior<TState, TTrigger>(
                    action,
                    entryActionDescription));
        }

        public void AddExitAction(Func<Transition<TState, TTrigger>, Task> action, Reflection.InvocationInfo exitActionDescription)
        {
            ExitActions.Add(new AsyncExitActionBehavior<TState, TTrigger>(action, exitActionDescription));
        }

        public void AddSubstate(AsyncStateRepresentation<TState, TTrigger> substate)
        {
            _substates.Add(substate);
        }

        public void AddTriggerBehaviour(TriggerBehaviour<TState, TTrigger> triggerBehaviour)
        {
            if (!TriggerBehaviours.TryGetValue(triggerBehaviour.Trigger, out ICollection<TriggerBehaviour<TState, TTrigger>> allowed))
            {
                allowed = new List<TriggerBehaviour<TState, TTrigger>>();
                TriggerBehaviours.Add(triggerBehaviour.Trigger, allowed);
            }
            allowed.Add(triggerBehaviour);
        }

        public async Task ActivateAsync()
        {
            if (_superstate != null)
                await _superstate.ActivateAsync().ConfigureAwait(false);

            await ExecuteActivationActionsAsync().ConfigureAwait(false);
        }

        public async Task DeactivateAsync()
        {
            await ExecuteDeactivationActionsAsync().ConfigureAwait(false);

            if (_superstate != null)
                await _superstate.DeactivateAsync().ConfigureAwait(false);
        }

        async Task ExecuteActivationActionsAsync()
        {
            foreach (var action in ActivateActions)
                await action.ExecuteAsync().ConfigureAwait(false);
        }

        async Task ExecuteDeactivationActionsAsync()
        {
            foreach (var action in DeactivateActions)
                await action.ExecuteAsync().ConfigureAwait(false);
        }

        public async Task EnterAsync(Transition<TState, TTrigger> transition, params object[] entryArgs)
        {
            if (transition.IsReentry)
            {
                await ExecuteEntryActionsAsync(transition, entryArgs).ConfigureAwait(false);
            }
            else if (!Includes(transition.Source))
            {
                if (_superstate != null && !(transition is InitialTransition<TState, TTrigger>))
                    await _superstate.EnterAsync(transition, entryArgs).ConfigureAwait(false);

                await ExecuteEntryActionsAsync(transition, entryArgs).ConfigureAwait(false);
            }
        }

        public async Task<Transition<TState, TTrigger>> ExitAsync(Transition<TState, TTrigger> transition)
        {
            if (transition.IsReentry)
            {
                await ExecuteExitActionsAsync(transition).ConfigureAwait(false);
            }
            else if (!Includes(transition.Destination))
            {
                await ExecuteExitActionsAsync(transition).ConfigureAwait(false);

                if (_superstate != null)
                {
                    // Check if destination is within the state list
                    if (IsIncludedIn(transition.Destination))
                    {
                        // Destination state is within the list, exit first superstate only if it is NOT the the first
                        if (!_superstate.UnderlyingState.Equals(transition.Destination))
                        {
                            return await _superstate.ExitAsync(transition).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        return await _superstate.ExitAsync(transition).ConfigureAwait(false);
                    }
                }
            }
            return transition;
        }

        public bool Includes(TState state)
        {
            return _state.Equals(state) || _substates.Any(s => s.Includes(state));
        }

        public bool IsIncludedIn(TState state)
        {
            return
                _state.Equals(state) ||
                (_superstate != null && _superstate.IsIncludedIn(state));
        }

        public bool TryFindHandler(TTrigger trigger, object[] args, out TriggerBehaviourResult<TState, TTrigger> handler)
        {
            return (TryFindLocalHandler(trigger, args, out handler) ||
                (Superstate != null && Superstate.TryFindHandler(trigger, args, out handler)));
        }

        private bool TryFindLocalHandler(TTrigger trigger, object[] args, out TriggerBehaviourResult<TState, TTrigger> handlerResult)
        {
            // Get list of candidate trigger handlers
            if (!TriggerBehaviours.TryGetValue(trigger, out ICollection<TriggerBehaviour<TState, TTrigger>> possible))
            {
                handlerResult = null;
                return false;
            }
           
            // Guard functions are executed here
            var actual = possible
                .Select(h => new TriggerBehaviourResult<TState, TTrigger>(h, h.UnmetGuardConditions(args)))
                .ToArray();

            // Find a handler for the trigger
            handlerResult = TryFindLocalHandlerResult(trigger, actual)
                ?? TryFindLocalHandlerResultWithUnmetGuardConditions(actual);

            if (handlerResult == null)
                return false;

            return !handlerResult.UnmetGuardConditions.Any();
        }

        private TriggerBehaviourResult<TState, TTrigger> TryFindLocalHandlerResult(TTrigger trigger, IEnumerable<TriggerBehaviourResult<TState, TTrigger>> results)
        {
            var actual = results
                .Where(r => !r.UnmetGuardConditions.Any())
                .ToList();

            if (actual.Count <= 1)
                return actual.FirstOrDefault();

            var message = string.Format(StateRepresentationResources.MultipleTransitionsPermitted, trigger, _state);
            throw new InvalidOperationException(message);
        }

        private static TriggerBehaviourResult<TState, TTrigger> TryFindLocalHandlerResultWithUnmetGuardConditions(IEnumerable<TriggerBehaviourResult<TState, TTrigger>> results)
        {
           return results.FirstOrDefault(r => r.UnmetGuardConditions.Any());
        }

        async Task ExecuteEntryActionsAsync(Transition<TState, TTrigger> transition, object[] entryArgs)
        {
            foreach (var action in EntryActions)
                await action.ExecuteAsync(transition, entryArgs).ConfigureAwait(false);
        }

        async Task ExecuteExitActionsAsync(Transition<TState, TTrigger> transition)
        {
            foreach (var action in ExitActions)
                await action.ExecuteAsync(transition).ConfigureAwait(false);
        }

        async Task ExecuteInternalActionsAsync(Transition<TState, TTrigger> transition, object[] args)
        {
            AsyncInternalTriggerBehaviour<TState, TTrigger> internalTransition = null;

            // Look for actions in superstate(s) recursivly until we hit the topmost superstate, or we actually find some trigger handlers.
            AsyncStateRepresentation<TState, TTrigger> aStateRep = this;
            while (aStateRep != null)
            {
                if (aStateRep.TryFindLocalHandler(transition.Trigger, args, out TriggerBehaviourResult<TState, TTrigger> result))
                {
                    // Trigger handler(s) found in this state
                    internalTransition = result.Handler as AsyncInternalTriggerBehaviour<TState, TTrigger>;
                    break;
                }
                // Try to look for trigger handlers in superstate (if it exists)
                aStateRep = aStateRep._superstate;
            }

            // Execute internal transition event handler
            if (internalTransition == null) throw new ArgumentNullException("The configuration is incorrect, no action assigned to this internal transition.");
            await (internalTransition.ExecuteAsync(transition, args)).ConfigureAwait(false);
        }

        internal Task InternalActionAsync(Transition<TState, TTrigger> transition, object[] args)
        {
            return ExecuteInternalActionsAsync(transition, args);
        }

        internal void SetInitialTransition(TState state)
        {
            InitialTransitionTarget = state;
            HasInitialTransition = true;
        }
    }
}

#endif

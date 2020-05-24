using System;
using System.Collections.Generic;
using System.Linq;

namespace Stateless
{
    internal partial class StateRepresentation<TState, TTrigger>
    {
        protected readonly TState _state;

        internal IDictionary<TTrigger, ICollection<TriggerBehaviour<TState, TTrigger>>> TriggerBehaviours { get; } = new Dictionary<TTrigger, ICollection<TriggerBehaviour<TState, TTrigger>>>();
        internal ICollection<SyncEntryActionBehavior<TState, TTrigger>> EntryActions { get; } = new List<SyncEntryActionBehavior<TState, TTrigger>>();
        internal ICollection<SyncExitActionBehavior<TState, TTrigger>> ExitActions { get; } = new List<SyncExitActionBehavior<TState, TTrigger>>();
        internal ICollection<SyncActivateActionBehaviour<TState>> ActivateActions { get; } = new List<SyncActivateActionBehaviour<TState>>();
        internal ICollection<SyncDeactivateActionBehaviour<TState>> DeactivateActions { get; } = new List<SyncDeactivateActionBehaviour<TState>>();

        StateRepresentation<TState, TTrigger> _superstate; // null

        readonly ICollection<StateRepresentation<TState, TTrigger>> _substates = new List<StateRepresentation<TState, TTrigger>>();
        public TState InitialTransitionTarget { get; private set; } = default(TState);

        public StateRepresentation(TState state)
        {
            _state = state;
        }

        internal ICollection<StateRepresentation<TState, TTrigger>> GetSubstates()
        {
            return _substates;
        }

        public bool CanHandle(TTrigger trigger, params object[] args)
        {
            return TryFindHandler(trigger, args, out TriggerBehaviourResult<TState, TTrigger> unused);
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

        public void AddActivateAction(Action action, Reflection.InvocationInfo activateActionDescription)
        {
            ActivateActions.Add(new SyncActivateActionBehaviour<TState>(_state, action, activateActionDescription));
        }

        public void AddDeactivateAction(Action action, Reflection.InvocationInfo deactivateActionDescription)
        {
            DeactivateActions.Add(new SyncDeactivateActionBehaviour<TState>(_state, action, deactivateActionDescription));
        }

        public void AddEntryAction(TTrigger trigger, Action<Transition<TState, TTrigger>, object[]> action, Reflection.InvocationInfo entryActionDescription)
        {
            EntryActions.Add(new SyncFromEntryActionBehavior<TState, TTrigger, TTrigger>(trigger, action, entryActionDescription));
        }

        public void AddEntryAction(Action<Transition<TState, TTrigger>, object[]> action, Reflection.InvocationInfo entryActionDescription)
        {
            EntryActions.Add(new SyncEntryActionBehavior<TState, TTrigger>(action, entryActionDescription));
        }

        public void AddExitAction(Action<Transition<TState, TTrigger>> action, Reflection.InvocationInfo exitActionDescription)
        {
            ExitActions.Add(new SyncExitActionBehavior<TState, TTrigger>(action, exitActionDescription));
        }

        public void Activate()
        {
            if (_superstate != null)
                _superstate.Activate();

            ExecuteActivationActions();
        }

        public void Deactivate()
        {
            ExecuteDeactivationActions();

            if (_superstate != null)
                _superstate.Deactivate();
        }

        void ExecuteActivationActions()
        {
            foreach (var action in ActivateActions)
                action.Execute();
        }

        void ExecuteDeactivationActions()
        {
            foreach (var action in DeactivateActions)
                action.Execute();
        }

        public void Enter(Transition<TState, TTrigger> transition, params object[] entryArgs)
        {
            if (transition.IsReentry)
            {
                ExecuteEntryActions(transition, entryArgs);
            }
            else if (!Includes(transition.Source))
            {
                if (_superstate != null && !(transition is InitialTransition<TState, TTrigger>))
                    _superstate.Enter(transition, entryArgs);

                ExecuteEntryActions(transition, entryArgs);
            }
        }

        public Transition<TState, TTrigger> Exit(Transition<TState, TTrigger> transition)
        {
            if (transition.IsReentry)
            {
                ExecuteExitActions(transition);
            }
            else if (!Includes(transition.Destination))
            {
                ExecuteExitActions(transition);

                // Must check if there is a superstate, and if we are leaving that superstate
                if (_superstate != null)
                {
                    // Check if destination is within the state list
                    if (IsIncludedIn(transition.Destination))
                    {
                        // Destination state is within the list, exit first superstate only if it is NOT the the first
                        if (!_superstate.UnderlyingState.Equals(transition.Destination))
                        {
                            return _superstate.Exit(transition);
                        }
                    }
                    else
                    {
                        // Exit the superstate as well
                        return _superstate.Exit(transition);
                    }
                }
            }
            return transition;
        }

        void ExecuteEntryActions(Transition<TState, TTrigger> transition, object[] entryArgs)
        {
            foreach (var action in EntryActions)
                action.Execute(transition, entryArgs);
        }

        void ExecuteExitActions(Transition<TState, TTrigger> transition)
        {
            foreach (var action in ExitActions)
                action.Execute(transition);
        }
        internal void InternalAction(Transition<TState, TTrigger> transition, object[] args)
        {
            SyncInternalTriggerBehaviour<TState, TTrigger> internalTransition = null;

            // Look for actions in superstate(s) recursivly until we hit the topmost superstate, or we actually find some trigger handlers.
            StateRepresentation<TState, TTrigger> aStateRep = this;
            while (aStateRep != null)
            {
                if (aStateRep.TryFindLocalHandler(transition.Trigger, args, out TriggerBehaviourResult<TState, TTrigger> result))
                {
                    internalTransition = result.Handler as SyncInternalTriggerBehaviour<TState, TTrigger>;
                    break;
                }
                // Try to look for trigger handlers in superstate (if it exists)
                aStateRep = aStateRep._superstate;
            }

            // Execute internal transition event handler
            if (internalTransition == null) throw new ArgumentNullException("The configuration is incorrect, no action assigned to this internal transition.");
            internalTransition.InternalAction(transition, args);
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

        public StateRepresentation<TState, TTrigger> Superstate
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

        public void AddSubstate(StateRepresentation<TState, TTrigger> substate)
        {
            _substates.Add(substate);
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

        public IEnumerable<TTrigger> PermittedTriggers
        {
            get
            {
                return GetPermittedTriggers();
            }
        }

        public IEnumerable<TTrigger> GetPermittedTriggers(params object[] args)
        {
            var result = TriggerBehaviours
                .Where(t => t.Value.Any(a => !a.UnmetGuardConditions(args).Any()))
                .Select(t => t.Key);

            if (Superstate != null)
                result = result.Union(Superstate.GetPermittedTriggers(args));

            return result;
        }

        internal void SetInitialTransition(TState state)
        {
            InitialTransitionTarget = state;
            HasInitialTransition = true;
        }
        public bool HasInitialTransition { get; private set; }
    }
}

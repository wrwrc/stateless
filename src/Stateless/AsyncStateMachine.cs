#if TASKS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stateless.Reflection;

namespace Stateless
{
    /// <summary>
    /// Models behaviour as transitions between a finite set of states.
    /// </summary>
    /// <typeparam name="TState">The type used to represent the states.</typeparam>
    /// <typeparam name="TTrigger">The type used to represent the triggers that cause state transitions.</typeparam>
    public class AsyncStateMachine<TState, TTrigger>
    {
        private readonly IDictionary<TState, AsyncStateRepresentation<TState, TTrigger>> _stateConfiguration = new Dictionary<TState, AsyncStateRepresentation<TState, TTrigger>>();
        private readonly IDictionary<TTrigger, TriggerWithParameters<TTrigger>> _triggerConfiguration = new Dictionary<TTrigger, TriggerWithParameters<TTrigger>>();
        private readonly Func<TState> _stateAccessor;
        private readonly Action<TState> _stateMutator;
        private UnhandledTriggerAction<TState, TTrigger> _unhandledTriggerAction;
        private OnTransitionedEvent<TState, TTrigger> _onTransitionedEvent;
        private readonly FiringMode _firingMode;
        private readonly Queue<QueuedTrigger<TTrigger>> _eventQueue = new Queue<QueuedTrigger<TTrigger>>();
        private bool _firing;

        /// <summary>
        /// Construct a state machine with external state storage.
        /// </summary>
        /// <param name="stateAccessor">A function that will be called to read the current state value.</param>
        /// <param name="stateMutator">An action that will be called to write new state values.</param>
        public AsyncStateMachine(Func<TState> stateAccessor, Action<TState> stateMutator) :this(stateAccessor, stateMutator, FiringMode.Queued)
        {
        }

        /// <summary>
        /// Construct a state machine.
        /// </summary>
        /// <param name="initialState">The initial state.</param>
        public AsyncStateMachine(TState initialState) : this(initialState, FiringMode.Queued)
        {
        }

        /// <summary>
        /// Construct a state machine with external state storage.
        /// </summary>
        /// <param name="stateAccessor">A function that will be called to read the current state value.</param>
        /// <param name="stateMutator">An action that will be called to write new state values.</param>
        /// <param name="firingMode">Optional specification of fireing mode.</param>
        public AsyncStateMachine(Func<TState> stateAccessor, Action<TState> stateMutator, FiringMode firingMode) : this()
        {
            _stateAccessor = stateAccessor ?? throw new ArgumentNullException(nameof(stateAccessor));
            _stateMutator = stateMutator ?? throw new ArgumentNullException(nameof(stateMutator));

            _firingMode = firingMode;
        }

        /// <summary>
        /// Construct a state machine.
        /// </summary>
        /// <param name="initialState">The initial state.</param>
        /// <param name="firingMode">Optional specification of fireing mode.</param>
        public AsyncStateMachine(TState initialState, FiringMode firingMode) : this()
        {
            var reference = new StateReference<TState> { State = initialState };
            _stateAccessor = () => reference.State;
            _stateMutator = s => reference.State = s;

            _firingMode = firingMode;
        }


        /// <summary>
        /// Default constuctor
        /// </summary>
        AsyncStateMachine()
        {
            _unhandledTriggerAction = new SyncUnhandledTriggerAction<TState, TTrigger>(DefaultUnhandledTriggerAction);
            _onTransitionedEvent = new OnTransitionedEvent<TState, TTrigger>();
        }

        /// <summary>
        /// The current state.
        /// </summary>
        public TState State
        {
            get
            {
                return _stateAccessor();
            }
            private set
            {
                _stateMutator(value);
            }
        }

        AsyncStateRepresentation<TState, TTrigger> CurrentRepresentation
        {
            get
            {
                return GetRepresentation(State);
            }
        }

        /// <summary>
        /// Begin configuration of the entry/exit actions and allowed transitions
        /// when the state machine is in a particular state.
        /// </summary>
        /// <param name="state">The state to configure.</param>
        /// <returns>A configuration object through which the state can be configured.</returns>
        public AsyncStateConfiguration<TState, TTrigger> Configure(TState state)
        {
            return new AsyncStateConfiguration<TState, TTrigger>(this, GetRepresentation(state), GetRepresentation);
        }

        /// <summary>
        /// Activates current state in asynchronous fashion. Actions associated with activating the currrent state
        /// will be invoked. The activation is idempotent and subsequent activation of the same current state 
        /// will not lead to re-execution of activation callbacks.
        /// </summary>
        public Task ActivateAsync()
        {
            var representativeState = GetRepresentation(State);
            return representativeState.ActivateAsync();
        }

        /// <summary>
        /// Deactivates current state in asynchronous fashion. Actions associated with deactivating the currrent state
        /// will be invoked. The deactivation is idempotent and subsequent deactivation of the same current state 
        /// will not lead to re-execution of deactivation callbacks.
        /// </summary>
        public Task DeactivateAsync()
        {
            var representativeState = GetRepresentation(State);
            return representativeState.DeactivateAsync();
        }

        /// <summary>
        /// Transition from the current state via the specified trigger in async fashion.
        /// The target state is determined by the configuration of the current state.
        /// Actions associated with leaving the current state and entering the new one
        /// will be invoked.
        /// </summary>
        /// <param name="trigger">The trigger to fire.</param>
        /// <exception cref="System.InvalidOperationException">The current state does
        /// not allow the trigger to be fired.</exception>
        public Task FireAsync(TTrigger trigger)
        {
            return InternalFireAsync(trigger, new object[0]);
        }

        /// <summary>
        /// Transition from the current state via the specified trigger in async fashion.
        /// The target state is determined by the configuration of the current state.
        /// Actions associated with leaving the current state and entering the new one
        /// will be invoked.
        /// </summary>
        /// <typeparam name="TArg0">Type of the first trigger argument.</typeparam>
        /// <param name="trigger">The trigger to fire.</param>
        /// <param name="arg0">The first argument.</param>
        /// <exception cref="System.InvalidOperationException">The current state does
        /// not allow the trigger to be fired.</exception>
        public Task FireAsync<TArg0>(TriggerWithParameters<TTrigger, TArg0> trigger, TArg0 arg0)
        {
            if (trigger == null) throw new ArgumentNullException(nameof(trigger));

            return InternalFireAsync(trigger.Trigger, arg0);
        }

        /// <summary>
        /// Transition from the current state via the specified trigger in async fashion.
        /// The target state is determined by the configuration of the current state.
        /// Actions associated with leaving the current state and entering the new one
        /// will be invoked.
        /// </summary>
        /// <typeparam name="TArg0">Type of the first trigger argument.</typeparam>
        /// <typeparam name="TArg1">Type of the second trigger argument.</typeparam>
        /// <param name="arg0">The first argument.</param>
        /// <param name="arg1">The second argument.</param>
        /// <param name="trigger">The trigger to fire.</param>
        /// <exception cref="System.InvalidOperationException">The current state does
        /// not allow the trigger to be fired.</exception>
        public Task FireAsync<TArg0, TArg1>(TriggerWithParameters<TTrigger, TArg0, TArg1> trigger, TArg0 arg0, TArg1 arg1)
        {
            if (trigger == null) throw new ArgumentNullException(nameof(trigger));

            return InternalFireAsync(trigger.Trigger, arg0, arg1);
        }

        /// <summary>
        /// Transition from the current state via the specified trigger in async fashion.
        /// The target state is determined by the configuration of the current state.
        /// Actions associated with leaving the current state and entering the new one
        /// will be invoked.
        /// </summary>
        /// <typeparam name="TArg0">Type of the first trigger argument.</typeparam>
        /// <typeparam name="TArg1">Type of the second trigger argument.</typeparam>
        /// <typeparam name="TArg2">Type of the third trigger argument.</typeparam>
        /// <param name="arg0">The first argument.</param>
        /// <param name="arg1">The second argument.</param>
        /// <param name="arg2">The third argument.</param>
        /// <param name="trigger">The trigger to fire.</param>
        /// <exception cref="System.InvalidOperationException">The current state does
        /// not allow the trigger to be fired.</exception>
        public Task FireAsync<TArg0, TArg1, TArg2>(TriggerWithParameters<TTrigger, TArg0, TArg1, TArg2> trigger, TArg0 arg0, TArg1 arg1, TArg2 arg2)
        {
            if (trigger == null) throw new ArgumentNullException(nameof(trigger));

            return InternalFireAsync(trigger.Trigger, arg0, arg1, arg2);
        }

        /// <summary>
        /// Determine how to Fire the trigger
        /// </summary>
        /// <param name="trigger">The trigger. </param>
        /// <param name="args">A variable-length parameters list containing arguments. </param>
        async Task InternalFireAsync(TTrigger trigger, params object[] args)
        {
            switch (_firingMode)
            {
                case FiringMode.Immediate:
                    await InternalFireOneAsync(trigger, args);
                    break;
                case FiringMode.Queued:
                    await InternalFireQueuedAsync(trigger, args);
                    break;
                default:
                    // If something is completely messed up we let the user know ;-)
                    throw new InvalidOperationException("The firing mode has not been configured!");
            }
        }

        /// <summary>
        /// Queue events and then fire in order.
        /// If only one event is queued, this behaves identically to the non-queued version.
        /// </summary>
        /// <param name="trigger">  The trigger. </param>
        /// <param name="args">     A variable-length parameters list containing arguments. </param>
        async Task InternalFireQueuedAsync(TTrigger trigger, params object[] args)
        {
            if (_firing)
            {
                _eventQueue.Enqueue(new QueuedTrigger<TTrigger> { Trigger = trigger, Args = args });
                return;
            }

            try
            {
                _firing = true;

                await InternalFireOneAsync(trigger, args).ConfigureAwait(false);

                while (_eventQueue.Count != 0)
                {
                    var queuedEvent = _eventQueue.Dequeue();
                    await InternalFireOneAsync(queuedEvent.Trigger, queuedEvent.Args).ConfigureAwait(false);
                }
            }
            finally
            {
                _firing = false;
            }
        }
        async Task InternalFireOneAsync(TTrigger trigger, params object[] args)
        {
            // If this is a trigger with parameters, we must validate the parameter(s)
            if (_triggerConfiguration.TryGetValue(trigger, out var configuration))
                configuration.ValidateParameters(args);

            var source = State;
            var representativeState = GetRepresentation(source);

            // Try to find a trigger handler, either in the current state or a super state.
            if (!representativeState.TryFindHandler(trigger, args, out var result))
            {
                await _unhandledTriggerAction.ExecuteAsync(representativeState.UnderlyingState, trigger, result?.UnmetGuardConditions).ConfigureAwait(false);
                return;
            }
            // Check if this trigger should be ignored
            if (result.Handler is IgnoredTriggerBehaviour<TState, TTrigger>)
            {
                return;
            }

            // Handle special case, re-entry in superstate
            if (result.Handler is ReentryTriggerBehaviour<TState, TTrigger> handler)
            {
                // Handle transition, and set new state
                var transition = new Transition<TState, TTrigger>(source, handler.Destination, trigger, args);
                transition = await representativeState.ExitAsync(transition);
                State = transition.Destination;
                var newRepresentation = GetRepresentation(transition.Destination);

                if (!source.Equals(transition.Destination))
                {
                    // Then Exit the final superstate
                    transition = new Transition<TState, TTrigger>(handler.Destination, handler.Destination, trigger, args);
                    await newRepresentation.ExitAsync(transition);
                }

                await _onTransitionedEvent.InvokeAsync(new Transition<TState, TTrigger>(source, handler.Destination, trigger, args));

                await newRepresentation.EnterAsync(transition, args);
            }
            // Check if it is an internal transition, or a transition from one state to another.
            else if (result.Handler.ResultsInTransitionFrom(source, args, out var destination))
            {
                var transition = new Transition<TState, TTrigger>(source, destination, trigger, args);

                transition = await representativeState.ExitAsync(transition).ConfigureAwait(false);

                State = transition.Destination;
                var newRepresentation = GetRepresentation(transition.Destination);

                // Alert all listeners of state transition
                await _onTransitionedEvent.InvokeAsync(transition);

                await newRepresentation.EnterAsync(transition, args);

                // Check if there is an intital transition configured
                if (newRepresentation.HasInitialTransition)
                {
                    // Verify that the target state is a substate
                    if (!newRepresentation.GetSubstates().Any(s => s.UnderlyingState.Equals(newRepresentation.InitialTransitionTarget)))
                    {
                        throw new InvalidOperationException($"The target ({newRepresentation.InitialTransitionTarget}) for the initial transition is not a substate.");
                    }

                    // Check if state has substate(s), and if an initial transition(s) has been set up.
                    while (newRepresentation.GetSubstates().Any() && newRepresentation.HasInitialTransition)
                    {
                        var initialTransition = new InitialTransition<TState, TTrigger>(source, newRepresentation.InitialTransitionTarget, trigger, args);
                        newRepresentation = GetRepresentation(newRepresentation.InitialTransitionTarget);
                        await newRepresentation.EnterAsync(initialTransition, args);
                        State = newRepresentation.UnderlyingState;
                    }
                }
            }
            else
            {
                var transition = new Transition<TState, TTrigger>(source, destination, trigger, args);

                await CurrentRepresentation.InternalActionAsync(transition, args).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Provides an info object which exposes the states, transitions, and actions of this machine.
        /// </summary>
        public StateMachineInfo GetInfo()
        {
            var initialState = StateInfo.CreateStateInfo(new StateRepresentation<TState, TTrigger>(State));

            var representations = _stateConfiguration.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            var behaviours = _stateConfiguration.SelectMany(kvp => kvp.Value.TriggerBehaviours.SelectMany(b => b.Value.OfType<TransitioningTriggerBehaviour<TState, TTrigger>>().Select(tb => tb.Destination))).ToList();
            behaviours.AddRange(_stateConfiguration.SelectMany(kvp => kvp.Value.TriggerBehaviours.SelectMany(b => b.Value.OfType<ReentryTriggerBehaviour<TState, TTrigger>>().Select(tb => tb.Destination))).ToList());

            var reachable = behaviours
                .Distinct()
                .Except(representations.Keys)
                .Select(underlying => new AsyncStateRepresentation<TState, TTrigger>(underlying))
                .ToArray();

            foreach (var representation in reachable)
                representations.Add(representation.UnderlyingState, representation);

            var info = representations.ToDictionary(kvp => kvp.Key, kvp => StateInfo.CreateStateInfo(kvp.Value));

            foreach (var state in info)
                StateInfo.AddRelationships(state.Value, representations[state.Key], k => info[k]);

            return new StateMachineInfo(info.Values, typeof(TState), typeof(TTrigger), initialState);
        }

        /// <summary>
        /// Override the default behaviour of throwing an exception when an unhandled trigger
        /// is fired.
        /// </summary>
        /// <param name="unhandledTriggerAction"></param>
        public void OnUnhandledTriggerAsync(Func<TState, TTrigger, Task> unhandledTriggerAction)
        {
            if (unhandledTriggerAction == null) throw new ArgumentNullException(nameof(unhandledTriggerAction));
            _unhandledTriggerAction = new AsyncUnhandledTriggerAction<TState, TTrigger>((s, t, c) => unhandledTriggerAction(s, t));
        }

        /// <summary>
        /// Override the default behaviour of throwing an exception when an unhandled trigger
        /// is fired.
        /// </summary>
        /// <param name="unhandledTriggerAction">An asynchronous action to call when an unhandled trigger is fired.</param>
        public void OnUnhandledTriggerAsync(Func<TState, TTrigger, ICollection<string>, Task> unhandledTriggerAction)
        {
            if (unhandledTriggerAction == null) throw new ArgumentNullException(nameof(unhandledTriggerAction));
            _unhandledTriggerAction = new AsyncUnhandledTriggerAction<TState, TTrigger>(unhandledTriggerAction);
        }

        /// <summary>
        /// Registers an asynchronous callback that will be invoked every time the statemachine
        /// transitions from one state into another.
        /// </summary>
        /// <param name="onTransitionAction">The asynchronous action to execute, accepting the details
        /// of the transition.</param>
        public void OnTransitionedAsync(Func<Transition<TState, TTrigger>, Task> onTransitionAction)
        {
            if (onTransitionAction == null) throw new ArgumentNullException(nameof(onTransitionAction));
            _onTransitionedEvent.Register(onTransitionAction);
        }

        void DefaultUnhandledTriggerAction(TState state, TTrigger trigger, ICollection<string> unmetGuardConditions)
        {
            if (unmetGuardConditions?.Any() ?? false)
                throw new InvalidOperationException(
                    string.Format(
                        StateMachineResources.NoTransitionsUnmetGuardConditions,
                        trigger, state, string.Join(", ", unmetGuardConditions)));

            throw new InvalidOperationException(
                string.Format(
                    StateMachineResources.NoTransitionsPermitted,
                    trigger, state));
        }

        AsyncStateRepresentation<TState, TTrigger> GetRepresentation(TState state)
        {
            if (!_stateConfiguration.TryGetValue(state, out AsyncStateRepresentation<TState, TTrigger> result))
            {
                result = new AsyncStateRepresentation<TState, TTrigger>(state);
                _stateConfiguration.Add(state, result);
            }

            return result;
        }
    }
}

#endif

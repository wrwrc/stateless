using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Stateless.Tests
{

    public class StateRepresentationFixture
    {
        [Fact]
        public void UponEntering_EnteringActionsExecuted()
        {
            var stateRepresentation = CreateRepresentation(State.B);
            Transition<State, Trigger>
                transition = new Transition<State, Trigger>(State.A, State.B, Trigger.X),
                actualTransition = null;
            stateRepresentation.AddEntryAction((t, a) => actualTransition = t, Reflection.InvocationInfo.Create(null, "entryActionDescription"));
            stateRepresentation.Enter(transition);
            Assert.Equal(transition, actualTransition);
        }

        [Fact]
        public void UponLeaving_EnteringActionsNotExecuted()
        {
            var stateRepresentation = CreateRepresentation(State.B);
            Transition<State, Trigger>
                transition = new Transition<State, Trigger>(State.A, State.B, Trigger.X),
                actualTransition = null;
            stateRepresentation.AddEntryAction((t, a) => actualTransition = t, Reflection.InvocationInfo.Create(null, "entryActionDescription"));
            stateRepresentation.Exit(transition);
            Assert.Null(actualTransition);
        }

        [Fact]
        public void UponLeaving_LeavingActionsExecuted()
        {
            var stateRepresentation = CreateRepresentation(State.A);
            Transition<State, Trigger>
                transition = new Transition<State, Trigger>(State.A, State.B, Trigger.X),
                actualTransition = null;
            stateRepresentation.AddExitAction(t => actualTransition = t, Reflection.InvocationInfo.Create(null, "entryActionDescription"));
            stateRepresentation.Exit(transition);
            Assert.Equal(transition, actualTransition);
        }

        [Fact]
        public void UponEntering_LeavingActionsNotExecuted()
        {
            var stateRepresentation = CreateRepresentation(State.A);
            Transition<State, Trigger>
                transition = new Transition<State, Trigger>(State.A, State.B, Trigger.X),
                actualTransition = null;
            stateRepresentation.AddExitAction(t => actualTransition = t, Reflection.InvocationInfo.Create(null, "exitActionDescription"));
            stateRepresentation.Enter(transition);
            Assert.Null(actualTransition);
        }

        [Fact]
        public void IncludesUnderlyingState()
        {
            var stateRepresentation = CreateRepresentation(State.B);
            Assert.True(stateRepresentation.Includes(State.B));
        }

        [Fact]
        public void DoesNotIncludeUnrelatedState()
        {
            var stateRepresentation = CreateRepresentation(State.B);
            Assert.False(stateRepresentation.Includes(State.C));
        }

        [Fact]
        public void IncludesSubstate()
        {
            var stateRepresentation = CreateRepresentation(State.B);
            stateRepresentation.AddSubstate(CreateRepresentation(State.C));
            Assert.True(stateRepresentation.Includes(State.C));
        }

        [Fact]
        public void DoesNotIncludeSuperstate()
        {
            var stateRepresentation = CreateRepresentation(State.B);
            stateRepresentation.Superstate = CreateRepresentation(State.C);
            Assert.False(stateRepresentation.Includes(State.C));
        }

        [Fact]
        public void IsIncludedInUnderlyingState()
        {
            var stateRepresentation = CreateRepresentation(State.B);
            Assert.True(stateRepresentation.IsIncludedIn(State.B));
        }

        [Fact]
        public void IsNotIncludedInUnrelatedState()
        {
            var stateRepresentation = CreateRepresentation(State.B);
            Assert.False(stateRepresentation.IsIncludedIn(State.C));
        }

        [Fact]
        public void IsNotIncludedInSubstate()
        {
            var stateRepresentation = CreateRepresentation(State.B);
            stateRepresentation.AddSubstate(CreateRepresentation(State.C));
            Assert.False(stateRepresentation.IsIncludedIn(State.C));
        }

        [Fact]
        public void IsIncludedInSuperstate()
        {
            var stateRepresentation = CreateRepresentation(State.B);
            stateRepresentation.Superstate = CreateRepresentation(State.C);
            Assert.True(stateRepresentation.IsIncludedIn(State.C));
        }

        [Fact]
        public void WhenTransitioningFromSubToSuperstate_SubstateEntryActionsExecuted()
        {
            CreateSuperSubstatePair(out StateRepresentation<State, Trigger> super, out StateRepresentation<State, Trigger> sub);

            var executed = false;
            sub.AddEntryAction((t, a) => executed = true, Reflection.InvocationInfo.Create(null, "entryActionDescription"));
            var transition = new Transition<State, Trigger>(super.UnderlyingState, sub.UnderlyingState, Trigger.X);
            sub.Enter(transition);
            Assert.True(executed);
        }

        [Fact]
        public void WhenTransitioningFromSubToSuperstate_SubstateExitActionsExecuted()
        {
            CreateSuperSubstatePair(out StateRepresentation<State, Trigger> super, out StateRepresentation<State, Trigger> sub);

            var executed = false;
            sub.AddExitAction(t => executed = true, Reflection.InvocationInfo.Create(null, "exitActionDescription"));
            var transition = new Transition<State, Trigger>(sub.UnderlyingState, super.UnderlyingState, Trigger.X);
            sub.Exit(transition);
            Assert.True(executed);
        }

        [Fact]
        public void WhenTransitioningToSuperFromSubstate_SuperEntryActionsNotExecuted()
        {
            CreateSuperSubstatePair(out StateRepresentation<State, Trigger> super, out StateRepresentation<State, Trigger> sub);

            var executed = false;
            super.AddEntryAction((t, a) => executed = true, Reflection.InvocationInfo.Create(null, "entryActionDescription"));
            var transition = new Transition<State, Trigger>(super.UnderlyingState, sub.UnderlyingState, Trigger.X);
            super.Enter(transition);
            Assert.False(executed);
        }

        [Fact]
        public void WhenTransitioningFromSuperToSubstate_SuperExitActionsNotExecuted()
        {
            CreateSuperSubstatePair(out StateRepresentation<State, Trigger> super, out StateRepresentation<State, Trigger> sub);

            var executed = false;
            super.AddExitAction(t => executed = true, Reflection.InvocationInfo.Create(null, "exitActionDescription"));
            var transition = new Transition<State, Trigger>(super.UnderlyingState, sub.UnderlyingState, Trigger.X);
            super.Exit(transition);
            Assert.False(executed);
        }

        [Fact]
        public void WhenEnteringSubstate_SuperEntryActionsExecuted()
        {
            CreateSuperSubstatePair(out StateRepresentation<State, Trigger> super, out StateRepresentation<State, Trigger> sub);

            var executed = false;
            super.AddEntryAction((t, a) => executed = true, Reflection.InvocationInfo.Create(null, "entryActionDescription"));
            var transition = new Transition<State, Trigger>(State.C, sub.UnderlyingState, Trigger.X);
            sub.Enter(transition);
            Assert.True(executed);
        }

        [Fact]
        public void WhenLeavingSubstate_SuperExitActionsExecuted()
        {
            CreateSuperSubstatePair(out StateRepresentation<State, Trigger> super, out StateRepresentation<State, Trigger> sub);

            var executed = false;
            super.AddExitAction(t => executed = true, Reflection.InvocationInfo.Create(null, "exitActionDescription"));
            var transition = new Transition<State, Trigger>(sub.UnderlyingState, State.C, Trigger.X);
            sub.Exit(transition);
            Assert.True(executed);
        }

        [Fact]
        public void EntryActionsExecuteInOrder()
        {
            var actual = new List<int>();

            var rep = CreateRepresentation(State.B);
            rep.AddEntryAction((t, a) => actual.Add(0), Reflection.InvocationInfo.Create(null, "entryActionDescription"));
            rep.AddEntryAction((t, a) => actual.Add(1), Reflection.InvocationInfo.Create(null, "entryActionDescription"));

            rep.Enter(new Transition<State, Trigger>(State.A, State.B, Trigger.X));

            Assert.Equal(2, actual.Count);
            Assert.Equal(0, actual[0]);
            Assert.Equal(1, actual[1]);
        }

        [Fact]
        public void ExitActionsExecuteInOrder()
        {
            var actual = new List<int>();

            var rep = CreateRepresentation(State.B);
            rep.AddExitAction(t => actual.Add(0), Reflection.InvocationInfo.Create(null, "entryActionDescription"));
            rep.AddExitAction(t => actual.Add(1), Reflection.InvocationInfo.Create(null, "entryActionDescription"));

            rep.Exit(new Transition<State, Trigger>(State.B, State.C, Trigger.X));

            Assert.Equal(2, actual.Count);
            Assert.Equal(0, actual[0]);
            Assert.Equal(1, actual[1]);
        }

        [Fact]
        public void WhenTransitionExists_TriggerCannotBeFired()
        {
            var rep = CreateRepresentation(State.B);
            Assert.False(rep.CanHandle(Trigger.X));
        }

        [Fact]
        public void WhenTransitionDoesNotExist_TriggerCanBeFired()
        {
            var rep = CreateRepresentation(State.B);
            rep.AddTriggerBehaviour(new IgnoredTriggerBehaviour<State, Trigger>(Trigger.X, null));
            Assert.True(rep.CanHandle(Trigger.X));
        }

        [Fact]
        public void WhenTransitionExistsInSupersate_TriggerCanBeFired()
        {
            var rep = CreateRepresentation(State.B);
            rep.AddTriggerBehaviour(new IgnoredTriggerBehaviour<State, Trigger>(Trigger.X, null));
            var sub = CreateRepresentation(State.C);
            sub.Superstate = rep;
            rep.AddSubstate(sub);
            Assert.True(sub.CanHandle(Trigger.X));
        }

        [Fact]
        public void WhenEnteringSubstate_SuperstateEntryActionsExecuteBeforeSubstate()
        {
            CreateSuperSubstatePair(out StateRepresentation<State, Trigger> super, out StateRepresentation<State, Trigger> sub);

            int order = 0, subOrder = 0, superOrder = 0;
            super.AddEntryAction((t, a) => superOrder = order++, Reflection.InvocationInfo.Create(null, "entryActionDescription"));
            sub.AddEntryAction((t, a) => subOrder = order++, Reflection.InvocationInfo.Create(null, "entryActionDescription"));
            var transition = new Transition<State, Trigger>(State.C, sub.UnderlyingState, Trigger.X);
            sub.Enter(transition);
            Assert.True(superOrder < subOrder);
        }

        [Fact]
        public void WhenExitingSubstate_SubstateEntryActionsExecuteBeforeSuperstate()
        {
            CreateSuperSubstatePair(out StateRepresentation<State, Trigger> super, out StateRepresentation<State, Trigger> sub);

            int order = 0, subOrder = 0, superOrder = 0;
            super.AddExitAction(t => superOrder = order++, Reflection.InvocationInfo.Create(null, "entryActionDescription"));
            sub.AddExitAction(t => subOrder = order++, Reflection.InvocationInfo.Create(null, "entryActionDescription"));
            var transition = new Transition<State, Trigger>(sub.UnderlyingState, State.C, Trigger.X);
            sub.Exit(transition);
            Assert.True(subOrder < superOrder);
        }

        [Fact]
        public void WhenTransitionUnmetGuardConditions_TriggerCannotBeFired()
        {
            var rep = CreateRepresentation(State.B);

            var falseConditions = new[] {
                new Tuple<Func<object[], bool>, string>(args => true, "1"),
                new Tuple<Func<object[], bool>, string>(args => false, "2")
            };

            var transitionGuard = new TransitionGuard(falseConditions);
            var transition = new TransitioningTriggerBehaviour<State, Trigger>(Trigger.X, State.C, transitionGuard);
            rep.AddTriggerBehaviour(transition);

            Assert.False(rep.CanHandle(Trigger.X));
        }

        [Fact]
        public void WhenTransitioGuardConditionsMet_TriggerCanBeFired()
        {
            var rep = CreateRepresentation(State.B);

            var trueConditions = new[] {
                new Tuple<Func<object[], bool>, string>(args => true, "1"),
                new Tuple<Func<object[], bool>, string>(args => true, "2")
            };

            var transitionGuard = new TransitionGuard(trueConditions);
            var transition = new TransitioningTriggerBehaviour<State, Trigger>(Trigger.X, State.C, transitionGuard);
            rep.AddTriggerBehaviour(transition);

            Assert.True(rep.CanHandle(Trigger.X));
        }

        [Fact]
        public void WhenTransitionExistAndSuperstateUnmetGuardConditions_FireNotPossible()
        {
            CreateSuperSubstatePair(out StateRepresentation<State, Trigger> super, out StateRepresentation<State, Trigger> sub);

            var falseConditions = new[] {
                new Tuple<Func<object[], bool>, string>(args => true, "1"),
                new Tuple<Func<object[], bool>, string>(args => false, "2")
            };
            var transitionGuard = new TransitionGuard(falseConditions);
            var transition = new TransitioningTriggerBehaviour<State, Trigger>(Trigger.X, State.C, transitionGuard);
            super.AddTriggerBehaviour(transition);

            var reslt= sub.TryFindHandler(Trigger.X, new object[0], out TriggerBehaviourResult<State, Trigger> result);

            Assert.False(reslt);
            Assert.False(sub.CanHandle(Trigger.X));
            Assert.False(super.CanHandle(Trigger.X));
            
        }
        [Fact]
        public void WhenTransitionExistSuperstateMetGuardConditions_CanBeFired()
        {
            CreateSuperSubstatePair(out StateRepresentation<State, Trigger> super, out StateRepresentation<State, Trigger> sub);

            var trueConditions = new[] {
                new Tuple<Func<object[], bool>, string>(args => true, "1"),
                new Tuple<Func<object[], bool>, string>(args => true, "2")
            };
            var transitionGuard = new TransitionGuard(trueConditions);
            var transition = new TransitioningTriggerBehaviour<State, Trigger>(Trigger.X, State.C, transitionGuard);

            super.AddTriggerBehaviour(transition);
            sub.TryFindHandler(Trigger.X, new object[0], out TriggerBehaviourResult<State, Trigger> result);

            Assert.True(sub.CanHandle(Trigger.X));
            Assert.True(super.CanHandle(Trigger.X));
            Assert.NotNull(result);     
            Assert.True(result?.Handler.GuardConditionsMet());
            Assert.False(result?.UnmetGuardConditions.Any());

        }

        void CreateSuperSubstatePair(out StateRepresentation<State, Trigger> super, out StateRepresentation<State, Trigger> sub)
        {
            super = CreateRepresentation(State.A);
            sub = CreateRepresentation(State.B);
            super.AddSubstate(sub);
            sub.Superstate = super;
        }

        StateRepresentation<State, Trigger> CreateRepresentation(State state)
        {
            return new StateRepresentation<State, Trigger>(state);
        }
    }
}

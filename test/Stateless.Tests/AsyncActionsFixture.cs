#if TASKS

using System;
using System.Threading.Tasks;

using Xunit;

namespace Stateless.Tests
{
    public class AsyncActionsFixture
    {
        [Fact]
        public void StateMutatorShouldBeCalledOnlyOnce()
        {
            var state = State.B;
            var count = 0;
            var sm = new AsyncStateMachine<State, Trigger>(() => state, (s) => { state = s; count++; });
            sm.Configure(State.B).Permit(Trigger.X, State.C);
            sm.FireAsync(Trigger.X);
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task CanFireAsyncEntryAction()
        {
            var sm = new AsyncStateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
              .Permit(Trigger.X, State.B);

            var test = "";
            sm.Configure(State.B)
              .OnEntry(() => Task.Run(() => test = "foo"));

            await sm.FireAsync(Trigger.X).ConfigureAwait(false);

            Assert.Equal("foo", test); // Should await action
            Assert.Equal(State.B, sm.State); // Should transition to destination state
        }

        [Fact]
        public async Task CanFireAsyncExitAction()
        {
            var sm = new AsyncStateMachine<State, Trigger>(State.A);

            var test = "";
            sm.Configure(State.A)
              .OnExit(() => Task.Run(() => test = "foo"))
              .Permit(Trigger.X, State.B);

            await sm.FireAsync(Trigger.X).ConfigureAwait(false);

            Assert.Equal("foo", test); // Should await action
            Assert.Equal(State.B, sm.State); // Should transition to destination state
        }

        [Fact]
        public async Task CanFireInternalAsyncAction()
        {
            var sm = new AsyncStateMachine<State, Trigger>(State.A);

            var test = "";
            sm.Configure(State.A)
              .InternalTransition(Trigger.X, () => Task.Run(() => test = "foo"));

            await sm.FireAsync(Trigger.X).ConfigureAwait(false);

            Assert.Equal("foo", test); // Should await action
        }

        [Fact]
        public async Task CanInvokeOnTransitionedAsyncAction()
        {
            var sm = new AsyncStateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
              .Permit(Trigger.X, State.B);

            var test = "";
            sm.OnTransitionedAsync(_ => Task.Run(() => test = "foo"));

            await sm.FireAsync(Trigger.X).ConfigureAwait(false);

            Assert.Equal("foo", test); // Should await action
        }

        [Fact]
        public async Task CanInvokeOnUnhandledTriggerAsyncAction()
        {
            var sm = new AsyncStateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
              .Permit(Trigger.X, State.B);

            var test = "";
            sm.OnUnhandledTriggerAsync((s, t, u) => Task.Run(() => test = "foo"));

            await sm.FireAsync(Trigger.Z).ConfigureAwait(false);

            Assert.Equal("foo", test); // Should await action
        }

        [Fact]
        public async Task WhenActivateAsync()
        {
            var sm = new AsyncStateMachine<State, Trigger>(State.A);

            var activated = false;
            sm.Configure(State.A)
              .OnActivate(() => Task.Run(() => activated = true));

            await sm.ActivateAsync().ConfigureAwait(false);

            Assert.Equal(true, activated); // Should await action
        }

        [Fact]
        public async Task WhenDeactivateAsync()
        {
            var sm = new AsyncStateMachine<State, Trigger>(State.A);

            var deactivated = false;
            sm.Configure(State.A)
              .OnDeactivate(() => Task.Run(() => deactivated = true));

            await sm.ActivateAsync().ConfigureAwait(false);
            await sm.DeactivateAsync().ConfigureAwait(false);

            Assert.Equal(true, deactivated); // Should await action
        }

        [Fact]
        public async void IfSelfTransitionPermited_ActionsFire_InSubstate_async()
        {
            var sm = new AsyncStateMachine<State, Trigger>(State.A);

            bool onEntryStateBfired = false;
            bool onExitStateBfired = false;
            bool onExitStateAfired = false;

            sm.Configure(State.B)
                .OnEntry(t => Task.Run(() => onEntryStateBfired = true))
                .PermitReentry(Trigger.X)
                .OnExit(t => Task.Run(() => onExitStateBfired = true));

            sm.Configure(State.A)
                .SubstateOf(State.B)
                .OnExit(t => Task.Run(() => onExitStateAfired = true));

            await sm.FireAsync(Trigger.X).ConfigureAwait(false);

            Assert.Equal(State.B, sm.State);
            Assert.True(onExitStateAfired);
            Assert.True(onExitStateBfired);
            Assert.True(onEntryStateBfired);
        }

        [Fact]
        public async void TransitionToSuperstateDoesNotExitSuperstate()
        {
            AsyncStateMachine<State, Trigger> sm = new AsyncStateMachine<State, Trigger>(State.B);

            bool superExit = false;
            bool superEntry = false;
            bool subExit = false;

            sm.Configure(State.A)
                .OnEntry(t => Task.Run(() => superEntry = true))
                .OnExit(t => Task.Run(() => superExit = true));

            sm.Configure(State.B)
                .SubstateOf(State.A)
                .Permit(Trigger.Y, State.A)
                .OnExit(t => Task.Run(() => subExit = true));

            await sm.FireAsync(Trigger.Y);

            Assert.True(subExit);
            Assert.False(superEntry);
            Assert.False(superExit);
        }

        [Fact]
        public async void IgnoredTriggerMustBeIgnoredAsync()
        {
            bool nullRefExcThrown = false;
            var stateMachine = new AsyncStateMachine<State, Trigger>(State.B);
            stateMachine.Configure(State.A)
                .Permit(Trigger.X, State.C);

            stateMachine.Configure(State.B)
                .SubstateOf(State.A)
                .Ignore(Trigger.X);

            try
            {
                // >>> The following statement should not throw a NullReferenceException
                await stateMachine.FireAsync(Trigger.X);
            }
            catch (NullReferenceException )
            {
                nullRefExcThrown = true;
            }

            Assert.False(nullRefExcThrown);
        }

        [Fact]
        public void VerifyNotEnterSuperstateWhenDoingInitialTransition()
        {
            var sm = new AsyncStateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
                .Permit(Trigger.X, State.B);

            sm.Configure(State.B)
                .InitialTransition(State.C)
                .OnEntry(() => sm.FireAsync(Trigger.Y))
                .Permit(Trigger.Y, State.D);

            sm.Configure(State.C)
                .SubstateOf(State.B)
                .Permit(Trigger.Y, State.D);

            sm.FireAsync(Trigger.X);

            Assert.Equal(State.D, sm.State);
        }
    }
}

#endif

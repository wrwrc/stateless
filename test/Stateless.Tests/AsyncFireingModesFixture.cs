#if TASKS
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;


namespace Stateless.Tests
{
    /// <summary>
    /// This test class verifies that the firing modes are working as expected
    /// </summary>
    public class AsyncFireingModesFixture
    {
        /// <summary>
        /// Check that the immediate fireing modes executes entry/exit out of order.
        /// </summary>
        [Fact]
        public void ImmediateEntryAProcessedBeforeEnterB()
        {
            var record = new List<string>();
            var sm = new AsyncStateMachine<State, Trigger>(State.A, FiringMode.Immediate);

            sm.Configure(State.A)
                .OnEntry(() =>
                {
                    record.Add("EnterA");
                    return Task.CompletedTask;
                })
                .Permit(Trigger.X, State.B)
                .OnExit(() => { record.Add("ExitA"); return Task.CompletedTask; });

            sm.Configure(State.B)
                .OnEntry(() =>
                {
                    record.Add("EnterB");
                    // Fire this before finishing processing the entry action
                    return sm.FireAsync(Trigger.Y);
                })
                .Permit(Trigger.Y, State.A)
                .OnExit(() => { record.Add("ExitB"); return Task.CompletedTask; });

            sm.FireAsync(Trigger.X);

            // Expected sequence of events: Exit A -> Exit B -> Enter A -> Enter B
            Assert.Equal("ExitA", record[0]);
            Assert.Equal("EnterB", record[1]);
            Assert.Equal("ExitB", record[2]);
            Assert.Equal("EnterA", record[3]);

        }

        /// <summary>
        /// Checks that queued fireing mode executes triggers in order
        /// </summary>
        [Fact]
        public void ImmediateEntryAProcessedBeforeEterB()
        {
            var record = new List<string>();
            var sm = new AsyncStateMachine<State, Trigger>(State.A, FiringMode.Queued);

            sm.Configure(State.A)
                .OnEntry(() =>
                {
                    record.Add("EnterA");
                    return Task.CompletedTask;
                })
                .Permit(Trigger.X, State.B)
                .OnExit(() => { record.Add("ExitA"); return Task.CompletedTask; });

            sm.Configure(State.B)
                .OnEntry(async () =>
                {
                    // Fire this before finishing processing the entry action
                    await sm.FireAsync(Trigger.Y);
                    record.Add("EnterB");
                })
                .Permit(Trigger.Y, State.A)
                .OnExit(() => { record.Add("ExitB"); return Task.CompletedTask; });

            sm.FireAsync(Trigger.X);

            // Expected sequence of events: Exit A -> Enter B -> Exit B -> Enter A
            Assert.Equal("ExitA", record[0]);
            Assert.Equal("EnterB", record[1]);
            Assert.Equal("ExitB", record[2]);
            Assert.Equal("EnterA", record[3]);
        }

        /// <summary>
        /// Check that the immediate fireing modes executes entry/exit out of order.
        /// </summary>
        [Fact]
        public void ImmediateFireingOnEntryEndsUpInCorrectState()
        {
            var record = new List<string>();
            var sm = new AsyncStateMachine<State, Trigger>(State.A, FiringMode.Immediate);

            sm.Configure(State.A)
                .OnEntry(() =>
                {
                    record.Add("EnterA");
                    return Task.CompletedTask;
                })
                .Permit(Trigger.X, State.B)
                .OnExit(() =>
                {
                    record.Add("ExitA");
                    return Task.CompletedTask;
                });

            sm.Configure(State.B)
                .OnEntry(() =>
                {
                    record.Add("EnterB");
                    // Fire this before finishing processing the entry action
                    return sm.FireAsync(Trigger.X);
                })
                .Permit(Trigger.X, State.C)
                .OnExit(() =>
                {
                    record.Add("ExitB");
                    return Task.CompletedTask;
                });

            sm.Configure(State.C)
                .OnEntry(() =>
                {
                    record.Add("EnterC");
                    return Task.CompletedTask;
                })
                .Permit(Trigger.X, State.A)
                .OnExit(() =>
                {
                    record.Add("ExitC");
                    return Task.CompletedTask;
                });

            sm.FireAsync(Trigger.X);

            // Expected sequence of events: Exit A -> Exit B -> Enter A -> Enter B
            Assert.Equal("ExitA", record[0]);
            Assert.Equal("EnterB", record[1]);
            Assert.Equal("ExitB", record[2]);
            Assert.Equal("EnterC", record[3]);

            Assert.Equal(State.C, sm.State);
        }

        /// <summary>
        /// Check that the immediate fireing modes executes entry/exit out of order.
        /// </summary>
        [Fact]
        public async Task ImmediateModeTransitionsAreInCorrectOrderWithAsyncDriving()
        {
            var record = new List<State>();
            var sm = new AsyncStateMachine<State, Trigger>(State.A, FiringMode.Immediate);

            sm.OnTransitionedAsync((t) =>
            {
                record.Add(t.Destination);
                return Task.CompletedTask;
            });

            sm.Configure(State.A)
                .Permit(Trigger.X, State.B);

            sm.Configure(State.B)
                .OnEntry(async () =>
                {
                    await sm.FireAsync(Trigger.Y).ConfigureAwait(false);
                })
                .Permit(Trigger.Y, State.C);

            sm.Configure(State.C)
                .OnEntry(async () =>
                {
                    await sm.FireAsync(Trigger.Z).ConfigureAwait(false);
                })
                .Permit(Trigger.Z, State.A);

            await sm.FireAsync(Trigger.X);

            Assert.Equal(new List<State>() { 
                State.B,
                State.C,
                State.A
            }, record);
        }

        [Fact]
        public async void EntersSubStateofSubstateAsyncOnEntryCountAndOrder()
        {
            var sm = new AsyncStateMachine<State, Trigger>(State.A);

            var onEntryCount = "";

            sm.Configure(State.A)
                .OnEntry(async () =>
                {
                    onEntryCount += "A";
                    await Task.Delay(10);
                })
                .Permit(Trigger.X, State.B);

            sm.Configure(State.B)
                .OnEntry(async () =>
                {
                    onEntryCount += "B";
                    await Task.Delay(10);
                })
                .InitialTransition(State.C);

            sm.Configure(State.C)
                .OnEntry(async () =>
                {
                    onEntryCount += "C";
                    await Task.Delay(10);
                })
                .InitialTransition(State.D)
                .SubstateOf(State.B);

            sm.Configure(State.D)
                .OnEntry(async () =>
                {
                    onEntryCount += "D";
                    await Task.Delay(10);
                })
                .SubstateOf(State.C);

            await sm.FireAsync(Trigger.X);

            Assert.Equal("BCD", onEntryCount);
        }
    }
}
#endif
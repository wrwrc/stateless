using System;
using Xunit;

namespace Stateless.Tests
{
    public class TriggerBehaviourFixture
    {
        [Fact]
        public void ExposesCorrectUnderlyingTrigger()
        {
            var transitioning = new TransitioningTriggerBehaviour<State, Trigger>(
                Trigger.X, State.C, null);

            Assert.Equal(Trigger.X, transitioning.Trigger);
        }

        protected bool False(params object[] args)
        {
            return false;
        }

        [Fact]
        public void WhenGuardConditionFalse_GuardConditionsMetIsFalse()
        {
            var transitioning = new TransitioningTriggerBehaviour<State, Trigger>(
                Trigger.X, State.C, new TransitionGuard(False));

            Assert.False(transitioning.GuardConditionsMet());
        }

        protected bool True(params object[] args)
        {
            return true;
        }

        [Fact]
        public void WhenGuardConditionTrue_GuardConditionsMetIsTrue()
        {
            var transitioning = new TransitioningTriggerBehaviour<State, Trigger>(
                Trigger.X, State.C, new TransitionGuard(True));

            Assert.True(transitioning.GuardConditionsMet());
        }

        [Fact]
        public void WhenOneOfMultipleGuardConditionsFalse_GuardConditionsMetIsFalse()
        {
            var falseGuard = new[] {
                new Tuple<Func<object[], bool>, string>(args => true, "1"),
                new Tuple<Func<object[], bool>, string>(args => true, "2")
            };

            var transitioning = new TransitioningTriggerBehaviour<State, Trigger>(
                Trigger.X, State.C, new TransitionGuard(falseGuard));

            Assert.True(transitioning.GuardConditionsMet());
        }

        [Fact]
        public void WhenAllMultipleGuardConditionsFalse_IsGuardConditionsMetIsFalse()
        {
            var falseGuard = new[] {
                new Tuple<Func<object[], bool>, string>(args => false, "1"),
                new Tuple<Func<object[], bool>, string>(args => false, "2")
            };

            var transitioning = new TransitioningTriggerBehaviour<State, Trigger>(
                Trigger.X, State.C, new TransitionGuard(falseGuard));

            Assert.False(transitioning.GuardConditionsMet());
        }

        [Fact]
        public void WhenAllGuardConditionsTrue_GuardConditionsMetIsTrue()
        {
            var trueGuard = new[] {
                new Tuple<Func<object[], bool>, string>(args => true, "1"),
                new Tuple<Func<object[], bool>, string>(args => true, "2")
            };

            var transitioning = new TransitioningTriggerBehaviour<State, Trigger>(
                Trigger.X, State.C, new TransitionGuard(trueGuard));

            Assert.True(transitioning.GuardConditionsMet());
        }
    }
}

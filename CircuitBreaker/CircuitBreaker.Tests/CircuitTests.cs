using System;

using FluentAssertions;

using IDL.Net.CircuitBreaker;

using NUnit.Framework;

namespace CircuitBreaker.Tests
{
    [TestFixture]
    public class CircuitTests
    {
        [Test]
        public void CheckThatExcludedExceptionsAreThrown()
        {
            var circuit = new Circuit<string>(() => { throw new ArgumentException(); }, 1)
            {
                ExcludedExceptions = new[] { typeof(NullReferenceException) }
            };

            var circuit1 = circuit;
            Action execution = () => circuit1.Execute();
            execution.ShouldNotThrow<ArgumentException>();
            circuit.State.Position.Should().Be(CircuitPosition.Open);

            circuit = new Circuit<string>(() => { throw new NullReferenceException(); }, 1)
            {
                ExcludedExceptions = new[] { typeof(NullReferenceException) }
            };

            execution = () => circuit.Execute();
            execution.ShouldThrow<NullReferenceException>();
        }

        [Test]
        public void CheckThatTheCircuitFailsUntilTheCircuitIsHalfOpenAgainThenClosesAgainOnSuccess()
        {
            var timeSpan = TimeSpan.FromSeconds(1);
            bool[] throwException = { true };
            var failSafe = DateTime.UtcNow.Add(timeSpan.Add(timeSpan));
            var circuit = new Circuit<string>(
                () =>
                {
                    if (throwException[0])
                    {
                        throw new Exception();
                    }

                    return "success";
                },
                1,
                timeSpan);

            circuit.Execute();
            circuit.State.Position.Should().Be(CircuitPosition.Open);

            while (DateTime.UtcNow < circuit.State.ResetTime)
            {
                Action action = () => circuit.Execute();
                action.ShouldThrow<CircuitOpenException>();

                if (DateTime.UtcNow > failSafe)
                {
                    throw new Exception("Test timeout");
                }
            }

            circuit.State.Position.Should().Be(CircuitPosition.HalfOpen);

            throwException[0] = false;
            circuit.Execute();

            circuit.State.Position.Should().Be(CircuitPosition.Closed);
        }

        [Test]
        public void CheckThatTheCircuitFailsUntilTheCircuitIsHalfOpenAgainThenOpensAgainOnFailure()
        {
            var timeSpan = TimeSpan.FromSeconds(1);
            var circuit = new Circuit<string>(() => { throw new Exception(); }, 1, timeSpan);
            var failSafe = DateTime.UtcNow.Add(timeSpan.Add(timeSpan));

            circuit.Execute();
            circuit.State.Position.Should().Be(CircuitPosition.Open);

            while (DateTime.UtcNow < circuit.State.ResetTime)
            {
                Action action = () => circuit.Execute();
                action.ShouldThrow<CircuitOpenException>();

                if (DateTime.UtcNow > failSafe)
                {
                    throw new Exception("Test timeout");
                }
            }

            circuit.State.Position.Should().Be(CircuitPosition.HalfOpen);
            circuit.Execute();
            circuit.State.Position.Should().Be(CircuitPosition.Open);
        }

        [Test]
        public void CheckThatTheCircuitIsSetToHalfOpenAfterATimeout()
        {
            var timeSpan = TimeSpan.FromSeconds(1);
            var circuit = new Circuit<string>(() => { throw new Exception(); }, 1, timeSpan);
            var failSafe = DateTime.UtcNow.Add(timeSpan.Add(timeSpan));

            circuit.State.Position.Should().Be(CircuitPosition.Closed);

            circuit.Execute();
            circuit.State.Position.Should().Be(CircuitPosition.Open);

            while (DateTime.UtcNow < circuit.State.ResetTime)
            {
                if (DateTime.UtcNow > failSafe)
                {
                    throw new Exception("Test timeout");
                }
            }

            circuit.State.Position.Should().Be(CircuitPosition.HalfOpen);
        }

        [Test]
        public void CheckThatTheThresholdIsResetAfterASuccessfulOperation()
        {
            var timeSpan = TimeSpan.FromSeconds(1);
            var failSafe = DateTime.UtcNow.Add(timeSpan.Add(timeSpan));
            bool[] throwException = { true };
            const int threshold = 2;
            var circuit = new Circuit<string>(
                () =>
                {
                    if (throwException[0])
                    {
                        throw new Exception();
                    }

                    return "success";
                },
                threshold,
                timeSpan);

            circuit.Execute();
            circuit.Execute();
            circuit.State.Position.Should().Be(CircuitPosition.Open);
            circuit.State.CurrentIteration.Should().BeGreaterThan(threshold);

            while (DateTime.UtcNow < circuit.State.ResetTime)
            {
                if (DateTime.UtcNow > failSafe)
                {
                    throw new Exception("Test timeout");
                }
            }

            throwException[0] = false;
            circuit.Execute();

            circuit.State.Position.Should().Be(CircuitPosition.Closed);
            circuit.State.CurrentIteration.Should().Be(1);
        }

        [Test]
        public void CircuitBreakerBreaksOnFailureAndSetsTheStateToOpenAfterTheThresholdIsReached()
        {
            var circuit = new Circuit<string>(() => { throw new Exception(); }, 2);

            circuit.Execute();
            circuit.State.Position.Should().Be(CircuitPosition.Closed);

            circuit.Execute();
            circuit.State.Position.Should().Be(CircuitPosition.Open);
        }
    }
}

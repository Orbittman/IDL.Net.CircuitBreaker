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
            var exception = new ArgumentException();
            var circuit = new Circuit<string>(() => { throw exception; }, 1)
            {
                ExcludedExceptions = new[] { typeof(NullReferenceException) }
            };

            var circuit1 = circuit;
            Action execution = () => circuit1.Execute();
            execution.ShouldThrow<ArgumentException>().Where(e => e == exception);
            circuit.State.Position.Should().Be(CircuitPosition.Open);

            circuit = new Circuit<string>(() => { throw new NullReferenceException(); }, 1)
            {
                ExcludedExceptions = new[] { typeof(NullReferenceException) }
            };

            execution = () => circuit.Execute();
            execution.ShouldThrow<NullReferenceException>();
        }

        [Test]
        public void CheckThatExcludedExceptionsAreIgnored()
        {
            var exception = new NullReferenceException();
            var circuit = new Circuit<string>(() => { throw exception; }, 1)
            {
                ExcludedExceptions = new[] { typeof(NullReferenceException) }
            };

            var circuit1 = circuit;
            Action execution = () => circuit1.Execute();
            execution.ShouldThrow<NullReferenceException>().Where(e => e == exception);
            circuit.State.Position.Should().Be(CircuitPosition.Closed);
        }

        [Test]
        public void CheckThatFilteredxceptionsAreIgnored()
        {
            const string message = "testMessage";
            var exception = new Exception(message);
            var circuit = new Circuit<string>(() => { throw exception; }, 1);
            circuit.ExceptionFilters.Add(e => e.Message == message);

            var circuit1 = circuit;
            Action execution = () => circuit1.Execute();
            execution.ShouldThrow<Exception>().Where(e => e == exception);
            circuit.State.Position.Should().Be(CircuitPosition.Closed);
        }

        [Test]
        public void CheckThatTheCircuitFailsUntilTheCircuitIsHalfOpenAgainThenClosesAgainOnSuccess()
        {
            var timeSpan = TimeSpan.FromSeconds(1);
            bool[] throwException = { true };
            var failSafe = DateTime.UtcNow.Add(timeSpan.Add(timeSpan));
            var exception = new Exception();
            var circuit = new Circuit<string>(
                () =>
                {
                    if (throwException[0])
                    {
                        throw exception;
                    }

                    return "success";
                },
                1,
                timeSpan);

            Action first = () => circuit.Execute();
            first.ShouldThrow<Exception>().Where(e => e == exception);
            circuit.State.Position.Should().Be(CircuitPosition.Open);

            while (DateTime.UtcNow < circuit.State.ResetTime)
            {
                Action action = () => circuit.Execute();
                action.ShouldThrow<CircuitOpenException>();

                failSafe.Should().BeAfter(DateTime.UtcNow, "otherwise the test has timed out");
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

            try
            {
                circuit.Execute();
            }
            catch
            {
                circuit.State.Position.Should().Be(CircuitPosition.Open);
            } 

            while (DateTime.UtcNow < circuit.State.ResetTime)
            {
                Action action = () => circuit.Execute();
                action.ShouldThrow<CircuitOpenException>();

                failSafe.Should().BeAfter(DateTime.UtcNow, "otherwise the test has timed out");
            }

            circuit.State.Position.Should().Be(CircuitPosition.HalfOpen);
            try
            {
                circuit.Execute();
            }
            catch
            {
                circuit.State.Position.Should().Be(CircuitPosition.Open);
            } 
        }

        [Test]
        public void CheckThatTheCircuitIsSetToHalfOpenAfterATimeout()
        {
            var timeSpan = TimeSpan.FromSeconds(1);
            var exception = new Exception();
            var circuit = new Circuit<string>(() => { throw exception; }, 1, timeSpan);
            var failSafe = DateTime.UtcNow.Add(timeSpan.Add(timeSpan));

            circuit.State.Position.Should().Be(CircuitPosition.Closed);

            Action first = () => circuit.Execute();
            first.ShouldThrow<Exception>().Where(e => e == exception);

            circuit.State.Position.Should().Be(CircuitPosition.Open);

            while (DateTime.UtcNow < circuit.State.ResetTime)
            {
                failSafe.Should().BeAfter(DateTime.UtcNow, "otherwise the test has timed out");
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
            var exception = new Exception();
            var circuit = new Circuit<string>(
                () =>
                {
                    if (throwException[0])
                    {
                        throw exception;
                    }

                    return "success";
                },
                threshold,
                timeSpan);

            Action first = () => circuit.Execute();
            Action second = () => circuit.Execute();
            first.ShouldThrow<Exception>().Where(e => e == exception);
            second.ShouldThrow<Exception>().Where(e => e == exception);

            circuit.State.Position.Should().Be(CircuitPosition.Open);
            circuit.State.CurrentIteration.Should().Be(threshold);

            while (DateTime.UtcNow < circuit.State.ResetTime)
            {
                failSafe.Should().BeAfter(DateTime.UtcNow, "otherwise the test has timed out");
            }

            throwException[0] = false;
            circuit.Execute();

            circuit.State.Position.Should().Be(CircuitPosition.Closed);
            circuit.State.CurrentIteration.Should().Be(0);
        }

        [Test]
        public void CircuitBreakerBreaksOnFailureAndSetsTheStateToOpenAfterTheThresholdIsReached()
        {
            var exception = new Exception();
            var circuit = new Circuit<string>(() => { throw exception; }, 2);

            Action first = () => circuit.Execute();
            first.ShouldThrow<Exception>().Where(e => e == exception);
            circuit.State.Position.Should().Be(CircuitPosition.Closed);

            Action second = () => circuit.Execute();
            second.ShouldThrow<Exception>().Where(e => e == exception);
            circuit.State.Position.Should().Be(CircuitPosition.Open);
        }
    }
}

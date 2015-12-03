using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IDL.Net.CircuitBreaker
{
    public class Circuit<TResult> : ICircuit<TResult>
    {
        private readonly int _threshold;

        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

        public Circuit(int threshold, TimeSpan? timeout = null, CircuitState state = null)
        {
            _threshold = threshold;
            if (timeout.HasValue)
            {
                _timeout = timeout.Value;
            }

            State = state ?? new CircuitState { Position = CircuitPosition.Closed };
            ExceptionFilters = new List<Func<Exception, bool>>();
        }

        public Type[] ExcludedExceptions { get; set; }

        public List<Func<Exception, bool>> ExceptionFilters { get; set; }

        public Action<string> Logger { private get; set; }

        public CircuitState State { get; private set; }

        public async Task<TResult> ExecuteAsync(Func<Task<TResult>> function)
        {
            AssertState(State.Position);

            try
            {
                var response = await function().ConfigureAwait(false);
                State.Reset();

                return response;
            }
            catch (AggregateException ex)
            {
                ex.Handle(
                    e =>
                    {
                        HandleException(e, State);
                        return false;
                    });

                throw;
            }
            catch (Exception ex)
            {
                HandleException(ex, State);
                throw;
            }
        }

        public TResult Execute(Func<TResult> function)
        {
            AssertState(State.Position);

            try
            {
                var response = function();
                State.Reset();

                return response;
            }
            catch (AggregateException ex)
            {
                ex.Handle(
                    e =>
                    {
                        HandleException(e, State);
                        return false;
                    });

                throw;
            }
            catch (Exception ex)
            {
                HandleException(ex, State);
                throw;
            }
        }

        private void AssertState(CircuitPosition position)
        {
            if (position == CircuitPosition.Open)
            {
                throw new CircuitOpenException();
            }
        }

        private void HandleException(Exception exception, CircuitState state)
        {
            if (ExceptionFilters.Any(filter => filter(exception)))
            {
                return;
            }

            if (ExcludedExceptions == null || !ExcludedExceptions.Contains(exception.GetType()))
            {
                state.ResetTime = DateTime.UtcNow.Add(_timeout);
                state.Increment();
                if (state.Position == CircuitPosition.HalfOpen || state.CurrentIteration >= _threshold)
                {
                    state.Position = CircuitPosition.Open;
                }

                if (Logger != null)
                {
                    Logger(exception.Message);
                }
            }
        }
    }
}

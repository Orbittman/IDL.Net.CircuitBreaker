using System;
using System.Linq;

namespace IDL.Net.CircuitBreaker
{
    public class Circuit<TResult> : ICircuit<TResult>
    {
        private readonly Func<TResult> _function;

        private readonly int _threshold;

        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

        public Circuit(Func<TResult> function, int threshold, TimeSpan? timeout = null)
        {
            _function = function;
            _threshold = threshold;
            if (timeout.HasValue)
            {
                _timeout = timeout.Value;
            }

            State = new CircuitState { Position = CircuitPosition.Closed };
        }

        public Type[] ExcludedExceptions { get; set; }

        public Action<string> Logger { private get; set; }

        public CircuitState State { get; private set; }

        public TResult Execute()
        {
            return Execute(State);
        }

        public TResult Execute(CircuitState state)
        {
            if (state.Position == CircuitPosition.Open)
            {
                throw new CircuitOpenException();
            }

            try
            {
                var response = _function();
                state.Reset();

                return response;
            }
            catch (AggregateException ex)
            {
                var handled = true;
                ex.Handle(
                    e =>
                    {
                        var response = HandleException(e, state);
                        if (!response)
                        {
                            handled = false;
                        }

                        return response;
                    });

                if (!handled)
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                if (!HandleException(ex, state))
                {
                    throw;
                }
            }

            return default(TResult);
        }

        private bool HandleException(Exception exception, CircuitState state)
        {
            if (ExcludedExceptions != null && ExcludedExceptions.Contains(exception.GetType()))
            {
                return false;
            }

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

            return true;
        }
    }
}

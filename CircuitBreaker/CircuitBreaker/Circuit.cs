using System;
using System.Collections.Generic;
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

        public Action<string> Logger { private get; set; }

        public CircuitState State { get; private set; }

        public Type[] ExcludedExceptions { get; set; } 

        public TResult Execute()
        {
            return Execute(State);
        }

        public TResult Execute(CircuitState state)
        {
            if (State.Position == CircuitPosition.Open)
            {
                throw new CircuitOpenException();
            }

            try
            {
                var response =  _function();
                state.CurrentIteration = 1;
                state.Position = CircuitPosition.Closed;

                return response;
            }
            catch(Exception ex)
            {
                if (ExcludedExceptions != null && ExcludedExceptions.Contains(ex.GetType()))
                {
                    throw;
                }

                state.ResetTime = DateTime.UtcNow.Add(_timeout);
                if (state.Position == CircuitPosition.HalfOpen || state.CurrentIteration == _threshold)
                {
                    state.Position = CircuitPosition.Open;
                }

                state.CurrentIteration++;

                if (Logger != null)
                {
                    Logger(ex.Message);
                }

                return default(TResult);
            }
        }
    }
}

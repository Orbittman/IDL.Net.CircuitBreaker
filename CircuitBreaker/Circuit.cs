using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IDL.Net.CircuitBreaker
{
    public class Circuit : ICircuit
    {
        protected readonly int Threshold;

        protected readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

        public Circuit(int threshold, TimeSpan? timeout, CircuitState state)
        {
            Threshold = threshold;
            if (timeout.HasValue)
            {
                Timeout = timeout.Value;
            }

            State = state ?? new CircuitState { Position = CircuitPosition.Closed };
            ExceptionFilters = new List<Func<Exception, bool>>();
        }

        public Type[] ExcludedExceptions { get; set; }

        public List<Func<Exception, bool>> ExceptionFilters { get; set; }

        public Action<string> Logger { private get; set; }

        public CircuitState State { get; private set; }

        public async Task ExecuteAsync(Action action)
        {
            AssertState(State.Position);

            try
            {
                await Task.Run(() => action).ConfigureAwait(false);
                State.Reset();
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

        public void Execute(Action action)
        {
            AssertState(State.Position);

            try
            {
                action();
                State.Reset();
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

        protected void HandleException(Exception exception, CircuitState state)
        {
            if (ExceptionFilters.Any(filter => filter(exception)))
            {
                return;
            }

            if (ExcludedExceptions == null || !ExcludedExceptions.Contains(exception.GetType()))
            {
                state.ResetTime = DateTime.UtcNow.Add(Timeout);
                state.Increment();
                if (state.Position == CircuitPosition.HalfOpen || state.CurrentIteration >= Threshold)
                {
                    state.Position = CircuitPosition.Open;
                }

                if (Logger != null)
                {
                    Logger.Invoke(exception.Message);
                }
            }
        }

        protected void AssertState(CircuitPosition position)
        {
            if (position == CircuitPosition.Open)
            {
                throw new CircuitOpenException();
            }
        }
    }

    public class Circuit<TResult> : Circuit, ICircuit<TResult>
    {
        public Circuit(int threshold, TimeSpan? timeout = null, CircuitState state = null)
            : base(threshold, timeout, state)
        {
        }

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
    }
}

using System;
using System.Threading;

namespace IDL.Net.CircuitBreaker
{
    public class CircuitState
    {
        private int _currentIteration;

        private CircuitPosition _position;

        public CircuitState()
        {
            Reset();
        }

        public void Reset()
        {
            CurrentIteration = 0;
            _position = CircuitPosition.Closed;
        }

        public int CurrentIteration
        {
            get
            {
                return _currentIteration;
            }
            internal set
            {
                _currentIteration = value;
            }
        }

        public CircuitPosition Position
        {
            get
            {
                if (_position == CircuitPosition.Open && DateTime.UtcNow > ResetTime)
                {
                    return CircuitPosition.HalfOpen;
                }

                return _position;
            }

            set
            {
                _position = value;
            }
        }

        public DateTime ResetTime { get; internal set; }

        public void Increment()
        {
            Interlocked.Increment(ref _currentIteration);
        }
    }
}

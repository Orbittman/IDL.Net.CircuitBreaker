using System;

namespace IDL.Net.CircuitBreaker
{
    public class CircuitState
    {
        private CircuitPosition _position;

        public CircuitState()
        {
            CurrentIteration = 1;
        }

        public int CurrentIteration { get; internal set; }

        public DateTime ResetTime { get; internal set; }

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

            set { _position = value; }
        }
    }
}

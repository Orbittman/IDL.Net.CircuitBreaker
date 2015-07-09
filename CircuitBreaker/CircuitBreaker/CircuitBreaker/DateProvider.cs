using System;

namespace IDL.Net.CircuitBreaker
{
    public class DateProvider : IDateProvider
    {
        public DateTime GetDate()
        {
            return DateTime.UtcNow;
        }
    }

    public interface IDateProvider
    {
        DateTime GetDate();
    }
}

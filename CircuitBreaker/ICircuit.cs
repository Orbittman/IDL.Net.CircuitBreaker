using System;
using System.Threading.Tasks;

namespace IDL.Net.CircuitBreaker
{
    public interface ICircuit<TResult>
    {
        TResult Execute(CircuitState state, Func<TResult> function);

        Task<TResult> ExecuteAsync(CircuitState state, Func<Task<TResult>> function);
    }
}

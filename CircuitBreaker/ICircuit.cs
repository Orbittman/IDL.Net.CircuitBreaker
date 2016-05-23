using System;
using System.Threading.Tasks;

namespace IDL.Net.CircuitBreaker
{
    public interface ICircuit<TResult>
    {
        TResult Execute(Func<TResult> function);

        Task<TResult> ExecuteAsync(Func<Task<TResult>> function);
    }

    public interface ICircuit
    {
        void Execute(Action action);

        Task ExecuteAsync(Action action);
    }
}

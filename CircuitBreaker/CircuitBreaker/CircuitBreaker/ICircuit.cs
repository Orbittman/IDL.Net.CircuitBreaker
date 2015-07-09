namespace IDL.Net.CircuitBreaker
{
    public interface ICircuit<out TResult>
    {
        TResult Execute();
    }
}

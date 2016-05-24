[![idl-public MyGet Build Status](https://www.myget.org/BuildSource/Badge/idl-public?identifier=cfa528a1-c980-4463-ad89-f0b12c9a39bb)](https://www.myget.org/)
# IDL.Net.CircuitBreaker
This is a simple circuit breaker library with the ability to maintain the state outside of the circuit class itself. This allows it to be used in an environment where multiple typed responses are required but using a single circuit state for all of them. 

We needed to not have a timer that would reset the circuit but have that based on a timestamp. This way a new circuit could be interrogated at any point and evaluate it's state based on the CircuitState object that was passed to the operation excecution.

The circuit state maintains the position of the circuit Open | Half open | Closed, the current failure count and a time that the circuit is reset. 

````csharp
var state = new CircuitState();
var circuit = new Circuit<TResponse>(2, TimeSpan.FromSeconds(5), state);

for(int i = 0; i < 6; i++)
{
   try
   {
      circuit.Execute(() => { throw new Exception());
   }
   catch(CircuitOpenException)
   {
      Console.WriteLine("The circuit is now open");
   }
   catch(Exception)
   {
      Console.WriteLine("The circuit is closed");
   }
}
````
This will output:  
The circuit is closed  
The circuit is closed
The circuit is now open  

After 5 seconds the circuit will become 'half open' and allow one more request before entering the open state again. If the circuit operation is successful the circuit will close and the failure count reset.
````c#
Thread.Sleep(5000);
Console.WriteLine(state.State);
// outputs "HalfOpen"

circuit = new Circuit<string>(2, TimeSpan.FromSeconds(5), state);
Console.WriteLine(circuit.Execute(() => "Test passed"));
// outputs "Test passed"

Console.WriteLine(state.State);
// outputs "Closed"
````

For asynchronous operations use
````c#
var state = new CircuitState();
var circuit = new Circuit<TResponse>(2, TimeSpan.FromSeconds(5), state);
var asyncResponse = circuit.ExecuteAsync(() => Task.Run(() => {}))
````

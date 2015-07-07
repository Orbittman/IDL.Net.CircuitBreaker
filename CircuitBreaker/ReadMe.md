# IDL.CircuitBreaker
This is a simple circuit breaker library with the ability to maintain the state outside of the circuit class itself. This allows it to be used in an environment where multiple typed responses are required but using a single circuit state for all of them. 

The circuit state maintains the position of the circuit Open | Half open | Closed, the current failure count and a time that the circuit is reset. 

````c#
var circuit = new Circuit<TResponse>(() => { throw new Exception();  }, 5, TimeSpan.FromSeconds(5));
var state = new CircuitState();

for(int i = 0; i < 6; i++){
  try{
    circuit.Execute(state);
  }catch(CircuitOpenException){
    Console.WriteLine("The circuit is now open");
  }catch(Exception){
    Console.WriteLine("The circuit is closed");
  }
}
````
This will output:  
The circuit is closed  
The circuit is closed  
The circuit is closed  
The circuit is closed  
The circuit is closed  
The circuit is now open  

After 5 seconds the circuit will become 'half open' and allow one more request before entering the open state again. If the circuit operation is successful the circuit will close and the failure count reset.

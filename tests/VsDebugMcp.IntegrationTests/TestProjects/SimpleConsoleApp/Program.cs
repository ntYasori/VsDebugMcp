namespace SimpleConsoleApp;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Starting test application...");

        // Simple variables for inspection
        int counter = 0;
        string message = "Hello, Debugger!";
        var numbers = new List<int> { 1, 2, 3, 4, 5 };

        // Loop for stepping and breakpoint testing
        for (int i = 0; i < numbers.Count; i++)
        {
            counter += numbers[i];
            Console.WriteLine($"Running iteration {i}: sum = {counter}");
        }

        // Method call for step-into testing
        var result = Calculate(counter, 2);
        Console.WriteLine($"Result: {result}");

        // Exception handling for debugging
        try
        {
            RiskyOperation(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Caught: {ex.Message}");
        }

        Console.WriteLine($"Final message: {message}, counter: {counter}");
    }

    static double Calculate(int value, int divisor)
    {
        // Good place to set breakpoint and inspect locals
        var intermediate = value * 3.14;
        return intermediate / divisor;
    }

    static void RiskyOperation(double value)
    {
        if (value > 100)
            throw new InvalidOperationException($"Value {value} is too large!");
        Console.WriteLine($"Safe value: {value}");
    }
}

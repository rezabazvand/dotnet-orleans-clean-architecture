namespace SampleOrleans.Domain.Primitives;

public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}

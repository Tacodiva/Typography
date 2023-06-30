
namespace Typography;

public class TypographyException : Exception
{
    public TypographyException() { }
    public TypographyException(string message) : base(message) { }
    public TypographyException(string message, Exception inner) : base(message, inner) { }
}
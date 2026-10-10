namespace LifeTrack.Application.Common;

public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message) { }
}

namespace stockmind.Commons.Errors;

public interface IResponseCode
{
    string Code { get; }
    string MessageTemplate { get; }
}

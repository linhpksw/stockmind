namespace stockmind.Commons.Responses;

public interface IBaseResponseModel
{
    string Code { get; }
    string Message { get; }
}

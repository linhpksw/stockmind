using stockmind.Commons.Constants;

namespace stockmind.Commons.Responses;

public class ResponseModel<T> : IBaseResponseModel
{
    public ResponseModel(T? data)
        : this(AppConstants.SuccessCode, AppConstants.SuccessMessage, data)
    {
    }

    public ResponseModel(string code, string message)
        : this(code, message, default)
    {
    }

    public ResponseModel(string code, string message, T? data)
    {
        Code = code;
        Message = message;
        Data = data;
    }

    public string Code { get; }

    public string Message { get; }

    public T? Data { get; }
}

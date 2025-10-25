using System;
using System.Collections.Generic;

namespace stockmind.Commons.Responses;

public class ErrorResponseModel : IBaseResponseModel
{
    public ErrorResponseModel(string code, string message)
        : this(code, message, Array.Empty<string>())
    {
    }

    public ErrorResponseModel(string code, string message, IEnumerable<string>? errors)
    {
        Code = code;
        Message = message;
        Errors = errors switch
        {
            null => Array.Empty<string>(),
            IReadOnlyCollection<string> readOnly => readOnly,
            _ => new List<string>(errors).AsReadOnly()
        };
    }

    public string Code { get; }

    public string Message { get; }

    public IReadOnlyCollection<string> Errors { get; }
}

using System.Collections.Generic;
using System.Linq;
using stockmind.Commons.Constants;

namespace stockmind.Commons.Responses;

public class ListResponseModel<T> : IBaseResponseModel
{
    public ListResponseModel(IEnumerable<T>? data = null)
    {
        var items = data?.ToList() ?? new List<T>();
        Code = AppConstants.SuccessCode;
        Message = AppConstants.SuccessMessage;
        Data = items.AsReadOnly();
        Total = items.Count;
    }

    public string Code { get; }

    public string Message { get; }

    public IReadOnlyList<T> Data { get; }

    public int Total { get; }
}

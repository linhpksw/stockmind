using System.Collections.Generic;
using System.Linq;
using stockmind.Commons.Constants;

namespace stockmind.Commons.Responses;

public class PageResponseModel<T> : IBaseResponseModel
{
    public PageResponseModel(int pageSize, int pageNum, int total, IEnumerable<T>? data)
    {
        PageSize = pageSize;
        PageNum = pageNum;
        Total = total;

        var items = data?.ToList() ?? new List<T>();
        Data = items.AsReadOnly();

        Code = AppConstants.SuccessCode;
        Message = AppConstants.SuccessMessage;
    }

    public string Code { get; }

    public string Message { get; }

    public int PageSize { get; }

    public int PageNum { get; }

    public int Total { get; }

    public IReadOnlyList<T> Data { get; }
}

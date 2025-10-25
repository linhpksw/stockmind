using System.Collections.Generic;
using stockmind.Commons.Errors;

namespace stockmind.Commons.Exceptions;

public class BizDataAlreadyExistsException : BizException
{
    public BizDataAlreadyExistsException(IResponseCode error)
        : base(error)
    {
    }

    public BizDataAlreadyExistsException(IResponseCode error, IEnumerable<string> parameters)
        : base(error, parameters)
    {
    }
}

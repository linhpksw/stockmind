using System.Collections.Generic;
using stockmind.Commons.Errors;

namespace stockmind.Commons.Exceptions;

public class BizAuthorizationException : BizException
{
    public BizAuthorizationException(IResponseCode error)
        : base(error)
    {
    }

    public BizAuthorizationException(IResponseCode error, IEnumerable<string> parameters)
        : base(error, parameters)
    {
    }
}

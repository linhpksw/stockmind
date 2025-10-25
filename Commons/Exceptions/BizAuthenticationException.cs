using System.Collections.Generic;
using stockmind.Commons.Errors;

namespace stockmind.Commons.Exceptions;

public class BizAuthenticationException : BizException
{
    public BizAuthenticationException(IResponseCode error)
        : base(error)
    {
    }

    public BizAuthenticationException(IResponseCode error, IEnumerable<string> parameters)
        : base(error, parameters)
    {
    }
}

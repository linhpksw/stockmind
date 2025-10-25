using System.Collections.Generic;
using stockmind.Commons.Errors;

namespace stockmind.Commons.Exceptions;

public class BizNotFoundException : BizException
{
    public BizNotFoundException(IResponseCode error)
        : base(error)
    {
    }

    public BizNotFoundException(IResponseCode error, IEnumerable<string> parameters)
        : base(error, parameters)
    {
    }
}

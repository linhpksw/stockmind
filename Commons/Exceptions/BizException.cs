using System;
using System.Collections.Generic;
using stockmind.Commons.Errors;

namespace stockmind.Commons.Exceptions;

public class BizException : Exception
{
    public BizException(IResponseCode error)
        : this(error, Array.Empty<string>())
    {
    }

    public BizException(IResponseCode error, IEnumerable<string> parameters)
        : base(error?.MessageTemplate ?? throw new ArgumentNullException(nameof(error)))
    {
        Error = error;
        Params = parameters switch
        {
            null => Array.Empty<string>(),
            string[] array => Array.AsReadOnly(array),
            IReadOnlyCollection<string> readOnly => readOnly,
            _ => new List<string>(parameters).AsReadOnly()
        };
    }

    public IResponseCode Error { get; }

    public IReadOnlyCollection<string> Params { get; }
}

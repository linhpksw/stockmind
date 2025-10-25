namespace stockmind.Commons.Errors;

public static class ErrorCode4xx
{
    public static readonly IResponseCode InvalidInput = new ResponseCode("400000", "Invalid input: {0}");
    public static readonly IResponseCode Unauthorized = new ResponseCode("401000", "Unauthorized");
    public static readonly IResponseCode Forbidden = new ResponseCode("403000", "Forbidden");
    public static readonly IResponseCode NotFound = new ResponseCode("404000", "Resource not found: {0}");
    public static readonly IResponseCode DataAlreadyExists = new ResponseCode("409000", "Resource already exists: {0}");
    public static readonly IResponseCode MissingRequiredParameter = new ResponseCode("400100", "Missing required parameter: {0}");
    public static readonly IResponseCode FileTooLarge = new ResponseCode("400200", "Uploaded file is too large");
}

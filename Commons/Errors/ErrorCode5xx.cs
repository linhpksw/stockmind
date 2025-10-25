namespace stockmind.Commons.Errors;

public static class ErrorCode5xx
{
    public static readonly IResponseCode InternalServerError = new ResponseCode("500000", "Internal server error");
}

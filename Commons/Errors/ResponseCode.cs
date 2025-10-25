namespace stockmind.Commons.Errors;

public sealed record ResponseCode(string Code, string MessageTemplate) : IResponseCode;

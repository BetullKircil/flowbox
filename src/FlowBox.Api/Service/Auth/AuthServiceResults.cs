namespace FlowBox.Api.Service.Auth;

public abstract record RegisterResult
{
    public sealed record Success(Guid UserId, string Email, string Role) : RegisterResult;
    public sealed record EmailAlreadyRegistered : RegisterResult;
}

public abstract record LoginResult
{
    public sealed record Success(string Token, DateTime ExpiresAt, string Role) : LoginResult;
    public sealed record InvalidCredentials : LoginResult;
}

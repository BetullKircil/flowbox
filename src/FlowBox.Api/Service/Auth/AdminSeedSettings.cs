namespace FlowBox.Api.Service.Auth;

public class AdminSeedSettings
{
    public const string SectionName = "AdminSeed";

    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

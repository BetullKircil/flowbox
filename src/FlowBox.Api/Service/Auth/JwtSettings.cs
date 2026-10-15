namespace FlowBox.Api.Service.Auth;

/// <summary>
/// appsettings.json'daki "Jwt" bölümünden bağlanır. Secret, development için
/// appsettings.Development.json'da; production'da ortam değişkeninden
/// (Jwt__Secret) ya da bir secret manager'dan gelmeli — asla repoya commit'lenmemeli.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;
}

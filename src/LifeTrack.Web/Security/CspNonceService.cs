namespace LifeTrack.Web.Security;

public sealed class CspNonceService : ICspNonceService
{
    public string Nonce { get; } = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
}

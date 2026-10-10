namespace LifeTrack.Web.ViewModels;

public sealed record ErrorViewModel(string RequestId, string? Message)
{
    public const string DefaultMessage = "Ha ocurrido un error inesperado. Por favor, inténtelo de nuevo más tarde.";
}

using System.ComponentModel.DataAnnotations;

namespace LifeTrack.Web.ViewModels.Transactions;

public sealed record SetBudgetInputModel
{
    public const int MinYear = 2000;
    public const int MaxYear = 2100;
    public const int MinMonth = 1;
    public const int MaxMonth = 12;
    public const double MinLimit = 0.01;
    public const double MaxLimit = 1000000000;

    [Required(ErrorMessage = "La categoría es obligatoria.")]
    public Guid? CategoryId { get; init; }

    [Range(MinYear, MaxYear, ErrorMessage = "El año no es válido.")]
    public int Year { get; init; }

    [Range(MinMonth, MaxMonth, ErrorMessage = "El mes no es válido.")]
    public int Month { get; init; }

    [Required(ErrorMessage = "El límite es obligatorio.")]
    [Range(MinLimit, MaxLimit, ErrorMessage = "El límite debe ser mayor que 0.")]
    public decimal? Limit { get; init; }
}

using System.ComponentModel.DataAnnotations;

namespace LifeTrack.Web.ViewModels.Transactions;

public sealed record SetBudgetInputModel
{
    [Required(ErrorMessage = "La categoría es obligatoria.")]
    public Guid? CategoryId { get; init; }

    [Range(2000, 2100, ErrorMessage = "El año no es válido.")]
    public int Year { get; init; }

    [Range(1, 12, ErrorMessage = "El mes no es válido.")]
    public int Month { get; init; }

    [Required(ErrorMessage = "El límite es obligatorio.")]
    [Range(0.01, 1000000000, ErrorMessage = "El límite debe ser mayor que 0.")]
    public decimal? Limit { get; init; }
}

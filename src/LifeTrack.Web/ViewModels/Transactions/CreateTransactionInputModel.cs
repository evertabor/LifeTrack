using System.ComponentModel.DataAnnotations;

namespace LifeTrack.Web.ViewModels.Transactions;

public sealed record CreateTransactionInputModel
{
    public const decimal MinAmount = 0.01m;
    public const decimal MaxAmount = 1000000000m;
    public const int DescriptionMaxLength = 200;

    [Required(ErrorMessage = "La categoría es obligatoria.")]
    public Guid? CategoryId { get; init; }

    [Required(ErrorMessage = "El tipo es obligatorio.")]
    public bool? Type { get; init; }

    [Required(ErrorMessage = "El importe es obligatorio.")]
    [AmountRange(ErrorMessage = "El importe debe ser mayor que 0 y no superar 1.000.000.000.")]
    public decimal? Amount { get; init; }

    [Required(ErrorMessage = "La fecha es obligatoria.")]
    [DataType(DataType.Date)]
    public DateOnly? Date { get; init; }

    [StringLength(DescriptionMaxLength, ErrorMessage = "La descripción no puede superar los 200 caracteres.")]
    public string? Description { get; init; }
}

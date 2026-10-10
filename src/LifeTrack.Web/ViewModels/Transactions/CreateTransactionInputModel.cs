using System.ComponentModel.DataAnnotations;

namespace LifeTrack.Web.ViewModels.Transactions;

public sealed record CreateTransactionInputModel
{
    [Required(ErrorMessage = "La categoría es obligatoria.")]
    public Guid? CategoryId { get; init; }

    public bool Type { get; init; }

    [Required(ErrorMessage = "El importe es obligatorio.")]
    [Range(0.01, 1000000000, ErrorMessage = "El importe debe ser mayor que 0.")]
    public decimal? Amount { get; init; }

    [Required(ErrorMessage = "La fecha es obligatoria.")]
    [DataType(DataType.Date)]
    public DateOnly? Date { get; init; }

    [StringLength(200, ErrorMessage = "La descripción no puede superar los 200 caracteres.")]
    public string? Description { get; init; }
}

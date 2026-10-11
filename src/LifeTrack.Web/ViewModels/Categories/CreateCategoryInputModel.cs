using System.ComponentModel.DataAnnotations;

namespace LifeTrack.Web.ViewModels.Categories;

public sealed record CreateCategoryInputModel
{
    public const int NameMaxLength = 50;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(NameMaxLength, ErrorMessage = "El nombre no puede superar los 50 caracteres.")]
    public string Name { get; init; } = string.Empty;
}

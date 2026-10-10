using System.ComponentModel.DataAnnotations;

namespace LifeTrack.Web.ViewModels.Categories;

public sealed record CreateCategoryInputModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(50, ErrorMessage = "El nombre no puede superar los 50 caracteres.")]
    public string Name { get; init; } = string.Empty;
}

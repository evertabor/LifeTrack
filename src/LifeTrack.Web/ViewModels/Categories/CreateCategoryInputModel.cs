using System.ComponentModel.DataAnnotations;

namespace LifeTrack.Web.ViewModels.Categories;

public sealed record CreateCategoryInputModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Name { get; init; } = string.Empty;
}

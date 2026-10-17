using LifeTrack.Application.Finance.Commands.ListCategories;

namespace LifeTrack.Web.ViewModels.Categories;

public sealed record CategoryFormViewModel
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string TypeLabel { get; init; } = string.Empty;
    public string NewName { get; init; } = string.Empty;
    public string EditLabel { get; init; } = string.Empty;
    public string DeleteLabel { get; init; } = string.Empty;
    public string EditModalId { get; init; } = string.Empty;
    public string EditModalTitleId { get; init; } = string.Empty;
    public string EditHelpId { get; init; } = string.Empty;
    public string DeleteModalId { get; init; } = string.Empty;
    public string DeleteModalTitleId { get; init; } = string.Empty;
    public string DeleteConfirmMessage { get; init; } = string.Empty;

    public static CategoryFormViewModel From(CategorySummary summary) =>
        new()
        {
            Id = summary.Id,
            Name = summary.Name,
            TypeLabel = summary.TypeLabel,
            NewName = summary.Name,
            EditLabel = $"Editar {summary.Name}",
            DeleteLabel = $"Eliminar {summary.Name}",
            EditModalId = $"category-edit-modal-{summary.Id:D}",
            EditModalTitleId = $"category-edit-modal-title-{summary.Id:D}",
            EditHelpId = $"category-edit-modal-help-{summary.Id:D}",
            DeleteModalId = $"category-delete-modal-{summary.Id:D}",
            DeleteModalTitleId = $"category-delete-modal-title-{summary.Id:D}",
            DeleteConfirmMessage = $"¿Eliminar «{summary.Name}»? Esta acción no se puede deshacer."
        };
}

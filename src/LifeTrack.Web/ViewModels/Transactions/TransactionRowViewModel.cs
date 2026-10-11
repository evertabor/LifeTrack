using System.Globalization;
using LifeTrack.Application.Finance.Commands.ListMonthlyTransactions;

namespace LifeTrack.Web.ViewModels.Transactions;

public sealed record TransactionRowViewModel
{
    private static readonly CultureInfo Spanish = new("es-ES");

    public Guid Id { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string TypeLabel { get; init; } = string.Empty;
    public string AmountText { get; init; } = string.Empty;
    public string AmountCssClass { get; init; } = string.Empty;
    public string DateText { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsIncome { get; init; }
    public string EditLabel { get; init; } = string.Empty;
    public string DeleteLabel { get; init; } = string.Empty;
    public string DeleteModalId { get; init; } = string.Empty;
    public string DeleteModalTitleId { get; init; } = string.Empty;
    public string DeleteConfirmMessage { get; init; } = string.Empty;

    public static TransactionRowViewModel From(TransactionSummary summary) =>
        new()
        {
            Id = summary.Id,
            CategoryName = summary.CategoryName,
            TypeLabel = summary.TypeLabel,
            AmountText = summary.IsIncome
                ? $"+{summary.Amount.ToString("N2", Spanish)}"
                : $"−{summary.Amount.ToString("N2", Spanish)}",
            AmountCssClass = summary.IsIncome ? "app-amount app-amount--income" : "app-amount app-amount--expense",
            DateText = summary.Date.ToString("dd/MM/yyyy"),
            Description = summary.Description,
            IsIncome = summary.IsIncome,
            EditLabel = "Editar movimiento",
            DeleteLabel = "Eliminar movimiento",
            DeleteModalId = $"transaction-delete-modal-{summary.Id:D}",
            DeleteModalTitleId = $"transaction-delete-modal-title-{summary.Id:D}",
            DeleteConfirmMessage = "¿Eliminar este movimiento? Esta acción no se puede deshacer."
        };
}

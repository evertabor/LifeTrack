using System.Globalization;
using LifeTrack.Application.Finance.Commands.ListCategories;
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
    public EditTransactionInputModel Input { get; init; } = new();
    public IReadOnlyList<TransactionCategoryOption> CategoryOptions { get; init; } = [];
    public string EditLabel { get; init; } = string.Empty;
    public string EditModalId { get; init; } = string.Empty;
    public string EditModalTitleId { get; init; } = string.Empty;
    public string EditAmountHelpId { get; init; } = string.Empty;
    public string DeleteLabel { get; init; } = string.Empty;
    public string DeleteModalId { get; init; } = string.Empty;
    public string DeleteModalTitleId { get; init; } = string.Empty;
    public string DeleteConfirmMessage { get; init; } = string.Empty;
    public string MinEditDateText { get; init; } = string.Empty;
    public string MaxEditDateText { get; init; } = string.Empty;

    public static TransactionRowViewModel From(
        TransactionSummary summary,
        IReadOnlyList<CategorySummary> categories,
        DateOnly? today = null)
    {
        var monthStart = new DateOnly(summary.Date.Year, summary.Date.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var ceiling = today ?? summary.Date;
        var maxDay = monthEnd <= ceiling ? monthEnd : ceiling;
        return new()
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
            Input = new EditTransactionInputModel
            {
                CategoryId = summary.CategoryId,
                Type = summary.IsIncome,
                Amount = summary.Amount,
                Date = summary.Date,
                Description = summary.Description
            },
            CategoryOptions = categories
                .Select(c => TransactionCategoryOption.From(c, summary.CategoryId))
                .ToList(),
            EditLabel = "Editar movimiento",
            EditModalId = $"transaction-edit-modal-{summary.Id:D}",
            EditModalTitleId = $"transaction-edit-modal-title-{summary.Id:D}",
            EditAmountHelpId = $"transaction-edit-amount-help-{summary.Id:D}",
            DeleteLabel = "Eliminar movimiento",
            DeleteModalId = $"transaction-delete-modal-{summary.Id:D}",
            DeleteModalTitleId = $"transaction-delete-modal-title-{summary.Id:D}",
            DeleteConfirmMessage = "¿Eliminar este movimiento? Esta acción no se puede deshacer.",
            MinEditDateText = monthStart.ToString("yyyy-MM-dd"),
            MaxEditDateText = maxDay.ToString("yyyy-MM-dd")
        };
    }
}

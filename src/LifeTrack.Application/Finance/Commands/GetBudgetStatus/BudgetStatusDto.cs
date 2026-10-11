namespace LifeTrack.Application.Finance.Commands.GetBudgetStatus;

public sealed record BudgetStatusDto(
    Guid CategoryId,
    string CategoryName,
    decimal Limit,
    decimal Spent,
    decimal Percent,
    string State);

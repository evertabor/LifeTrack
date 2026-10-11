using LifeTrack.Application.Finance.Commands.ListMonthlyTransactions;
using LifeTrack.Web.ViewModels.Transactions;

namespace LifeTrack.Integration.Tests;

public class TransactionListViewModelTests
{
    [Fact]
    public void From_SelectedMonth_BuildsTwelveSpanishOptions()
    {
        var result = new MonthlyTransactionsResult(2026, 10, 0m, 0m, [], []);

        var viewModel = TransactionListViewModel.From(result, [], [], new TransactionListOptions(null));

        Assert.Equal(Enumerable.Range(1, 12), viewModel.MonthOptions.Select(o => o.Month));
        Assert.Equal("Octubre", viewModel.MonthOptions.Single(o => o.IsSelected).Name);
    }
}

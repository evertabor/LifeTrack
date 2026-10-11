using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LifeTrack.Web.Filters;

public sealed class ActiveSectionFilter : IActionFilter
{
    public const string ViewDataKey = "ActiveSection";

    private static readonly Dictionary<string, string> SectionByController = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Budgets"] = "Transactions",
        ["TransactionImports"] = "Transactions",
    };

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.Controller is Controller controller)
        {
            var name = context.RouteData.Values["controller"] as string;
            controller.ViewData[ViewDataKey] = name is not null && SectionByController.TryGetValue(name, out var section)
                ? section
                : name;
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LifeTrack.Web.Filters;

public sealed class ActiveSectionFilter : IActionFilter
{
    public const string ViewDataKey = "ActiveSection";

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.Controller is Controller controller)
            controller.ViewData[ViewDataKey] = context.RouteData.Values["controller"] as string;
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}

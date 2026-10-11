using LifeTrack.Web.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace LifeTrack.Integration.Tests;

public class ActiveSectionFilterTests
{
    private sealed class StubController : Controller
    {
    }

    [Fact]
    public void OnActionExecuting_ControllerRoute_SetsActiveSection()
    {
        var controller = new StubController();
        var context = CreateContext(controller, "Transactions");

        new ActiveSectionFilter().OnActionExecuting(context);

        Assert.Equal("Transactions", controller.ViewData[ActiveSectionFilter.ViewDataKey]);
    }

    [Fact]
    public void OnActionExecuting_MissingControllerRouteValue_SetsNull()
    {
        var controller = new StubController();
        var context = CreateContext(controller, null);

        new ActiveSectionFilter().OnActionExecuting(context);

        Assert.Null(controller.ViewData[ActiveSectionFilter.ViewDataKey]);
    }

    [Fact]
    public void OnActionExecuting_NonController_DoesNotThrow()
    {
        var context = CreateContext(new object(), "Transactions");

        new ActiveSectionFilter().OnActionExecuting(context);
    }

    private static ActionExecutingContext CreateContext(object controller, string? controllerName)
    {
        var routeData = new RouteData();
        if (controllerName is not null)
            routeData.Values["controller"] = controllerName;
        var actionContext = new ActionContext(new DefaultHttpContext(), routeData, new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
        return new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), controller);
    }
}

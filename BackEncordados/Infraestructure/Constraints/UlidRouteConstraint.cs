using Microsoft.AspNetCore.Routing;

namespace BackEncordados.Infraestructure.Constraints;

public class UlidRouteConstraint : IRouteConstraint
{
    public bool Match(
        HttpContext? httpContext,
        IRouter? route,
        string routeKey,
        RouteValueDictionary values,
        RouteDirection routeDirection)
    {
        if (!values.TryGetValue(routeKey, out var value) || value is not string stringValue)
            return false;

        return Ulid.TryParse(stringValue, out _);
    }
}
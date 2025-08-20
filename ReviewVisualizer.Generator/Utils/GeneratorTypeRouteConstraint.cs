namespace ReviewVisualizer.Generator.Utils
{
    public class GeneratorTypeRouteConstraint<TEnum> : IRouteConstraint where TEnum : struct, Enum
    {
        public bool Match(HttpContext? httpContext,
                          IRouter? route,
                          string routeKey,
                          RouteValueDictionary values,
                          RouteDirection routeDirection)
        {
            if (values.TryGetValue(routeKey, out var value) && value != null)
            {
                return Enum.TryParse<TEnum>(value.ToString(), true, out _);
            }
            return false;
        }
    }
}

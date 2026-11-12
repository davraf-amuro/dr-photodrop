namespace dr_photodrop.api.Endpoints;

/// <summary>Endpoint di salute <c>/health</c>: liveness del processo, usata dall'healthcheck del container.</summary>
public static class HealthMapping
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapHealthChecks("/health");
        return routes;
    }
}

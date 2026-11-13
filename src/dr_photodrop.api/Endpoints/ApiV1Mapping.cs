using dr_photodrop.api.Options;

namespace dr_photodrop.api.Endpoints;

/// <summary>Gruppo <c>/api/v1</c>: tutte le rotte richiedono la chiave API (<c>X-Api-Key</c>); <c>/health</c> resta fuori e anonimo.</summary>
public static class ApiV1Mapping
{
    public static IEndpointRouteBuilder MapApiV1Endpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(InboxConstants.ApiGroupPrefix).RequireAuthorization();
        group.MapScopesEndpoints();
        group.MapInboxEndpoints();
        return routes;
    }
}

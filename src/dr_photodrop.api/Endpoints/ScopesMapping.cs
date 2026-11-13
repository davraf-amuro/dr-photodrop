using dr_photodrop.api.Dto;
using dr_photodrop.api.Services;

namespace dr_photodrop.api.Endpoints;

/// <summary>Endpoint <c>GET /scopes</c>: ambiti disponibili con i loro campi (senza <c>targetFolder</c>).</summary>
public static class ScopesMapping
{
    private const string Tag = "Scopes";

    public static IEndpointRouteBuilder MapScopesEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/scopes", GetScopeListAsync)
            .WithSummary("Elenco degli ambiti")
            .WithDescription("Restituisce gli ambiti configurati in scopes/*.json con campi, default e limiti; la cartella di destinazione non è esposta.")
            .WithTags(Tag)
            .WithName("GetScopeList")
            .Produces<IReadOnlyList<ScopeDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);
        return routes;
    }

    private static async Task<IResult> GetScopeListAsync(IScopeCatalog catalog, CancellationToken cancellationToken)
    {
        // Lettura dal catalogo (cache invalidata dai file): un nuovo ambito compare senza riavvio
        var scopes = await catalog.GetScopesAsync(cancellationToken);
        var dto = scopes
            .Select(s => new ScopeDto(
                s.Id, s.Name, s.Icon, s.PhotoHints,
                s.Fields.Select(f => new ScopeFieldDto(f.Key, f.Label, f.Type, f.Required, f.Default, f.Min, f.MaxLength)).ToList()))
            .ToList();
        return TypedResults.Ok(dto);
    }
}

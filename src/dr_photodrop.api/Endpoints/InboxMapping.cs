using dr_photodrop.api.Dto;
using dr_photodrop.api.Services;

namespace dr_photodrop.api.Endpoints;

/// <summary>Endpoint <c>POST /inbox/photos</c>: ricezione di una foto con i suoi metadati.</summary>
public static class InboxMapping
{
    private const string Tag = "Inbox";

    public static IEndpointRouteBuilder MapInboxEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/inbox/photos", PostPhotoAsync)
            // Antiforgery disattivato: l'API non usa cookie né sessioni, la chiave arriva solo nell'header X-Api-Key
            // (un sito terzo non può inviarla da un browser), quindi il token antiforgery non protegge nulla
            .DisableAntiforgery()
            .WithSummary("Riceve una foto")
            .WithDescription("Multipart con file, scope, photoId, batchId, capturedAt e fields (JSON). 201 se nuova, 200 con duplicate=true se già ricevuta.")
            .WithTags(Tag)
            .WithName("PostInboxPhoto")
            .Produces<UploadPhotoResponse>(StatusCodes.Status201Created)
            .Produces<UploadPhotoResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status413PayloadTooLarge);
        return routes;
    }

    private static Task<IResult> PostPhotoAsync(HttpRequest request, UploadPhotoHandler handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(request, cancellationToken);
}

using System.Globalization;
using dr_photodrop.api.Dto;
using dr_photodrop.api.Helpers;
using dr_photodrop.api.Models;
using dr_photodrop.api.Options;
using dr_photodrop.api.Validators;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;

namespace dr_photodrop.api.Services;

/// <summary>Orchestra un upload: limite di body, lettura del multipart, validazione, scrittura. Restituisce 201, 200, 400 o 413.</summary>
public class UploadPhotoHandler(
    IScopeCatalog catalog,
    IValidator<UploadPhotoRequest> validator,
    IPhotoStore store,
    IOptions<InboxOptions> options,
    ILogger<UploadPhotoHandler> logger)
{
    private const long BytesPerMb = 1024L * 1024L;
    private const long BytesPerKb = 1024L;

    public async Task<IResult> HandleAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var bodyLimit = settings.MaxPhotoSizeMb * BytesPerMb + settings.MultipartOverheadKb * BytesPerKb;

        // Limite di body esplicito: rifiuto anticipato dal Content-Length e tetto imposto a Kestrel per i body senza lunghezza
        if (request.ContentLength > bodyLimit)
        {
            logger.LogWarning("Upload rifiutato: corpo oltre il limite di {LimitBytes} byte", bodyLimit);
            return TypedResults.Problem(statusCode: StatusCodes.Status413PayloadTooLarge, title: "Corpo della richiesta troppo grande.");
        }

        var sizeFeature = request.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (sizeFeature is { IsReadOnly: false })
        {
            sizeFeature.MaxRequestBodySize = bodyLimit;
        }

        if (!request.HasFormContentType)
        {
            return Invalid(InboxConstants.FilePart, "La richiesta deve essere multipart/form-data.");
        }

        IFormCollection form;
        try
        {
            form = await request.ReadFormAsync(cancellationToken);
        }
        catch (BadHttpRequestException ex) when (ex.StatusCode == StatusCodes.Status413PayloadTooLarge)
        {
            logger.LogWarning("Upload rifiutato: corpo oltre il limite di {LimitBytes} byte", bodyLimit);
            return TypedResults.Problem(statusCode: StatusCodes.Status413PayloadTooLarge, title: "Corpo della richiesta troppo grande.");
        }
        catch (Exception ex) when (ex is InvalidDataException or BadHttpRequestException or IOException)
        {
            logger.LogWarning(ex, "Upload rifiutato: multipart non leggibile");
            return Invalid(InboxConstants.FilePart, "Il multipart non è leggibile.");
        }

        var files = form.Files.Where(f => f.Name == InboxConstants.FilePart).ToList();
        if (files.Count > 1)
        {
            return Invalid(InboxConstants.FilePart, "Una sola foto per richiesta.");
        }

        var file = files.FirstOrDefault();
        var scopeId = Part(form, InboxConstants.ScopePart);
        var scope = await catalog.FindAsync(scopeId, cancellationToken);

        byte[] header = [];
        if (file is { Length: > 0 })
        {
            header = await ReadHeaderAsync(file, cancellationToken);
        }

        var upload = new UploadPhotoRequest(
            scopeId, scope,
            Part(form, InboxConstants.PhotoIdPart),
            Part(form, InboxConstants.BatchIdPart),
            Part(form, InboxConstants.CapturedAtPart),
            Part(form, InboxConstants.FieldsPart),
            file is not null, file?.ContentType, file?.Length ?? 0, header);

        var validation = validator.Validate(upload);
        if (!validation.IsValid || scope is null || file is null)
        {
            logger.LogInformation("Upload non valido: Scope={Scope} ErrorParts={ErrorParts}", scope?.Id, string.Join(',', validation.Errors.Keys));
            return TypedResults.ValidationProblem(validation.Errors);
        }

        // Dopo la validazione i valori sono conformi: il parsing non può più fallire
        var photoId = Guid.Parse(upload.PhotoId!);
        var batchId = Guid.Parse(upload.BatchId!);
        var capturedAt = DateTimeOffset.Parse(upload.CapturedAt!, CultureInfo.InvariantCulture);
        var fields = ScopeFieldsParser.Parse(upload.Fields, scope).Values;
        var contentType = ImageSignature.Detect(header)!;

        await using var content = file.OpenReadStream();
        var command = new SavePhotoCommand(
            scope, photoId, batchId, capturedAt, fields,
            FileNameSanitizer.Sanitize(file.FileName, settings.OriginalFileNameMaxLength), contentType, content);
        var result = await store.SaveAsync(command, cancellationToken);

        var response = new UploadPhotoResponse(photoId, batchId, scope.Id, result.Duplicate);
        return result.Duplicate
            ? TypedResults.Ok(response)
            : TypedResults.Json(response, statusCode: StatusCodes.Status201Created);
    }

    private static string? Part(IFormCollection form, string name) =>
        form.TryGetValue(name, out var values) && values.Count == 1 ? values[0] : null;

    private static IResult Invalid(string part, string message) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { [part] = [message] });

    private static async Task<byte[]> ReadHeaderAsync(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var buffer = new byte[(int)Math.Min(InboxConstants.HeaderLength, file.Length)];
        await stream.ReadExactlyAsync(buffer, cancellationToken);
        return buffer;
    }
}

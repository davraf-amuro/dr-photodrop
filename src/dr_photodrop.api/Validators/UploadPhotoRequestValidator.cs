using System.Globalization;
using System.Text.RegularExpressions;
using dr_photodrop.api.Helpers;
using dr_photodrop.api.Models;
using dr_photodrop.api.Options;
using Microsoft.Extensions.Options;

namespace dr_photodrop.api.Validators;

/// <summary>
/// Valida le parti multipart di un upload: ambito esistente, id GUID, foto (content-type, magic bytes, dimensione),
/// <c>capturedAt</c> plausibile e campi conformi all'ambito. Le chiavi degli errori sono i nomi delle parti.
/// </summary>
public partial class UploadPhotoRequestValidator(IOptions<InboxOptions> options, TimeProvider timeProvider) : IValidator<UploadPhotoRequest>
{
    private const long BytesPerMb = 1024L * 1024L;

    // Offset obbligatorio: un orario senza fuso è ambiguo e non è accettato
    [GeneratedRegex(@"(Z|[+-]\d{2}:\d{2})$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OffsetRegex();

    public ValidationResult Validate(UploadPhotoRequest input)
    {
        var errors = new Dictionary<string, string[]>();
        var settings = options.Value;

        ValidateScope(input, errors);
        ValidateGuid(input.PhotoId, InboxConstants.PhotoIdPart, errors);
        ValidateGuid(input.BatchId, InboxConstants.BatchIdPart, errors);
        ValidateCapturedAt(input.CapturedAt, settings, errors);
        ValidateFile(input, settings, errors);

        // Senza ambito noto non c'è una definizione contro cui controllare i campi
        if (input.ResolvedScope is { } scope)
        {
            var parsed = ScopeFieldsParser.Parse(input.Fields, scope);
            if (parsed.Errors.Count > 0)
            {
                errors[InboxConstants.FieldsPart] = [.. parsed.Errors];
            }
        }

        return ValidationResult.From(errors);
    }

    private static void ValidateScope(UploadPhotoRequest input, Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(input.Scope))
        {
            errors[InboxConstants.ScopePart] = ["L'ambito è obbligatorio."];
        }
        else if (input.ResolvedScope is null)
        {
            // Non si riporta il valore ricevuto: l'ambito sconosciuto non deve finire in risposta né nei log
            errors[InboxConstants.ScopePart] = ["Ambito sconosciuto."];
        }
    }

    private static void ValidateGuid(string? value, string part, Dictionary<string, string[]> errors)
    {
        if (!Guid.TryParse(value, out var guid) || guid == Guid.Empty)
        {
            errors[part] = ["Deve essere un GUID valido."];
        }
    }

    private void ValidateCapturedAt(string? value, InboxOptions settings, Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(value)
            || !OffsetRegex().IsMatch(value)
            || !DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var capturedAt))
        {
            errors[InboxConstants.CapturedAtPart] = ["Deve essere una data ISO 8601 con offset (es. 2026-11-13T10:15:00+01:00)."];
            return;
        }

        var now = timeProvider.GetUtcNow();
        if (capturedAt > now.AddMinutes(settings.CapturedAtMaxFutureMinutes))
        {
            errors[InboxConstants.CapturedAtPart] = ["La data di scatto è nel futuro."];
        }
        else if (capturedAt < now.AddDays(-settings.CapturedAtMaxAgeDays))
        {
            errors[InboxConstants.CapturedAtPart] = ["La data di scatto è troppo vecchia."];
        }
    }

    private static void ValidateFile(UploadPhotoRequest input, InboxOptions settings, Dictionary<string, string[]> errors)
    {
        if (!input.HasFile || input.FileLength <= 0)
        {
            errors[InboxConstants.FilePart] = ["La foto è obbligatoria e non può essere vuota."];
            return;
        }

        if (input.FileLength > settings.MaxPhotoSizeMb * BytesPerMb)
        {
            errors[InboxConstants.FilePart] = [$"La foto supera {settings.MaxPhotoSizeMb} MB."];
            return;
        }

        var declared = input.FileContentType?.Split(';')[0].Trim().ToLowerInvariant();
        if (declared is null || !settings.AllowedContentTypes.Contains(declared, StringComparer.OrdinalIgnoreCase)
            || ImageSignature.ExtensionFor(declared) is null)
        {
            errors[InboxConstants.FilePart] = ["Tipo di file non ammesso."];
            return;
        }

        // Il content-type è dichiarato dal client: deve combaciare con quello riconosciuto dai primi byte
        var detected = ImageSignature.Detect(input.FileHeader);
        if (detected is null || !string.Equals(detected, declared, StringComparison.Ordinal))
        {
            errors[InboxConstants.FilePart] = ["Il contenuto non corrisponde a un'immagine del tipo dichiarato."];
        }
    }
}

using System.Globalization;
using System.Text.Json;
using dr_photodrop.api.Models;
using dr_photodrop.api.Options;

namespace dr_photodrop.api.Helpers;

/// <summary>Interpreta e controlla la parte <c>fields</c> (oggetto JSON) contro la definizione dei campi dell'ambito.</summary>
public static class ScopeFieldsParser
{
    /// <summary>Esito: valori accettati (cloni indipendenti dal documento JSON) ed errori in italiano che non riportano i valori del client.</summary>
    public record Result(Dictionary<string, JsonElement> Values, List<string> Errors);

    public static Result Parse(string? json, ScopeDefinition scope)
    {
        var values = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        var errors = new List<string>();

        if (!string.IsNullOrWhiteSpace(json))
        {
            ReadObject(json, scope, values, errors);
        }

        // Campi obbligatori assenti (anche dopo aver scartato le stringhe vuote)
        foreach (var field in scope.Fields.Where(f => f.Required && !values.ContainsKey(f.Key)))
        {
            errors.Add($"Il campo '{field.Key}' è obbligatorio.");
        }

        return new Result(values, errors);
    }

    private static void ReadObject(string json, ScopeDefinition scope, Dictionary<string, JsonElement> values, List<string> errors)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            errors.Add("Il valore non è JSON valido.");
            return;
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                errors.Add("Il valore deve essere un oggetto JSON.");
                return;
            }

            var unknown = 0;
            foreach (var property in document.RootElement.EnumerateObject())
            {
                var definition = scope.Fields.FirstOrDefault(f => string.Equals(f.Key, property.Name, StringComparison.Ordinal));
                if (definition is null)
                {
                    unknown++;
                    continue;
                }

                ReadField(definition, property.Value, values, errors);
            }

            if (unknown > 0)
            {
                errors.Add($"Contiene {unknown} chiavi non definite nell'ambito.");
            }
        }
    }

    private static void ReadField(ScopeFieldDefinition definition, JsonElement value, Dictionary<string, JsonElement> values, List<string> errors)
    {
        switch (definition.Type)
        {
            case InboxConstants.FieldTypeNumber:
                if (value.ValueKind != JsonValueKind.Number || !value.TryGetDecimal(out var number))
                {
                    errors.Add($"Il campo '{definition.Key}' deve essere un numero.");
                }
                else if (definition.Min is { } min && number < min)
                {
                    errors.Add($"Il campo '{definition.Key}' deve essere almeno {min.ToString(CultureInfo.InvariantCulture)}.");
                }
                else
                {
                    values[definition.Key] = value.Clone();
                }

                break;

            case InboxConstants.FieldTypeDate:
                if (value.ValueKind != JsonValueKind.String
                    || !DateOnly.TryParseExact(value.GetString(), InboxConstants.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                {
                    errors.Add($"Il campo '{definition.Key}' deve essere una data {InboxConstants.DateFormat}.");
                }
                else
                {
                    values[definition.Key] = value.Clone();
                }

                break;

            default:
                ReadText(definition, value, values, errors);
                break;
        }
    }

    private static void ReadText(ScopeFieldDefinition definition, JsonElement value, Dictionary<string, JsonElement> values, List<string> errors)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            errors.Add($"Il campo '{definition.Key}' deve essere un testo.");
            return;
        }

        var text = value.GetString();
        if (string.IsNullOrWhiteSpace(text))
        {
            // Testo vuoto = campo non compilato: errore solo se obbligatorio (lo segnala il controllo dei mancanti)
            return;
        }

        if (definition.MaxLength is { } maxLength && text.Length > maxLength)
        {
            errors.Add($"Il campo '{definition.Key}' supera {maxLength} caratteri.");
            return;
        }

        values[definition.Key] = value.Clone();
    }
}

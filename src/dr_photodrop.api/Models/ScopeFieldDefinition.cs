namespace dr_photodrop.api.Models;

/// <summary>Definizione di un campo di un ambito, come scritta nel file <c>scopes/&lt;id&gt;.json</c>.</summary>
public record ScopeFieldDefinition(
    string Key,
    string Label,
    string Type,
    bool Required,
    string? Default,
    decimal? Min,
    int? MaxLength);

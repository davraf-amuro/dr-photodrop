namespace dr_photodrop.api.Models;

/// <summary>Ambito validato: id, cartella di destinazione (mai esposta al client) e campi.</summary>
public record ScopeDefinition(
    string Id,
    string Name,
    string Icon,
    string TargetFolder,
    IReadOnlyList<string> PhotoHints,
    IReadOnlyList<ScopeFieldDefinition> Fields);

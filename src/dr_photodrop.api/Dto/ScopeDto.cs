namespace dr_photodrop.api.Dto;

/// <summary>Ambito come esposto alla PWA: <c>targetFolder</c> non compare, resta un dettaglio del server.</summary>
public record ScopeDto(string Id, string Name, string Icon, IReadOnlyList<string> PhotoHints, IReadOnlyList<ScopeFieldDto> Fields);

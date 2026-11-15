namespace dr_photodrop.api.Dto;

/// <summary>Campo di un ambito come esposto all'app.</summary>
public record ScopeFieldDto(string Key, string Label, string Type, bool Required, string? Default, decimal? Min, int? MaxLength);

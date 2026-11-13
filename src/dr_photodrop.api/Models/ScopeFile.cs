namespace dr_photodrop.api.Models;

/// <summary>Forma grezza del file di ambito, prima della validazione.</summary>
public class ScopeFile
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public string? TargetFolder { get; set; }
    public string[]? PhotoHints { get; set; }
    public ScopeFieldFile[]? Fields { get; set; }
}

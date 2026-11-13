namespace dr_photodrop.api.Models;

/// <summary>Forma grezza di un campo nel file di ambito, prima della validazione.</summary>
public class ScopeFieldFile
{
    public string? Key { get; set; }
    public string? Label { get; set; }
    public string? Type { get; set; }
    public bool Required { get; set; }
    public string? Default { get; set; }
    public decimal? Min { get; set; }
    public int? MaxLength { get; set; }
}

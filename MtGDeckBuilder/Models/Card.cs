namespace MtGDeckBuilder.Models; 

public class Card {
    public int Id { get; set; }                      
    public Guid OracleId { get; set; }    
    public string Name { get; set; } = "";

    public string? ManaCost { get; set; }
    public string? TypeLine { get; set; }
    public string? OracleText { get; set; }
    public decimal? Cmc { get; set; }

    public string[] Colors { get; set; } = Array.Empty<string>();        // e.g. ["U","R"]
    public string[] ColorIdentity { get; set; } = Array.Empty<string>(); // e.g. ["U","R"]
    public bool IsReserved { get; set; }

    public string? ImageUri { get; set; } = "";
}

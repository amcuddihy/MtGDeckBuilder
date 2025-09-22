namespace MtGDeckBuilder.Models;

public class Deck {
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = "";
    public string Notes { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<CardInDeck> AllCards { get; set; } = new();

    public List<CardInDeck> MainDeck => AllCards.Where(dc => dc.Section == DeckSection.MainDeck).ToList();
    public List<CardInDeck> Sideboard => AllCards.Where(dc => dc.Section == DeckSection.Sideboard).ToList();
    public List<CardInDeck> Commander => AllCards.Where(dc => dc.Section == DeckSection.Commander).ToList();
    public List<CardInDeck> InConsideration => AllCards.Where(dc => dc.Section == DeckSection.InConsideration).ToList();

    public Card? FeaturedCard { get; set; } = new();
    public int VersionId { get; set; } = 0;
    public string VersionName { get; set; } = "";
}

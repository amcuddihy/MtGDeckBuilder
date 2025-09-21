namespace MtGDeckBuilder.Models;

public enum DeckSection 
{
    MainDeck = 0,
    Sideboard = 1,
    Commander = 2,
    InConsideration = 3
}

public class CardInDeck 
{
    public int CardInDeckId { get; set; }

    public Deck? Deck { get; set; }
    public Card? Card { get; set; }
    
    public DeckSection Section { get; set; } = DeckSection.MainDeck;
}

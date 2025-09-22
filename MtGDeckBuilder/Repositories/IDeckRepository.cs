using MtGDeckBuilder.Models;

namespace MtGDeckBuilder.Repositories; 

public interface IDeckRepository 
{
    public List<Deck> GetAllDecksForUser(int userId);
    public int CreateDeck(Deck deck);
    public bool UpdateDeck(Deck deck);
    public bool DeleteDeck(Deck deck);
}

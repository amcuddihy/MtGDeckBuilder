using MtGDeckBuilder.Models;

namespace MtGDeckBuilder.Repositories;

public interface ICardRepository 
{
    public List<Card> GetAllCards();
    public void SaveAllCards(List<Card> cards);
    public void DeleteAllCards();
}

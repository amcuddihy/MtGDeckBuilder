using Microsoft.Data.Sqlite;
using MtGDeckBuilder.Models;
using MtGDeckBuilder.Services;

namespace MtGDeckBuilder.Repositories; 

public class SqliteDeckRepository 
{
    private readonly SqliteConnection _sqliteConn;
    private readonly CardIndex _cardIndex;  

    public SqliteDeckRepository(SqliteConnection sqliteConn, CardIndex cardIndex) {
        _sqliteConn = sqliteConn;
        _cardIndex = cardIndex;
    }

    public List<Deck> GetAllDecksForUser(int userId) { 
        var command = _sqliteConn.CreateCommand();
        command.CommandText = "SELECT Id, UserId, Name, Notes, CreatedAt, VersionId, VersionName FROM Decks WHERE UserId = $userId ORDER BY CreatedAt DESC";
        command.Parameters.AddWithValue("$userId", userId);

        using var reader = command.ExecuteReader();
        var decks = new List<Deck>();
        while (reader.Read()) {
            decks.Add(new Deck
            {
                Id = reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                Name = reader.IsDBNull(2) ? "" : reader.GetString(2),
                Notes = reader.IsDBNull(3) ? "" : reader.GetString(3),
                CreatedAt = reader.IsDBNull(4) ? DateTime.UtcNow : reader.GetDateTime(4),
                VersionId = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                VersionName = reader.IsDBNull(6) ? "" : reader.GetString(6),
                FeaturedCard = _cardIndex.Cards.FirstOrDefault(c => c.Id == reader.GetInt32(7))
            });
        }

        return decks;
    }
    
    public int CreateDeck(Deck deck) { 
        var command = _sqliteConn.CreateCommand();
        command.CommandText = "INSERT INTO Decks (UserId, Name, Notes, CreatedAt, VersionId, VersionName, FeaturedCardId) " +
                              "VALUES (@userId, @name, @notes, @createdAt, @versionId, @versionName, @featuredCardId); " +
                              "SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("@userId", deck.UserId);
        command.Parameters.AddWithValue("@name", deck.Name);
        command.Parameters.AddWithValue("@notes", deck.Notes);
        command.Parameters.AddWithValue("@createdAt", deck.CreatedAt);
        command.Parameters.AddWithValue("@versionId", deck.VersionId);
        command.Parameters.AddWithValue("@versionName", deck.VersionName);
        command.Parameters.AddWithValue("@featuredCardId", deck.FeaturedCard is null? 0 : deck.FeaturedCard.Id);

        var nextId = command.ExecuteScalar();
        if (nextId is null) {
            throw new Exception("Failed to retrieve last insert row id after creating deck.");
        }
        
        return Convert.ToInt32(nextId);
    }

    public bool UpdateDeck(Deck deck) { 
        var command = _sqliteConn.CreateCommand();
        command.CommandText = "UPDATE Decks SET Name = @name, Notes = @notes, VersionId = @versionId, VersionName = @versionName, FeaturedCardId = @featuredCardId " +
                              "WHERE Id = @id AND UserId = @userId";
        
        command.Parameters.AddWithValue("@id", deck.Id);
        command.Parameters.AddWithValue("@userId", deck.UserId);
        command.Parameters.AddWithValue("@name", deck.Name);
        command.Parameters.AddWithValue("@notes", deck.Notes);
        command.Parameters.AddWithValue("@versionId", deck.VersionId);
        command.Parameters.AddWithValue("@versionName", deck.VersionName);
        command.Parameters.AddWithValue("@featuredCardId", deck.FeaturedCard is null ? 0 : deck.FeaturedCard.Id);

        var rowsAffected = command.ExecuteNonQuery();
        return rowsAffected > 0;
    }

    public bool DeleteDeck(Deck deck) { 
        var command = _sqliteConn.CreateCommand();
        command.CommandText = "DELETE FROM Decks WHERE Id = @id AND UserId = @userId";
        command.Parameters.AddWithValue("@id", deck.Id);
        command.Parameters.AddWithValue("@userId", deck.UserId);

        var rowsAffected = command.ExecuteNonQuery();
        return rowsAffected > 0;
    }
}

using Microsoft.Data.Sqlite;
using MtGDeckBuilder.Models;
using System.Text.Json;

namespace MtGDeckBuilder.Repositories; 

public class SqliteCardRepository : ICardRepository 
{
    private readonly SqliteConnection _sqliteConn;

    public SqliteCardRepository(SqliteConnection sqliteConn) {
        _sqliteConn = sqliteConn;
    }

    public List<Card> GetAllCards() { 
        var command = _sqliteConn.CreateCommand();
        command.CommandText = "SELECT Id, OracleId, Name, ManaCost, TypeLine, OracleText, Cmc, Colors, ColorIdentity, IsReserved, ImageUri " + 
                                "FROM Cards ORDER BY Name COLLATE NOCASE";
        var reader = command.ExecuteReader();

        var cards = new List<Card>();
        while (reader.Read()) {
            cards.Add(new Card()
            {
                Id = reader.GetInt32(0),
                OracleId = reader.GetGuid(1),
                Name = reader.IsDBNull(2) ? "" : reader.GetString(2),
                ManaCost = reader.IsDBNull(3) ? "" : reader.GetString(3),
                TypeLine = reader.IsDBNull(4) ? "" : reader.GetString(4),
                OracleText = reader.IsDBNull(5) ? "" : reader.GetString(5),
                Cmc = reader.IsDBNull(6) ? 0 : reader.GetDecimal(6),
                Colors = ReadJsonStringArray(reader, 7),
                ColorIdentity = ReadJsonStringArray(reader, 8),
                IsReserved = reader.GetBoolean(9),
                ImageUri = reader.IsDBNull(10) ? "" : reader.GetString(10)
            }); 
        }

        return cards;
    }

    private static string[] ReadJsonStringArray(SqliteDataReader reader, int index) {
        if (reader.IsDBNull(index)) {
            return [];
        }

        var rawString = reader.GetString(index);
        if (string.IsNullOrWhiteSpace(rawString)) { 
            return []; 
        }

        try {
            var stringAsArray = JsonSerializer.Deserialize<string[]>(rawString);
            if (stringAsArray is null) {
                return [];
            }
            return stringAsArray;
        }
        catch (JsonException) {
            // if something goes wrong, return the raw string as a single-item array
            return [ rawString ];
        }
    }

    public void SaveAllCards(List<Card> cards) {
        var command = _sqliteConn.CreateCommand();
        command.CommandText = "INSERT OR REPLACE INTO Cards " +
                              "(Id, OracleId, Name, ManaCost, TypeLine, OracleText, Cmc, Colors, ColorIdentity, IsReserved, ImageUri) " +
                              "VALUES (@id, @oracleId, @name, @manaCost, @typeLine, @oracleText, @cmc, @colors, @colorIdentity, @isReserved, @imageUri)";

        command.Parameters.Add("@id", SqliteType.Integer);
        command.Parameters.Add("@oracleId", SqliteType.Text);
        command.Parameters.Add("@name", SqliteType.Text);
        command.Parameters.Add("@manaCost", SqliteType.Text);
        command.Parameters.Add("@typeLine", SqliteType.Text);
        command.Parameters.Add("@oracleText", SqliteType.Text); 
        command.Parameters.Add("@cmc", SqliteType.Real);
        command.Parameters.Add("@colors", SqliteType.Text);
        command.Parameters.Add("@colorIdentity", SqliteType.Text);
        command.Parameters.Add("@isReserved", SqliteType.Integer);
        command.Parameters.Add("@imageUri", SqliteType.Text);

        foreach (var card in cards) { 
            command.Parameters["@id"].Value = card.Id;
            command.Parameters["@oracleId"].Value = card.OracleId.ToString();
            command.Parameters["@name"].Value = card.Name ?? "";
            command.Parameters["@manaCost"].Value = card.ManaCost ?? "";
            command.Parameters["@typeLine"].Value = card.TypeLine ?? "";
            command.Parameters["@oracleText"].Value = card.OracleText ?? "";
            command.Parameters["@cmc"].Value = card.Cmc ?? 0;
            command.Parameters["@colors"].Value = JsonSerializer.Serialize(card.Colors ?? []);
            command.Parameters["@colorIdentity"].Value = JsonSerializer.Serialize(card.ColorIdentity ?? []);
            command.Parameters["@isReserved"].Value = card.IsReserved ? 1 : 0;
            command.Parameters["@imageUri"].Value = card.ImageUri ?? "";

            command.ExecuteNonQuery();
        }
    }

    public void DeleteAllCards() {
        var command = _sqliteConn.CreateCommand();
        command.CommandText = "DELETE FROM Cards";
        command.ExecuteNonQuery();
    }
}

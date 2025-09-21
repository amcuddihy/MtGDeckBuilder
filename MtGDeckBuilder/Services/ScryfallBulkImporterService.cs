namespace MTG_Deck_Builder.Services;

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;

using MtGDeckBuilder.Models;
using MtGDeckBuilder.Repositories;

public class ScryfallBulkImporterService 
{
    private const string ScryfallBulkDataEndpoint = "bulk-data";
    private const string OracleCardsTypeName = "oracle_cards";

    private readonly IHttpClientFactory _httpFactory;
    private readonly ICardRepository _cardRepository;

    public ScryfallBulkImporterService(IHttpClientFactory httpFactory, ICardRepository cardRepository) {
        _httpFactory = httpFactory;
        _cardRepository = cardRepository;
    }

    public async Task<int> DownloadOracleCardsAsync(CancellationToken ct = default) {
        var scryfallHttpClient = _httpFactory.CreateClient("scryfall");

        using var bulkDataResponse = await scryfallHttpClient.GetAsync(ScryfallBulkDataEndpoint, ct);

        if (!bulkDataResponse.IsSuccessStatusCode) {
            throw new InvalidOperationException($"Scryfall /{ScryfallBulkDataEndpoint} failed: {(int)bulkDataResponse.StatusCode} {bulkDataResponse.ReasonPhrase}");
        }

        var bulkDataRawJSON = await bulkDataResponse.Content.ReadAsStringAsync(ct);

        var jsonOptions = new JsonSerializerOptions();
        jsonOptions.PropertyNameCaseInsensitive = true;

        var bulkData = JsonSerializer.Deserialize<BulkTypesList>(bulkDataRawJSON, jsonOptions);

        if (bulkData is null || bulkData.BulkDataTypes.Count == 0) {
            throw new InvalidOperationException($"Failed to deserialize /{ScryfallBulkDataEndpoint}'s raw JSON");
        }

        var oracleBulkType = bulkData.BulkDataTypes.FirstOrDefault(d => d.Type == OracleCardsTypeName);

        if (oracleBulkType is null) {
            throw new InvalidOperationException($"No {OracleCardsTypeName} entry found in /{ScryfallBulkDataEndpoint}");
        }

        var foundValidDownloadUri = Uri.TryCreate(oracleBulkType.DownloadUri, UriKind.Absolute, out var downloadUri);

        if (!foundValidDownloadUri) {
            throw new InvalidOperationException($"Invalid download_uri: {oracleBulkType.DownloadUri}");
        }

        using var bulkCardListResponse = await scryfallHttpClient.GetAsync(downloadUri, HttpCompletionOption.ResponseHeadersRead, ct);

        if (!bulkCardListResponse.IsSuccessStatusCode) {
            throw new InvalidOperationException($"{OracleCardsTypeName} download failed: {(int)bulkCardListResponse.StatusCode} {bulkCardListResponse.ReasonPhrase}\nURL: {downloadUri}");
        }

        // this list is very big, tens of thousands of cards, so we want to stream it rather than load it all into memory at once.
        await using var stream = await bulkCardListResponse.Content.ReadAsStreamAsync(ct);

        var existingCards = _cardRepository.GetAllCards();
        var newCardCount = 0;
        var cardId = 0;
        
        await foreach (var cardData in JsonSerializer.DeserializeAsyncEnumerable<ScryfallOracleCard>(stream, jsonOptions, ct)) {
            if (cardData is null || string.IsNullOrWhiteSpace(cardData.OracleId) || string.IsNullOrWhiteSpace(cardData.Name))
                continue;

            if (!Guid.TryParse(cardData.OracleId, out var oid)) {
                continue;
            }

            var card = existingCards.FirstOrDefault(c => c.OracleId == oid);

            // card being null means this is a new card
            if (card is null) {
                newCardCount++;
                card = new Card { OracleId = oid };
                existingCards.Add(card);
            }

            card.Id = cardId;
            card.Name = cardData.Name;
            card.ManaCost = cardData.ManaCost;
            card.TypeLine = cardData.TypeLine;
            card.OracleText = cardData.OracleText;
            card.Cmc = cardData.Cmc;
            card.Colors = cardData.Colors ?? Array.Empty<string>();
            card.ColorIdentity = cardData.ColorIdentity ?? Array.Empty<string>();
            card.IsReserved = cardData.Reserved;
            card.ImageUri = cardData.ImageUris.BorderCrop;

            cardId++;
        }

        _cardRepository.SaveAllCards(existingCards);
        return newCardCount;
    }

    // Scryfall's bulk-data response doesn't return the list of cards itself.
    // Instead, it returns the list of available bulk data types and their respective download URIs.
    private class BulkTypesList {
        [JsonPropertyName("data")]
        public List<BulkDataType> BulkDataTypes { get; set; } = [];
    }

    // Scryfall has several bulk data types to allow you to specify exactly what you mean by "All Cards".
    // Currently, this application uses the "oracle_cards" type, which contains one entry per logical card (no duplicates for re-prints or alternate artworks).
    private class BulkDataType {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("download_uri")]
        public string? DownloadUri { get; set; }

        [JsonPropertyName("file_name")]
        public string? FileName { get; set; }
    }

    // This class represents the fields we care about from each card in the "oracle_cards" bulk data.
    private class ScryfallOracleCard {
        [JsonPropertyName("oracle_id")]
        public string? OracleId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("mana_cost")]
        public string? ManaCost { get; set; }

        [JsonPropertyName("type_line")]
        public string? TypeLine { get; set; }

        [JsonPropertyName("oracle_text")]
        public string? OracleText { get; set; }

        [JsonPropertyName("cmc")]
        public decimal? Cmc { get; set; }

        [JsonPropertyName("colors")]
        public string[]? Colors { get; set; }

        [JsonPropertyName("color_identity")]
        public string[]? ColorIdentity { get; set; }

        [JsonPropertyName("reserved")]
        public bool Reserved { get; set; }

        [JsonPropertyName("image_uris")]
        public ScryfallCardImageUris ImageUris { get; set; } = new();
    }

    private class ScryfallCardImageUris {
        [JsonPropertyName("border_crop")]
        public string? BorderCrop { get; set; } = "";
    }
}
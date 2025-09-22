using Microsoft.Data.Sqlite;
using MTG_Deck_Builder.Services;
using MtGDeckBuilder.Repositories;
using MtGDeckBuilder.Services;

var builder = WebApplication.CreateBuilder(args);

// HttpClient for Scryfall. This is where all of the card info comes from
// This must be registered here as a service in order for our ScryfallBulkImporterService to use it 
builder.Services.AddHttpClient("scryfall", client =>
{
    client.BaseAddress = new Uri("https://api.scryfall.com/");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("MTG-Deck-Builder/0.1 (alexcuddihy@gmail.com)"); // per Scryfall API rules
});

builder.Services.AddScoped<SqliteConnection>(_ =>
{
    var connectionString = builder.Configuration.GetConnectionString("SqliteConnection");
    var connection = new SqliteConnection(connectionString);
    
    connection.Open();
    return connection;
});

builder.Services.AddScoped<ICardRepository, SqliteCardRepository>();
builder.Services.AddSingleton<CardIndex>();

builder.Services.AddScoped<ScryfallBulkImporterService>();

builder.Services.AddRazorPages();

var app = builder.Build();

app.UseRouting();
app.MapRazorPages();

// Admin endpoint to trigger a sync from a browser/Postman.
// This will only work if the ScryfallBulkImporterService has been added to the builder Services.
app.MapPost("/admin/scryfall/sync", async (ScryfallBulkImporterService importer) =>
{
    var addedCardCount = await importer.DownloadOracleCardsAsync();
    return Results.Ok($"Added {addedCardCount} new cards to the database.");
});


// On startup, load all cards from the database into the CardIndex singleton
// This needs to happen before anything else is loaded (except for the User List) as all of the other data (decks, collections, etc)
// stores a card ID number and retrieves the full card info from the CardIndex
using (var scope = app.Services.CreateScope()) { 
    var cardRepo = scope.ServiceProvider.GetRequiredService<ICardRepository>();
    var cardIndex = scope.ServiceProvider.GetRequiredService<CardIndex>();

    cardIndex.Cards = cardRepo.GetAllCards();
}

app.Run();

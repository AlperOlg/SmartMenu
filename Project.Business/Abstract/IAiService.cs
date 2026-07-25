using Project.Core.Entities.RAG;



namespace Project.Business.Abstract;



public interface IAiService

{

    Task<string> GenerateResponseAsync(

        string prompt,

        int currentUserId,

        int? restaurantId = null,

        CancellationToken cancellationToken = default);



    /// <summary>Metni Ollama embedding modeli ile vektöre dönüştürür.</summary>

    Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);



    Task IndexMenuItemAsync(int restaurantId, int menuItemId, string text, CancellationToken cancellationToken = default);

    Task RemoveMenuItemIndexAsync(int menuItemId, CancellationToken cancellationToken = default);

    /// <param name="restaurantId">Verilirse sadece o restoranın menüsü; null/0 ise tüm platform menüleri arasında arar.</param>
    Task<List<MenuEmbeddingModel>> SearchMenuAsync(
        string query,
        int? restaurantId = null,
        int limit = 3,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// SQL'deki tüm platform verisini (restoran bilgisi/masa durumu, yorumlar, menü ürünleri)
    /// ChunkType bazlı olarak vektör deposuna yükler (Enterprise Multi-Chunk RAG seed'i).
    /// </summary>
    Task SeedAllPlatformDataIndexAsync(CancellationToken cancellationToken = default);

}



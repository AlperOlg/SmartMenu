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
    Task<List<MenuEmbeddingModel>> SearchMenuAsync(int restaurantId, string query, int limit = 3, CancellationToken cancellationToken = default);
}

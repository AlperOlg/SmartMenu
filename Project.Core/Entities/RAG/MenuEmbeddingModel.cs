using Microsoft.Extensions.VectorData;

namespace Project.Core.Entities.RAG;

/// <summary>
/// Menu item record for the in-memory vector store.
/// VectorData 10.x renamed VectorStoreRecord* attributes to VectorStore*.
/// </summary>
public class MenuEmbeddingModel
{
    /// <summary>Menu item id (vector store key).</summary>
    [VectorStoreKey]
    public int MenuItemId { get; set; }

    /// <summary>Restaurant id for multi-tenant filtering.</summary>
    [VectorStoreData(IsIndexed = true)]
    public int RestaurantId { get; set; }

    /// <summary>Source text that was embedded.</summary>
    [VectorStoreData]
    public string Text { get; set; } = string.Empty;

    /// <summary>nomic-embed-text vector (768 dims).</summary>
    [VectorStoreVector(768)]
    public ReadOnlyMemory<float> Embedding { get; set; }
}

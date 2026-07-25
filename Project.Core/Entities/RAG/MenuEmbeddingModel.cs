using Microsoft.Extensions.VectorData;

namespace Project.Core.Entities.RAG;

/// <summary>
/// Enterprise Multi-Chunk RAG kaydı: menü ürünü, restoran bilgisi (masa/puan) ve
/// yorum özetleri gibi farklı veri tiplerini tek koleksiyonda <see cref="ChunkType"/>
/// alanıyla ayırt ederek saklar.
/// VectorData 10.x renamed VectorStoreRecord* attributes to VectorStore*.
/// </summary>
public class MenuEmbeddingModel
{
    /// <summary>Vector store key. Chunk tipine göre deterministik string kimlik (ör. "menuitem-12").</summary>
    [VectorStoreKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Chunk tipi: "MenuItem", "RestaurantInfo", "Review" vb.</summary>
    [VectorStoreData(IsIndexed = true)]
    public string ChunkType { get; set; } = string.Empty;

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

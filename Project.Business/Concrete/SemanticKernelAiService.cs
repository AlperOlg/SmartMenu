using System.Text;
using Microsoft.SemanticKernel;
using Project.Business.Abstract;
using Project.Core.Entities;

namespace Project.Business.Concrete;

public class SemanticKernelAiService : IAiService
{
    private readonly Kernel _kernel;
    private readonly IGenericService<MenuItem> _menuItemService;
    private readonly IRestaurantService _restaurantService;
    private readonly IGenericService<RestaurantLoyalty> _restaurantLoyaltyService;

    public SemanticKernelAiService(
        Kernel kernel,
        IGenericService<MenuItem> menuItemService,
        IRestaurantService restaurantService,
        IGenericService<RestaurantLoyalty> restaurantLoyaltyService)
    {
        _kernel = kernel;
        _menuItemService = menuItemService;
        _restaurantService = restaurantService;
        _restaurantLoyaltyService = restaurantLoyaltyService;
    }

    public async Task<string> GenerateResponseAsync(string prompt, int currentUserId, CancellationToken cancellationToken = default)
    {
        // 1. Detaylarıyla aktif restoranları ve tüm menü elemanlarını çekiyoruz
        var restaurants = await _restaurantService.GetAllRestaurantsWithDetailsAsync(justActive: true);
        var menuItems = await _menuItemService.GetAllAsync(useTracking: false);

        // 2. Kullanıcının tüm restoranlardaki sadakat puanlarını liste olarak çekiyoruz (Hata önlendi)
        // Not: Eğer generic servisiniz expression filtre desteklemiyorsa, sendeki uygun listeleme metodunu yazabilirsin.
        var userLoyalties = await _restaurantLoyaltyService.GetAllAsync(x => x.AppUserId == currentUserId, useTracking: false);

        // 3. Gelişmiş sistem context'ini oluşturuyoruz
        var systemContext = BuildAdvancedSystemContext(restaurants, menuItems, userLoyalties);

        var fullPrompt = $"""
        Sen gelişmiş bir akıllı restoran platformu asistanısın. Aşağıdaki güncel ve gerçek zamanlı sistem verilerini kullanarak müşterinin sorusunu yanıtla.
        
        [SİSTEM KURALLARI]
        1. Sadece sana sağlanan veriler dahilindeki restoranları, menüleri, fiyatları ve masaları öner. Veritabanında olmayan hiçbir şeyi uydurma.
        2. Müşterinin sadakat puanı (Loyalty Points) varsa, bunu harcayabileceğini samimi bir dille hatırlat (Her 1 puan = 1 TL değerindedir).
        3. Eğer müşteri kalabalık bir grup için rezervasyon veya masa durumu sorarsa, restoranların toplam masa sayısına ve doluluk oranına (IsOccupied durumlarına) bakarak mantıklı çıkarımlar yap.
        4. Cevaplarını her zaman KESİNLİKLE TÜRKÇE, samimi, yardımsever, net ve profesyonel bir dille yaz. 
        5. Eğer menüde vegan/gluten free gibi detaylar varsa, bunları akıllıca analiz edip müşteriye sun.

        [GERÇEK ZAMANLI SİSTEM VERİLERİ]
        {systemContext}

        [MÜŞTERİ BİLGİSİ]
        Müşteri ID: {currentUserId}
        
        [MÜŞTERİ SORUSU]
        {prompt}
        
        Asistan Yanıtı:
        """;

        var result = await _kernel.InvokePromptAsync(fullPrompt, cancellationToken: cancellationToken);
        return result.ToString() ?? string.Empty;
    }

    private string BuildAdvancedSystemContext(
        IEnumerable<Restaurant> restaurants,
        IEnumerable<MenuItem> menuItems,
        IEnumerable<RestaurantLoyalty> userLoyalties) // Liste tipine çevrildi
    {
        var sb = new StringBuilder();

        sb.AppendLine("=== AKTİF RESTORANLAR VE MASALARIN DURUMU ===");
        foreach (var r in restaurants)
        {
            int totalTables = r.Tables?.Count ?? 0;
            int occupiedTables = r.Tables?.Count(t => t.IsOccupied) ?? 0;
            int availableTables = totalTables - occupiedTables;

            // Müşterinin SADECE BU RESTORANA ait sadakat puanını buluyoruz
            var loyalty = userLoyalties.FirstOrDefault(l => l.RestaurantId == r.Id);
            decimal loyaltyPoints = loyalty?.TotalPoints ?? 0;

            sb.AppendLine($"- Restoran: {r.Name} (ID: {r.Id})");
            sb.AppendLine($"  * Masa Durumu: Toplam {totalTables} masa var. {occupiedTables} tanesi DOLU, {availableTables} tanesi BOŞ.");
            sb.AppendLine($"  * Giriş Yapan Müşterinin Bu Restorandaki Sadakat Puanı: {loyaltyPoints} Puan ({loyaltyPoints} TL indirim hakkı var).");

            // Bu restorana ait menü elemanlarını filtreleyip ekliyoruz
            sb.AppendLine($"  * Menü Ürünleri:");
            var rItems = menuItems.Where(m => m.RestaurantId == r.Id);
            foreach (var item in rItems)
            {
                sb.AppendLine($"    > [{item.Category?.Name ?? "Genel"}] {item.Name} - Fiyat: {item.Price} TL | Açıklama: {item.Description} | (Vegan: {(item.IsVegan ? "Evet" : "Hayır")}, GlutenFree: {(item.ContainsGluten ? "Evet" : "Hayır")})");
            }
            sb.AppendLine(new string('-', 40));
        }

        return sb.ToString();
    }
}
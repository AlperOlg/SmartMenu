using Project.Business.Abstract;

namespace Project.Web.BackgroundServices;

public class TableReleaseBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TableReleaseBackgroundService> _logger;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    public TableReleaseBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<TableReleaseBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var tableService = scope.ServiceProvider.GetRequiredService<ITableService>();
                await tableService.ReleaseExpiredTablesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Masa otomatik boşaltma işlemi sırasında hata oluştu.");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }
}
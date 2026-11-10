using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SchoolAdmission.Application.Interfaces;

namespace SchoolAdmission.Infrastructure.BackgroundServices;

/// <summary>
/// Proactively expires overdue HELD reservations. The database is authoritative: every reservation,
/// confirmation and capacity change also expires overdue holds inside its own transaction, so a crashed
/// or delayed worker can never cause incorrect seat accounting. Runs on every backend instance; instances
/// coordinate through the class row lock.
/// </summary>
public class ReservationExpirationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<ReservationExpirationWorker> _logger;
    private readonly TimeSpan _interval;

    public ReservationExpirationWorker(IServiceScopeFactory scopes, ILogger<ReservationExpirationWorker> logger, TimeSpan interval)
    {
        _scopes = scopes;
        _logger = logger;
        _interval = interval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // The first pass runs immediately, which recovers holds that expired while the app was down.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var expiration = scope.ServiceProvider.GetRequiredService<IExpirationService>();
                var count = await expiration.ExpireAllOverdueAsync(stoppingToken);
                if (count > 0) _logger.LogInformation("Expired {Count} overdue reservation(s)", count);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Expiration pass failed; will retry");
            }

            try { await Task.Delay(_interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}

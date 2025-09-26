using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LostAndFound.Domain.Entities;
using LostAndFound.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LostAndFound.Api.Services;

public class AutoDisposalService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AutoDisposalService> _logger;
    private readonly IConfiguration _config;

    public AutoDisposalService(IServiceScopeFactory scopeFactory, ILogger<AutoDisposalService> logger, IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = _config.GetValue<int?>("Disposal:RunIntervalMinutes") ?? 60;
        if (intervalMinutes < 5) intervalMinutes = 5; // safety lower bound

        _logger.LogInformation("AutoDisposalService started. IntervalMinutes={Interval}", intervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AutoDisposalService failed a cycle");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // ignore on shutdown
            }
        }
    }

    private async Task ProcessAsync(CancellationToken ct)
    {
        var retentionDays = _config.GetValue<int?>("Disposal:RetentionDays") ?? 90;
        if (retentionDays < 1) retentionDays = 1; // safety lower bound

        var cutoffUtc = DateTime.UtcNow.AddDays(-retentionDays);
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Strategy: mark items that have been in the system longer than retention as ReadyToDispose
        // Using CreatedAt as proxy for retention start. Optionally this could be FoundAt (from Deposit) if preferred.
        var candidates = await db.FoundItems
            .Include(i => i.Deposit)
            .Where(i => i.Status == ItemStatus.InStorage)
            .OrderBy(i => i.CreatedAt)
            .Take(500) // process in batches
            .ToListAsync(ct);

        if (candidates.Count == 0)
        {
            _logger.LogInformation("AutoDisposalService: no candidates found for ReadyToDispose.");
            return;
        }

        foreach (var item in candidates)
        {
            // Double-check status
            if (item.Status != ItemStatus.InStorage) continue;
            var foundAtUtc = item.Deposit?.FoundAt.HasValue == true
                ? DateTime.SpecifyKind(item.Deposit!.FoundAt!.Value, DateTimeKind.Local).ToUniversalTime()
                : (DateTime?)null;
            var basisUtc = foundAtUtc ?? item.CreatedAt;
            if (basisUtc > cutoffUtc) continue; // not yet due

            item.Status = ItemStatus.ReadyToDispose;

            db.CustodyLogs.Add(new CustodyLog
            {
                FoundItemId = item.Id,
                ActionType = "MarkReadyToDispose",
                ActorUserId = "system",
                Timestamp = DateTime.UtcNow,
                Notes = $"Auto-marked after {retentionDays} days"
            });

            db.ItemAuditLogs.Add(new ItemAuditLog
            {
                FoundItemId = item.Id,
                Action = "MarkReadyToDispose",
                PerformedByUserId = "system",
                PerformedByEmail = null,
                Details = $"Auto-marked after {retentionDays} days",
                OccurredAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("AutoDisposalService: marked {Count} items as ReadyToDispose.", candidates.Count);
    }
}

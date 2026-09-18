using System.Net.Http.Headers;
using System.Text;
using FintechPlatform.Api.Data;
using FintechPlatform.Api.Domain;
using FintechPlatform.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FintechPlatform.Api.Services;

public sealed class WebhookDeliveryService(
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<WebhookDeliveryService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await DeliverBatchAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Webhook worker batch failed."); }
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
    }

    private async Task DeliverBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var maxAttempts = configuration.GetValue("Webhooks:MaxAttempts", 5);
        var signingSecret = configuration["Webhooks:SigningSecret"]
            ?? throw new InvalidOperationException("Webhooks:SigningSecret is missing.");

        var items = await db.WebhookOutbox
            .Where(x => x.Status == WebhookStatus.Pending && x.NextAttemptAt <= now)
            .OrderBy(x => x.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            if (!await WebhookUrlPolicy.IsAllowedAsync(item.DestinationUrl, cancellationToken))
            {
                item.Status = WebhookStatus.DeadLetter;
                item.LastError = "Destination rejected by webhook URL policy.";
                await db.SaveChangesAsync(cancellationToken);
                continue;
            }

            item.AttemptCount++;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, item.DestinationUrl);
                request.Content = new StringContent(item.PayloadJson, Encoding.UTF8, "application/json");
                request.Headers.Add("X-Webhook-Id", item.Id.ToString());
                request.Headers.Add("X-Webhook-Event", item.EventType);
                request.Headers.Add("X-Webhook-Signature", WebhookSigner.Sign(item.PayloadJson, signingSecret));
                request.Headers.UserAgent.Add(new ProductInfoHeaderValue("FintechPlatformDemo", "1.0"));

                var client = httpClientFactory.CreateClient("webhooks");
                using var response = await client.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    item.Status = WebhookStatus.Delivered;
                    item.DeliveredAt = DateTimeOffset.UtcNow;
                    item.LastError = null;
                }
                else ScheduleRetry(item, maxAttempts, $"HTTP {(int)response.StatusCode}");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                ScheduleRetry(item, maxAttempts, ex.Message);
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static void ScheduleRetry(WebhookOutbox item, int maxAttempts, string error)
    {
        item.LastError = error.Length <= 1000 ? error : error[..1000];
        if (item.AttemptCount >= maxAttempts)
        {
            item.Status = WebhookStatus.DeadLetter;
            return;
        }

        var delaySeconds = Math.Min(300, (int)Math.Pow(2, item.AttemptCount) * 5);
        item.NextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(delaySeconds);
    }
}

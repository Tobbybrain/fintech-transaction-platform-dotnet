using System.Security.Claims;
using FintechPlatform.Api.Data;
using FintechPlatform.Api.Domain;
using FintechPlatform.Api.Dtos;
using FintechPlatform.Api.Infrastructure;
using FintechPlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FintechPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/transfers")]
public sealed class TransfersController(AppDbContext db, TransferService transferService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<TransferResponse>> Create(CreateTransferRequest request, CancellationToken cancellationToken)
    {
        if (!Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyHeader))
            return BadRequest(new { error = "Idempotency-Key header is required." });

        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        var source = await db.Wallets.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.FromWalletId, cancellationToken);
        if (source is null) return NotFound(new { error = "Source wallet not found." });
        if (!User.IsInRole(Roles.Admin) && source.OwnerId != userId) return Forbid();

        if (!string.IsNullOrWhiteSpace(request.WebhookUrl) &&
            !await WebhookUrlPolicy.IsAllowedAsync(request.WebhookUrl, cancellationToken))
            return BadRequest(new { error = "Webhook URL must be a public HTTPS endpoint." });

        try
        {
            var transfer = await transferService.CreateAsync(request, idempotencyHeader.ToString(), cancellationToken);
            return Ok(ToResponse(transfer));
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TransferResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        var transfer = await db.Transfers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (transfer is null) return NotFound();

        if (!User.IsInRole(Roles.Admin))
        {
            var sourceOwnerId = await db.Wallets.AsNoTracking()
                .Where(x => x.Id == transfer.FromWalletId)
                .Select(x => x.OwnerId)
                .SingleOrDefaultAsync(cancellationToken);

            if (sourceOwnerId != userId) return Forbid();
        }

        return Ok(ToResponse(transfer));
    }

    private static TransferResponse ToResponse(Transfer x) => new(
        x.Id, x.Reference, x.IdempotencyKey, x.FromWalletId, x.ToWalletId,
        x.Currency, x.Amount, x.Status.ToString(), x.CreatedAt, x.CompletedAt);
}

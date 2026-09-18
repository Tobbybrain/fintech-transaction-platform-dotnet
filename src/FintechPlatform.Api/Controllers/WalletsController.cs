using System.Security.Claims;
using FintechPlatform.Api.Data;
using FintechPlatform.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FintechPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/wallets")]
public sealed class WalletsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WalletResponse>>> Mine(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        var isAdmin = User.IsInRole("Admin");
        var query = db.Wallets.AsNoTracking();
        if (!isAdmin) query = query.Where(x => x.OwnerId == userId);

        var wallets = await query.OrderBy(x => x.Currency)
            .Select(x => new WalletResponse(x.Id, x.OwnerId, x.Currency, x.Balance, x.Version))
            .ToListAsync(cancellationToken);

        return Ok(wallets);
    }

    [HttpGet("{walletId:guid}/ledger")]
    public async Task<ActionResult<IReadOnlyList<LedgerEntryResponse>>> Ledger(Guid walletId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        var wallet = await db.Wallets.AsNoTracking().SingleOrDefaultAsync(x => x.Id == walletId, cancellationToken);
        if (wallet is null) return NotFound();
        if (!User.IsInRole("Admin") && wallet.OwnerId != userId) return Forbid();

        var entries = await db.LedgerEntries.AsNoTracking()
            .Where(x => x.WalletId == walletId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .Select(x => new LedgerEntryResponse(x.Id, x.TransferId, x.Type.ToString(), x.Currency, x.Amount, x.BalanceAfter, x.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(entries);
    }
}

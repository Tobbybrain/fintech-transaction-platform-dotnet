using Xunit;
using FintechPlatform.Api.Infrastructure;

namespace FintechPlatform.Tests;

public sealed class WebhookSignerTests
{
    [Fact]
    public void Signature_is_deterministic_and_changes_with_payload()
    {
        var first = WebhookSigner.Sign("{\"amount\":100}", "secret");
        var second = WebhookSigner.Sign("{\"amount\":100}", "secret");
        var changed = WebhookSigner.Sign("{\"amount\":101}", "secret");

        Assert.Equal(first, second);
        Assert.NotEqual(first, changed);
        Assert.Equal(64, first.Length);
    }
}

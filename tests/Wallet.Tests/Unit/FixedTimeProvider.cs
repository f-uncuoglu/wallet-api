namespace Wallet.Tests.Unit;

/// <summary>A clock that always shows the same instant, so time-dependent code is deterministic.</summary>
public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

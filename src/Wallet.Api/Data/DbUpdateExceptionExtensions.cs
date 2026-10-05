using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Wallet.Api.Data;

public static class DbUpdateExceptionExtensions
{
    /// <summary>True when the save failed because a unique index or constraint was violated (SQLSTATE 23505).</summary>
    public static bool IsUniqueViolation(this DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}

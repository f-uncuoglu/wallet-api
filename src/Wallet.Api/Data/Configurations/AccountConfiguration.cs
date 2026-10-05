using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wallet.Api.Entities;

namespace Wallet.Api.Data.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        // Last line of defence: even a bug in the application cannot persist a negative balance.
        builder.ToTable(table => table.HasCheckConstraint("CK_Accounts_Balance_NonNegative", "\"Balance\" >= 0"));

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.Name).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Currency).HasConversion<string>().HasMaxLength(3);

        // Money is numeric(18,2): exact decimal arithmetic, never floating point.
        builder.Property(a => a.Balance).HasPrecision(18, 2);

        // Maps to PostgreSQL's xmin system column; EF Core adds it to the WHERE clause
        // of every UPDATE and throws DbUpdateConcurrencyException when no row matches.
        builder.Property(a => a.Version).IsRowVersion();

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.UserId);
    }
}

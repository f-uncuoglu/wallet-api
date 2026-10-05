using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wallet.Api.Entities;

namespace Wallet.Api.Data.Configurations;

public class AccountTransactionConfiguration : IEntityTypeConfiguration<AccountTransaction>
{
    public void Configure(EntityTypeBuilder<AccountTransaction> builder)
    {
        builder.ToTable("Transactions", table =>
            table.HasCheckConstraint("CK_Transactions_Amount_Positive", "\"Amount\" > 0"));

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.Amount).HasPrecision(18, 2);
        builder.Property(t => t.BalanceAfter).HasPrecision(18, 2);
        builder.Property(t => t.Description).HasMaxLength(200);

        builder.HasOne(t => t.Account)
            .WithMany()
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // Serves the transaction history query: filter by account, newest first.
        builder.HasIndex(t => new { t.AccountId, t.CreatedAt });
    }
}

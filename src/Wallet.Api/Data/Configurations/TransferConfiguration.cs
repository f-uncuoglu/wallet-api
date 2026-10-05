using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wallet.Api.Entities;

namespace Wallet.Api.Data.Configurations;

public class TransferConfiguration : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> builder)
    {
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_Transfers_Amount_Positive", "\"Amount\" > 0");
            table.HasCheckConstraint("CK_Transfers_DifferentAccounts", "\"FromAccountId\" <> \"ToAccountId\"");
        });

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Amount).HasPrecision(18, 2);
        builder.Property(t => t.Currency).HasConversion<string>().HasMaxLength(3);
        builder.Property(t => t.Description).HasMaxLength(200);
        builder.Property(t => t.IdempotencyKey).IsRequired().HasMaxLength(100);

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(t => t.FromAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(t => t.ToAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.InitiatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // The database, not the application, guarantees that a key is used at most once per user.
        // Two concurrent requests with the same key cannot both commit.
        builder.HasIndex(t => new { t.InitiatedByUserId, t.IdempotencyKey }).IsUnique();
    }
}

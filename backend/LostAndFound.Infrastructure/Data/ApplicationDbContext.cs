using LostAndFound.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<FoundItem> FoundItems => Set<FoundItem>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<Transfer> Transfers => Set<Transfer>();
    public DbSet<CustodyLog> CustodyLogs => Set<CustodyLog>();
    public DbSet<OwnerClaim> OwnerClaims => Set<OwnerClaim>();
    public DbSet<StorageLocation> StorageLocations => Set<StorageLocation>();
    public DbSet<RoleAuditLog> RoleAuditLogs => Set<RoleAuditLog>();
    public DbSet<ItemAuditLog> ItemAuditLogs => Set<ItemAuditLog>();
    public DbSet<Deposit> Deposits => Set<Deposit>();
    public DbSet<DepositCashDenomination> DepositCashDenominations => Set<DepositCashDenomination>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<CurrencyDenomination> CurrencyDenominations => Set<CurrencyDenomination>();
    public DbSet<FoundItemCash> FoundItemCashes => Set<FoundItemCash>();
    public DbSet<FoundItemCashEntry> FoundItemCashEntries => Set<FoundItemCashEntry>();
    public DbSet<BusLine> BusLines => Set<BusLine>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Driver> Drivers => Set<Driver>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<FoundItem>(e =>
        {
            e.Property(p => p.Category).IsRequired().HasMaxLength(100);
            e.Property(p => p.OtherCategoryText).HasMaxLength(200);
            e.Property(p => p.Details).IsRequired();
            e.HasOne(p => p.Deposit)
                .WithMany(d => d.Items)
                .HasForeignKey(p => p.DepositId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(i => i.StorageLocation)
                .WithMany()
                .HasForeignKey(i => i.StorageLocationId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Deposit>(e =>
        {
            e.HasIndex(d => new { d.Year, d.Serial }).IsUnique();
            e.Property(d => d.DepositNumber).IsRequired().HasMaxLength(16);
            e.HasOne(d => d.Cash)
                .WithOne(c => c.Deposit)
                .HasForeignKey<DepositCashDenomination>(c => c.DepositId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<DepositCashDenomination>(e =>
        {
            // optional additional config
        });

        builder.Entity<Currency>(e =>
        {
            e.HasIndex(c => c.Code).IsUnique();
            e.Property(c => c.Code).IsRequired().HasMaxLength(8);
            e.Property(c => c.Name).IsRequired().HasMaxLength(64);
        });

        builder.Entity<CurrencyDenomination>(e =>
        {
            e.HasOne(d => d.Currency)
                .WithMany(c => c.Denominations)
                .HasForeignKey(d => d.CurrencyId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(d => new { d.CurrencyId, d.SortOrder });
        });

        builder.Entity<FoundItemCash>(e =>
        {
            e.HasOne(fc => fc.FoundItem)
                .WithOne()
                .HasForeignKey<FoundItemCash>(fc => fc.FoundItemId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(fc => fc.Currency)
                .WithMany()
                .HasForeignKey(fc => fc.CurrencyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<FoundItemCashEntry>(e =>
        {
            e.HasOne(fe => fe.FoundItemCash)
                .WithMany(fc => fc.Entries)
                .HasForeignKey(fe => fe.FoundItemCashId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(fe => fe.CurrencyDenomination)
                .WithMany()
                .HasForeignKey(fe => fe.CurrencyDenominationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Attachment>(e =>
        {
            e.HasOne(a => a.FoundItem)
                .WithMany(i => i.Attachments)
                .HasForeignKey(a => a.FoundItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Transfer>(e =>
        {
            e.HasOne(t => t.FoundItem)
                .WithMany(i => i.Transfers)
                .HasForeignKey(t => t.FoundItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CustodyLog>(e =>
        {
            e.HasOne(c => c.FoundItem)
                .WithMany(i => i.CustodyLogs)
                .HasForeignKey(c => c.FoundItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<OwnerClaim>(e =>
        {
            e.HasOne(c => c.FoundItem)
                .WithMany(i => i.OwnerClaims)
                .HasForeignKey(c => c.FoundItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Deposit>(e =>
        {
            e.HasOne(d => d.BusLine)
                .WithMany()
                .HasForeignKey(d => d.BusLineId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<BusLine>(e =>
        {
            e.Property(b => b.Name).IsRequired().HasMaxLength(200);
            e.HasIndex(b => b.SortOrder);
        });

        builder.Entity<Vehicle>(e =>
        {
            e.Property(v => v.LicensePlate).IsRequired().HasMaxLength(32);
            e.HasIndex(v => v.LicensePlate).IsUnique();
        });

        builder.Entity<Driver>(e =>
        {
            e.Property(d => d.Code).IsRequired().HasMaxLength(64);
            e.Property(d => d.Name).IsRequired().HasMaxLength(200);
            e.HasIndex(d => d.Code).IsUnique();
            e.HasIndex(d => d.Name);
        });
    }
}

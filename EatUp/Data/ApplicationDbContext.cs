using EatUp.Models;
using Microsoft.EntityFrameworkCore;

namespace EatUp.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<MenuItemChangeRequest> MenuItemChangeRequests => Set<MenuItemChangeRequest>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<PromoCode> PromoCodes => Set<PromoCode>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Favorite>()
            .HasIndex(f => new { f.CustomerId, f.RestaurantId })
            .IsUnique();

        modelBuilder.Entity<Review>()
            .HasIndex(r => new { r.CustomerId, r.RestaurantId })
            .IsUnique();

        modelBuilder.Entity<PromoCode>()
            .HasIndex(p => p.Code)
            .IsUnique();

        modelBuilder.Entity<Restaurant>()
            .HasOne(r => r.User)
            .WithOne(u => u.Restaurant)
            .HasForeignKey<Restaurant>(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Customer)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Restaurant)
            .WithMany(r => r.Orders)
            .HasForeignKey(o => o.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Review>()
            .HasOne(r => r.Customer)
            .WithMany(u => u.Reviews)
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Review>()
            .HasOne(r => r.Restaurant)
            .WithMany(r => r.Reviews)
            .HasForeignKey(r => r.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Review>()
            .HasOne<Order>()
            .WithMany()
            .HasForeignKey(r => r.OrderId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Favorite>()
            .HasOne(f => f.Customer)
            .WithMany(u => u.Favorites)
            .HasForeignKey(f => f.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Favorite>()
            .HasOne(f => f.Restaurant)
            .WithMany(r => r.Favorites)
            .HasForeignKey(f => f.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MenuItemChangeRequest>()
            .HasOne(m => m.MenuItem)
            .WithMany(i => i.ChangeRequests)
            .HasForeignKey(m => m.MenuItemId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<MenuItemChangeRequest>()
            .HasOne(m => m.Restaurant)
            .WithMany(r => r.ChangeRequests)
            .HasForeignKey(m => m.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Order)
            .WithMany(o => o.OrderItems)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.MenuItem)
            .WithMany(mi => mi.OrderItems)
            .HasForeignKey(oi => oi.MenuItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

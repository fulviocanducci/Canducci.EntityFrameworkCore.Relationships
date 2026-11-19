using Canducci.EntityFrameworkCore.Relationships.Extensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Canducci.EntityFrameworkCore.Relationships.Tests
{
    public class RelationshipExtensionsTests
    {
        [Fact]
        public async Task Synchronize_DbSet_ShouldAddAndRemoveRelationships()
        {
            SeedData seed = await CreateSeededDatabaseAsync();

            using (AppDbContext context = new AppDbContext(seed.Options))
            {
                User user = context.Users.Single(x => x.Id == seed.UserId);

                context.Users.Synchronize(user, new[] { seed.AdministratorId, seed.GuestId, seed.GuestId }, x => x.Roles);

                context.SaveChanges();
            }

            using (AppDbContext context = new AppDbContext(seed.Options))
            {
                User user = context.Users.Include(x => x.Roles).Single(x => x.Id == seed.UserId);

                Assert.Equal(new[] { seed.AdministratorId, seed.GuestId }.OrderBy(x => x), user.Roles.Select(x => x.Id).OrderBy(x => x));
            }
        }

        [Fact]
        public async Task SynchronizeAsync_DbSet_ShouldAddAndRemoveRelationships()
        {
            SeedData seed = await CreateSeededDatabaseAsync();

            using (AppDbContext context = new AppDbContext(seed.Options))
            {
                User user = await context.Users.SingleAsync(x => x.Id == seed.UserId);

                await context.Users.SynchronizeAsync(user, new[] { seed.AdministratorId, seed.GuestId }, x => x.Roles);

                await context.SaveChangesAsync();
            }

            using (AppDbContext context = new AppDbContext(seed.Options))
            {
                User user = await context.Users.Include(x => x.Roles).SingleAsync(x => x.Id == seed.UserId);

                Assert.Equal(new[] { seed.AdministratorId, seed.GuestId }.OrderBy(x => x), user.Roles.Select(x => x.Id).OrderBy(x => x));
            }
        }

        [Fact]
        public async Task Synchronize_DbContext_ShouldAddAndRemoveRelationships()
        {
            SeedData seed = await CreateSeededDatabaseAsync();

            using (AppDbContext context = new AppDbContext(seed.Options))
            {
                User user = context.Users.Single(x => x.Id == seed.UserId);

                context.Synchronize(user, new[] { seed.GuestId }, x => x.Roles);

                context.SaveChanges();
            }

            using (AppDbContext context = new AppDbContext(seed.Options))
            {
                User user = context.Users.Include(x => x.Roles).Single(x => x.Id == seed.UserId);

                Assert.Equal(new[] { seed.GuestId }, user.Roles.Select(x => x.Id));
            }
        }

        [Fact]
        public async Task SynchronizeAsync_DbContext_ShouldAddAndRemoveRelationships()
        {
            SeedData seed = await CreateSeededDatabaseAsync();

            using (AppDbContext context = new AppDbContext(seed.Options))
            {
                User user = await context.Users.SingleAsync(x => x.Id == seed.UserId);

                await context.SynchronizeAsync(user, new[] { seed.GuestId }, x => x.Roles);

                await context.SaveChangesAsync();
            }

            using (AppDbContext context = new AppDbContext(seed.Options))
            {
                User user = await context.Users.Include(x => x.Roles).SingleAsync(x => x.Id == seed.UserId);

                Assert.Equal(new[] { seed.GuestId }, user.Roles.Select(x => x.Id));
            }
        }

        [Fact]
        public async Task Synchronize_ShouldThrowWhenRequestedIdDoesNotExist()
        {
            SeedData seed = await CreateSeededDatabaseAsync();

            using (AppDbContext context = new AppDbContext(seed.Options))
            {
                User user = context.Users.Single(x => x.Id == seed.UserId);

                Assert.Throws<KeyNotFoundException>(() => context.Users.Synchronize(user, new[] { int.MaxValue }, x => x.Roles));

                Assert.Equal(new[] { seed.AdministratorId, seed.EditorId }.OrderBy(x => x), user.Roles.Select(x => x.Id).OrderBy(x => x));
            }
        }

        private static async Task<SeedData> CreateSeededDatabaseAsync()
        {
            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

            using (AppDbContext context = new AppDbContext(options))
            {
                User user = new User { Name = "User" };
                Role administrator = new Role { Name = "Administrator" };
                Role editor = new Role { Name = "Editor" };
                Role guest = new Role { Name = "Guest" };

                user.Roles.Add(administrator);
                user.Roles.Add(editor);

                context.Users.Add(user);
                context.Roles.Add(guest);

                await context.SaveChangesAsync();

                return new SeedData(options, user.Id, administrator.Id, editor.Id, guest.Id);
            }
        }

        private sealed class SeedData
        {
            public SeedData(DbContextOptions<AppDbContext> options, int userId, int administratorId, int editorId, int guestId)
            {
                Options = options;
                UserId = userId;
                AdministratorId = administratorId;
                EditorId = editorId;
                GuestId = guestId;
            }

            public DbContextOptions<AppDbContext> Options { get; }
            public int UserId { get; }
            public int AdministratorId { get; }
            public int EditorId { get; }
            public int GuestId { get; }
        }
    }

    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasMany(x => x.Roles).WithMany(x => x.Users);
        }
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ICollection<Role> Roles { get; set; } = new List<Role>();
    }

    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}

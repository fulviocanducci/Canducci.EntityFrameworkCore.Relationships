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
                await context.Users.SynchronizeAsync(user, new[] { seed.AdministratorId, seed.GuestId, seed.GuestId }, x => x.Roles);
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
        public async Task Synchronize_ShouldPreserveRelationshipsWhenIdsAreUnchanged()
        {
            SeedData seed = await CreateSeededDatabaseAsync();
            using (AppDbContext context = new AppDbContext(seed.Options))
            {
                User user = context.Users.Include(x => x.Roles).Single(x => x.Id == seed.UserId);
                Role administrator = user.Roles.Single(x => x.Id == seed.AdministratorId);
                Role editor = user.Roles.Single(x => x.Id == seed.EditorId);
                context.Synchronize(user, new[] { seed.EditorId, seed.AdministratorId }, x => x.Roles);
                Assert.Same(administrator, user.Roles.Single(x => x.Id == seed.AdministratorId));
                Assert.Same(editor, user.Roles.Single(x => x.Id == seed.EditorId));
                Assert.Equal(2, user.Roles.Count);
            }
        }

        [Fact]
        public async Task Synchronize_ShouldRemoveAllRelationshipsWhenIdsAreEmpty()
        {
            SeedData seed = await CreateSeededDatabaseAsync();
            using (AppDbContext context = new AppDbContext(seed.Options))
            {
                User user = context.Users.Single(x => x.Id == seed.UserId);
                context.Synchronize(user, Array.Empty<int>(), x => x.Roles);
                Assert.Empty(user.Roles);
            }
        }

        [Fact]
        public async Task SynchronizeAsync_ShouldRemoveAllRelationshipsWhenIdsAreEmpty()
        {
            SeedData seed = await CreateSeededDatabaseAsync();
            using (AppDbContext context = new AppDbContext(seed.Options))
            {
                User user = await context.Users.SingleAsync(x => x.Id == seed.UserId);
                await context.SynchronizeAsync(user, Array.Empty<int>(), x => x.Roles);
                Assert.Empty(user.Roles);
            }
        }

        [Fact]
        public async Task Synchronize_ShouldReuseAlreadyTrackedRelatedEntity()
        {
            SeedData seed = await CreateSeededDatabaseAsync();
            using (AppDbContext context = new AppDbContext(seed.Options))
            {
                User user = context.Users.Single(x => x.Id == seed.UserId);
                Role guest = context.Roles.Single(x => x.Id == seed.GuestId);
                context.Synchronize(user, new[] { seed.GuestId }, x => x.Roles);
                Assert.Same(guest, user.Roles.Single());
            }
        }

        [Fact]
        public async Task SynchronizeAsync_ShouldReuseAlreadyTrackedRelatedEntity()
        {
            SeedData seed = await CreateSeededDatabaseAsync();
            using (AppDbContext context = new AppDbContext(seed.Options))
            {
                User user = await context.Users.SingleAsync(x => x.Id == seed.UserId);
                Role guest = await context.Roles.SingleAsync(x => x.Id == seed.GuestId);
                await context.SynchronizeAsync(user, new[] { seed.GuestId }, x => x.Roles);
                Assert.Same(guest, user.Roles.Single());
            }
        }

        [Fact]
        public async Task Synchronize_ShouldLoadNavigationWhenNotLoaded()
        {
            SeedData seed = await CreateSeededDatabaseAsync();
            using (AppDbContext context = new AppDbContext(seed.Options))
            {
                User user = context.Users.Single(x => x.Id == seed.UserId);
                Assert.False(context.Entry(user).Collection(x => x.Roles).IsLoaded);
                context.Synchronize(user, new[] { seed.GuestId }, x => x.Roles);
                Assert.True(context.Entry(user).Collection(x => x.Roles).IsLoaded);
                Assert.Equal(new[] { seed.GuestId }, user.Roles.Select(x => x.Id));
            }
        }

        [Fact]
        public async Task SynchronizeAsync_ShouldLoadNavigationWhenNotLoaded()
        {
            SeedData seed = await CreateSeededDatabaseAsync();
            using (AppDbContext context = new AppDbContext(seed.Options))
            {
                User user = await context.Users.SingleAsync(x => x.Id == seed.UserId);
                Assert.False(context.Entry(user).Collection(x => x.Roles).IsLoaded);
                await context.SynchronizeAsync(user, new[] { seed.GuestId }, x => x.Roles);
                Assert.True(context.Entry(user).Collection(x => x.Roles).IsLoaded);
                Assert.Equal(new[] { seed.GuestId }, user.Roles.Select(x => x.Id));
            }
        }

        [Fact]
        public async Task Synchronize_ShouldThrowWhenRequestedIdDoesNotExistWithoutChangingCollection()
        {
            SeedData seed = await CreateSeededDatabaseAsync();
            using (AppDbContext context = new AppDbContext(seed.Options))
            {
                User user = context.Users.Single(x => x.Id == seed.UserId);
                Assert.Throws<KeyNotFoundException>(() => context.Synchronize(user, new[] { seed.GuestId, int.MaxValue }, x => x.Roles));
                Assert.Equal(new[] { seed.AdministratorId, seed.EditorId }.OrderBy(x => x), user.Roles.Select(x => x.Id).OrderBy(x => x));
            }
        }

        [Fact]
        public async Task SynchronizeAsync_ShouldThrowWhenRequestedIdDoesNotExistWithoutChangingCollection()
        {
            SeedData seed = await CreateSeededDatabaseAsync();
            using (AppDbContext context = new AppDbContext(seed.Options))
            {
                User user = await context.Users.SingleAsync(x => x.Id == seed.UserId);
                await Assert.ThrowsAsync<KeyNotFoundException>(() => context.SynchronizeAsync(user, new[] { seed.GuestId, int.MaxValue }, x => x.Roles));
                Assert.Equal(new[] { seed.AdministratorId, seed.EditorId }.OrderBy(x => x), user.Roles.Select(x => x.Id).OrderBy(x => x));
            }
        }

        [Fact]
        public async Task Synchronize_ShouldThrowWhenTrackedRelatedEntityIsDeleted()
        {
            SeedData seed = await CreateSeededDatabaseAsync();
            using (AppDbContext context = new AppDbContext(seed.Options))
            {
                User user = context.Users.Single(x => x.Id == seed.UserId);
                Role guest = context.Roles.Single(x => x.Id == seed.GuestId);
                context.Entry(guest).State = EntityState.Deleted;
                Assert.Throws<InvalidOperationException>(() => context.Synchronize(user, new[] { seed.GuestId }, x => x.Roles));
            }
        }

        [Fact]
        public async Task SynchronizeAsync_ShouldThrowWhenTrackedRelatedEntityIsDeleted()
        {
            SeedData seed = await CreateSeededDatabaseAsync();
            using (AppDbContext context = new AppDbContext(seed.Options))
            {
                User user = await context.Users.SingleAsync(x => x.Id == seed.UserId);
                Role guest = await context.Roles.SingleAsync(x => x.Id == seed.GuestId);
                context.Entry(guest).State = EntityState.Deleted;
                await Assert.ThrowsAsync<InvalidOperationException>(() => context.SynchronizeAsync(user, new[] { seed.GuestId }, x => x.Roles));
            }
        }

        [Fact]
        public async Task Synchronize_ShouldThrowWhenEntityIsNotTracked()
        {
            SeedData seed = await CreateSeededDatabaseAsync();
            using (AppDbContext context = new AppDbContext(seed.Options))
            {
                User user = new User { Id = seed.UserId, Name = "Detached" };
                Assert.Throws<InvalidOperationException>(() => context.Synchronize(user, new[] { seed.GuestId }, x => x.Roles));
            }
        }

        [Fact]
        public async Task Synchronize_ShouldThrowWhenNavigationCollectionIsNull()
        {
            SeedData seed = await CreateSeededDatabaseAsync();
            using (AppDbContext context = new AppDbContext(seed.Options))
            {
                User user = context.Users.Single(x => x.Id == seed.UserId);
                context.Entry(user).Collection(x => x.Roles).IsLoaded = true;
                user.Roles = null;
                Assert.Throws<InvalidOperationException>(() => context.Synchronize(user, Array.Empty<int>(), x => x.Roles));
            }
        }

        [Fact]
        public async Task SynchronizeAsync_ShouldThrowWhenNavigationCollectionIsNull()
        {
            SeedData seed = await CreateSeededDatabaseAsync();
            using (AppDbContext context = new AppDbContext(seed.Options))
            {
                User user = await context.Users.SingleAsync(x => x.Id == seed.UserId);
                context.Entry(user).Collection(x => x.Roles).IsLoaded = true;
                user.Roles = null;
                await Assert.ThrowsAsync<InvalidOperationException>(() => context.SynchronizeAsync(user, Array.Empty<int>(), x => x.Roles));
            }
        }

        [Fact]
        public async Task Synchronize_ShouldThrowWhenNavigationExpressionIsNotDirectProperty()
        {
            SeedData seed = await CreateSeededDatabaseAsync();
            using (AppDbContext context = new AppDbContext(seed.Options))
            {
                User user = context.Users.Single(x => x.Id == seed.UserId);
                Assert.Throws<ArgumentException>(() => context.Synchronize(user, Array.Empty<int>(), x => x.Profile.Roles));
            }
        }

        [Fact]
        public async Task Synchronize_ShouldThrowWhenRelatedEntityIsNotInModel()
        {
            SeedData seed = await CreateSeededDatabaseAsync();
            using (AppDbContext context = new AppDbContext(seed.Options))
            {
                User user = context.Users.Single(x => x.Id == seed.UserId);
                Assert.Throws<InvalidOperationException>(() => context.Synchronize<User, UnmappedRole, int>(user, Array.Empty<int>(), x => x.UnmappedRoles));
            }
        }

        [Fact]
        public async Task Synchronize_ShouldThrowWhenKeyTypeDoesNotMatch()
        {
            SeedData seed = await CreateSeededDatabaseAsync();
            using (AppDbContext context = new AppDbContext(seed.Options))
            {
                User user = context.Users.Single(x => x.Id == seed.UserId);
                Assert.Throws<InvalidOperationException>(() => context.Synchronize<User, Role, string>(user, Array.Empty<string>(), x => x.Roles));
            }
        }

        [Fact]
        public async Task Synchronize_ShouldThrowWhenRelatedEntityHasNoPrimaryKey()
        {
            SeedData seed = await CreateSeededDatabaseAsync();
            using (AppDbContext context = new AppDbContext(seed.Options))
            {
                User user = context.Users.Single(x => x.Id == seed.UserId);
                Assert.Throws<InvalidOperationException>(() => context.Synchronize<User, NoKeyRole, int>(user, Array.Empty<int>(), x => x.NoKeyRoles));
            }
        }

        [Fact]
        public async Task Synchronize_ShouldThrowWhenRelatedEntityHasCompositePrimaryKey()
        {
            SeedData seed = await CreateSeededDatabaseAsync();
            using (AppDbContext context = new AppDbContext(seed.Options))
            {
                User user = context.Users.Single(x => x.Id == seed.UserId);
                Assert.Throws<InvalidOperationException>(() => context.Synchronize<User, CompositeRole, int>(user, Array.Empty<int>(), x => x.CompositeRoles));
            }
        }

        [Fact]
        public void Synchronize_ShouldValidateNullArguments()
        {
            AppDbContext context = new AppDbContext(CreateOptions());
            User user = new User();
            Assert.Throws<ArgumentNullException>(() => RelationshipExtensions.Synchronize<User, Role, int>(null, user, Array.Empty<int>(), x => x.Roles));
            Assert.Throws<ArgumentNullException>(() => context.Synchronize<User, Role, int>(null, Array.Empty<int>(), x => x.Roles));
            Assert.Throws<ArgumentNullException>(() => context.Synchronize<User, Role, int>(user, null, x => x.Roles));
            Assert.Throws<ArgumentNullException>(() => context.Synchronize<User, Role, int>(user, Array.Empty<int>(), null));
            context.Dispose();
        }

        [Fact]
        public async Task SynchronizeAsync_ShouldValidateNullArguments()
        {
            AppDbContext context = new AppDbContext(CreateOptions());
            User user = new User();
            await Assert.ThrowsAsync<ArgumentNullException>(() => RelationshipExtensions.SynchronizeAsync<User, Role, int>(null, user, Array.Empty<int>(), x => x.Roles));
            await Assert.ThrowsAsync<ArgumentNullException>(() => context.SynchronizeAsync<User, Role, int>(null, Array.Empty<int>(), x => x.Roles));
            await Assert.ThrowsAsync<ArgumentNullException>(() => context.SynchronizeAsync<User, Role, int>(user, null, x => x.Roles));
            await Assert.ThrowsAsync<ArgumentNullException>(() => context.SynchronizeAsync<User, Role, int>(user, Array.Empty<int>(), null));
            context.Dispose();
        }

        [Fact]
        public void Synchronize_DbSet_ShouldThrowWhenDbSetIsNull()
        {
            DbSet<User> dbSet = null;
            Assert.Throws<ArgumentNullException>(() => RelationshipExtensions.Synchronize<User, Role, int>(dbSet, new User(), Array.Empty<int>(), x => x.Roles));
        }

        [Fact]
        public async Task SynchronizeAsync_DbSet_ShouldThrowWhenDbSetIsNull()
        {
            DbSet<User> dbSet = null;
            await Assert.ThrowsAsync<ArgumentNullException>(() => RelationshipExtensions.SynchronizeAsync<User, Role, int>(dbSet, new User(), Array.Empty<int>(), x => x.Roles));
        }

        private static DbContextOptions<AppDbContext> CreateOptions()
        {
            return new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        }

        private static async Task<SeedData> CreateSeededDatabaseAsync()
        {
            DbContextOptions<AppDbContext> options = CreateOptions();
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
        public DbSet<NoKeyRole> NoKeyRoles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasMany(x => x.Roles).WithMany(x => x.Users);
            modelBuilder.Entity<NoKeyRole>().HasNoKey();
            modelBuilder.Entity<CompositeRole>().HasKey(x => new { x.FirstKey, x.SecondKey });
        }
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ICollection<Role> Roles { get; set; } = new List<Role>();
        public ICollection<UnmappedRole> UnmappedRoles { get; set; } = new List<UnmappedRole>();
        public ICollection<NoKeyRole> NoKeyRoles { get; set; } = new List<NoKeyRole>();
        public ICollection<CompositeRole> CompositeRoles { get; set; } = new List<CompositeRole>();
        public UserProfile Profile { get; set; } = new UserProfile();
    }

    public class UserProfile
    {
        public ICollection<Role> Roles { get; set; } = new List<Role>();
    }

    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ICollection<User> Users { get; set; } = new List<User>();
    }

    public class UnmappedRole
    {
        public int Id { get; set; }
    }

    public class NoKeyRole
    {
        public int Id { get; set; }
    }

    public class CompositeRole
    {
        public int FirstKey { get; set; }
        public int SecondKey { get; set; }
    }
}
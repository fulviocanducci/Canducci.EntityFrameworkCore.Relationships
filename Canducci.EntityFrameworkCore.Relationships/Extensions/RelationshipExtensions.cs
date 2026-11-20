
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Canducci.EntityFrameworkCore.Relationships.Extensions
{
    public static class RelationshipExtensions
    {
        public static void Synchronize<TEntity, TRelated, TKey>(this DbSet<TEntity> dbSet, TEntity entity, IEnumerable<TKey> relatedIds, Expression<Func<TEntity, ICollection<TRelated>>> navigation) where TEntity : class where TRelated : class
        {
            if (dbSet == null)
            {
                throw new ArgumentNullException(nameof(dbSet));
            }

            IServiceProvider serviceProvider = ((IInfrastructure<IServiceProvider>)dbSet).Instance;

            ICurrentDbContext currentDbContext = serviceProvider.GetService(typeof(ICurrentDbContext)) as ICurrentDbContext;

            if (currentDbContext == null)
            {
                throw new InvalidOperationException("The DbContext associated with the DbSet could not be obtained.");
            }

            currentDbContext.Context.Synchronize(entity, relatedIds, navigation);
        }

        public static async Task SynchronizeAsync<TEntity, TRelated, TKey>(this DbSet<TEntity> dbSet, TEntity entity, IEnumerable<TKey> relatedIds, Expression<Func<TEntity, ICollection<TRelated>>> navigation) where TEntity : class where TRelated : class
        {
            if (dbSet == null)
            {
                throw new ArgumentNullException(nameof(dbSet));
            }

            IServiceProvider serviceProvider = ((IInfrastructure<IServiceProvider>)dbSet).Instance;

            ICurrentDbContext currentDbContext = serviceProvider.GetService(typeof(ICurrentDbContext)) as ICurrentDbContext;

            if (currentDbContext == null)
            {
                throw new InvalidOperationException("The DbContext associated with the DbSet could not be obtained.");
            }

            await currentDbContext.Context.SynchronizeAsync(entity, relatedIds, navigation);
        }


        public static void Synchronize<TEntity, TRelated, TKey>(this DbContext context, TEntity entity, IEnumerable<TKey> relatedIds, Expression<Func<TEntity, ICollection<TRelated>>> navigation) where TEntity : class where TRelated : class
        {
            IProperty primaryKeyProperty = Validate(context, entity, relatedIds, navigation);

            List<TKey> requestedIds = relatedIds.Distinct().ToList();

            string navigationName = ((MemberExpression)navigation.Body).Member.Name;

            CollectionEntry collection = context.Entry(entity).Collection(navigationName);

            if (!collection.IsLoaded)
            {
                collection.Load();
            }

            ICollection<TRelated> relatedEntities = navigation.Compile()(entity);

            SynchronizeCore<TRelated, TKey>(context, relatedEntities, requestedIds, primaryKeyProperty);
        }

        public static async Task SynchronizeAsync<TEntity, TRelated, TKey>(this DbContext context, TEntity entity, IEnumerable<TKey> relatedIds, Expression<Func<TEntity, ICollection<TRelated>>> navigation) where TEntity : class where TRelated : class
        {
            IProperty primaryKeyProperty = Validate(context, entity, relatedIds, navigation);

            List<TKey> requestedIds = relatedIds.Distinct().ToList();

            string navigationName = ((MemberExpression)navigation.Body).Member.Name;

            CollectionEntry collection = context.Entry(entity).Collection(navigationName);

            if (!collection.IsLoaded)
            {
                await collection.LoadAsync();
            }

            ICollection<TRelated> relatedEntities = navigation.Compile()(entity);

            if (relatedEntities == null)
            {
                throw new InvalidOperationException("The navigation collection is null.");
            }

            List<TKey> existingIds = GetExistingIds<TRelated, TKey>(context, relatedEntities, primaryKeyProperty);

            List<TKey> idsToAdd = requestedIds.Except(existingIds).ToList();

            List<TKey> idsToRemove = existingIds.Except(requestedIds).ToList();

            List<TRelated> entitiesToAdd = GetTrackedEntities<TRelated, TKey>(context, idsToAdd, primaryKeyProperty, out List<TKey> idsToQuery);

            if (idsToQuery.Count > 0)
            {
                Expression<Func<TRelated, bool>> predicate = CreatePredicate<TRelated, TKey>(idsToQuery, primaryKeyProperty);

                List<TRelated> databaseEntities = await context.Set<TRelated>().Where(predicate).ToListAsync();

                entitiesToAdd.AddRange(databaseEntities);
            }

            CompleteSynchronization<TRelated, TKey>(context, relatedEntities, entitiesToAdd, idsToAdd, idsToRemove, primaryKeyProperty);
        }

        public static void Synchronize<TEntity, TRelated>(this DbContext context, TEntity entity, IEnumerable<TRelated> relatedEntities, Expression<Func<TEntity, ICollection<TRelated>>> navigation) where TEntity : class where TRelated : class
        {
            IProperty primaryKeyProperty = ValidateRelatedEntities(context, entity, relatedEntities, navigation, out List<TRelated> requestedEntities);
            SynchronizeRelatedEntities(context, entity, requestedEntities, navigation, primaryKeyProperty);
        }

        public static async Task SynchronizeAsync<TEntity, TRelated>(this DbContext context, TEntity entity, IEnumerable<TRelated> relatedEntities, Expression<Func<TEntity, ICollection<TRelated>>> navigation) where TEntity : class where TRelated : class
        {
            IProperty primaryKeyProperty = ValidateRelatedEntities(context, entity, relatedEntities, navigation, out List<TRelated> requestedEntities);
            await SynchronizeRelatedEntitiesAsync(context, entity, requestedEntities, navigation, primaryKeyProperty);
        }

        public static void Synchronize<TEntity, TRelated>(this DbSet<TEntity> dbSet, TEntity entity, IEnumerable<TRelated> relatedEntities, Expression<Func<TEntity, ICollection<TRelated>>> navigation) where TEntity : class where TRelated : class
        {
            if (dbSet == null)
            {
                throw new ArgumentNullException(nameof(dbSet));
            }

            IServiceProvider serviceProvider = ((IInfrastructure<IServiceProvider>)dbSet).Instance;
            ICurrentDbContext currentDbContext = serviceProvider.GetService(typeof(ICurrentDbContext)) as ICurrentDbContext;

            if (currentDbContext == null)
            {
                throw new InvalidOperationException("The DbContext associated with the DbSet could not be obtained.");
            }

            currentDbContext.Context.Synchronize(entity, relatedEntities, navigation);
        }

        public static async Task SynchronizeAsync<TEntity, TRelated>(this DbSet<TEntity> dbSet, TEntity entity, IEnumerable<TRelated> relatedEntities, Expression<Func<TEntity, ICollection<TRelated>>> navigation) where TEntity : class where TRelated : class
        {
            if (dbSet == null)
            {
                throw new ArgumentNullException(nameof(dbSet));
            }

            IServiceProvider serviceProvider = ((IInfrastructure<IServiceProvider>)dbSet).Instance;
            ICurrentDbContext currentDbContext = serviceProvider.GetService(typeof(ICurrentDbContext)) as ICurrentDbContext;

            if (currentDbContext == null)
            {
                throw new InvalidOperationException("The DbContext associated with the DbSet could not be obtained.");
            }

            await currentDbContext.Context.SynchronizeAsync(entity, relatedEntities, navigation);
        }

        private static IProperty ValidateRelatedEntities<TEntity, TRelated>(DbContext context, TEntity entity, IEnumerable<TRelated> relatedEntities, Expression<Func<TEntity, ICollection<TRelated>>> navigation, out List<TRelated> requestedEntities) where TEntity : class where TRelated : class
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (relatedEntities == null)
            {
                throw new ArgumentNullException(nameof(relatedEntities));
            }

            if (navigation == null)
            {
                throw new ArgumentNullException(nameof(navigation));
            }

            MemberExpression memberExpression = navigation.Body as MemberExpression;

            if (memberExpression == null || memberExpression.Expression != navigation.Parameters[0] || !(memberExpression.Member is PropertyInfo))
            {
                throw new ArgumentException("The navigation expression must be a direct property of the entity.", nameof(navigation));
            }

            if (context.Entry(entity).State == EntityState.Detached)
            {
                throw new InvalidOperationException("The entity must be tracked by the current DbContext.");
            }

            IEntityType entityType = context.Model.FindEntityType(typeof(TRelated));

            if (entityType == null)
            {
                throw new InvalidOperationException($"Entity type '{typeof(TRelated).Name}' was not found in the DbContext model.");
            }

            IKey primaryKey = entityType.FindPrimaryKey();

            if (primaryKey == null)
            {
                throw new InvalidOperationException($"Entity type '{typeof(TRelated).Name}' does not have a primary key.");
            }

            if (primaryKey.Properties.Count != 1)
            {
                throw new InvalidOperationException($"Entity type '{typeof(TRelated).Name}' must have a single primary key.");
            }

            IProperty primaryKeyProperty = primaryKey.Properties[0];
            requestedEntities = relatedEntities.ToList();

            foreach (TRelated relatedEntity in requestedEntities)
            {
                if (relatedEntity == null)
                {
                    throw new ArgumentException("The related entities collection cannot contain null items.", nameof(relatedEntities));
                }

                EntityState state = context.Entry(relatedEntity).State;

                if (state == EntityState.Detached)
                {
                    throw new InvalidOperationException("All related entities must be tracked by the current DbContext. One or more related entities are not tracked.");
                }

                if (state == EntityState.Deleted)
                {
                    throw new InvalidOperationException("One or more related entities are marked as deleted.");
                }
            }

            return primaryKeyProperty;
        }

        private static void SynchronizeRelatedEntities<TEntity, TRelated>(DbContext context, TEntity entity, List<TRelated> requestedEntities, Expression<Func<TEntity, ICollection<TRelated>>> navigation, IProperty primaryKeyProperty) where TEntity : class where TRelated : class
        {
            string navigationName = ((MemberExpression)navigation.Body).Member.Name;
            CollectionEntry collection = context.Entry(entity).Collection(navigationName);

            if (!collection.IsLoaded)
            {
                collection.Load();
            }

            ICollection<TRelated> currentEntities = navigation.Compile()(entity);
            CompleteRelatedEntitySynchronization(context, requestedEntities, currentEntities, primaryKeyProperty);
        }

        private static async Task SynchronizeRelatedEntitiesAsync<TEntity, TRelated>(DbContext context, TEntity entity, List<TRelated> requestedEntities, Expression<Func<TEntity, ICollection<TRelated>>> navigation, IProperty primaryKeyProperty) where TEntity : class where TRelated : class
        {
            string navigationName = ((MemberExpression)navigation.Body).Member.Name;
            CollectionEntry collection = context.Entry(entity).Collection(navigationName);

            if (!collection.IsLoaded)
            {
                await collection.LoadAsync();
            }

            ICollection<TRelated> currentEntities = navigation.Compile()(entity);
            CompleteRelatedEntitySynchronization(context, requestedEntities, currentEntities, primaryKeyProperty);
        }

        private static void CompleteRelatedEntitySynchronization<TRelated>(DbContext context, List<TRelated> requestedEntities, ICollection<TRelated> currentEntities, IProperty primaryKeyProperty) where TRelated : class
        {
            if (currentEntities == null)
            {
                throw new InvalidOperationException("The navigation collection is null.");
            }

            List<TRelated> distinctRequestedEntities = new List<TRelated>();
            HashSet<object> requestedKeys = new HashSet<object>();

            foreach (TRelated relatedEntity in requestedEntities)
            {
                object key = context.Entry(relatedEntity).Property(primaryKeyProperty.Name).CurrentValue;

                if (requestedKeys.Add(key))
                {
                    distinctRequestedEntities.Add(relatedEntity);
                }
            }

            List<TRelated> entitiesToRemove = currentEntities
                .Where(x => !requestedKeys.Contains(context.Entry(x).Property(primaryKeyProperty.Name).CurrentValue))
                .ToList();

            HashSet<object> existingKeys = new HashSet<object>(
                currentEntities.Select(x => context.Entry(x).Property(primaryKeyProperty.Name).CurrentValue));

            foreach (TRelated relatedEntity in distinctRequestedEntities)
            {
                object key = context.Entry(relatedEntity).Property(primaryKeyProperty.Name).CurrentValue;

                if (!existingKeys.Contains(key))
                {
                    currentEntities.Add(relatedEntity);
                    existingKeys.Add(key);
                }
            }

            foreach (TRelated relatedEntity in entitiesToRemove)
            {
                currentEntities.Remove(relatedEntity);
            }
        }

        private static IProperty Validate<TEntity, TRelated, TKey>(DbContext context, TEntity entity, IEnumerable<TKey> relatedIds, Expression<Func<TEntity, ICollection<TRelated>>> navigation) where TEntity : class where TRelated : class
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (relatedIds == null)
            {
                throw new ArgumentNullException(nameof(relatedIds));
            }

            if (navigation == null)
            {
                throw new ArgumentNullException(nameof(navigation));
            }

            MemberExpression memberExpression = navigation.Body as MemberExpression;

            if (memberExpression == null || memberExpression.Expression != navigation.Parameters[0] || !(memberExpression.Member is PropertyInfo))
            {
                throw new ArgumentException("The navigation expression must be a direct property of the entity.", nameof(navigation));
            }

            if (context.Entry(entity).State == EntityState.Detached)
            {
                throw new InvalidOperationException("The entity must be tracked by the current DbContext.");
            }

            IEntityType entityType = context.Model.FindEntityType(typeof(TRelated));

            if (entityType == null)
            {
                throw new InvalidOperationException($"Entity type '{typeof(TRelated).Name}' was not found in the DbContext model.");
            }

            IKey primaryKey = entityType.FindPrimaryKey();

            if (primaryKey == null)
            {
                throw new InvalidOperationException($"Entity type '{typeof(TRelated).Name}' does not have a primary key.");
            }

            if (primaryKey.Properties.Count != 1)
            {
                throw new InvalidOperationException($"Entity type '{typeof(TRelated).Name}' must have a single primary key.");
            }

            IProperty primaryKeyProperty = primaryKey.Properties[0];

            if (primaryKeyProperty.ClrType != typeof(TKey))
            {
                throw new InvalidOperationException($"The key type '{typeof(TKey).Name}' does not match the entity primary key type '{primaryKeyProperty.ClrType.Name}'.");
            }

            return primaryKeyProperty;
        }

        private static void SynchronizeCore<TRelated, TKey>(DbContext context, ICollection<TRelated> relatedEntities, List<TKey> requestedIds, IProperty primaryKeyProperty) where TRelated : class
        {
            if (relatedEntities == null)
            {
                throw new InvalidOperationException("The navigation collection is null.");
            }

            List<TKey> existingIds = GetExistingIds<TRelated, TKey>(context, relatedEntities, primaryKeyProperty);

            List<TKey> idsToAdd = requestedIds.Except(existingIds).ToList();

            List<TKey> idsToRemove = existingIds.Except(requestedIds).ToList();

            List<TRelated> entitiesToAdd = GetTrackedEntities<TRelated, TKey>(context, idsToAdd, primaryKeyProperty, out List<TKey> idsToQuery);

            if (idsToQuery.Count > 0)
            {
                Expression<Func<TRelated, bool>> predicate = CreatePredicate<TRelated, TKey>(idsToQuery, primaryKeyProperty);

                List<TRelated> databaseEntities = context.Set<TRelated>().Where(predicate).ToList();

                entitiesToAdd.AddRange(databaseEntities);
            }

            CompleteSynchronization<TRelated, TKey>(context, relatedEntities, entitiesToAdd, idsToAdd, idsToRemove, primaryKeyProperty);
        }

        private static List<TKey> GetExistingIds<TRelated, TKey>(DbContext context, ICollection<TRelated> relatedEntities, IProperty primaryKeyProperty) where TRelated : class
        {
            return relatedEntities.Select(x => (TKey)context.Entry(x).Property(primaryKeyProperty.Name).CurrentValue).ToList();
        }

        private static List<TRelated> GetTrackedEntities<TRelated, TKey>(DbContext context, List<TKey> idsToAdd, IProperty primaryKeyProperty, out List<TKey> idsToQuery) where TRelated : class
        {
            List<TRelated> entitiesToAdd = new List<TRelated>();

            List<TKey> trackedIds = new List<TKey>();

            List<EntityEntry<TRelated>> trackedEntries = context.ChangeTracker.Entries<TRelated>().ToList();

            foreach (TKey id in idsToAdd)
            {
                EntityEntry<TRelated> trackedEntry = trackedEntries.FirstOrDefault(x => EqualityComparer<TKey>.Default.Equals((TKey)x.Property(primaryKeyProperty.Name).CurrentValue, id));

                if (trackedEntry != null)
                {
                    if (trackedEntry.State == EntityState.Deleted)
                    {
                        throw new InvalidOperationException($"Entity with key '{id}' is marked as deleted.");
                    }

                    entitiesToAdd.Add(trackedEntry.Entity);

                    trackedIds.Add(id);
                }
            }

            idsToQuery = idsToAdd.Except(trackedIds).ToList();

            return entitiesToAdd;
        }

        private static Expression<Func<TRelated, bool>> CreatePredicate<TRelated, TKey>(List<TKey> ids, IProperty primaryKeyProperty) where TRelated : class
        {
            ParameterExpression parameter = Expression.Parameter(typeof(TRelated), "x");

            MethodCallExpression propertyExpression = Expression.Call(typeof(EF), nameof(EF.Property), new[] { typeof(TKey) }, parameter, Expression.Constant(primaryKeyProperty.Name));

            MethodCallExpression containsExpression = Expression.Call(typeof(Enumerable), nameof(Enumerable.Contains), new[] { typeof(TKey) }, Expression.Constant(ids), propertyExpression);

            return Expression.Lambda<Func<TRelated, bool>>(containsExpression, parameter);
        }

        private static void CompleteSynchronization<TRelated, TKey>(DbContext context, ICollection<TRelated> relatedEntities, List<TRelated> entitiesToAdd, List<TKey> idsToAdd, List<TKey> idsToRemove, IProperty primaryKeyProperty) where TRelated : class
        {
            List<TKey> foundIds = entitiesToAdd.Select(x => (TKey)context.Entry(x).Property(primaryKeyProperty.Name).CurrentValue).ToList();

            List<TKey> missingIds = idsToAdd.Except(foundIds).ToList();

            if (missingIds.Count > 0)
            {
                throw new KeyNotFoundException($"The following related IDs were not found: {string.Join(", ", missingIds)}.");
            }

            foreach (TRelated relatedEntity in entitiesToAdd)
            {
                relatedEntities.Add(relatedEntity);
            }

            if (idsToRemove.Count > 0)
            {
                List<TRelated> entitiesToRemove = relatedEntities.Where(x => idsToRemove.Contains((TKey)context.Entry(x).Property(primaryKeyProperty.Name).CurrentValue)).ToList();

                foreach (TRelated relatedEntity in entitiesToRemove)
                {
                    relatedEntities.Remove(relatedEntity);
                }
            }
        }
    }
}

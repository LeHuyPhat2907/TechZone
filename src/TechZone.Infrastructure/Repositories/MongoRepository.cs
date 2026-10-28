using System.Linq.Expressions;
using MongoDB.Driver;
using TechZone.Domain.Common;
using TechZone.Domain.Interfaces;
using TechZone.Infrastructure.Data;

namespace TechZone.Infrastructure.Repositories;

public class MongoRepository<T> : IRepository<T> where T : BaseEntity, IAggregateRoot
{
    protected readonly IMongoCollection<T> _collection;

    public MongoRepository(IMongoDbContext context)
    {
        _collection = context.GetCollection<T>();
    }

    protected virtual FilterDefinition<T> CombineWithNotDeleted(FilterDefinition<T>? filter)
    {
        var notDeletedFilter = Builders<T>.Filter.Eq(e => e.IsDeleted, false);
        return filter == null ? notDeletedFilter : Builders<T>.Filter.And(filter, notDeletedFilter);
    }

    public async Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<T>.Filter.And(
            Builders<T>.Filter.Eq(e => e.Id, id),
            Builders<T>.Filter.Eq(e => e.IsDeleted, false));
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<T>.Filter.Eq(e => e.IsDeleted, false);
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var filter = Builders<T>.Filter.And(
            Builders<T>.Filter.Where(predicate),
            Builders<T>.Filter.Eq(e => e.IsDeleted, false));
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<T?> FindOneAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var filter = Builders<T>.Filter.And(
            Builders<T>.Filter.Where(predicate),
            Builders<T>.Filter.Eq(e => e.IsDeleted, false));
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<T> Items, long TotalCount)> GetPagedAsync(
        Expression<Func<T, bool>>? predicate,
        int pageIndex,
        int pageSize,
        Expression<Func<T, object>>? orderBy = null,
        bool isAscending = true,
        CancellationToken cancellationToken = default)
    {
        FilterDefinition<T> filter = predicate != null
            ? Builders<T>.Filter.Where(predicate)
            : Builders<T>.Filter.Empty;

        filter = CombineWithNotDeleted(filter);

        var totalCount = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        var query = _collection.Find(filter);

        if (orderBy != null)
        {
            query = isAscending ? query.SortBy(orderBy) : query.SortByDescending(orderBy);
        }
        else
        {
            query = query.SortByDescending(e => e.CreatedAt);
        }

        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<long> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        FilterDefinition<T> filter = predicate != null
            ? Builders<T>.Filter.Where(predicate)
            : Builders<T>.Filter.Empty;

        filter = CombineWithNotDeleted(filter);
        return await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var filter = Builders<T>.Filter.And(
            Builders<T>.Filter.Where(predicate),
            Builders<T>.Filter.Eq(e => e.IsDeleted, false));
        return await _collection.Find(filter).AnyAsync(cancellationToken);
    }

    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.IsDeleted = false;
        await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
        return entity;
    }

    public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        var list = entities.ToList();
        foreach (var item in list)
        {
            item.CreatedAt = DateTime.UtcNow;
            item.IsDeleted = false;
        }
        if (list.Count != 0)
        {
            await _collection.InsertManyAsync(list, cancellationToken: cancellationToken);
        }
    }

    public async Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        var filter = Builders<T>.Filter.And(
            Builders<T>.Filter.Eq(e => e.Id, entity.Id),
            Builders<T>.Filter.Eq(e => e.IsDeleted, false));
        var result = await _collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id, bool softDelete = true, CancellationToken cancellationToken = default)
    {
        if (softDelete)
        {
            var filter = Builders<T>.Filter.Eq(e => e.Id, id);
            var update = Builders<T>.Update
                .Set(e => e.IsDeleted, true)
                .Set(e => e.DeletedAt, DateTime.UtcNow);
            var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
            return result.ModifiedCount > 0;
        }
        else
        {
            var filter = Builders<T>.Filter.Eq(e => e.Id, id);
            var result = await _collection.DeleteOneAsync(filter, cancellationToken);
            return result.DeletedCount > 0;
        }
    }

    public async Task<long> DeleteRangeAsync(Expression<Func<T, bool>> predicate, bool softDelete = true, CancellationToken cancellationToken = default)
    {
        var filter = Builders<T>.Filter.Where(predicate);
        if (softDelete)
        {
            var update = Builders<T>.Update
                .Set(e => e.IsDeleted, true)
                .Set(e => e.DeletedAt, DateTime.UtcNow);
            var result = await _collection.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
            return result.ModifiedCount;
        }
        else
        {
            var result = await _collection.DeleteManyAsync(filter, cancellationToken);
            return result.DeletedCount;
        }
    }
}

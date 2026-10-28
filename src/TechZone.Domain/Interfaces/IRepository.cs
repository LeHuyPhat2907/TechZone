using System.Linq.Expressions;
using TechZone.Domain.Common;

namespace TechZone.Domain.Interfaces;

public interface IRepository<T> where T : BaseEntity, IAggregateRoot
{
    Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<T?> FindOneAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<T> Items, long TotalCount)> GetPagedAsync(
        Expression<Func<T, bool>>? predicate,
        int pageIndex,
        int pageSize,
        Expression<Func<T, object>>? orderBy = null,
        bool isAscending = true,
        CancellationToken cancellationToken = default);
    Task<long> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, bool softDelete = true, CancellationToken cancellationToken = default);
    Task<long> DeleteRangeAsync(Expression<Func<T, bool>> predicate, bool softDelete = true, CancellationToken cancellationToken = default);
}

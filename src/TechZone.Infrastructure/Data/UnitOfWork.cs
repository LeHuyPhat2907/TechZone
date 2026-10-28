using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using TechZone.Domain.Interfaces;

namespace TechZone.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly IMongoDbContext _context;
    private readonly ILogger<UnitOfWork> _logger;
    private IClientSessionHandle? _session;
    private bool _disposed;

    public UnitOfWork(IMongoDbContext context, ILogger<UnitOfWork> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var session = await _context.Client.StartSessionAsync(cancellationToken: cancellationToken);
            _session = session;
            session.StartTransaction();

            try
            {
                var result = await operation(cancellationToken);
                await session.CommitTransactionAsync(cancellationToken);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Transaction failed. Aborting transaction...");
                await session.AbortTransactionAsync(cancellationToken);
                throw;
            }
        }
        catch (NotSupportedException)
        {
            // MongoDB standalone mode does not support multi-document transactions without replica set
            _logger.LogInformation("Standalone MongoDB instance detected without replica set. Executing operation without transaction boundary.");
            return await operation(cancellationToken);
        }
        catch (MongoCommandException mce) when (mce.CodeName == "IllegalOperation" || mce.Message.Contains("Standalone servers do not support transactions"))
        {
            _logger.LogInformation("Standalone MongoDB instance detected without replica set. Executing operation without transaction boundary.");
            return await operation(cancellationToken);
        }
        finally
        {
            _session = null;
        }
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        await ExecuteInTransactionAsync<bool>(async ct =>
        {
            await operation(ct);
            return true;
        }, cancellationToken);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _session?.Dispose();
                _session = null;
            }
            _disposed = true;
        }
    }
}

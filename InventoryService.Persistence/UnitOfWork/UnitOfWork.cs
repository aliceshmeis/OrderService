using InventoryService.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data;

namespace InventoryService.Persistence.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<UnitOfWork> _logger;
        private NpgsqlConnection? _connection;
        private NpgsqlTransaction? _transaction;
        private IInventoryRepository? _inventoryRepository;
        private bool _disposed = false;

        public UnitOfWork(IConfiguration configuration, ILogger<UnitOfWork> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public IInventoryRepository InventoryRepository
        {
            get
            {
                if (_inventoryRepository == null)
                {
                    var connection = GetConnection();
                    var repositoryLogger = LoggerFactory.Create(builder => { })
     .CreateLogger<InventoryRepository>();
                    _inventoryRepository = new InventoryRepository(connection, repositoryLogger);
                }
                return _inventoryRepository;
            }
        }

        private IDbConnection GetConnection()
        {
            if (_connection == null)
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                _connection = new NpgsqlConnection(connectionString);
            }

            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            return _connection;
        }

        public async Task<int> CompleteAsync()
        {
            try
            {
                if (_transaction != null)
                {
                    await _transaction.CommitAsync();
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
                return 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing transaction");
                if (_transaction != null)
                {
                    await _transaction.RollbackAsync();
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
                throw;
            }
        }

        public async Task BeginTransactionAsync()
        {
            var connection = GetConnection();
            if (_transaction == null && connection is NpgsqlConnection npgsqlConnection)
            {
                _transaction = await npgsqlConnection.BeginTransactionAsync();
                _logger.LogInformation("Transaction started");
            }
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
                _logger.LogInformation("Transaction committed");
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
                _logger.LogInformation("Transaction rolled back");
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                if (_transaction != null)
                {
                    _transaction.Dispose();
                    _transaction = null;
                }

                if (_connection != null)
                {
                    if (_connection.State == ConnectionState.Open)
                    {
                        _connection.Close();
                    }
                    _connection.Dispose();
                    _connection = null;
                }

                _disposed = true;
            }
        }
    }
}
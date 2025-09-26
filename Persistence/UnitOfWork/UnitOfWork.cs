using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using OrderService.Persistence.Context;
using OrderService.Persistence.Repositories;
using OrderService.Persistence.Repositories.Interfaces;

namespace OrderService.Persistence.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private IDbContextTransaction? _transaction;  // Made nullable
        private IOrderRepository? _orders;  // Made nullable

        public UnitOfWork(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public IOrderRepository Orders => _orders ??= new OrderRepository(_context, _configuration);

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context?.Dispose();
        }
    }
}
using OrderService.Persistence.Repositories.Interfaces;

namespace OrderService.Persistence.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IOrderRepository Orders { get; }
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
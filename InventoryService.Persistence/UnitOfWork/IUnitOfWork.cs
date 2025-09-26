using InventoryService.Persistence.Repositories;

namespace InventoryService.Persistence.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IInventoryRepository InventoryRepository { get; }
        Task<int> CompleteAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
using ClientLancher.Implement.Repositories.Interface;

namespace ClientLancher.Implement.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        // Repositories
        IApplicationRepository Applications { get; }
        IInstallationLogRepository InstallationLogs { get; }

        // Transaction methods
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();

        // Legacy support
        Task<int> CompleteAsync();
    }
}
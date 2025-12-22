using ClientLancher.Implement.ApplicationDbContext;
using ClientLancher.Implement.Repositories;
using ClientLancher.Implement.Repositories.Interface;
using Microsoft.EntityFrameworkCore.Storage;

namespace ClientLancher.Implement.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ClientLancherDbContext _context;
        private IDbContextTransaction? _transaction;

        // Lazy initialization for repositories
        private IApplicationRepository? _applications;
        private IInstallationLogRepository? _installationLogs;

        public UnitOfWork(ClientLancherDbContext context)
        {
            _context = context;
        }

        // Repository properties with lazy initialization
        public IApplicationRepository Applications =>
            _applications ??= new ApplicationRepository(_context);

        public IInstallationLogRepository InstallationLogs =>
            _installationLogs ??= new InstallationLogRepository(_context);

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<int> CompleteAsync() => await SaveChangesAsync();

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
            _context.Dispose();
        }
    }
}
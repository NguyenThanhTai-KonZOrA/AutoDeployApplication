using ClientLauncher.Implement.ApplicationDbContext;
using ClientLauncher.Implement.EntityModels;
using ClientLauncher.Implement.Repositories.Interface;
using ClientLauncher.Implement.ViewModels.Request;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientLauncher.Implement.Repositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly DeploymentManagerDbContext _context;

        public AuditLogRepository(DeploymentManagerDbContext context)
        {
            _context = context;
        }

        public async Task<AuditLog> AddAsync(AuditLog auditLog)
        {
            await _context.AuditLogs.AddAsync(auditLog);
            await _context.SaveChangesAsync();
            return auditLog;
        }

        public async Task<AuditLog?> GetByIdAsync(int id)
        {
            return await _context.AuditLogs
                .FirstOrDefaultAsync(a => a.ID == id);
        }

        public async Task<List<AuditLog>> GetAllAsync(int page, int pageSize)
        {
            return await _context.AuditLogs
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<AuditLog>> GetByUserNameAsync(string userName, int page, int pageSize)
        {
            return await _context.AuditLogs
                .Where(a => a.UserName == userName)
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<AuditLog>> GetByActionAsync(string action, int page, int pageSize)
        {
            return await _context.AuditLogs
                .Where(a => a.Action == action)
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<AuditLog>> GetByEntityAsync(string entityType, int entityId, int page, int pageSize)
        {
            return await _context.AuditLogs
                .Where(a => a.EntityType == entityType && a.EntityId == entityId)
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<AuditLog>> GetFailedLogsAsync(int page, int pageSize)
        {
            return await _context.AuditLogs
                .Where(a => !a.IsSuccess)
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<AuditLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int page, int pageSize)
        {
            return await _context.AuditLogs
                .Where(a => a.CreatedAt >= startDate && a.CreatedAt <= endDate)
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync()
        {
            return await _context.AuditLogs.CountAsync();
        }

        public async Task<List<AuditLog>> GetPaginatedLogsAsync(AuditLogPaginationRequest request)
        {
            var query = _context.AuditLogs.AsNoTracking().AsQueryable();

            // Apply filters dynamically
            query = ApplyFilters(query, request);

            // Calculate skip value
            int skip = request.Skip ?? (request.Page - 1) * request.PageSize;
            int take = request.Take ?? request.PageSize;

            // Apply pagination and return
            return await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        // Get total count with filters applied
        public async Task<int> GetFilteredCountAsync(AuditLogPaginationRequest request)
        {
            var query = _context.AuditLogs.AsQueryable();
            query = ApplyFilters(query, request);
            return await query.CountAsync();
        }

        public async Task<int> GetTotalUsedApplicationsAsync()
        {
            return await _context.AuditLogs.CountAsync(a => a.EntityType == "Authentication");
        }

        // Helper method to apply filters
        private IQueryable<AuditLog> ApplyFilters(IQueryable<AuditLog> query, AuditLogPaginationRequest request)
        {
            // Filter by UserName
            if (!string.IsNullOrWhiteSpace(request.UserName))
            {
                query = query.Where(a => a.UserName.Contains(request.UserName));
            }

            // Filter by Action
            if (!string.IsNullOrWhiteSpace(request.Action))
            {
                query = query.Where(a => a.Action.Contains(request.Action));
            }

            // Filter by EntityType
            if (!string.IsNullOrWhiteSpace(request.EntityType))
            {
                query = query.Where(a => a.EntityType == request.EntityType);
            }

            // Filter by IsSuccess
            if (request.IsSuccess.HasValue)
            {
                query = query.Where(a => a.IsSuccess == request.IsSuccess.Value);
            }

            // Filter by Date Range
            if (request.FromDate.HasValue)
            {
                query = query.Where(a => a.CreatedAt >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                // Include the entire end date (23:59:59)
                var endDate = request.ToDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(a => a.CreatedAt <= endDate);
            }

            return query;
        }
    }
}

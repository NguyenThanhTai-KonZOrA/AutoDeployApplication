using ClientLauncher.Implement.ApplicationDbContext;
using ClientLauncher.Implement.EntityModels;
using ClientLauncher.Implement.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace ClientLauncher.Implement.Repositories
{
    public class ClientMachineRepository : GenericRepository<ClientMachine>, IClientMachineRepository
    {
        public ClientMachineRepository(DeploymentManagerDbContext context) : base(context)
        {
        }

        public async Task<ClientMachine?> GetByMachineIdAsync(string machineId)
        {
            return await _dbSet
                .Include(c => c.DeploymentTasks)
                .FirstOrDefaultAsync(c => c.MachineId == machineId);
        }

        public async Task<IEnumerable<ClientMachine>> GetOnlineMachinesAsync(int heartbeatThresholdMinutes = 2)
        {
            var thresholdTime = DateTime.UtcNow.AddMinutes(-heartbeatThresholdMinutes);

            return await _dbSet
                .Where(c => c.LastHeartbeat != null && c.LastHeartbeat >= thresholdTime)
                .OrderByDescending(c => c.LastHeartbeat)
                .ToListAsync();
        }

        public async Task<IEnumerable<ClientMachine>> GetByStatusAsync(string status)
        {
            return await _dbSet
                .Where(c => c.Status == status)
                .OrderByDescending(c => c.LastHeartbeat)
                .ToListAsync();
        }

        public async Task<IEnumerable<ClientMachine>> GetByUserNameAsync(string userName)
        {
            return await _dbSet
                .Where(c => c.UserName.ToLower() == userName.ToLower())
                .OrderByDescending(c => c.LastHeartbeat)
                .ToListAsync();
        }

        public async Task<IEnumerable<ClientMachine>> GetMachinesWithAppInstalledAsync(string appCode)
        {
            return await _dbSet
                .Where(c => c.InstalledApplications != null &&
                           c.InstalledApplications.Contains($"\"{appCode}\""))
                .ToListAsync();
        }

        public async Task<bool> UpdateHeartbeatAsync(string machineId)
        {
            var machine = await _dbSet.FirstOrDefaultAsync(c => c.MachineId == machineId);
            if (machine == null)
                return false;

            machine.LastHeartbeat = DateTime.UtcNow;
            machine.Status = "Online";
            machine.UpdatedAt = DateTime.UtcNow;

            _context.Update(machine);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<int> MarkOfflineMachinesAsync(int heartbeatThresholdMinutes = 2)
        {
            var thresholdTime = DateTime.UtcNow.AddMinutes(-heartbeatThresholdMinutes);

            var offlineMachines = await _dbSet
                .Where(c => c.Status == "Online" &&
                           (c.LastHeartbeat == null || c.LastHeartbeat < thresholdTime))
                .ToListAsync();

            foreach (var machine in offlineMachines)
            {
                machine.Status = "Offline";
                machine.UpdatedAt = DateTime.UtcNow;
            }

            if (offlineMachines.Any())
            {
                _context.UpdateRange(offlineMachines);
                await _context.SaveChangesAsync();
            }

            return offlineMachines.Count;
        }

        public async Task<ClientMachineStatistics> GetStatisticsAsync()
        {
            var thresholdTime = DateTime.UtcNow.AddMinutes(-2);

            var machines = await _dbSet.ToListAsync();

            return new ClientMachineStatistics
            {
                TotalMachines = machines.Count,
                OnlineMachines = machines.Count(m => m.LastHeartbeat != null && m.LastHeartbeat >= thresholdTime),
                OfflineMachines = machines.Count(m => m.LastHeartbeat == null || m.LastHeartbeat < thresholdTime),
                BusyMachines = machines.Count(m => m.Status == "Busy"),
                LastRegistration = machines.Any() ? machines.Max(m => m.RegisteredAt) : (DateTime?)null
            };
        }
    }
}

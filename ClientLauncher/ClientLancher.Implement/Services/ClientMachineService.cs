using ClientLauncher.Implement.Services.Interface;
using ClientLauncher.Implement.UnitOfWork;
using ClientLauncher.Implement.ViewModels.Request;
using ClientLauncher.Implement.ViewModels.Response;
using ClientLauncher.Implement.EntityModels;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ClientLauncher.Implement.Services
{
    public class ClientMachineService : IClientMachineService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ClientMachineService> _logger;

        public ClientMachineService(IUnitOfWork unitOfWork, ILogger<ClientMachineService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ClientMachineResponse> RegisterMachineAsync(ClientMachineRegisterRequest request)
        {
            try
            {
                _logger.LogInformation("Registering machine: {MachineId} - {MachineName}", 
                    request.MachineId, request.MachineName);

                var existingMachine = await _unitOfWork.ClientMachines.GetByMachineIdAsync(request.MachineId);

                if (existingMachine != null)
                {
                    // Update existing machine
                    existingMachine.MachineName = request.MachineName;
                    existingMachine.ComputerName = request.ComputerName;
                    existingMachine.UserName = request.UserName;
                    existingMachine.DomainName = request.DomainName;
                    existingMachine.IPAddress = request.IPAddress;
                    existingMachine.MACAddress = request.MACAddress;
                    existingMachine.OSVersion = request.OSVersion;
                    existingMachine.OSArchitecture = request.OSArchitecture;
                    existingMachine.CPUInfo = request.CPUInfo;
                    existingMachine.TotalMemoryMB = request.TotalMemoryMB;
                    existingMachine.AvailableDiskSpaceGB = request.AvailableDiskSpaceGB;
                    existingMachine.InstalledApplications = request.InstalledApplications != null 
                        ? JsonSerializer.Serialize(request.InstalledApplications) 
                        : null;
                    existingMachine.ClientVersion = request.ClientVersion;
                    existingMachine.Location = request.Location;
                    existingMachine.Status = "Online";
                    existingMachine.LastHeartbeat = DateTime.UtcNow;
                    existingMachine.UpdatedAt = DateTime.UtcNow;

                    _unitOfWork.ClientMachines.Update(existingMachine);
                    await _unitOfWork.SaveChangesAsync();

                    _logger.LogInformation("Machine updated: {MachineId}", request.MachineId);
                    return MapToResponse(existingMachine);
                }
                else
                {
                    // Register new machine
                    var newMachine = new ClientMachine
                    {
                        MachineId = request.MachineId,
                        MachineName = request.MachineName,
                        ComputerName = request.ComputerName,
                        UserName = request.UserName,
                        DomainName = request.DomainName,
                        IPAddress = request.IPAddress,
                        MACAddress = request.MACAddress,
                        OSVersion = request.OSVersion,
                        OSArchitecture = request.OSArchitecture,
                        CPUInfo = request.CPUInfo,
                        TotalMemoryMB = request.TotalMemoryMB,
                        AvailableDiskSpaceGB = request.AvailableDiskSpaceGB,
                        InstalledApplications = request.InstalledApplications != null 
                            ? JsonSerializer.Serialize(request.InstalledApplications) 
                            : null,
                        ClientVersion = request.ClientVersion,
                        Location = request.Location,
                        Status = "Online",
                        LastHeartbeat = DateTime.UtcNow,
                        RegisteredAt = DateTime.UtcNow
                    };

                    await _unitOfWork.ClientMachines.AddAsync(newMachine);
                    await _unitOfWork.SaveChangesAsync();

                    _logger.LogInformation("New machine registered: {MachineId}", request.MachineId);
                    return MapToResponse(newMachine);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering machine: {MachineId}", request.MachineId);
                throw;
            }
        }

        public async Task<bool> UpdateHeartbeatAsync(ClientMachineHeartbeatRequest request)
        {
            try
            {
                var machine = await _unitOfWork.ClientMachines.GetByMachineIdAsync(request.MachineId);
                if (machine == null)
                {
                    _logger.LogWarning("Machine not found for heartbeat: {MachineId}", request.MachineId);
                    return false;
                }

                machine.Status = request.Status;
                machine.LastHeartbeat = DateTime.UtcNow;
                machine.AvailableDiskSpaceGB = request.AvailableDiskSpaceGB ?? machine.AvailableDiskSpaceGB;
                
                if (request.InstalledApplications != null)
                {
                    machine.InstalledApplications = JsonSerializer.Serialize(request.InstalledApplications);
                }

                machine.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.ClientMachines.Update(machine);
                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating heartbeat: {MachineId}", request.MachineId);
                return false;
            }
        }

        public async Task<IEnumerable<ClientMachineResponse>> GetOnlineMachinesAsync()
        {
            try
            {
                var machines = await _unitOfWork.ClientMachines.GetOnlineMachinesAsync();
                return machines.Select(MapToResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting online machines");
                throw;
            }
        }

        public async Task<ClientMachineResponse?> GetMachineByIdAsync(int id)
        {
            try
            {
                var machine = await _unitOfWork.ClientMachines.GetByIdAsync(id);
                return machine != null ? MapToResponse(machine) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting machine by ID: {Id}", id);
                throw;
            }
        }

        public async Task<ClientMachineResponse?> GetMachineByMachineIdAsync(string machineId)
        {
            try
            {
                var machine = await _unitOfWork.ClientMachines.GetByMachineIdAsync(machineId);
                return machine != null ? MapToResponse(machine) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting machine by MachineId: {MachineId}", machineId);
                throw;
            }
        }

        public async Task<IEnumerable<ClientMachineResponse>> GetAllMachinesAsync()
        {
            try
            {
                var machines = await _unitOfWork.ClientMachines.GetAllAsync();
                return machines.Select(MapToResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all machines");
                throw;
            }
        }

        public async Task<IEnumerable<ClientMachineResponse>> GetMachinesWithAppAsync(string appCode)
        {
            try
            {
                var machines = await _unitOfWork.ClientMachines.GetMachinesWithAppInstalledAsync(appCode);
                return machines.Select(MapToResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting machines with app: {AppCode}", appCode);
                throw;
            }
        }

        public async Task<int> MarkOfflineMachinesAsync()
        {
            try
            {
                return await _unitOfWork.ClientMachines.MarkOfflineMachinesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking offline machines");
                throw;
            }
        }

        public async Task<object> GetStatisticsAsync()
        {
            try
            {
                return await _unitOfWork.ClientMachines.GetStatisticsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting statistics");
                throw;
            }
        }

        private ClientMachineResponse MapToResponse(ClientMachine machine)
        {
            List<string>? installedApps = null;
            if (!string.IsNullOrEmpty(machine.InstalledApplications))
            {
                try
                {
                    installedApps = JsonSerializer.Deserialize<List<string>>(machine.InstalledApplications);
                }
                catch { }
            }

            return new ClientMachineResponse
            {
                Id = machine.Id,
                MachineId = machine.MachineId,
                MachineName = machine.MachineName,
                ComputerName = machine.ComputerName,
                UserName = machine.UserName,
                DomainName = machine.DomainName,
                IPAddress = machine.IPAddress,
                MACAddress = machine.MACAddress,
                OSVersion = machine.OSVersion,
                OSArchitecture = machine.OSArchitecture,
                Status = machine.Status,
                LastHeartbeat = machine.LastHeartbeat,
                RegisteredAt = machine.RegisteredAt,
                InstalledApplications = installedApps,
                ClientVersion = machine.ClientVersion,
                Location = machine.Location,
                PendingTasksCount = machine.DeploymentTasks?.Count(t => t.Status == "Queued" || t.Status == "InProgress") ?? 0
            };
        }
    }
}

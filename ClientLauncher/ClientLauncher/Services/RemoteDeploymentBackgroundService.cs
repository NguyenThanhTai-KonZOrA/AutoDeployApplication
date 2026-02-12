using ClientLauncher.Services.Interface;
using NLog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ClientLauncher.Services
{
    public class RemoteDeploymentBackgroundService : IDisposable
    {
        private readonly IClientRegistrationService _registrationService;
        private readonly IDeploymentPollingService _pollingService;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private Timer? _heartbeatTimer;
        private Timer? _pollingTimer;
        private bool _isRegistered;
        private bool _isDisposed;

        // Configuration
        private readonly int _heartbeatIntervalSeconds = 30; // 30 seconds
        private readonly int _pollingIntervalSeconds = 30; // 30 seconds
        private bool _isProcessingTasks = false;

        public RemoteDeploymentBackgroundService(
            IClientRegistrationService registrationService,
            IDeploymentPollingService pollingService)
        {
            _registrationService = registrationService;
            _pollingService = pollingService;
        }

        public async Task StartAsync()
        {
            try
            {
                Logger.Info("Starting Remote Deployment Background Service...");

                // Initial registration
                _isRegistered = await _registrationService.RegisterMachineAsync();

                if (_isRegistered)
                {
                    Logger.Info("Machine registered successfully");

                    // Start heartbeat timer
                    _heartbeatTimer = new Timer(
                        async _ => await SendHeartbeat(),
                        null,
                        TimeSpan.Zero,
                        TimeSpan.FromSeconds(_heartbeatIntervalSeconds));

                    // Start polling timer
                    _pollingTimer = new Timer(
                        async _ => await PollForTasks(),
                        null,
                        TimeSpan.FromSeconds(5), // Start after 5 seconds
                        TimeSpan.FromSeconds(_pollingIntervalSeconds));

                    Logger.Info("Background timers started: Heartbeat={0}s, Polling={1}s", 
                        _heartbeatIntervalSeconds, _pollingIntervalSeconds);
                }
                else
                {
                    Logger.Warn("Failed to register machine, will retry...");
                    
                    // Retry registration after 60 seconds
                    _heartbeatTimer = new Timer(
                        async _ => await RetryRegistration(),
                        null,
                        TimeSpan.FromSeconds(60),
                        TimeSpan.FromSeconds(60));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error starting background service");
            }
        }

        private async Task SendHeartbeat()
        {
            try
            {
                if (!_isRegistered)
                    return;

                await _registrationService.SendHeartbeatAsync();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error sending heartbeat");
            }
        }

        private async Task PollForTasks()
        {
            try
            {
                if (!_isRegistered || _isProcessingTasks)
                    return;

                _isProcessingTasks = true;

                Logger.Debug("Polling for deployment tasks...");
                await _pollingService.ProcessPendingTasksAsync();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error polling for tasks");
            }
            finally
            {
                _isProcessingTasks = false;
            }
        }

        private async Task RetryRegistration()
        {
            try
            {
                Logger.Info("Retrying machine registration...");
                _isRegistered = await _registrationService.RegisterMachineAsync();

                if (_isRegistered)
                {
                    Logger.Info("Machine registration successful on retry");
                    
                    // Stop retry timer and start normal timers
                    _heartbeatTimer?.Dispose();
                    await StartAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error retrying registration");
            }
        }

        public void Stop()
        {
            Logger.Info("Stopping Remote Deployment Background Service...");
            
            _heartbeatTimer?.Dispose();
            _pollingTimer?.Dispose();
            
            _heartbeatTimer = null;
            _pollingTimer = null;

            Logger.Info("Background service stopped");
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                Stop();
                _isDisposed = true;
            }
        }
    }
}

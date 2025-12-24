using ClientLauncher.Helpers;
using ClientLauncher.Models;
using ClientLauncher.Services;
using ClientLauncher.Services.Interface;
using NLog;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

namespace ClientLauncher.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IApiService _apiService;
        private readonly IShortcutService _shortcutService;
        private readonly IManifestService _manifestService; // ✅ Add
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        // Properties
        private ObservableCollection<ApplicationDto> _applications = new();
        private DispatcherTimer _clockTimer;
        public ObservableCollection<ApplicationDto> Applications
        {
            get => _applications;
            set => SetProperty(ref _applications, value);
        }

        private ApplicationDto? _selectedApplication;
        public ApplicationDto? SelectedApplication
        {
            get => _selectedApplication;
            set
            {
                SetProperty(ref _selectedApplication, value);
                // Update command can execute state
                InstallCommand?.RaiseCanExecuteChanged();
                UninstallCommand?.RaiseCanExecuteChanged();
            }
        }

        private int _currentStep = 1;
        public int CurrentStep
        {
            get => _currentStep;
            set => SetProperty(ref _currentStep, value);
        }

        private bool _isProcessing;
        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private double _progressValue;
        public double ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        private string _installationResult = string.Empty;
        public string InstallationResult
        {
            get => _installationResult;
            set => SetProperty(ref _installationResult, value);
        }

        private bool _installationSuccess;
        public bool InstallationSuccess
        {
            get => _installationSuccess;
            set => SetProperty(ref _installationSuccess, value);
        }

        private string _currentTime;
        private string _currentDate;
        public string CurrentTime
        {
            get => _currentTime;
            set { _currentTime = value; OnPropertyChanged(); }
        }

        public string CurrentDate
        {
            get => DateTime.Now.ToString("dddd, dd MMMM yyyy");
            set { _currentDate = value; OnPropertyChanged(); }
        }

        private void InitializeClockTimer()
        {
            try
            {
                _clockTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1)
                };

                _clockTimer.Tick += (s, e) =>
                {
                    CurrentTime = DateTime.Now.ToString("HH:mm:ss");
                };

                _clockTimer.Start();
                CurrentTime = DateTime.Now.ToString("HH:mm:ss");
            }
            catch (Exception ex)
            {
            }
        }

        // Visibility for Steps
        public bool IsStep1Visible => CurrentStep == 1;
        public bool IsStep2Visible => CurrentStep == 2;
        public bool IsStep3Visible => CurrentStep == 3;

        // Commands
        public AsyncRelayCommand LoadApplicationsCommand { get; }
        public AsyncRelayCommand InstallCommand { get; }
        public AsyncRelayCommand UninstallCommand { get; } 
        public RelayCommand BackToListCommand { get; }

        public MainViewModel()
        {
            _apiService = new ApiService();
            _shortcutService = new ShortcutService();
            _manifestService = new ManifestService(); // ✅ Initialize

            LoadApplicationsCommand = new AsyncRelayCommand(async _ => await LoadApplicationsAsync());
            InstallCommand = new AsyncRelayCommand(
                async _ => await InstallApplicationAsync(),
                _ => SelectedApplication != null && !IsProcessing
            );
            BackToListCommand = new RelayCommand(_ => BackToList());

            _ = LoadApplicationsAsync();
            InitializeClockTimer();
        }

        /// <summary>
        /// UPDATED: Load applications with version check
        /// </summary>
        private async Task LoadApplicationsAsync()
        {
            try
            {
                _logger.Info("Loading applications list");
                StatusMessage = "Loading applications...";
                IsProcessing = true;

                var apps = await _apiService.GetAllApplicationsAsync();

                foreach (var app in apps)
                {
                    _logger.Debug("Checking status for application: {AppCode}", app.AppCode);

                    app.IsInstalled = await _apiService.IsApplicationInstalledAsync(app.AppCode);

                    if (app.IsInstalled)
                    {
                        app.InstalledVersion = await _apiService.GetInstalledVersionAsync(app.AppCode);
                        var serverVersionInfo = await _apiService.GetServerVersionAsync(app.AppCode);

                        if (serverVersionInfo != null)
                        {
                            app.ServerVersion = serverVersionInfo.BinaryVersion;

                            if (!string.IsNullOrEmpty(app.InstalledVersion) &&
                                !string.IsNullOrEmpty(app.ServerVersion))
                            {
                                app.HasUpdate = IsNewerVersion(app.ServerVersion, app.InstalledVersion);

                                if (app.HasUpdate)
                                {
                                    _logger.Info("Update available for {AppCode}: {InstalledVersion} -> {ServerVersion}",
                                        app.AppCode, app.InstalledVersion, app.ServerVersion);
                                }
                            }
                        }

                        app.StatusText = app.HasUpdate
                            ? $"📦 Installed v{app.InstalledVersion} → 🆕 v{app.ServerVersion} available"
                            : $"Installed v{app.InstalledVersion}";
                    }
                    else
                    {
                        app.StatusText = "❌ Not Installed";
                        var serverVersionInfo = await _apiService.GetServerVersionAsync(app.AppCode);
                        if (serverVersionInfo != null)
                        {
                            app.ServerVersion = serverVersionInfo.BinaryVersion;
                        }
                    }
                }

                Applications = new ObservableCollection<ApplicationDto>(apps);
                StatusMessage = $"Loaded {apps.Count} applications";
                _logger.Info("Successfully loaded {Count} applications", apps.Count);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to load applications");
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show($"Failed to load applications: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// Helper method to compare versions
        /// </summary>
        private bool IsNewerVersion(string serverVersion, string localVersion)
        {
            try
            {
                var server = new Version(serverVersion);
                var local = new Version(localVersion);
                return server > local;
            }
            catch
            {
                return serverVersion != localVersion;
            }
        }

        /// <summary>
        /// ✅ NEW INSTALL FLOW: Download manifest → Create shortcut (NO package download yet)
        /// </summary>
        private async Task InstallApplicationAsync()
        {
            if (SelectedApplication == null) return;

            try
            {
                _logger.Info($"Starting installation setup for {SelectedApplication.Name} ({SelectedApplication.AppCode})");

                CurrentStep = 2;
                OnPropertyChanged(nameof(IsStep1Visible));
                OnPropertyChanged(nameof(IsStep2Visible));
                OnPropertyChanged(nameof(IsStep3Visible));

                IsProcessing = true;
                ProgressValue = 0;
                StatusMessage = "Preparing installation...";

                await Task.Delay(500);
                ProgressValue = 20;

                // ✅ STEP 1: Check if manifest exists locally
                StatusMessage = "Checking manifest...";
                var localManifest = await _manifestService.GetLocalManifestAsync(SelectedApplication.AppCode);

                if (localManifest == null)
                {
                    _logger.Info($"No local manifest found, downloading from server for {SelectedApplication.AppCode}");
                    
                    ProgressValue = 40;
                    StatusMessage = "Downloading manifest from server...";

                    // Download manifest from server
                    var serverManifest = await _manifestService.DownloadManifestFromServerAsync(SelectedApplication.AppCode);

                    if (serverManifest == null)
                    {
                        throw new Exception("Failed to download manifest from server");
                    }

                    // Save manifest locally
                    await _manifestService.SaveManifestAsync(SelectedApplication.AppCode, serverManifest);
                    localManifest = serverManifest;

                    _logger.Info($"Manifest downloaded and saved for {SelectedApplication.AppCode}");
                }
                else
                {
                    _logger.Info($"Using existing local manifest for {SelectedApplication.AppCode}");
                }

                ProgressValue = 60;

                // ✅ STEP 2: Create desktop shortcut
                StatusMessage = "Creating desktop shortcut...";
                
                var launcherPath = Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe");
                var iconPath = GetIconPathForCategory(SelectedApplication.Category);
                
                var shortcutCreated = _shortcutService.CreateDesktopShortcut(
                    SelectedApplication.AppCode,
                    SelectedApplication.Name,
                    launcherPath,
                    iconPath
                );

                if (!shortcutCreated)
                {
                    _logger.Warn("Failed to create desktop shortcut");
                    throw new Exception("Failed to create desktop shortcut");
                }

                _logger.Info($"Desktop shortcut created for {SelectedApplication.AppCode}");

                ProgressValue = 90;
                StatusMessage = "Finalizing...";
                await Task.Delay(300);

                ProgressValue = 100;

                CurrentStep = 3;
                OnPropertyChanged(nameof(IsStep1Visible));
                OnPropertyChanged(nameof(IsStep2Visible));
                OnPropertyChanged(nameof(IsStep3Visible));

                InstallationSuccess = true;
                InstallationResult = $"✓ {SelectedApplication.Name} setup completed!\n\n" +
                      $"Version: {localManifest.Binary?.Version}\n" +
                      $"📦 The application package will be downloaded when you first run it.\n\n" +
                      $"✅ Desktop shortcut created!\n\n" +
                      $"Click the desktop icon to download and launch the application.";

                _logger.Info($"Installation setup completed for {SelectedApplication.AppCode}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Installation setup failed");

                CurrentStep = 3;
                OnPropertyChanged(nameof(IsStep1Visible));
                OnPropertyChanged(nameof(IsStep2Visible));
                OnPropertyChanged(nameof(IsStep3Visible));

                InstallationSuccess = false;
                InstallationResult = $"✗ Installation setup failed\n\n{ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// Get icon path based on category
        /// </summary>
        private string? GetIconPathForCategory(string category)
        {
            var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            
            var iconMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Cage", "Assets\\Icons\\app_cage.ico" },
                { "HTR", "Assets\\Icons\\app_htr.ico" },
                { "Finance", "Assets\\Icons\\app_finance.ico" }
            };

            if (iconMap.TryGetValue(category, out var iconPath))
            {
                var fullPath = Path.Combine(basePath ?? string.Empty, iconPath);
                return File.Exists(fullPath) ? fullPath : null;
            }

            return null;
        }

        /// <summary>
        /// Uninstall application
        /// </summary>
        private async Task UninstallApplicationAsync()
        {
            if (SelectedApplication == null || !SelectedApplication.IsInstalled) return;

            var result = MessageBox.Show(
                $"Are you sure you want to uninstall '{SelectedApplication.Name}'?",
                "Confirm Uninstall",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                CurrentStep = 2;
                OnPropertyChanged(nameof(IsStep1Visible));
                OnPropertyChanged(nameof(IsStep2Visible));
                OnPropertyChanged(nameof(IsStep3Visible));

                IsProcessing = true;
                ProgressValue = 0;
                StatusMessage = "Preparing uninstallation...";

                await Task.Delay(500);
                ProgressValue = 30;

                StatusMessage = "Removing application files...";
                var userName = Environment.UserName;
                var uninstallResult = await _apiService.UninstallApplicationAsync(
                    SelectedApplication.AppCode,
                    userName
                );

                ProgressValue = 80;

                // Remove desktop shortcut
                if (uninstallResult.Success)
                {
                    StatusMessage = "Removing desktop shortcut...";
                    _shortcutService.RemoveDesktopShortcut(SelectedApplication.Name);
                    ProgressValue = 90;
                }

                StatusMessage = "Finalizing...";
                ProgressValue = 100;

                CurrentStep = 3;
                OnPropertyChanged(nameof(IsStep1Visible));
                OnPropertyChanged(nameof(IsStep2Visible));
                OnPropertyChanged(nameof(IsStep3Visible));

                InstallationSuccess = uninstallResult.Success;
                InstallationResult = uninstallResult.Success
                    ? $"✓ {SelectedApplication.Name} uninstalled successfully!\n\n" +
                      $"Uninstalled by: {userName}"
                    : $"✗ Uninstallation failed\n\n{uninstallResult.Message}\n{uninstallResult.ErrorDetails}";
            }
            catch (Exception ex)
            {
                CurrentStep = 3;
                OnPropertyChanged(nameof(IsStep1Visible));
                OnPropertyChanged(nameof(IsStep2Visible));
                OnPropertyChanged(nameof(IsStep3Visible));

                InstallationSuccess = false;
                InstallationResult = $"✗ Uninstallation failed\n\n{ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void BackToList()
        {
            CurrentStep = 1;
            SelectedApplication = null;
            StatusMessage = string.Empty;
            ProgressValue = 0;
            InstallationResult = string.Empty;

            OnPropertyChanged(nameof(IsStep1Visible));
            OnPropertyChanged(nameof(IsStep2Visible));
            OnPropertyChanged(nameof(IsStep3Visible));

            // Reload applications
            _ = LoadApplicationsAsync();
        }
    }
}
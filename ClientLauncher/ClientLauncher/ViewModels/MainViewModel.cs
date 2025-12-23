using ClientLauncher.Helpers;
using ClientLauncher.Models;
using ClientLauncher.Services;
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
        public AsyncRelayCommand UninstallCommand { get; } // ✅ NEW
        public RelayCommand BackToListCommand { get; }

        public MainViewModel()
        {
            _apiService = new ApiService();
            _shortcutService = new ShortcutService();

            LoadApplicationsCommand = new AsyncRelayCommand(async _ => await LoadApplicationsAsync());
            InstallCommand = new AsyncRelayCommand(
                async _ => await InstallApplicationAsync(),
                _ => SelectedApplication != null && !IsProcessing
            );
            UninstallCommand = new AsyncRelayCommand( // ✅ NEW
                async _ => await UninstallApplicationAsync(),
                _ => SelectedApplication != null && SelectedApplication.IsInstalled && !IsProcessing
            );
            BackToListCommand = new RelayCommand(_ => BackToList());

            // Load applications on startup
            _ = LoadApplicationsAsync();
            InitializeClockTimer();
        }

        /// <summary>
        /// ✅ UPDATED: Load applications with version check
        /// </summary>
        private async Task LoadApplicationsAsync()
        {
            try
            {
                StatusMessage = "Loading applications...";
                IsProcessing = true;

                var apps = await _apiService.GetAllApplicationsAsync();

                // ✅ Check installation status and versions for each app
                foreach (var app in apps)
                {
                    // Check if installed
                    app.IsInstalled = await _apiService.IsApplicationInstalledAsync(app.AppCode);

                    if (app.IsInstalled)
                    {
                        // Get installed version
                        app.InstalledVersion = await _apiService.GetInstalledVersionAsync(app.AppCode);

                        // Get server version
                        var serverVersionInfo = await _apiService.GetServerVersionAsync(app.AppCode);
                        if (serverVersionInfo != null)
                        {
                            app.ServerVersion = serverVersionInfo.BinaryVersion;

                            // Check if update available
                            if (!string.IsNullOrEmpty(app.InstalledVersion) &&
                                !string.IsNullOrEmpty(app.ServerVersion))
                            {
                                app.HasUpdate = IsNewerVersion(app.ServerVersion, app.InstalledVersion);
                            }
                        }

                        // Set status text
                        if (app.HasUpdate)
                        {
                            app.StatusText = $"📦 Installed v{app.InstalledVersion} → 🆕 v{app.ServerVersion} available";
                        }
                        else
                        {
                            app.StatusText = $"✅ Installed v{app.InstalledVersion}";
                        }
                    }
                    else
                    {
                        app.StatusText = "❌ Not Installed";

                        // Still get server version for display
                        var serverVersionInfo = await _apiService.GetServerVersionAsync(app.AppCode);
                        if (serverVersionInfo != null)
                        {
                            app.ServerVersion = serverVersionInfo.BinaryVersion;
                        }
                    }
                }

                Applications = new ObservableCollection<ApplicationDto>(apps);
                StatusMessage = $"Loaded {apps.Count} applications";
            }
            catch (Exception ex)
            {
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
        /// ✅ Helper method to compare versions
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

        private async Task InstallApplicationAsync()
        {
            if (SelectedApplication == null) return;

            try
            {
                CurrentStep = 2;
                OnPropertyChanged(nameof(IsStep1Visible));
                OnPropertyChanged(nameof(IsStep2Visible));
                OnPropertyChanged(nameof(IsStep3Visible));

                IsProcessing = true;
                ProgressValue = 0;
                StatusMessage = "Preparing installation...";

                await Task.Delay(500);
                ProgressValue = 20;

                StatusMessage = "Downloading application...";
                await Task.Delay(1000);
                ProgressValue = 50;

                StatusMessage = "Installing...";
                var userName = Environment.UserName;
                var result = await _apiService.InstallApplicationAsync(
                    SelectedApplication.AppCode,
                    userName
                );

                ProgressValue = 70;

                // Create desktop shortcut if installation succeeded
                if (result.Success)
                {
                    StatusMessage = "Creating desktop shortcut...";
                    await Task.Delay(300);

                    var launcherPath = Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe");
                    var shortcutCreated = _shortcutService.CreateDesktopShortcut(
                        SelectedApplication.AppCode,
                        SelectedApplication.Name,
                        launcherPath
                    );

                    if (!shortcutCreated)
                    {
                        MessageBox.Show(
                            "Application installed successfully but failed to create desktop shortcut.",
                            "Warning",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                    }

                    ProgressValue = 90;
                }

                StatusMessage = "Finalizing...";
                ProgressValue = 100;

                CurrentStep = 3;
                OnPropertyChanged(nameof(IsStep1Visible));
                OnPropertyChanged(nameof(IsStep2Visible));
                OnPropertyChanged(nameof(IsStep3Visible));

                InstallationSuccess = result.Success;
                InstallationResult = result.Success
                    ? $"✓ {SelectedApplication.Name} installed successfully!\n\n" +
                      $"Version: {result.InstalledVersion}\n" +
                      $"Installed by: {userName}\n\n" +
                      $"✅ Desktop shortcut created successfully!"
                    : $"✗ Installation failed\n\n{result.Message}\n{result.ErrorDetails}";
            }
            catch (Exception ex)
            {
                CurrentStep = 3;
                OnPropertyChanged(nameof(IsStep1Visible));
                OnPropertyChanged(nameof(IsStep2Visible));
                OnPropertyChanged(nameof(IsStep3Visible));

                InstallationSuccess = false;
                InstallationResult = $"✗ Installation failed\n\n{ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// ✅ NEW: Uninstall application
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
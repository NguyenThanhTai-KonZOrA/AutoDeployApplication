using ClientLauncher.Helpers;
using ClientLauncher.Models;
using ClientLauncher.Services;
using ClientLauncher.Services.Interface;
using NLog;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;

namespace ClientLauncher.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IApiService _apiService;
        private readonly IShortcutService _shortcutService;
        private readonly IManifestService _manifestService;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        // Properties
        private ObservableCollection<ApplicationDto> _applications = new();
        private ObservableCollection<ApplicationDto> _allApplications = new(); // Store all apps for filtering
        private DispatcherTimer? _clockTimer;

        public ObservableCollection<ApplicationDto> Applications
        {
            get => _applications;
            set => SetProperty(ref _applications, value);
        }

        private ICollectionView? _applicationsView;
        public ICollectionView? ApplicationsView
        {
            get => _applicationsView;
            set => SetProperty(ref _applicationsView, value);
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

        private string _currentTime = string.Empty;
        private string _currentDate = string.Empty;
        private string _currentVersion;
        public string CurrentTime
        {
            get => _currentTime;
            set { _currentTime = value; OnPropertyChanged(); }
        }
        public string CurrentVersion
        {
            get => _currentVersion;
            set { _currentVersion = value; OnPropertyChanged(); }
        }
        public string CurrentDate
        {
            get => DateTime.Now.ToString("dddd, dd MMMM yyyy");
            set { _currentDate = value; OnPropertyChanged(); }
        }

        // NEW: Search and filter properties
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilter();
                }
            }
        }

        private string _selectedSortOption = "Name";
        public string SelectedSortOption
        {
            get => _selectedSortOption;
            set
            {
                if (SetProperty(ref _selectedSortOption, value))
                {
                    ApplySorting();
                }
            }
        }

        public List<string> SortOptions { get; } = new List<string> { "Name", "Category", "Status" };

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
                _logger.Error(ex, "Failed to initialize clock timer");
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
        public RelayCommand SelectAllCommand { get; }
        public RelayCommand SelectNoneCommand { get; }
        public RelayCommand ClearSearchCommand { get; }

        public MainViewModel()
        {
            _apiService = new ApiService();
            _shortcutService = new ShortcutService();
            _manifestService = new ManifestService();
            CurrentVersion = $"Version: {ConfigurationManager.AppSettings["ApplicationVersion"]}";
            LoadApplicationsCommand = new AsyncRelayCommand(async _ => await LoadApplicationsAsync());
            InstallCommand = new AsyncRelayCommand(
                async _ => await InstallSelectedApplicationsAsync(),
                _ => HasSelectedApplications && !IsProcessing
            );

            UninstallCommand = new AsyncRelayCommand(
                async _ => await UninstallApplicationAsync(),
                _ => SelectedApplication != null && !IsProcessing && SelectedApplication.IsInstalled
            );

            BackToListCommand = new RelayCommand(_ => BackToList());
            SelectAllCommand = new RelayCommand(_ => SelectAll());
            SelectNoneCommand = new RelayCommand(_ => SelectNone());
            ClearSearchCommand = new RelayCommand(_ => ClearSearch());

            _ = LoadApplicationsAsync();
            InitializeClockTimer();
        }

        /// <summary>
        /// UPDATED: Load applications with LOCAL installation check
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

                    // Check LOCAL installation (no API call)
                    app.IsInstalled = await _apiService.IsApplicationInstalledAsync(app.AppCode);

                    if (app.IsInstalled)
                    {
                        // Get LOCAL installed version
                        app.InstalledVersion = await _apiService.GetInstalledVersionAsync(app.AppCode);

                        // Get server version to check for updates
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
                            ? $"Installed {app.InstalledVersion} → 🆕 {app.ServerVersion} available"
                            : $"Installed {app.InstalledVersion}";
                    }
                    else
                    {
                        app.StatusText = "❌ Not Installed";

                        // Still get server version for display
                        var serverVersionInfo = await _apiService.GetServerVersionAsync(app.AppCode);
                        if (serverVersionInfo != null)
                        {
                            app.ServerVersion = serverVersionInfo.BinaryVersion;
                            app.StatusText = $"❌ Not Installed (Latest: {app.ServerVersion})";
                        }
                    }

                    _logger.Debug("App {AppCode}: IsInstalled={IsInstalled}, Version={Version}, HasUpdate={HasUpdate}",
                        app.AppCode, app.IsInstalled, app.InstalledVersion, app.HasUpdate);
                }

                _allApplications = new ObservableCollection<ApplicationDto>(apps);
                Applications = new ObservableCollection<ApplicationDto>(apps);

                // Subscribe to selection changes
                foreach (var app in Applications)
                {
                    app.SelectionChanged += (s, e) =>
                    {
                        OnPropertyChanged(nameof(HasSelectedApplications));
                        InstallCommand?.RaiseCanExecuteChanged();
                    };
                }

                // Setup CollectionView for grouping and filtering
                ApplicationsView = CollectionViewSource.GetDefaultView(Applications);
                if (ApplicationsView != null)
                {
                    ApplicationsView.GroupDescriptions.Add(new PropertyGroupDescription("Category"));
                    ApplicationsView.Filter = FilterApplications;
                }

                ApplySorting();

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
        /// Filter applications based on search text
        /// </summary>
        private bool FilterApplications(object obj)
        {
            if (obj is not ApplicationDto app)
                return false;

            if (string.IsNullOrWhiteSpace(SearchText))
                return true;

            var searchLower = SearchText.ToLower();
            return app.Name.ToLower().Contains(searchLower) ||
                   app.AppCode.ToLower().Contains(searchLower) ||
                   app.Description.ToLower().Contains(searchLower) ||
                   app.Category.ToLower().Contains(searchLower);
        }

        /// <summary>
        /// Apply filter to collection view
        /// </summary>
        private void ApplyFilter()
        {
            ApplicationsView?.Refresh();

            var filteredCount = Applications.Count(FilterApplications);
            StatusMessage = string.IsNullOrWhiteSpace(SearchText)
                ? $"Loaded {Applications.Count} applications"
                : $"Found {filteredCount} of {Applications.Count} applications";
        }

        /// <summary>
        /// Apply sorting to collection view
        /// </summary>
        private void ApplySorting()
        {
            if (ApplicationsView == null) return;

            ApplicationsView.SortDescriptions.Clear();

            switch (SelectedSortOption)
            {
                case "Name":
                    ApplicationsView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
                    break;
                case "Category":
                    ApplicationsView.SortDescriptions.Add(new SortDescription("Category", ListSortDirection.Ascending));
                    ApplicationsView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
                    break;
                case "Status":
                    ApplicationsView.SortDescriptions.Add(new SortDescription("IsInstalled", ListSortDirection.Descending));
                    ApplicationsView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
                    break;
            }

            ApplicationsView.Refresh();
        }

        /// <summary>
        /// Select all applications
        /// </summary>
        private void SelectAll()
        {
            foreach (var app in Applications)
            {
                app.IsSelected = true;
            }
            _logger.Info("Selected all applications");
        }

        /// <summary>
        /// Deselect all applications
        /// </summary>
        private void SelectNone()
        {
            foreach (var app in Applications)
            {
                app.IsSelected = false;
            }
            _logger.Info("Deselected all applications");
        }

        /// <summary>
        /// Clear search text
        /// </summary>
        private void ClearSearch()
        {
            SearchText = string.Empty;
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
        /// Install multiple selected applications
        /// </summary>
        private async Task InstallSelectedApplicationsAsync()
        {
            var selectedApps = Applications.Where(a => a.IsSelected).ToList();
            if (!selectedApps.Any()) return;

            try
            {
                _logger.Info($"Starting installation for {selectedApps.Count} application(s)");

                CurrentStep = 2;
                OnPropertyChanged(nameof(IsStep1Visible));
                OnPropertyChanged(nameof(IsStep2Visible));
                OnPropertyChanged(nameof(IsStep3Visible));

                IsProcessing = true;
                var successCount = 0;
                var failedApps = new List<string>();

                for (int i = 0; i < selectedApps.Count; i++)
                {
                    var app = selectedApps[i];
                    ProgressValue = (i * 100.0 / selectedApps.Count);
                    StatusMessage = $"Installing {app.Name} ({i + 1}/{selectedApps.Count})...";

                    try
                    {
                        var localManifest = await _manifestService.GetLocalManifestAsync(app.AppCode);

                        if (localManifest == null)
                        {
                            var serverManifest = await _manifestService.DownloadManifestFromServerAsync(app.AppCode);
                            if (serverManifest == null)
                            {
                                throw new Exception("Failed to download manifest");
                            }
                            await _manifestService.SaveManifestAsync(app.AppCode, serverManifest);
                            localManifest = serverManifest;
                        }

                        var launcherPath = Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe");
                        var iconPath = GetIconPathForCategory(app.Category);

                        var shortcutCreated = _shortcutService.CreateDesktopShortcut(
                            app.AppCode,
                            app.Name,
                            launcherPath,
                            iconPath
                        );

                        if (shortcutCreated)
                        {
                            successCount++;
                            _logger.Info($"Successfully installed {app.Name}");
                        }
                        else
                        {
                            failedApps.Add(app.Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, $"Failed to install {app.Name}");
                        failedApps.Add(app.Name);
                    }
                }

                ProgressValue = 100;
                StatusMessage = "Installation completed";

                CurrentStep = 3;
                OnPropertyChanged(nameof(IsStep1Visible));
                OnPropertyChanged(nameof(IsStep2Visible));
                OnPropertyChanged(nameof(IsStep3Visible));

                InstallationSuccess = failedApps.Count == 0;

                if (InstallationSuccess)
                {
                    InstallationResult = $"✓ Successfully installed {successCount} application(s)!\n\n" +
                                         $"Applications:\n" +
                                         string.Join("\n", selectedApps.Select(a => $"  • {a.Name}")) +
                                         $"\n\nDesktop shortcuts created!";
                }
                else
                {
                    InstallationResult = $"⚠ Partially completed:\n\n" +
                                         $"✓ Success: {successCount}\n" +
                                         $"✗ Failed: {failedApps.Count}\n\n" +
                                         $"Failed applications:\n" +
                                         string.Join("\n", failedApps.Select(f => $"  • {f}"));
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Batch installation failed");

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
            var stopwatch = Stopwatch.StartNew();
            if (SelectedApplication == null || !SelectedApplication.IsInstalled) return;

            var result = MessageBox.Show(
                $"Are you sure you want to uninstall '{SelectedApplication.Name}'?\n\n" +
                $"This will remove all application files from C:\\CompanyApps\\{SelectedApplication.AppCode}",
                "Confirm Uninstall",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                _logger.Info("Starting uninstallation for {AppCode}", SelectedApplication.AppCode);

                CurrentStep = 2;
                OnPropertyChanged(nameof(IsStep1Visible));
                OnPropertyChanged(nameof(IsStep2Visible));
                OnPropertyChanged(nameof(IsStep3Visible));

                IsProcessing = true;
                ProgressValue = 0;
                StatusMessage = "Preparing uninstallation...";
                await Task.Delay(500);

                ProgressValue = 20;
                StatusMessage = "Removing application files from C:\\CompanyApps...";
                await Task.Delay(300);

                // Use InstallationService to uninstall
                var installationService = new InstallationService();
                var uninstallResult = await installationService.UninstallApplicationAsync(
                    SelectedApplication.AppCode,
                    Environment.UserName);

                ProgressValue = 70;

                if (uninstallResult.Success)
                {
                    StatusMessage = "Removing desktop shortcut...";
                    _shortcutService.RemoveDesktopShortcut(SelectedApplication.Name);
                    ProgressValue = 90;
                    await Task.Delay(300);

                    _logger.Info("Successfully uninstalled {AppCode}", SelectedApplication.AppCode);
                }

                StatusMessage = "Finalizing...";
                ProgressValue = 100;
                await Task.Delay(300);

                CurrentStep = 3;
                OnPropertyChanged(nameof(IsStep1Visible));
                OnPropertyChanged(nameof(IsStep2Visible));
                OnPropertyChanged(nameof(IsStep3Visible));

                // Write Log
                stopwatch.Stop();
                await _apiService.NotifyInstallationAsync(SelectedApplication.AppCode, SelectedApplication.InstalledVersion, true, stopwatch.Elapsed, null, SelectedApplication.InstalledVersion, "Uninstall");

                InstallationSuccess = uninstallResult.Success;
                StatusMessage = "Uninstallation completed";
                InstallationResult = uninstallResult.Success
                    ? $"✓ {SelectedApplication.Name} uninstalled successfully!\n\n" +
                      $"All files removed from C:\\CompanyApps\\{SelectedApplication.AppCode}\n" +
                      $"Desktop shortcut removed\n\n" +
                      $"Uninstalled by: {Environment.UserName}"
                    : $"✗ Uninstallation failed\n\n{uninstallResult.Message}\n{uninstallResult.ErrorDetails}";
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Uninstallation failed for {AppCode}", SelectedApplication.AppCode);

                CurrentStep = 3;
                OnPropertyChanged(nameof(IsStep1Visible));
                OnPropertyChanged(nameof(IsStep2Visible));
                OnPropertyChanged(nameof(IsStep3Visible));

                InstallationSuccess = false;
                InstallationResult = $"✗ Uninstallation failed\n\n{ex.Message}";
                // Write Log
                stopwatch.Stop();
                await _apiService.NotifyInstallationAsync(SelectedApplication.AppCode, SelectedApplication.InstalledVersion, false, stopwatch.Elapsed, null, SelectedApplication.InstalledVersion, "Uninstall");
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

            // Clear all selections
            foreach (var app in Applications)
            {
                app.IsSelected = false;
            }

            OnPropertyChanged(nameof(IsStep1Visible));
            OnPropertyChanged(nameof(IsStep2Visible));
            OnPropertyChanged(nameof(IsStep3Visible));
            OnPropertyChanged(nameof(HasSelectedApplications));

            // Reload applications
            _ = LoadApplicationsAsync();
        }

        public bool HasSelectedApplications => Applications?.Any(a => a.IsSelected) ?? false;
    }
}
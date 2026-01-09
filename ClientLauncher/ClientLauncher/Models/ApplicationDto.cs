using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ClientLauncher.Models
{
    public class ApplicationDto : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string AppCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Properties for version tracking
        public bool IsInstalled { get; set; }
        public string? InstalledBinaryVersion { get; set; }
        public string? InstalledConfigVersion { get; set; }
        public string? ServerVersion { get; set; }
        public bool HasUpdate { get; set; }
        public bool HasBinaryUpdate { get; set; }
        public bool HasConfigUpdate { get; set; }
        public string StatusText { get; set; } = "Not Installed";

        //  For multi-selection with property changed notification
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                    // Notify that selection has changed
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        // Event to notify MainViewModel when selection changes
        public event EventHandler? SelectionChanged;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
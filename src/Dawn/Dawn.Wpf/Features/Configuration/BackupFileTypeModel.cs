using System.Diagnostics;

namespace Dawn.Wpf
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public sealed class BackupFileTypeModel
    {
        public bool IsEnabled { get; set; }
        public string Extension { get; set; }
        public string Name { get; set; }

        public BackupFileTypeModel()
        {
        }

        public BackupFileTypeModel(string extension, string name, bool isEnabled)
        {
            Extension = extension;
            Name = name;
            IsEnabled = isEnabled;
        }

        private string GetDebuggerDisplay()
        {
            return $"{Name} - {Extension}";
        }
    }
}

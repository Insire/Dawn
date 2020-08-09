using System.Collections.Generic;

namespace Dawn.Wpf
{
    public sealed class ConfigurationModel
    {
        public string TargetFolder { get; set; }
        public string BackupFolder { get; set; }
        public string FilePattern { get; set; }

        public List<BackupFileTypeModel> BackupFileTypes { get; set; }
    }
}

using System.Collections.Generic;

namespace Dawn.Wpf
{
    public sealed class ConfigurationModel
    {
        public string DeploymentFolder { get; set; }
        public string BackupFolder { get; set; }

        public bool FirstStart { get; set; }

        public List<BackupFileTypeModel> BackupFileTypes { get; set; }
    }
}

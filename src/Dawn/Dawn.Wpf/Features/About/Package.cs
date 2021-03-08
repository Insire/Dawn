using Microsoft.Toolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace Dawn.Wpf
{
    public sealed class Package : ObservableObject
    {
        [DisplayName("Package name")]
        public string PackageName { get; set; }

        [DisplayName("Version")]
        public string PackageVersion { get; set; }

        [DisplayName("Url")]
        public string PackageUrl { get; set; }

        public string Copyright { get; set; }

        [Browsable(false)]
        public string[] Authors { get; set; }

        public string Description { get; set; }
        public string LicenseUrl { get; set; }
        public string LicenseType { get; set; }
    }
}

using System.ComponentModel;

namespace Dawn.Core.Features.About
{
    public sealed class PackageViewModel : ObservableObject
    {
        [DisplayName("Package Id")]
        public string PackageId { get; set; } = string.Empty;

        [DisplayName("Version")]
        public string PackageVersion { get; set; } = string.Empty;

        [DisplayName("Url")]
        public string PackageProjectUrl { get; set; } = string.Empty;

        public string Copyright { get; set; } = string.Empty;

        [Browsable(false)]
        public string Authors { get; set; } = string.Empty;

        public string License { get; set; } = string.Empty;

        [DisplayName("License Url")]
        public string LicenseUrl { get; set; } = string.Empty;
    }
}

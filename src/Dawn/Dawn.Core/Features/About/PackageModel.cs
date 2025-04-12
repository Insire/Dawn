namespace Dawn.Core.Features.About
{
    public sealed record PackageModel
    {
        public string PackageId { get; set; } = string.Empty;

        public string PackageVersion { get; set; } = string.Empty;

        public string PackageProjectUrl { get; set; } = string.Empty;

        public string Copyright { get; set; } = string.Empty;

        public string Authors { get; set; } = string.Empty;

        public string License { get; set; } = string.Empty;
        public string LicenseUrl { get; set; } = string.Empty;

        /// <summary>
        ///
        /// </summary>
        /// <remarks>
        ///<list type="number">
        ///<item>Expression</item>
        ///<item>Url</item>
        ///<item>Unknown</item>
        ///<item>Ignored</item>
        ///<item>OVeerwrite</item>
        ///</list>
        /// </remarks>
        public int LicenseInformationOrigin { get; set; }
    }
}

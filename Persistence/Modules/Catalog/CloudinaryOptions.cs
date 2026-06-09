namespace Persistence.Modules.Catalog
{
    /// <summary>Cloudinary credentials, bound from the <c>Cloudinary</c> configuration section.</summary>
    public sealed class CloudinaryOptions
    {
        public const string SectionName = "Cloudinary";

        public string CloudName { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ApiSecret { get; set; } = string.Empty;

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(CloudName)
            && !string.IsNullOrWhiteSpace(ApiKey)
            && !string.IsNullOrWhiteSpace(ApiSecret);
    }
}

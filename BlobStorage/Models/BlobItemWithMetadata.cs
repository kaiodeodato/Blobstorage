namespace BlobStorage.Models
{
    public class BlobItemWithMetadata
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public long FileSize { get; set; }
    }
}

namespace SIAMS.Models
{
    public class Asset
    {
        public int AssetId { get; set; } // Non-nullable because IDs are always set
        public string AssetName { get; set; } = string.Empty; // Default value for non-nullable string
        public string? Category { get; set; } // Nullable because it can be null
        public int AssignedUserId { get; set; } // Non-nullable as IDs must be set
        public User? AssignedUser { get; set; } // Nullable as it may not always be populated
    }
}

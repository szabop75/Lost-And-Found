using System;

namespace LostAndFound.Domain.Entities;

public class Attachment : BaseEntity
{
    public Guid FoundItemId { get; set; }
    public string FileName { get; set; } = default!;
    public string MimeType { get; set; } = default!;
    public long Size { get; set; }
    public string StoragePath { get; set; } = default!; // vagy URL

    // Navigáció
    public FoundItem? FoundItem { get; set; }
}

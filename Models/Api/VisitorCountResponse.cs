namespace AspnetCoreMvcFull.Models.Api;

public record VisitorCountResponse(long Count, int ActiveUsers, DateTimeOffset UpdatedAt);
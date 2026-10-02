namespace DelightsBoutique.Application.Products.Dtos;

public record BulkUploadRowResult(int RowNumber, bool Success, string? Slug, string? Error);

public record BulkUploadResultDto(List<BulkUploadRowResult> Rows, int SuccessCount, int ErrorCount);

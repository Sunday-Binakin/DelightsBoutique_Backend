using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Products.Dtos;
using DelightsBoutique.Domain.Exceptions;
using FluentValidation;
using MediatR;

namespace DelightsBoutique.Application.Products.Commands;

public class BulkUploadProductsCommandHandler(IProductCsvParser csvParser, IMediator mediator)
    : IRequestHandler<BulkUploadProductsCommand, BulkUploadResultDto>
{
    public async Task<BulkUploadResultDto> Handle(BulkUploadProductsCommand request, CancellationToken ct)
    {
        var parsedRows = csvParser.Parse(request.FileBytes);
        var results = new List<BulkUploadRowResult>();

        foreach (var row in parsedRows)
        {
            if (row.ParseError is not null)
            {
                results.Add(new BulkUploadRowResult(row.RowNumber, false, null, row.ParseError));
                continue;
            }

            try
            {
                // Re-sent through the same pipeline as the single-product endpoint —
                // reuses validation, slug generation/dedup, and audit logging verbatim,
                // no parallel "what makes a product valid" logic to maintain.
                var command = new CreateProductCommand(
                    row.Slug, row.Name, row.CategorySlug, row.Description,
                    row.RetailPrice, row.WholesalePrice, row.WholesaleMoq, row.CompareAtPrice,
                    null, null, row.Tone, row.Badge, row.StockStatus,
                    row.Sizes, row.Colors, row.ImageUrls);

                var created = await mediator.Send(command, ct);
                results.Add(new BulkUploadRowResult(row.RowNumber, true, created.Slug, null));
            }
            catch (ValidationException ex)
            {
                var message = string.Join(" ", ex.Errors.Select(e => e.ErrorMessage));
                results.Add(new BulkUploadRowResult(row.RowNumber, false, null, message));
            }
            catch (DuplicateSlugException ex)
            {
                results.Add(new BulkUploadRowResult(row.RowNumber, false, null, ex.Message));
            }
        }

        return new BulkUploadResultDto(results, results.Count(r => r.Success), results.Count(r => !r.Success));
    }
}

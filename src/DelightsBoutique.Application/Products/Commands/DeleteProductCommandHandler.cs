using DelightsBoutique.Application.Abstractions;
using MediatR;

namespace DelightsBoutique.Application.Products.Commands;

public class DeleteProductCommandHandler(IProductRepository products) : IRequestHandler<DeleteProductCommand, bool>
{
    public async Task<bool> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetEntityBySlugAsync(request.Slug, cancellationToken);
        if (product is null)
            return false;

        await products.DeleteAsync(product, cancellationToken);
        return true;
    }
}

using DelightsBoutique.Application.Products.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Products.Queries;

public record GetProductBySlugQuery(string Slug) : IRequest<ProductDto?>;

using DelightsBoutique.Application.Categories.Dtos;
using DelightsBoutique.Application.Common;
using DelightsBoutique.Domain.Enums;
using MediatR;

namespace DelightsBoutique.Application.Categories.Commands;

public record UpdateCategoryCommand(string Slug, string? Name, Tone? Tone, string? ImageUrl)
    : IRequest<CategoryDto?>, IAuditableCommand
{
    public string DescribeAction() => $"Updated category '{Slug}'";
}

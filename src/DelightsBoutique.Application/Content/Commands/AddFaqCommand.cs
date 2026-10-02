using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Content.Dtos;
using MediatR;

namespace DelightsBoutique.Application.Content.Commands;

public record AddFaqCommand(string Question, string Answer) : IRequest<FaqItemDto>, IAuditableCommand
{
    public string DescribeAction() => "Added FAQ";
}

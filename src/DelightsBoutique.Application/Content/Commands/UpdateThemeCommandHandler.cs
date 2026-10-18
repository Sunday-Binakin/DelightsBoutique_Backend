using System.Text.Json;
using System.Text.RegularExpressions;
using DelightsBoutique.Application.Abstractions;
using DelightsBoutique.Application.Common;
using DelightsBoutique.Application.Content.Dtos;
using DelightsBoutique.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace DelightsBoutique.Application.Content.Commands;

public partial class UpdateThemeCommandHandler(ISiteContentRepository siteContent) : IRequestHandler<UpdateThemeCommand, ThemeDto>
{
    public async Task<ThemeDto> Handle(UpdateThemeCommand request, CancellationToken cancellationToken)
    {
        if (request.Primary is not null && !HexColor().IsMatch(request.Primary))
            throw new ValidationException([new ValidationFailure(nameof(request.Primary), "Must be a hex color like #0550c2.")]);
        if (request.Accent is not null && !HexColor().IsMatch(request.Accent))
            throw new ValidationException([new ValidationFailure(nameof(request.Accent), "Must be a hex color like #ff7101.")]);

        var json = await siteContent.GetValueJsonAsync(SiteContentKey.Theme, cancellationToken);
        var current = JsonSerializer.Deserialize<ThemeDto>(json!, ContentJsonOptions.Default)!;

        var updated = current with
        {
            Primary = request.Primary ?? current.Primary,
            Accent = request.Accent ?? current.Accent
        };

        await siteContent.UpsertAsync(SiteContentKey.Theme, JsonSerializer.Serialize(updated, ContentJsonOptions.Default), cancellationToken);
        return updated;
    }

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex HexColor();
}

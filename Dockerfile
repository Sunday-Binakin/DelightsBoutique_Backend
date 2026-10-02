# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first so `dotnet restore` is cached across builds that only change code.
COPY src/DelightsBoutique.Domain/DelightsBoutique.Domain.csproj src/DelightsBoutique.Domain/
COPY src/DelightsBoutique.Application/DelightsBoutique.Application.csproj src/DelightsBoutique.Application/
COPY src/DelightsBoutique.Infrastructure/DelightsBoutique.Infrastructure.csproj src/DelightsBoutique.Infrastructure/
COPY src/DelightsBoutique.Api/DelightsBoutique.Api.csproj src/DelightsBoutique.Api/
RUN dotnet restore src/DelightsBoutique.Api/DelightsBoutique.Api.csproj

COPY src/ src/
RUN dotnet publish src/DelightsBoutique.Api/DelightsBoutique.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Matches Kestrel's default HTTP port in a container; TLS termination is expected to happen
# at the hosting platform's edge (load balancer / reverse proxy), not inside this container.
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "DelightsBoutique.Api.dll"]

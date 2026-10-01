FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY DelightsBoutique.sln ./
COPY src/DelightsBoutique.Domain/*.csproj src/DelightsBoutique.Domain/
COPY src/DelightsBoutique.Application/*.csproj src/DelightsBoutique.Application/
COPY src/DelightsBoutique.Infrastructure/*.csproj src/DelightsBoutique.Infrastructure/
COPY src/DelightsBoutique.Api/*.csproj src/DelightsBoutique.Api/
RUN dotnet restore src/DelightsBoutique.Api/DelightsBoutique.Api.csproj

COPY src/ src/
RUN dotnet publish src/DelightsBoutique.Api/DelightsBoutique.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "DelightsBoutique.Api.dll"]

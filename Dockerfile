FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY src/TicketSystem.Api/TicketSystem.Api.csproj src/TicketSystem.Api/
COPY src/TicketSystem.Application/TicketSystem.Application.csproj src/TicketSystem.Application/
COPY src/TicketSystem.Contracts/TicketSystem.Contracts.csproj src/TicketSystem.Contracts/
COPY src/TicketSystem.Domain/TicketSystem.Domain.csproj src/TicketSystem.Domain/
COPY src/TicketSystem.Infrastructure/TicketSystem.Infrastructure.csproj src/TicketSystem.Infrastructure/

RUN dotnet restore src/TicketSystem.Api/TicketSystem.Api.csproj

COPY src/ ./src/
RUN dotnet publish src/TicketSystem.Api/TicketSystem.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "TicketSystem.Api.dll"]

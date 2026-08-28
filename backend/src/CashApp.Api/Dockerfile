# Build context attendu : backend/ (voir render.yaml -> dockerContext)

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/CashApp.Domain/CashApp.Domain.csproj src/CashApp.Domain/
COPY src/CashApp.Shared/CashApp.Shared.csproj src/CashApp.Shared/
COPY src/CashApp.Application/CashApp.Application.csproj src/CashApp.Application/
COPY src/CashApp.Infrastructure/CashApp.Infrastructure.csproj src/CashApp.Infrastructure/
COPY src/CashApp.Api/CashApp.Api.csproj src/CashApp.Api/
RUN dotnet restore src/CashApp.Api/CashApp.Api.csproj

COPY src/ src/
RUN dotnet publish src/CashApp.Api/CashApp.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Render fournit PORT au runtime ; Program.cs s'y bind dynamiquement (WebHost.UseUrls).
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "CashApp.Api.dll"]

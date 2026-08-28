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

# QuestPDF (génération des reçus/exports PDF) s'appuie sur SkiaSharp, qui a besoin de
# libfontconfig/libfreetype pour le rendu du texte. L'image aspnet minimale ne les
# inclut pas : sans elles, les PDF générés sont vides (aucun texte). Voir
# https://www.questpdf.com/faq/rendering-issues-linux.html
RUN apt-get update \
    && apt-get install -y --no-install-recommends libfontconfig1 libfreetype6 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

# Render fournit PORT au runtime ; Program.cs s'y bind dynamiquement (WebHost.UseUrls).
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "CashApp.Api.dll"]

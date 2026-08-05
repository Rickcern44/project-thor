FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Build context is src/ (not src/api), so this also picks up the sibling
# ServiceDefaults project that Api.csproj references via ../../ServiceDefaults.
COPY . .
RUN dotnet publish api/Api/ProjectThor.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "ProjectThor.Api.dll"]

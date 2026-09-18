FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/FintechPlatform.Api/FintechPlatform.Api.csproj src/FintechPlatform.Api/
RUN dotnet restore src/FintechPlatform.Api/FintechPlatform.Api.csproj
COPY . .
RUN dotnet publish src/FintechPlatform.Api/FintechPlatform.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "FintechPlatform.Api.dll"]

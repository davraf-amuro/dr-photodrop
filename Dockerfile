FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
WORKDIR /src

# Props e SDK condivisi (TargetFramework, Nullable, ImplicitUsings): servono già al restore
COPY Directory.Build.props global.json ./
COPY src/dr_photodrop.api/dr_photodrop.api.csproj src/dr_photodrop.api/
RUN dotnet restore src/dr_photodrop.api/dr_photodrop.api.csproj

COPY src/ src/
RUN dotnet publish src/dr_photodrop.api/dr_photodrop.api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:11.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "dr_photodrop.api.dll"]

# ---- build stage: full SDK, produces the published output -------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project file first so "dotnet restore" is cached as its own layer
# and is re-run only when the dependencies change, not on every code change.
COPY src/Wallet.Api/Wallet.Api.csproj src/Wallet.Api/
RUN dotnet restore src/Wallet.Api/Wallet.Api.csproj

COPY src/Wallet.Api/ src/Wallet.Api/
RUN dotnet publish src/Wallet.Api/Wallet.Api.csproj -c Release -o /app/publish --no-restore

# ---- runtime stage: small image with only the ASP.NET Core runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# The official images ship a non-root user; do not run the API as root.
USER $APP_UID

# ASP.NET Core listens on 8080 inside the container by default.
EXPOSE 8080
ENTRYPOINT ["dotnet", "Wallet.Api.dll"]

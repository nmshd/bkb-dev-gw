# Build
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /src
COPY . .
RUN dotnet restore "ApiGateway/ApiGateway.csproj"

WORKDIR "/src/ApiGateway"
RUN dotnet publish --configuration Release --output /app --no-restore

# Run	
FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine
EXPOSE 80
WORKDIR /app
COPY --from=build-env /app .

LABEL org.opencontainers.image.source = "https://github.com/nmshd/bkb-dev-gw"

ENTRYPOINT ["dotnet", "ApiGateway.dll"]

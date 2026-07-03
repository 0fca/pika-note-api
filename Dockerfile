FROM 192.168.1.252:5030/pika-cloud/pika.domain AS  build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet publish PikaNoteAPI.csproj -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy
EXPOSE 80
EXPOSE 443
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl file \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build-env /app/out .
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl --insecure -f https://localhost:443/health || exit 1
ENTRYPOINT ["dotnet", "PikaNoteAPI.dll"]

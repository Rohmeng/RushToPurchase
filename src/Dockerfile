FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine AS base
WORKDIR /app
EXPOSE 7021

ENV ASPNETCORE_URLS=http://*:7021

# Creates a non-root user with an explicit UID and adds permission to access the /app folder
# For more info, please refer to https://aka.ms/vscode-docker-dotnet-configure-containers
RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build
WORKDIR /src
COPY . .

WORKDIR "/src/RushToPurchase.WebApi"
RUN dotnet build "RushToPurchase.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RushToPurchase.WebApi.csproj" -c Release -o /app/publish \
    --runtime alpine-x64 \
    --self-contained true \
    /p:PublishTrimmed=true \
    /p:PublishSingleFile=true

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# ENTRYPOINT ["dotnet", "RushToPurchase.WebApi.dll"]
ENTRYPOINT ["./RushToPurchase.WebApi"]

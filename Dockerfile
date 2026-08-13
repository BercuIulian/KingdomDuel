FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/KingdomDuel.Api/KingdomDuel.Api.csproj src/KingdomDuel.Api/
COPY src/KingdomDuel.Application/KingdomDuel.Application.csproj src/KingdomDuel.Application/
COPY src/KingdomDuel.Client/KingdomDuel.Client.csproj src/KingdomDuel.Client/
COPY src/KingdomDuel.Domain/KingdomDuel.Domain.csproj src/KingdomDuel.Domain/
COPY src/KingdomDuel.Persistence/KingdomDuel.Persistence.csproj src/KingdomDuel.Persistence/
COPY src/KingdomDuel.Shared/KingdomDuel.Shared.csproj src/KingdomDuel.Shared/
RUN dotnet restore src/KingdomDuel.Api/KingdomDuel.Api.csproj

COPY . .
RUN dotnet publish src/KingdomDuel.Api/KingdomDuel.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

USER $APP_UID
ENTRYPOINT ["dotnet", "KingdomDuel.Api.dll"]

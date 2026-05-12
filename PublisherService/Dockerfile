FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY ["Common/Common.csproj", "Common/"]
COPY ["PublisherService/PublisherService.csproj", "PublisherService/"]
RUN dotnet restore "PublisherService/PublisherService.csproj"

COPY . .
WORKDIR "/app/PublisherService"
RUN dotnet publish "PublisherService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PublisherService.dll"]

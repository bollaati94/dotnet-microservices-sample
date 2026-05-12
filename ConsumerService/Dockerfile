FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY ["Common/Common.csproj", "Common/"]
COPY ["ConsumerService/ConsumerService.csproj", "ConsumerService/"]
RUN dotnet restore "ConsumerService/ConsumerService.csproj"

COPY . .
WORKDIR "/app/ConsumerService"
RUN dotnet publish "ConsumerService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ConsumerService.dll"]

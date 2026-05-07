FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["ValidationNode.Api/ValidationNode.Api.csproj", "ValidationNode.Api/"]
COPY ["Validation.Contracts/Validation.Contracts.csproj", "Validation.Contracts/"]
RUN dotnet restore "ValidationNode.Api/ValidationNode.Api.csproj"

COPY . .
RUN dotnet publish "ValidationNode.Api/ValidationNode.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ValidationNode.Api.dll"]
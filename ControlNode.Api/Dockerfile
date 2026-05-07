FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["ControlNode.Api/ControlNode.Api.csproj", "ControlNode.Api/"]
COPY ["Validation.Contracts/Validation.Contracts.csproj", "Validation.Contracts/"]
RUN dotnet restore "ControlNode.Api/ControlNode.Api.csproj"

COPY . .
RUN dotnet publish "ControlNode.Api/ControlNode.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ControlNode.Api.dll"]
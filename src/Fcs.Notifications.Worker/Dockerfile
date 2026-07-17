FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Fcs.Notifications.Worker/Fcs.Notifications.Worker.csproj", "src/Fcs.Notifications.Worker/"]
COPY ["src/Fcs.Notifications.Application/Fcs.Notifications.Application.csproj", "src/Fcs.Notifications.Application/"]
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
RUN dotnet restore "src/Fcs.Notifications.Worker/Fcs.Notifications.Worker.csproj"
COPY . .
WORKDIR "/src/src/Fcs.Notifications.Worker"
RUN dotnet build "Fcs.Notifications.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Fcs.Notifications.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Fcs.Notifications.Worker.dll"]

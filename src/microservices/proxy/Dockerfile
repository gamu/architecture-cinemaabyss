FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["proxy.csproj", "./"]
RUN dotnet restore "./proxy.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "proxy.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "proxy.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8000
ENTRYPOINT ["dotnet", "proxy.dll"] 
# Use the official .NET 9 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /app

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the source code
COPY . ./

# Build the application
RUN dotnet publish -c Release -o out

# Use the official .NET 9 runtime for the final image
FROM mcr.microsoft.com/dotnet/aspnet:9.0

WORKDIR /app

# Copy the build output
COPY --from=build /app/out .

# Expose the application port
EXPOSE 8082

# Set environment variables
ENV ASPNETCORE_URLS=http://*:8082

# Command to run the application
ENTRYPOINT ["dotnet", "EventsService.dll"] 
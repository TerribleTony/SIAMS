# Use the official .NET SDK image (Linux)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy and restore project dependencies
COPY *.sln .
COPY SIAMS/SIAMS.csproj SIAMS/
RUN dotnet restore SIAMS/SIAMS.csproj

# Copy the entire project and build
COPY . .
WORKDIR /app/SIAMS
RUN dotnet publish -c Release -o out

# Use ASP.NET Core runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/SIAMS/out .

# Set the entry point
ENTRYPOINT ["dotnet", "SIAMS.dll"]

# Use the official .NET SDK image for build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy and restore project dependencies
COPY *.sln . 
COPY SIAMS/SIAMS.csproj SIAMS/
RUN dotnet restore SIAMS/SIAMS.csproj

# Copy entire project and build the application
COPY . .
WORKDIR /app/SIAMS
RUN dotnet publish -c Release -o out

# Use the ASP.NET Core runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy the compiled output from the build stage
COPY --from=build /app/SIAMS/out .

# Ensure key storage directory exists
RUN mkdir /app/keys

VOLUME /app/keys


# Set the entry point
ENTRYPOINT ["dotnet", "SIAMS.dll"]

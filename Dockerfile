FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files to restore dependencies
COPY ["SubscriptionBillingAPI.sln", "./"]
COPY ["src/Domain/SubscriptionBillingAPI.Domain.csproj", "src/Domain/"]
COPY ["src/Application/SubscriptionBillingAPI.Application.csproj", "src/Application/"]
COPY ["src/Infrastructure/SubscriptionBillingAPI.Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/API/SubscriptionBillingAPI.API.csproj", "src/API/"]
COPY ["tests/UnitTests/SubscriptionBillingAPI.UnitTests.csproj", "tests/UnitTests/"]
COPY ["tests/IntegrationTests/SubscriptionBillingAPI.IntegrationTests.csproj", "tests/IntegrationTests/"]
RUN dotnet restore "SubscriptionBillingAPI.sln"

# Copy the remaining source code
COPY . .

# Build the application
WORKDIR "/src/src/API"
RUN dotnet build "SubscriptionBillingAPI.API.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "SubscriptionBillingAPI.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Build the final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Expose ports
EXPOSE 8080

# Run the application
ENTRYPOINT ["dotnet", "SubscriptionBillingAPI.API.dll"]

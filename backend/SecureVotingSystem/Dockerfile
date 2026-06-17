FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# 1. Copy ONLY csproj first (for caching)
COPY *.csproj ./

# 2. Restore dependencies (cached if no csproj changes)
RUN dotnet restore

# 3. Copy everything else
COPY . ./

# 4. Publish
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "SecureVotingSystem.dll"]
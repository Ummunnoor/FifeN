FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY API/API.csproj API/
COPY Application/Application.csproj Application/
COPY Domain/Domain.csproj Domain/
COPY Persistence/Persistence.csproj Persistence/

RUN dotnet restore API/API.csproj

COPY . .
WORKDIR /src/API

RUN dotnet publish API.csproj --configuration Release --no-restore --output /app/publish

# Install EF Core tools and create the migration bundle
RUN dotnet tool install --global dotnet-ef \
    && export PATH="$PATH:/root/.dotnet/tools" \
    && dotnet ef migrations bundle \
        --self-contained \
        --target-runtime linux-x64 \
        --output /app/publish/efbundle \
        --project ../Persistence/Persistence.csproj \
        --startup-project API.csproj

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "API.dll"]
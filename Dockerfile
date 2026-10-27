FROM mcr.microsoft.com/dotnet/sdk:10.0.102 AS build
WORKDIR /src

COPY . .
RUN dotnet restore ZEISS.ProductManagement.slnx
RUN dotnet publish src/ZEISS.ProductManagement.Api/ZEISS.ProductManagement.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/sdk:10.0.102 AS migrations
WORKDIR /src
COPY --from=build /src /src
RUN dotnet restore ZEISS.ProductManagement.slnx
RUN dotnet tool restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ZEISS.ProductManagement.Api.dll"]

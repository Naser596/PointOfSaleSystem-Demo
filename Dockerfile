FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY WebApplication3.csproj ./
RUN dotnet restore WebApplication3.csproj

COPY . ./
RUN dotnet publish WebApplication3.csproj -c Release -o /app/publish -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
RUN mkdir -p /app/wwwroot/images/company /app/wwwroot/images/products
COPY --from=build /app/publish ./

EXPOSE 8080
ENTRYPOINT ["dotnet", "WebApplication3.dll"]

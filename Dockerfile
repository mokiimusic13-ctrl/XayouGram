FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY server/*.csproj ./
RUN dotnet restore

COPY server/. ./
WORKDIR /app
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out ./

ENV ASPNETCORE_URLS=http://0.0.0.0:$PORT
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "XayouGram.Backend.dll"]
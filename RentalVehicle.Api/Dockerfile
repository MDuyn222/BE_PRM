FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY RentalVehicle.Api.csproj ./
RUN dotnet restore RentalVehicle.Api.csproj
COPY . .
RUN dotnet publish RentalVehicle.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
COPY --from=build /app/publish .
RUN mkdir -p /app/wwwroot/uploads
ENTRYPOINT ["dotnet", "RentalVehicle.Api.dll"]

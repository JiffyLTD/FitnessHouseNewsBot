FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY FitnessHouseNewsBot.sln ./
COPY FitnessHouseNewsBot/FitnessHouseNewsBot.csproj ./FitnessHouseNewsBot/

RUN dotnet restore ./FitnessHouseNewsBot.sln

COPY . .

RUN dotnet publish ./FitnessHouseNewsBot/FitnessHouseNewsBot.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish ./

RUN mkdir -p /app/storage

ENTRYPOINT ["dotnet", "FitnessHouseNewsBot.dll"]

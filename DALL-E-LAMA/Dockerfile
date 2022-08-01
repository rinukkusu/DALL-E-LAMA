#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["DALL-E-LAMA/DALL-E-LAMA.csproj", "DALL-E-LAMA/"]
COPY ["DALL-E-LAMA.Data/DALL-E-LAMA.Data.csproj", "DALL-E-LAMA.Data/"]
COPY ["DALL-E-LAMA.DalleApi/DALL-E-LAMA.DalleApi.csproj", "DALL-E-LAMA.DalleApi/"]
RUN dotnet restore "DALL-E-LAMA/DALL-E-LAMA.csproj"
COPY . .
WORKDIR "/src/DALL-E-LAMA"
RUN dotnet build "DALL-E-LAMA.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DALL-E-LAMA.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DALL-E-LAMA.dll"]
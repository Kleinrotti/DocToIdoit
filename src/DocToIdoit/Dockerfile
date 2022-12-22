#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

RUN apt-get update && apt-get install -y apt-utils libgdiplus libc6-dev

RUN mkdir /ocr
RUN mkdir /ocr/scan
RUN mkdir /ocr/processing
RUN mkdir /ocr/errors
RUN mkdir /ocr/results

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["DocToIdoit/DocToIdoit.csproj", "DocToIdoit/"]
RUN dotnet restore "DocToIdoit/DocToIdoit.csproj"
COPY . .
WORKDIR "/src/DocToIdoit"
RUN dotnet build "DocToIdoit.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish DocToIdoit.csproj /p:PublishProfile=Linux-X64 -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DocToIdoit.dll"]
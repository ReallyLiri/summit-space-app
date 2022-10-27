FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app
COPY SummIt/SummIt.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
RUN apt-get update && apt install -y git
COPY --from=build-env /app/out .
CMD ASPNETCORE_URLS=http://*:$PORT dotnet SummIt.dll

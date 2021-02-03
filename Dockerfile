# Take a base image from the public Docker Hub repositories
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
# Navigate to the “/app” folder (create if not exists)
WORKDIR /app
# Copy csproj and download the dependencies listed in that file
COPY *.csproj ./
RUN dotnet restore
# Copy all files in the project folder
COPY . ./
RUN dotnet publish -c Release -o out
# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:3.1 AS runtime
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "CanisLupus.dll"]
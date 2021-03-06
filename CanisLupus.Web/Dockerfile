# Pull down an image from Docker Hub that includes the .NET core SDK: 
# https://hub.docker.com/_/microsoft-dotnet-core-sdk
# This is so we have all the tools necessary to compile the app.
FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build

# Fetch and install Node 10. Make sure to include the --yes parameter 
# to automatically accept prompts during install, or it'll fail.
RUN curl --silent --location https://deb.nodesource.com/setup_10.x | bash -
RUN apt-get install --yes nodejs

# Copy the source from your machine onto the container.
WORKDIR /app
COPY . .

# Install dependencies. 
# https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-restore?tabs=netcore2x
RUN dotnet restore "./CanisLupus.Web.csproj"

# Compile, then pack the compiled app and dependencies into a deployable unit.
# https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-publish?tabs=netcore21
RUN dotnet publish "CanisLupus.Web.csproj" -c Release -o /app/publish

# Pull down an image from Docker Hub that includes only the ASP.NET core runtime:
# https://hub.docker.com/_/microsoft-dotnet-core-aspnet/
# We don't need the SDK anymore, so this will produce a lighter-weight image
# that can still run the app.
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim

# Expose port 80 to your local machine so you can access the app.
EXPOSE 80

WORKDIR /app
# Copy the published app to this new runtime-only container.
COPY --from=build /app/publish .

# To run the app, run `dotnet sample-app.dll`, which we just copied over.
ENTRYPOINT ["dotnet", "CanisLupus.Web.dll"]


# # Take a base image from the public Docker Hub repositories
# FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
# # Navigate to the “/app” folder (create if not exists)
# WORKDIR /app
# # Copy csproj and download the dependencies listed in that file
# COPY *.csproj ./
# RUN dotnet restore
# # Copy all files in the project folder
# COPY . ./
# RUN dotnet publish -c Release -o out
# # Build runtime image
# FROM mcr.microsoft.com/dotnet/runtime:3.1 AS runtime
# WORKDIR /app
# COPY --from=build-env /app/out .
# ENTRYPOINT ["dotnet", "CanisLupus.Web.dll"]

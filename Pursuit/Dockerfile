# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app

RUN apt-get update \
&& apt-get install -y --no-install-recommends libldap-2.4-2 \
&& apt-get install -y --no-install-recommends libldap-common \
&& rm -rf /var/lib/apt/lists/* \
&& ln -s -f /usr/lib/x86_64-linux-gnu/libldap-2.4.so.2 /usr/lib/x86_64-linux-gnu/libldap.so.2
    
# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet publish -c Release -o out
    
# Build runtime image
FROM mcr.microsoft.com/dotnet/sdk:6.0
WORKDIR /app
ENV DOTNET_URLS=http://+:5000
COPY --from=build-env /app/out .

# Replace the placeholders with environment variables in appsettings.json during build
RUN apt-get update && apt-get install -y gettext

# Copy the entrypoint script and appsettings.json.template to the container
COPY umm-entrypoint.sh /app/entrypoint.sh
COPY appsettings.env.json /app/appsettings.json.template
COPY appsettings.json /app/appsettings.json

# Grant execute permissions to the script
RUN chmod +x /app/entrypoint.sh

# Set the entrypoint
ENTRYPOINT ["/app/entrypoint.sh"]

# ENTRYPOINT ["dotnet", "Pursuit.dll"]
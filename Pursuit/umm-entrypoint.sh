#!/bin/bash
set -x

# Replace placeholders using envsubst
envsubst  < /app/appsettings.json.template > /app/appsettings.env.json

# Run your .NET application
dotnet Pursuit.dll

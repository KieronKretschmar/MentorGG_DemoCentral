# ===============
# BUILD IMAGE
# ===============
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /app


ENV PROJ "DemoCentral"

# Copy csproj and restore as distinct layers

WORKDIR /app/RabbitCommunicationLib
COPY ./RabbitCommunicationLib/*.csproj ./
RUN dotnet restore



WORKDIR /app/Database
COPY ./Database/*.csproj ./
RUN dotnet restore

WORKDIR /app/$PROJ
COPY ./$PROJ/*.csproj ./
RUN dotnet restore

# Copy everything else and build
WORKDIR /app
COPY ./RabbitCommunicationLib/ ./RabbitCommunicationLib
COPY ./Database ./Database
COPY ./$PROJ ./$PROJ

RUN dotnet publish $PROJ/ -c Release -o out

# ===============
# RUNTIME IMAGE
# ===============
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS runtime
WORKDIR /app

COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "DemoCentral.dll"]

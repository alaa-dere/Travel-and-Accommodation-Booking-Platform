FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY ["HotelBooking.API/HotelBooking.API.csproj", "HotelBooking.API/"]
COPY ["HotelBooking.Application/HotelBooking.Application.csproj", "HotelBooking.Application/"]
COPY ["HotelBooking.Domain/HotelBooking.Domain.csproj", "HotelBooking.Domain/"]
COPY ["HotelBooking.Infrastructure/HotelBooking.Infrastructure.csproj", "HotelBooking.Infrastructure/"]

RUN dotnet restore "HotelBooking.API/HotelBooking.API.csproj"

COPY . .

RUN dotnet publish "HotelBooking.API/HotelBooking.API.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore


FROM build AS migrations

RUN dotnet tool install --global dotnet-ef --version 8.0.31

ENV PATH="${PATH}:/root/.dotnet/tools"

ENTRYPOINT ["dotnet", "ef", "database", "update", \
    "--project", "HotelBooking.Infrastructure", \
    "--startup-project", "HotelBooking.API", \
    "--configuration", "Release", \
    "--no-build"]


FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

USER $APP_UID

ENTRYPOINT ["dotnet", "HotelBooking.API.dll"]
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

COPY . .

RUN dotnet restore
RUN dotnet publish Camunda-TZ/Camunda-TZ.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV TZ="Asia/Tashkent"

ENTRYPOINT ["dotnet", "Camunda-TZ.dll", "--urls=http://0.0.0.0:15000"]
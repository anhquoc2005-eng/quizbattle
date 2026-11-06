# Context build là thư mục backend/ (xem README, mục Deploy).
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish QuizBattle.Api/QuizBattle.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
# Image aspnet:8.0 có sẵn user "app" (APP_UID). Cổng mặc định 8080; Render cấp PORT thì Program.cs dùng PORT.
USER $APP_UID
ENTRYPOINT ["dotnet", "QuizBattle.Api.dll"]

# =========================================================================
# Build asamasi: .NET 9 SDK ile restore + publish
# Build context'i solution kok dizinidir (docker-compose.yml -> context: .)
# =========================================================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Once sadece proje dosyalarini kopyala ki "dotnet restore" katmani
# kod degisikliklerinde cache'ten kullanilabilsin
COPY ["Project.Web/Project.Web.csproj", "Project.Web/"]
COPY ["Project.Business/Project.Business.csproj", "Project.Business/"]
COPY ["Project.DataAccess/Project.DataAccess.csproj", "Project.DataAccess/"]
COPY ["Project.Core/Project.Core.csproj", "Project.Core/"]
RUN dotnet restore "Project.Web/Project.Web.csproj"

# Tum kaynak kodu kopyala ve publish et
COPY . .
WORKDIR /src/Project.Web
RUN dotnet publish "Project.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# =========================================================================
# Runtime asamasi: yalnizca ASP.NET Core runtime iceren kucuk imaj
# =========================================================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# .NET 8+ aspnet imajlari varsayilan olarak 8080 portunu dinler (ASPNETCORE_HTTP_PORTS=8080)
EXPOSE 8080

ENTRYPOINT ["dotnet", "Project.Web.dll"]

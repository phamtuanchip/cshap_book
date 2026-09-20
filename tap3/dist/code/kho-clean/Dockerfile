# syntax=docker/dockerfile:1
# ===== Giai doan 1: BUILD (co SDK, nang) =====
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Chep RIENG cac file csproj truoc -> lop "restore" duoc cache; chi khi doi package moi restore lai
COPY Kho.Domain/Kho.Domain.csproj Kho.Domain/
COPY Kho.Application/Kho.Application.csproj Kho.Application/
COPY Kho.Infrastructure/Kho.Infrastructure.csproj Kho.Infrastructure/
COPY Kho.Web/Kho.Web.csproj Kho.Web/
RUN dotnet restore Kho.Web/Kho.Web.csproj

COPY . .
RUN dotnet publish Kho.Web/Kho.Web.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ===== Giai doan 2: RUNTIME (chi co runtime ASP.NET, nho, it be mat tan cong) =====
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Du lieu SQLite nam o volume /data (container "vo trang thai", du lieu song ngoai vong doi container)
RUN mkdir -p /data && chown $APP_UID /data
ENV ConnectionStrings__Kho="Data Source=/data/kho.db" \
    ASPNETCORE_HTTP_PORTS=8080
VOLUME /data

# KHONG chay bang root
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Kho.Web.dll"]

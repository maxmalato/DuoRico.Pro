# Build (Usa o SDK completo para compilar o código)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia o arquivo de projeto e baixa as dependências (NuGet)
COPY ["DuoRico.Pro.csproj", "./"]
RUN dotnet restore "./DuoRico.Pro.csproj"

# Copia o resto do código e compila a versão final (Release)
COPY . .
RUN dotnet publish "DuoRico.Pro.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime (Usa uma imagem super leve apenas para rodar o app)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# O .NET 8 usa a porta 8080 por padrão no Docker
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

# Puxa os arquivos compilados e inicia o servidor
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DuoRico.Pro.dll"]
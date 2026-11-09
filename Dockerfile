FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY crudEbancoDist8/crudEbancoDist8.csproj crudEbancoDist8/
RUN dotnet restore crudEbancoDist8/crudEbancoDist8.csproj
COPY crudEbancoDist8/ crudEbancoDist8/
RUN dotnet publish crudEbancoDist8/crudEbancoDist8.csproj -c Release --no-restore -o /out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out/ .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "crudEbancoDist8.dll"]

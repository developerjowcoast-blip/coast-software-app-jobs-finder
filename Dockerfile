FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["JobFinder.sln", "./"]
COPY ["src/JobFinder.Domain/JobFinder.Domain.csproj", "src/JobFinder.Domain/"]
COPY ["src/JobFinder.Application/JobFinder.Application.csproj", "src/JobFinder.Application/"]
COPY ["src/JobFinder.Infrastructure/JobFinder.Infrastructure.csproj", "src/JobFinder.Infrastructure/"]
COPY ["src/JobFinder.Persistence/JobFinder.Persistence.csproj", "src/JobFinder.Persistence/"]
COPY ["src/JobFinder.API/JobFinder.API.csproj", "src/JobFinder.API/"]
RUN dotnet restore "src/JobFinder.API/JobFinder.API.csproj"
COPY . .
RUN dotnet publish "src/JobFinder.API/JobFinder.API.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
RUN mkdir -p /app/data
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionStrings__JobFinder="Data Source=/app/data/jobfinder.db"
EXPOSE 8080
ENTRYPOINT ["dotnet", "JobFinder.API.dll"]

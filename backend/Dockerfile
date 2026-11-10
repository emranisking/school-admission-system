FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first (cached layer unless a csproj changes)
COPY SchoolAdmission.Domain/SchoolAdmission.Domain.csproj SchoolAdmission.Domain/
COPY SchoolAdmission.Application/SchoolAdmission.Application.csproj SchoolAdmission.Application/
COPY SchoolAdmission.Infrastructure/SchoolAdmission.Infrastructure.csproj SchoolAdmission.Infrastructure/
COPY SchoolAdmission.Api/SchoolAdmission.Api.csproj SchoolAdmission.Api/
RUN dotnet restore SchoolAdmission.Api/SchoolAdmission.Api.csproj

COPY SchoolAdmission.Domain/ SchoolAdmission.Domain/
COPY SchoolAdmission.Application/ SchoolAdmission.Application/
COPY SchoolAdmission.Infrastructure/ SchoolAdmission.Infrastructure/
COPY SchoolAdmission.Api/ SchoolAdmission.Api/
RUN dotnet publish SchoolAdmission.Api/SchoolAdmission.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true
COPY --from=build /app/publish .
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "SchoolAdmission.Api.dll"]

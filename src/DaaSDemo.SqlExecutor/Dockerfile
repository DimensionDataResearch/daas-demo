FROM microsoft/aspnetcore-build:2.0-jessie AS build

COPY DaaSDemo.SqlExecutor.csproj /build/
RUN [ "dotnet", "restore" ]

COPY . /build/
RUN [ "dotnet", "publish", "-o", "/out" ]

FROM microsoft/aspnetcore:2.0-2.0-jessie

COPY --from=build /out /app

EXPOSE 5000
ENV PORT=5000 ASPNET_URLS=http://localhost:5000

ENTRYPOINT [ "dotnet", "DaaSDemo.SqlExecutor.dll" ]

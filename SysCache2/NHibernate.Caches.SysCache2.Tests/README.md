To run the UnitTests against a database use:

docker run -d --name nh-sql -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Str0ng!Passw0rd123" -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest

docker exec -it nh-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'Str0ng!Passw0rd123' -C -Q "IF DB_ID('nhibernate') IS NULL CREATE DATABASE nhibernate;"


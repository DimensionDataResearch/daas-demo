using System;
using System.Text;

namespace DaaSDemo.Provisioning
{
    /// <summary>
    ///     T-SQL for management of a tenant's SQL server and its databases.
    /// </summary>
    public static class ManagementSql
    {
        /// <summary>
        ///     Generate T-SQL to configure server memory consumption.
        /// </summary>
        /// <param name="maxMemoryMB">
        ///     The maximum memory that the server should use.
        /// </param>
        /// <returns>
        ///     The T-SQL.
        /// </returns>
        public static string ConfigureServerMemory(int maxMemoryMB)
        {
            return $@"
                Use [master];

                Go

                Exec sys.sp_configure N'show advanced options', N'1'
                    Reconfigure With Override;
                
                Go

                Exec sys.sp_configure N'max server memory (MB)', N'{maxMemoryMB}'
                    Reconfigure With Override;
                
                Go

                Exec sys.sp_configure N'show advanced options', N'0'
                    Reconfigure With Override;

                Go
            ";
        }

        /// <summary>
        ///     Generate T-SQL for creating a database.
        /// </summary>
        /// <param name="databaseName">
        ///     The database name.
        /// </param>
        /// <returns>
        ///     The T-SQL.
        /// </returns>
        public static string CreateDatabase(string databaseName)
        {
            if (String.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'databaseName'.", nameof(databaseName));
            
            return $@"
                Use [master]
                
                Go

                Create Database [{databaseName}]
                On Primary
                (
                    Name = N'{databaseName}',
                    FileName = N'/var/opt/mssql/data/{databaseName}.mdf',
                    Size = 8192KB,
                    FileGrowth = 65536KB
                )
                Log On
                (
                    Name = N'{databaseName}_log',
                    FileName = N'/var/opt/mssql/data/{databaseName}_log.ldf',
                    Size = 8192KB,
                    FileGrowth = 65536KB
                )
                
                Go

                Use [{databaseName}]
                Go

                If Not Exists
                (
                    Select name
                    From sys.filegroups
                    Where
                        is_default=1
                        And
                        name = N'PRIMARY'
                )
                Begin
                    Alter Database [{databaseName}]
                    Modify FileGroup [PRIMARY] Default
                End

                Go
            ";
        }

        /// <summary>
        ///     Generate T-SQL for deleting a database.
        /// </summary>
        /// <param name="databaseName">
        ///     The database name.
        /// </param>
        /// <returns>
        ///     The T-SQL.
        /// </returns>
        public static string DropDatabase(string databaseName)
        {
            if (String.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'databaseName'.", nameof(databaseName));
            
            return $@"
                Use [master]
                
                Go

                Alter Database
                    [{databaseName}]
                Set
                    SINGLE_USER With Rollback Immediate

                Go

                Use [master]
                
                Go

                Drop Database [{databaseName}]

                Go
            ";
        }
    }
}
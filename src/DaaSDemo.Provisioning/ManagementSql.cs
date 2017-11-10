using System;
using System.Collections.Generic;
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
        public static IEnumerable<string> ConfigureServerMemory(int maxMemoryMB)
        {
            yield return "Use [master];";

            yield return @"
                Exec sys.sp_configure N'show advanced options', N'1'
                    Reconfigure With Override;
            ";

            yield return $@"
                Exec sys.sp_configure N'max server memory (MB)', N'{maxMemoryMB}'
                    Reconfigure With Override;
            ";

            yield return @"
                Exec sys.sp_configure N'show advanced options', N'0'
                    Reconfigure With Override;
            ";
        }

        /// <summary>
        ///     Generate T-SQL for creating a database.
        /// </summary>
        /// <param name="databaseName">
        ///     The database name.
        /// </param>
        /// <param name="userName">
        ///     The database user name.
        /// </param>
        /// <param name="password">
        ///     The database password.
        /// </param>
        /// <returns>
        ///     The T-SQL.
        /// </returns>
        public static IEnumerable<string> CreateDatabase(string databaseName, string userName, string password)
        {
            if (String.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'databaseName'.", nameof(databaseName));

            if (String.IsNullOrWhiteSpace(userName))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'userName'.", nameof(userName));
            
            if (String.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'password'.", nameof(password));

            // Escape password.
            // AF: It sucks, but CREATE LOGIN doesn't allow you to pass a parameter or variable for the password.
            password = password.Replace("'", "''");
            
            yield return $@"
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
            ";

            yield return $@"
                Use [{databaseName}]
            ";

            yield return $@"
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
            ";

            yield return @"
                Use [master]
            ";

            yield return $@"
                Create Login [{userName}]
                With
                    Password=N'{password}',
                    DEFAULT_DATABASE=[{databaseName}],
                    CHECK_EXPIRATION=OFF,
                    CHECK_POLICY=ON
            ";

            yield return $@"
                Use [{databaseName}]
            ";

            yield return $@"
                Create User [{userName}]
                    For Login [{userName}]
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
        public static IEnumerable<string> DropDatabase(string databaseName)
        {
            if (String.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'databaseName'.", nameof(databaseName));
            
            yield return $@"
                Alter Database
                    [{databaseName}]
                Set
                    SINGLE_USER With Rollback Immediate
            ";

            yield return $@"
                Drop Database [{databaseName}]
            ";
        }
    }
}

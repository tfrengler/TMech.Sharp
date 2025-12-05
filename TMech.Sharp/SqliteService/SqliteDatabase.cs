using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace TMech.Sharp.SqliteService
{
    /// <summary>
    /// Represents a wrapper around a Sqlite-database file from which you can spawn instances of <see cref="SqliteDbQuery"/> to perform queries.
    /// Meant as a singleton service to be instantiated (per database of course) and reused across the lifetime of the application.
    /// </summary>
    public sealed class SqliteDatabase : IDisposable
    {
        private readonly string _connectionString;
        private readonly SqliteConnection _writeConnection;
        private readonly FileInfo _DbFile;
        private bool _isDisposed;

        public string DbFile { get => _DbFile.FullName; }

        public SqliteDatabase(FileInfo databaseFile)
        {
            if (!SQLiteConcurrentWriter.Running)
            {
                throw new Exception("The SQLiteConcurrentWriter is not running");
            }

            ArgumentNullException.ThrowIfNull(databaseFile);
            if (!databaseFile.Exists) throw new Exception("Database file does not exist: " + databaseFile.FullName);
            if (databaseFile.IsReadOnly) throw new Exception("Database file is read-only: " + databaseFile.FullName);

            _DbFile = databaseFile;

            _connectionString = new SqliteConnectionStringBuilder()
            {
                DataSource = databaseFile.FullName,
                ForeignKeys = true,
                Mode = SqliteOpenMode.ReadWrite
            }.ToString();

            _writeConnection = new SqliteConnection(_connectionString);
            PrepareConnection(_writeConnection);
        }

        private static int NoCaseCollation(string x, string y)
        {
            return string.Compare(x.Trim(), y.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static void PrepareConnection(SqliteConnection connection)
        {
            try
            {
                connection.Open();
                connection.CreateCollation("NOCASE", NoCaseCollation);

                using var command = connection.CreateCommand();

                command.CommandText = """
                    PRAGMA journal_mode = WAL;
                    PRAGMA synchronous = NORMAL;
                    PRAGMA temp_store = MEMORY;
                    PRAGMA cache_size = 32768;
                """;

                command.ExecuteNonQuery();
                connection.Close();
            }
            catch (Exception error)
            {
                throw new Exception("Failed to prepare SQLite connection. Setting up PRAGMA's threw an error: " + error.Message);
            }
        }

        /// <summary>
        /// Creates a connection to the database and returns a <see cref="SqliteDbQuery"/> you can use to perform queries.
        /// </summary>
        public SqliteDbQuery CreateQuery()
        {
            var connection = new SqliteConnection(_connectionString);
            PrepareConnection(connection);
            return new SqliteDbQuery(connection, true);
        }

        /// <summary>
        /// Creates a query used for fetching data in the underlying database.
        /// Creates a new connection each time and as such is safe for use in a concurrent or async context.
        /// </summary>
        public SqliteDbQuery CreateReadQuery()
        {
            var connection = new SqliteConnection(_connectionString);
            PrepareConnection(connection);
            return new SqliteDbQuery(connection, true);
        }

        /// <summary>
        /// Creates a query used for mutating data in the underlying database.
        /// Not safe for reads in a concurrent or async context due to sharing a single, persistent connection for the life time of this instance.
        /// </summary>
        public SqliteDbQuery CreateWriteQuery()
        {
            return new SqliteDbQuery(_writeConnection, false);
        }

        #region DISPOSAL

        public void Dispose()
        {
            if (_isDisposed == true)
            {
                return;
            }

            _isDisposed = true;
            _writeConnection.Dispose();
        }

        #endregion
    }
}

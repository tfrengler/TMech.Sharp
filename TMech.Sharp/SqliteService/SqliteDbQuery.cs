using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace TMech.Sharp.SqliteService
{
    /// <summary>
    /// Represents a query to the database. Comes with a fluent API for constructing a query and executing it.
    /// </summary>
    public sealed class SqliteDbQuery : IDisposable
    {
        private bool _isDisposed;
        private bool _statementPrepared;
        private IDbCommand _command = null!;
        private SqliteTransaction? _transaction;
        private readonly SqliteConnection _connection;
        private readonly bool _disposeConnection;

        public SqliteDbQuery(SqliteConnection dbConnection, bool disposeConnection)
        {
            Debug.Assert(dbConnection is not null);
            _disposeConnection = disposeConnection;
            _connection = dbConnection;
            _connection.Open();

            Reset();
        }

        public SqliteDbQuery Reset()
        {
            _transaction?.Dispose();
            _transaction = null;
            _command?.Dispose();

            _statementPrepared = false;
            _command = _connection.CreateCommand();

            return this;
        }

        public SqliteDbQuery WithTransaction()
        {
            Debug.Assert(_command is not null, "WithTransaction: cannot begin a transaction because no command has been set yet");

            _transaction = _connection.BeginTransaction();
            _command.Transaction = _transaction;

            return this;
        }

        public SqliteDbQuery WithCommand(string command)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(command);
            Debug.Assert(!_statementPrepared, "WithCommand: query has already been prepared and cannot have its command changed");

            _statementPrepared = true;
            _command.CommandText = command.Trim();

            return this;
        }

        #region PARAMS

        public SqliteDbQuery WithParamString(string name, string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(value);

            _command.Parameters.Add(new SqliteParameter()
            {
                ParameterName = name,
                DbType = DbType.String,
                Value = value.Trim()
            });

            return this;
        }

        public SqliteDbQuery WithParamInt(string name, int value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            _command.Parameters.Add(new SqliteParameter()
            {
                ParameterName = name,
                DbType = DbType.Int32,
                Value = value
            });

            return this;
        }

        public SqliteDbQuery WithParamLong(string name, long value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            _command.Parameters.Add(new SqliteParameter()
            {
                ParameterName = name,
                DbType = DbType.Int64,
                Value = value
            });

            return this;
        }

        public SqliteDbQuery WithParamDouble(string name, double value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            _command.Parameters.Add(new SqliteParameter()
            {
                ParameterName = name,
                DbType = DbType.Double,
                Value = value
            });

            return this;
        }

        public SqliteDbQuery WithParamDateTime(string name, DateTime value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            _command.Parameters.Add(new SqliteParameter()
            {
                ParameterName = name,
                DbType = DbType.String,
                Value = value.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture)
            });

            return this;
        }

        public SqliteDbQuery WithParamDate(string name, DateOnly value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            _command.Parameters.Add(new SqliteParameter()
            {
                ParameterName = name,
                DbType = DbType.String,
                Value = value.ToString("o", CultureInfo.InvariantCulture)
            });

            return this;
        }

        public SqliteDbQuery WithParamTimeSpan(string name, TimeSpan value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            _command.Parameters.Add(new SqliteParameter()
            {
                ParameterName = name,
                DbType = DbType.Int64,
                Value = value.Ticks
            });

            return this;
        }

        public SqliteDbQuery WithParam(SqliteParameter parameter)
        {
            ArgumentNullException.ThrowIfNull(parameter);
            _command.Parameters.Add(parameter);
            return this;
        }

        /// <summary>
        /// Constructs an IN-clause string out of a list of values.
        /// </summary>
        /// <param name="values">The list of longs to turn into a IN-clause</param>
        /// <returns>A string representation of <paramref name="values"/> as an IN-clause in the form of: <c>(1,2,3)</c></returns>
        public static string AsInClause(IEnumerable<long> values)
        {
            return '(' + string.Join(',', values) + ')';
        }

        /// <summary>
        /// Constructs an IN-clause string out of a list of values.
        /// </summary>
        /// <param name="values">The list of strings to turn into a IN-clause</param>
        /// <returns>A string representation of <paramref name="values"/> as an IN-clause in the form of: <c>('first','second','third')</c></returns>
        public static string AsInClause(IEnumerable<string> values)
        {
            IEnumerable<string> Intermediate = values.Select(x => '\'' + x.Replace("'", "''") + '\'');
            return '(' + string.Join(',', Intermediate) + ')';
        }

        #endregion

        #region QUERY EXECUTION

        /// <summary>
        /// Executes the query and expects it to insert multiple record. Returns the id's of the rows inserted.
        /// </summary>
        /// <returns>The id's of the rows inserted. Throws an exception if no data was returned by the query or the data returned could not be converted to an array of id's (long).</returns>
        public long[] Insert()
        {
            return (long[])InternalExecute(Action.INSERT);
        }

        /// <summary>
        /// Executes the query and expects it to insert a single row. Returns the id of the row inserted.
        /// </summary>
        /// <returns>The id of the row inserted. Throws an exception if no data was returned by the query or the data returned could not be converted to an id (long).</returns>
        public long InsertSingle()
        {
            return (long)InternalExecute(Action.INSERT_SINGLE);
        }

        /// <summary>
        /// Executes the query with the expectation it affects a single row. In debug mode it asserts that 1 record was affected and throws if not.
        /// </summary>
        /// <returns>A bool where <c>True</c> indicates a single record was affected and <c>False</c> otherwise.</returns>
        public bool UpdateSingle()
        {
            return (long)InternalExecute(Action.UPDATE_SINGLE) == 1;
        }

        /// <summary>
        /// Executes the query and expects it to update data. In debug mode it asserts that at least 1 record was affected.
        /// </summary>
        /// <returns>The amount of rows affected.</returns>
        public long Update()
        {
            return (long)InternalExecute(Action.UPDATE);
        }

        /// <summary>
        /// Executes the query and returns the result.
        /// </summary>
        /// <returns>A result set representing the rows matching the query.</returns>
        public ResultSet Read()
        {
            return (ResultSet)InternalExecute(Action.READ);
        }

        /// <summary>
        /// Executes the query and returns the first record from the result.
        /// </summary>
        /// <returns>A record representing the first row matching the query or <see cref="DatabaseRecord.Empty"/> if no rows were matched.</returns>
        public DatabaseRecord ReadSingle()
        {
            return (DatabaseRecord)InternalExecute(Action.READ_SINGLE);
        }

        /// <summary>
        /// Executes the query and returns the value from the first column in the first row as a long.
        /// </summary>
        /// <returns>The long matching the query or 0 if no rows matched. Throws an exception if the value cannot be converted to a long.</returns>
        public long ReadLong()
        {
            return (long)InternalExecute(Action.READ_LONG);
        }

        /// <summary>
        /// Executes the query and returns the value from the first column in the first row as a string.
        /// </summary>
        /// <returns>The string matching the query or <see cref="string.Empty"/> if no rows matched. Throws an exception if the value cannot be converted to a string.</returns>
        public string ReadString()
        {
            return (string)InternalExecute(Action.READ_STRING);
        }

        /// <summary>
        /// Executes the query, and returns the amount of rows affected (if any). Has no expectations and does no assertions.
        /// </summary>
        public long Execute()
        {
            return Convert.ToInt64(InternalExecute(Action.EXECUTE));
        }

        #endregion

        #region PRIVATE

        private long[] _InsertMultiple()
        {
            IDataReader Reader = _command.ExecuteReader();
            List<long> ReturnData = new();

            while (Reader.Read())
            {
                long PrimaryKey = Convert.ToInt64(Reader[0]);
                Debug.Assert(PrimaryKey > 0, $"Expected ExecuteReader in _InsertMultiple to return a record set where the first column of each is the primary key for the new records but the value in row {PrimaryKey} is 0");
                ReturnData.Add(PrimaryKey);
            }

            Debug.Assert(ReturnData.Count > 0, "Expected ExecuteReader in _InsertMultiple to return a record set where the first column of each is the primary key for the new records but no records were returned");
            return ReturnData.ToArray();
        }

        private long _InsertSingle()
        {
            object? PrimaryKey = _command.ExecuteScalar();
            Debug.Assert(PrimaryKey is not null, "_InsertSingle: ExecuteScalar() returned null but expected a value (64-bit int)");
            long ReturnData = Convert.ToInt64(PrimaryKey);
            Debug.Assert(ReturnData > 0, "Expected ExecuteScalar() in _InsertSingle to return the primary key of the new record but instead it returned a zero or negative number");
            return ReturnData;
        }

        private long _Update()
        {
            int RowsAffected = _command.ExecuteNonQuery();
            long ReturnData = Convert.ToInt64(RowsAffected);
            Debug.Assert(ReturnData > 0, "Expected ExecuteNonQuery in update to affect at least one record but it affected none");
            return ReturnData;
        }

        private long _Execute()
        {
            return _command.ExecuteNonQuery();
        }

        private long _UpdateSingle()
        {
            int RowsAffected = _command.ExecuteNonQuery();
            long ReturnData = Convert.ToInt64(RowsAffected);
            Debug.Assert(ReturnData == 1, "Expected ExecuteNonQuery in updateSingle to affect a single record but it affected " + RowsAffected);
            return ReturnData;
        }

        private ResultSet _Read()
        {
            IDataReader Reader = _command.ExecuteReader();
            Debug.Assert(Reader is not null, "_Read: expected IDataReader to not be null");
            var Buffer = new List<DatabaseRecord>();
            var Columns = new Dictionary<string, int>();

            for (int index = 0; index < Reader.FieldCount; index++)
            {
                string ColumnName = Reader.GetName(index);
                Columns.Add(ColumnName, index);
            }

            while (Reader.Read())
            {
                var CurrentRow = new object[Reader.FieldCount];
                int ColumnCount = Reader.GetValues(CurrentRow);
                Debug.Assert(Reader.FieldCount == ColumnCount);
                Buffer.Add(new DatabaseRecord(CurrentRow, Columns));
            }

            return ResultSet.From(Buffer);
        }

        private DatabaseRecord _ReadSingle()
        {
            IDataReader Reader = _command.ExecuteReader();
            Debug.Assert(Reader is not null, "_ReadSingle: expected IDataReader to not be null");

            var Columns = new Dictionary<string, int>();

            for (int index = 0; index < Reader.FieldCount; index++)
            {
                string ColumnName = Reader.GetName(index);
                Columns.Add(ColumnName, index);
            }

            if (!Reader.Read())
            {
                return DatabaseRecord.Empty;
            }

            var TheRecord = (IDataRecord)Reader;
            var Buffer = new object[TheRecord.FieldCount];
            TheRecord.GetValues(Buffer);

            return new DatabaseRecord(Buffer, Columns);
        }

        private long _ReadLong()
        {
            object? Result = _command.ExecuteScalar();
            if (Result is null) return 0;
            return Convert.ToInt64(Result);
        }

        private string _ReadString()
        {
            object? Result = _command.ExecuteScalar();
            if (Result is null) return string.Empty;
            return Convert.ToString(Result) ?? string.Empty;
        }

        private enum Action
        {
            INSERT, INSERT_SINGLE, UPDATE, UPDATE_SINGLE,
            READ, EXECUTE, READ_LONG, READ_STRING, READ_SINGLE
        }

        private object InternalExecute(Action action)
        {
#warning Debug info that should be handled better
            //Console.WriteLine("[DatabaseQuery::InternalExecute] EXECUTING QUERY:" + Environment.NewLine + Command.CommandText + Environment.NewLine);
            Debug.Assert(_isDisposed == false);

            try
            {
                object ReturnData = action switch
                {
                    Action.READ => _Read(),
                    Action.READ_SINGLE => _ReadSingle(),
                    Action.READ_LONG => _ReadLong(),
                    Action.READ_STRING => _ReadString(),
                    Action.INSERT => _InsertMultiple(),
                    Action.INSERT_SINGLE => _InsertSingle(),
                    Action.UPDATE => _Update(),
                    Action.UPDATE_SINGLE => _UpdateSingle(),
                    Action.EXECUTE => _Execute(),
                    _ => throw new Exception()
                };

                _transaction?.Commit();

                return ReturnData;
            }
            catch (Exception error)
            {
                var ErrorOutput = new StringBuilder();
                ErrorOutput.AppendLine("Error executing query!");
                ErrorOutput.AppendLine("----------------- QUERY |" + Environment.NewLine + _command.CommandText.Trim());

                if (_command.Parameters.Count > 0)
                {
                    ErrorOutput.AppendLine("----------------- PARAMS |");
                    foreach (SqliteParameter param in _command.Parameters.Cast<SqliteParameter>())
                    {
                        ErrorOutput.AppendLine($"-| {param.ParameterName} | {param.Value?.ToString()}");
                    }
                }

                if (_transaction is not null)
                {
                    try
                    {
                        _transaction.Rollback();
                    }
                    catch (Exception ex2)
                    {
                        throw new Exception(string.Format("Query rollback failed. Exception Type: {0}. Message: {1}", ex2.GetType(), ex2.Message));
                    }
                }

                throw new Exception(ErrorOutput.ToString(), error);
            }
        }

        #endregion

        #region DISPOSAL

        public void Dispose()
        {
            if (_isDisposed == true || !_disposeConnection)
            {
                return;
            }

            _isDisposed = true;
            _connection?.Dispose();
        }

        #endregion
    }
}

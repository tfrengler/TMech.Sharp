using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace TMech.Sharp.SqliteService
{
    /// <summary>
    /// Represents a record from the database which is a wrapper around an array of raw objects with each index representing the value of that column. You can index into any column and/or enumerate over them as you wish.
    /// </summary>
    public sealed class DatabaseRecord : IEnumerable
    {
        public int ColumnCount { get => ColumnMappings.Count; }
        public List<string> Columns { get => ColumnMappings.Keys.ToList(); }
        public bool IsEmpty { get => Values.Length == 0; }

        private readonly object[] Values;
        private readonly Dictionary<string, int> ColumnMappings;

        private DatabaseRecord()
        {
            Values = [];
            ColumnMappings = [];
        }

        public DatabaseRecord(object[] values, Dictionary<string, int> columnMappings)
        {
            ArgumentNullException.ThrowIfNull(values);
            ArgumentNullException.ThrowIfNull(columnMappings);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(values.Length);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columnMappings.Count);

            Values = values;
            ColumnMappings = columnMappings;
        }

        public object this[int i] => Values[i];

        /// <summary>
        /// Represents the empty record where <see cref="ColumnCount"/> is 0 and indexing any value will always throw an exception.
        /// </summary>
        public static DatabaseRecord Empty { get; } = new DatabaseRecord();

        public IEnumerator GetEnumerator()
        {
            return Values.GetEnumerator();
        }

        private int GetColumnIndex(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            if (ColumnMappings.TryGetValue(name, out int columnIndex))
            {
                return columnIndex;
            }

            throw new Exception("There is no column in this record with the name: " + name);
        }

        public string ParseAsString(string columnName)
        {
            return ParseAsString(GetColumnIndex(columnName));
        }

        /// <summary>
        /// Attempts to parse the value in a given column as a string.
        /// </summary>
        /// <returns>The value as a string or <see cref="string.Empty"/> if the value is <see langword="null"/>.</returns>
        public string ParseAsString(int columnIndex)
        {
            ThrowOnOutOfBounds(columnIndex);
            return Convert.ToString(Values[columnIndex]) ?? string.Empty;
        }

        public int ParseAsInt(string columnName)
        {
            return ParseAsInt(GetColumnIndex(columnName));
        }

        /// <summary>
        /// Attempts to parse the value in a given column as a 32-bit integer.
        /// </summary>
        /// <returns>The value as an int or 0 if the value is <see langword="null"/>.</returns>
        public int ParseAsInt(int columnIndex)
        {
            ThrowOnOutOfBounds(columnIndex);
            return Convert.ToInt32(Values[columnIndex]);
        }

        public long ParseAsLong(string columnName)
        {
            return ParseAsLong(GetColumnIndex(columnName));
        }

        /// <summary>
        /// Attempts to parse the value in a given column as a 64-bit integer.
        /// </summary>
        /// <returns>The value as a long or 0 if the value is <see langword="null"/>.</returns>
        public long ParseAsLong(int columnIndex)
        {
            ThrowOnOutOfBounds(columnIndex);
            return Convert.ToInt64(Values[columnIndex]);
        }

        public float ParseAsFloat(string columnName)
        {
            return ParseAsFloat(GetColumnIndex(columnName));
        }

        /// <summary>
        /// Attempts to parse the value in a given column as a 32-bit single-precision float.
        /// </summary>
        /// <returns>The value as a float or 0.0 if the value is <see langword="null"/>.</returns>
        public float ParseAsFloat(int columnIndex)
        {
            ThrowOnOutOfBounds(columnIndex);
            return Convert.ToSingle(Values[columnIndex]);
        }

        public double ParseAsDouble(string columnName)
        {
            return ParseAsDouble(GetColumnIndex(columnName));
        }

        /// <summary>
        /// Attempts to parse the value in a given column as a 64-bit double-precision float.
        /// </summary>
        /// <returns>The value as a double or 0.0 if the value is <see langword="null"/>.</returns>
        public double ParseAsDouble(int columnIndex)
        {
            ThrowOnOutOfBounds(columnIndex);
            return Convert.ToDouble(Values[columnIndex]);
        }

        public DateTime ParseAsDateTime(string columnName)
        {
            return ParseAsDateTime(GetColumnIndex(columnName));
        }

        /// <summary>
        /// Attempts to parse the value in a given column as a DateTime-object.
        /// </summary>
        /// <returns>The value as a DateTime-object. If it fails an exception will be thrown.</returns>
        public DateTime ParseAsDateTime(int columnIndex)
        {
            ThrowOnOutOfBounds(columnIndex);
            return DateTime.Parse((string)Values[columnIndex], CultureInfo.InvariantCulture);
        }

        public DateOnly ParseAsDate(string columnName)
        {
            return ParseAsDate(GetColumnIndex(columnName));
        }

        /// <summary>
        /// Attempts to parse the value in a given column as a DateOnly-object.
        /// </summary>
        /// <returns>The value as a DateOnly-object. If it fails an exception will be thrown.</returns>
        public DateOnly ParseAsDate(int columnIndex)
        {
            ThrowOnOutOfBounds(columnIndex);
            return DateOnly.Parse((string)Values[columnIndex], CultureInfo.InvariantCulture);
        }

        public byte[] ParseAsBinary(string columnName)
        {
            return ParseAsBinary(GetColumnIndex(columnName));
        }

        public byte[] ParseAsBinary(int columnIndex)
        {
            ThrowOnOutOfBounds(columnIndex);
            var data = Values[columnIndex];
            if (data is DBNull) return [];

            return (byte[])data;
        }

        public T ParseAs<T>(int columnIndex)
        {
            return (T)Values[columnIndex];
        }

        [Conditional("DEBUG")]
        private void ThrowOnOutOfBounds(int index)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(index, ColumnCount - 1, "columnIndex");
            ArgumentOutOfRangeException.ThrowIfNegative(index, "columnIndex");
        }
    }
}

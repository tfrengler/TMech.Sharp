using System;
using System.Collections;
using System.Collections.Generic;

namespace TMech.Sharp.SqliteService
{
    /// <summary>
    /// Represents the result of executing a query and is a wrapper around a collection of <see cref="DatabaseRecord"/> instances.
    /// </summary>
    public sealed class ResultSet : IEnumerable<DatabaseRecord>
    {
        public int RecordCount { get => Rows.Count; }
        public bool IsEmpty { get => Rows.Count == 0; }

        private readonly IList<DatabaseRecord> Rows;

        private ResultSet(IList<DatabaseRecord> rows)
        {
            ArgumentNullException.ThrowIfNull(rows);
            Rows = rows;
        }

        public DatabaseRecord this[int i] => Rows[i];

        public IEnumerator<DatabaseRecord> GetEnumerator()
        {
            return Rows.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Rows.GetEnumerator();
        }

        /// <summary>
        /// Constructs a new instance around a list of <see cref="DatabaseRecord"/>-instances.
        /// </summary>
        public static ResultSet From(IList<DatabaseRecord> rows)
        {
            return new ResultSet(rows);
        }
    }
}

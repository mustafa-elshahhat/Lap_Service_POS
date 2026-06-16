using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace AlJohary.ServiceHub.Infrastructure.Data
{
    public class SqlExecutor
    {
        private readonly DatabaseManager _db;

        public SqlExecutor(DatabaseManager db)
        {
            _db = db;
        }

        public int Execute(string query, Dictionary<string, object> parameters = null, SqliteTransaction transaction = null)
        {
            lock (DatabaseManager.InstanceLock)
            {
                using (var cmd = new SqliteCommand(query, _db.GetConnection()))
                {
                    cmd.Transaction = transaction ?? _db.CurrentTransaction;
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                    }
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        public long ExecuteAndGetId(string query, Dictionary<string, object> parameters = null, SqliteTransaction transaction = null)
        {
            lock (DatabaseManager.InstanceLock)
            {
                using (var cmd = new SqliteCommand(query, _db.GetConnection()))
                {
                    cmd.Transaction = transaction ?? _db.CurrentTransaction;
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                    }
                    cmd.ExecuteNonQuery();

                    using (var idCmd = new SqliteCommand("SELECT last_insert_rowid()", _db.GetConnection()))
                    {
                        idCmd.Transaction = transaction ?? _db.CurrentTransaction;
                        return (long)idCmd.ExecuteScalar();
                    }
                }
            }
        }

        public Dictionary<string, object> FetchOne(string query, Dictionary<string, object> parameters = null)
        {
            lock (DatabaseManager.InstanceLock)
            {
                using (var cmd = new SqliteCommand(query, _db.GetConnection()))
                {
                    if (_db.CurrentTransaction != null) cmd.Transaction = _db.CurrentTransaction;
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var row = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            }
                            return row;
                        }
                    }
                }
                return null;
            }
        }

        public List<Dictionary<string, object>> FetchAll(string query, Dictionary<string, object> parameters = null)
        {
            lock (DatabaseManager.InstanceLock)
            {
                var results = new List<Dictionary<string, object>>();
                using (var cmd = new SqliteCommand(query, _db.GetConnection()))
                {
                    if (_db.CurrentTransaction != null) cmd.Transaction = _db.CurrentTransaction;
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var row = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            }
                            results.Add(row);
                        }
                    }
                }
                return results;
            }
        }

        public object FetchScalar(string query, Dictionary<string, object> parameters = null)
        {
            lock (DatabaseManager.InstanceLock)
            {
                using (var cmd = new SqliteCommand(query, _db.GetConnection()))
                {
                    if (_db.CurrentTransaction != null) cmd.Transaction = _db.CurrentTransaction;
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                    }
                    return cmd.ExecuteScalar();
                }
            }
        }
    }
}

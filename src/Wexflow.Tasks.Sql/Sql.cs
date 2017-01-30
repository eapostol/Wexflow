﻿using System;;
using Wexflow.Core;
using System.Threading;
using System.Xml.Linq;
using System.Data.SqlClient;
using Oracle.DataAccess.Client;
using MySql.Data.MySqlClient;
using System.Data.SQLite;
using Npgsql;
using System.IO;
using System.Data.OleDb;
using Teradata.Client.Provider;

namespace Wexflow.Tasks.Sql
{
    public enum Type
    {
        SqlServer,
        Access,
        Oracle,
        MySql,
        Sqlite,
        PostGreSql,
        Teradata
    }

    // TODO Oracle, Teradata, Access?

    public class Sql:Task
    {
        public Type DbType {get; set;}
        public string ConnectionString { get; set; }
        public string SqlScript { get; set; }

        public Sql(XElement xe, Workflow wf)
            : base(xe, wf)
        {
            DbType = (Type)Enum.Parse(typeof(Type), GetSetting("type"), true);
            ConnectionString = GetSetting("connectionString");
            SqlScript = GetSetting("sql", string.Empty);
        }

        public override TaskStatus Run()
        {
            Info("Executing SQL scripts...");

            bool success = true;
            bool atLeastOneSucceed = false;

            // Execute SqlScript if necessary
            try
            {
                if (!string.IsNullOrEmpty(SqlScript))
                {
                    ExecuteSql(SqlScript);
                    Info("The script has been executed through the sql option of the task.");
                }
            }
            catch (ThreadAbortException)
            {
                throw;
            }
            catch (Exception e)
            {
                ErrorFormat("An error occured while executing sql script. Error: {0}", e.Message);
                success = false;
            }

            // Execute SQL files scripts
            foreach (FileInf file in SelectFiles())
            {
                try
                {
                    var sql = File.ReadAllText(file.Path);
                    ExecuteSql(sql);
                    InfoFormat("The script {0} has been executed.", file.Path);

                    if (!atLeastOneSucceed) atLeastOneSucceed = true;
                }
                catch (ThreadAbortException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    ErrorFormat("An error occured while executing sql script {0}. Error: {1}", file.Path, e.Message);
                    success = false;
                }
            }

            Status status = Status.Success;

            if (!success && atLeastOneSucceed)
            {
                status = Status.Warning;
            }
            else if (!success)
            {
                status = Status.Error;
            }

            Info("Task finished.");
            return new TaskStatus(status, false);
        }

        void ExecuteSql(string sql)
        {
            switch (DbType)
            {
                case Type.SqlServer:
                    using (SqlConnection conn = new SqlConnection(ConnectionString))
                    {
                        var comm = new SqlCommand(sql, conn);
                        conn.Open();
                        comm.ExecuteNonQuery();
                    }
                    break;
                case Type.Access:
                    using (OleDbConnection conn = new OleDbConnection(ConnectionString))
                    {
                        var comm = new OleDbCommand(sql, conn);
                        conn.Open();
                        comm.ExecuteNonQuery();
                    }
                    break;
                case Type.Oracle:
                    using (OracleConnection conn = new OracleConnection(ConnectionString))
                    {
                        var comm = new OracleCommand(sql, conn);
                        conn.Open();
                        comm.ExecuteNonQuery();
                    }
                    break;
                case Type.MySql:
                    using (MySqlConnection conn = new MySqlConnection(ConnectionString))
                    {
                        var comm = new MySqlCommand(sql, conn);
                        conn.Open();
                        comm.ExecuteNonQuery();
                    }
                    break;
                case Type.Sqlite:
                    using (SQLiteConnection conn = new SQLiteConnection(ConnectionString))
                    {
                        var comm = new SQLiteCommand(sql, conn);
                        conn.Open();
                        comm.ExecuteNonQuery();
                    }
                    break;
                case Type.PostGreSql:
                    using (NpgsqlConnection conn = new NpgsqlConnection(ConnectionString))
                    {
                        var comm = new NpgsqlCommand(sql, conn);
                        conn.Open();
                        comm.ExecuteNonQuery();
                    }
                    break;
                case Type.Teradata:
                    using (TdConnection conn = new TdConnection(ConnectionString))
                    {
                        var comm = new TdCommand(sql, conn);
                        conn.Open();
                        comm.ExecuteNonQuery();
                    }
                    break;
            }
        }
    }
}
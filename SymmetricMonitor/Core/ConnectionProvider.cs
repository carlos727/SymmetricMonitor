using Dapper;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Data.SqlClient;

namespace SymmetricMonitor.Core
{
    public class ConnectionProvider
    {
        DbConnection conn;
        string connectionString;
        DbProviderFactory factory;

        // Constructor that retrieves the connectionString from the config file
        public ConnectionProvider()
        {
            this.connectionString = ConfigurationManager.ConnectionStrings["EvaConn"].ConnectionString.ToString();
            factory = DbProviderFactories.GetFactory(ConfigurationManager.ConnectionStrings["EvaConn"].ProviderName.ToString());
        }

        // Constructor that accepts the connectionString and Database ProviderName i.e SQL or Oracle
        public ConnectionProvider(string connectionString, string connectionProviderName)
        {
            this.connectionString = connectionString;
            factory = DbProviderFactories.GetFactory(connectionProviderName);
        }

        // Only inherited classes can call this.
        public DbConnection GetOpenConnection()
        {
            conn = factory.CreateConnection();
            conn.ConnectionString = this.connectionString;
            conn.Open();

            return conn;
        }


        public IEnumerable<T> Query<T>(string sql, object param = null)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                return conn.Query<T>(sql, param);
            }
        }

        public int Execute(string sql, object param = null)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                return conn.Execute(sql, param);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Configuration;
using MySql.Data.MySqlClient;
namespace B040.Authentication
{
    public static class MariaDBHelper
    {
        private static string ConnectionString { get; } = Environment.GetEnvironmentVariable("B040_AUTH_MYSQL_CONNECTIONSTRING",EnvironmentVariableTarget.Machine);

        public static List<T> ExecuteQuery<T>(string query, Func<IDataReader, T> mapFunction, params MySqlParameter[] parameters)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new MySqlCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    using (var reader = command.ExecuteReader())
                    {
                        var results = new List<T>();
                        while (reader.Read())
                        {
                            results.Add(mapFunction(reader));
                        }
                        return results;
                    }
                }
            }
        }

        public static void ExecuteNonQuery(string query, params MySqlParameter[] parameters)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new MySqlCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    command.ExecuteNonQuery();
                }
            }
        }
    } 
}

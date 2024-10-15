using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using ReviewVisualizer.Data.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.Data.Sqlite;
using ReviewVisualizer.Data.Models;

namespace ReviewVisualizer.Data
{
    public class QueueController : IQueueController
    {
        public static object _lock = new object();
        private string _connectionString;

        public QueueController([FromServices] IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("QueueConnection");
        }

        public void AddReview(ReviewCreateDTO review)
        {
            lock (_lock)
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText =
                    @"
                    INSERT INTO Queue (Data) VALUES (@data)    
                ";

                command.Parameters.AddWithValue("@data", JsonSerializer.Serialize(review));

                command.ExecuteNonQuery();
            }
        }

        public Review? GetReview()
        {
            lock (_lock)
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                using var selectCommand = connection.CreateCommand();
                selectCommand.CommandText =
                    @"
                        SELECT r.Data
                        FROM (
                            SELECT q.Data, min(q.Id)
                            FROM Queue q
                        ) r;
                    ";

                string? data = selectCommand.ExecuteScalar() as string;
                if (data is null)
                    return null;

                Review? review = JsonSerializer.Deserialize<Review>(data);

                using var deleteCommand = connection.CreateCommand();
                deleteCommand.CommandText =
                    @"
                        DELETE FROM Queue
                        WHERE Id = (
                            SELECT min(q2.Id)
                            FROM Queue q2
                        );
                    ";
                deleteCommand.ExecuteNonQuery();

                return review;
            }
        }

        public int GetQueueSize()
        {
            lock (_lock)
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText =
                    @"
                        SELECT COUNT(*) FROM Queue    
                    ";

                long? data = (long?)command.ExecuteScalar();

                return (int)(data ?? 0);
            }
        }
    }
}

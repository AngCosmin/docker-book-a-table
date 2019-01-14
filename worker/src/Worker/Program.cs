using System;
using System.Data.Common;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Newtonsoft.Json;
using Npgsql;
using StackExchange.Redis;
using System.Collections.Generic;

namespace Worker
{
    public class Restaurant
    {
        public Restaurant() {
        }

        public int Id { get; set; }
        public String Name { get; set; }
        public String Email { get; set; }
        public String Logo { get; set; }
    }

    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                var pgsql = OpenDbConnection("Server=db;Username=postgres;");
                var redisConn = OpenRedisConnection("redis");
                var redis = redisConn.GetDatabase();

                // Keep alive is not implemented in Npgsql yet. This workaround was recommended:
                // https://github.com/npgsql/npgsql/issues/1214#issuecomment-235828359
                var keepAliveCommand = pgsql.CreateCommand();
                keepAliveCommand.CommandText = "SELECT 1";

                var definitionReserve = new { restaurant_id = 0, name = "", phone = "", date = "", time = "" };
                var definitionGetRestaurants = new { user_id = "" };
                string json;

                while (true)
                {
                    // Slow down to prevent CPU spike, only query each 100ms
                    Thread.Sleep(100);

                    // Reconnect redis if down
                    if (redisConn == null || !redisConn.IsConnected) {
                        Console.WriteLine("Reconnecting Redis");
                        redisConn = OpenRedisConnection("redis");
                        redis = redisConn.GetDatabase();
                    }

                    // string json = redis.ListLeftPopAsync("votes").Result;
                    
                    json = redis.ListLeftPopAsync("get_restaurants").Result;
                    if (json != null) {
                        Console.WriteLine("Get restaurants");

                        var command = pgsql.CreateCommand();
                        command.CommandText = "SELECT * FROM restaurants";

                        using (NpgsqlDataReader reader = command.ExecuteReader()) {
                            List<Restaurant> restaurants = new List<Restaurant>();
                            while (reader.Read())
                            {
                                Console.WriteLine(reader.GetValue(0).ToString());

                                Restaurant restaurant = new Restaurant();
                                restaurant.Id = (int)reader.GetValue(0);
                                restaurant.Name = reader.GetValue(1).ToString();
                                restaurant.Email = reader.GetValue(2).ToString();
                                restaurant.Logo = reader.GetValue(6).ToString();

                                restaurants.Add(restaurant);
                            }

                            redis.StringSet("restaurants", JsonConvert.SerializeObject(restaurants));
                        }
                    }
                    else
                    {
                        keepAliveCommand.ExecuteNonQuery();
                    }

                    json = redis.ListLeftPopAsync("reserve").Result;
                    if (json != null) {
                        Console.WriteLine("Reserve");
                        Console.WriteLine(json);

                        var reserve = JsonConvert.DeserializeAnonymousType(json, definitionReserve);

                        if (!pgsql.State.Equals(System.Data.ConnectionState.Open))
                        {
                            Console.WriteLine("Reconnecting DB");
                            pgsql = OpenDbConnection("Server=db;Username=postgres;");
                        }
                        else
                        { 
                            Reserve(pgsql, reserve.restaurant_id, reserve.name, reserve.phone, reserve.date, reserve.time);
                        }
                    }
                    else
                    {
                        keepAliveCommand.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return 1;
            }
        }

        private static NpgsqlConnection OpenDbConnection(string connectionString)
        {
            NpgsqlConnection connection;

            while (true)
            {
                try
                {
                    connection = new NpgsqlConnection(connectionString);
                    connection.Open();
                    break;
                }
                catch (SocketException)
                {
                    Console.Error.WriteLine("Waiting for db");
                    Thread.Sleep(1000);
                }
                catch (DbException)
                {
                    Console.Error.WriteLine("Waiting for db");
                    Thread.Sleep(1000);
                }
            }

            Console.Error.WriteLine("Connected to db");

            var command = connection.CreateCommand();
            command.CommandText = @"CREATE TABLE IF NOT EXISTS votes (
                                        id VARCHAR(255) NOT NULL UNIQUE,
                                        vote VARCHAR(255) NOT NULL
                                    )";
            command.ExecuteNonQuery();

            return connection;
        }

        private static ConnectionMultiplexer OpenRedisConnection(string hostname)
        {
            // Use IP address to workaround https://github.com/StackExchange/StackExchange.Redis/issues/410
            var ipAddress = GetIp(hostname);
            Console.WriteLine($"Found redis at {ipAddress}");

            while (true)
            {
                try
                {
                    Console.Error.WriteLine("Connecting to redis");
                    return ConnectionMultiplexer.Connect(ipAddress);
                }
                catch (RedisConnectionException)
                {
                    Console.Error.WriteLine("Waiting for redis");
                    Thread.Sleep(1000);
                }
            }
        }

        private static string GetIp(string hostname)
            => Dns.GetHostEntryAsync(hostname)
                .Result
                .AddressList
                .First(a => a.AddressFamily == AddressFamily.InterNetwork)
                .ToString();

        private static void Reserve(NpgsqlConnection connection, int restaurant_id, string name, string phone, string date, string time)
        {
            var command = connection.CreateCommand();
            try
            {
                Console.WriteLine($"{restaurant_id} {name} {phone} {date} {time}");

                command.CommandText = "INSERT INTO reserve (restaurant_id, name, phone, date, time) VALUES (@restaurant_id, @name, @phone, @date, @time)";
                command.Parameters.AddWithValue("@restaurant_id", restaurant_id);
                command.Parameters.AddWithValue("@name", name);
                command.Parameters.AddWithValue("@phone", phone);
                command.Parameters.AddWithValue("@date", date);
                command.Parameters.AddWithValue("@time", time);
                command.ExecuteNonQuery();
            }
            catch (DbException)
            {
                Console.WriteLine("There was an error");
                command.Dispose();
            }
            finally
            {
                command.Dispose();
            }
        }
    }
}

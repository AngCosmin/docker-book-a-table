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

                        try {
                            using (NpgsqlDataReader reader = command.ExecuteReader()) {
                                List<Restaurant> restaurants = new List<Restaurant>();
                                while (reader.Read())
                                {
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
                        catch(Exception) {
                            Console.WriteLine("Exception");
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
            command.CommandText = @"CREATE TABLE IF NOT EXISTS restaurants(id SERIAL PRIMARY KEY, name VARCHAR(60) NOT NULL, email VARCHAR(80) NOT NULL, phone VARCHAR(20), password VARCHAR(50) NOT NULL, no_places INT NOT NULL, logo VARCHAR(255), created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP);";
            command.ExecuteNonQuery();

            command.CommandText = @"CREATE TABLE IF NOT EXISTS reserve(id SERIAL PRIMARY KEY, restaurant_id INT REFERENCES restaurants(id), name VARCHAR(30), phone VARCHAR(20), date VARCHAR(15) NOT NULL, time VARCHAR(10) NOT NULL, created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP);";
            command.ExecuteNonQuery();

            try {
                command.CommandText = @"INSERT INTO restaurants VALUES (1, 'Oscar', 'oscar@gmail.com', '0728441126', 'oscar', 20, 'https://scontent.fotp3-2.fna.fbcdn.net/v/t1.0-9/29315152_1781851515455465_4033382164665423923_n.jpg?_nc_cat=106&_nc_ht=scontent.fotp3-2.fna&oh=f0cce5a143ec2de9d7a7d89bc0ec2a26&oe=5CD48B81', CURRENT_TIMESTAMP)";
                command.ExecuteNonQuery();
            }
            catch(Exception) {
                Console.WriteLine("Duplicate");
            }

            try {
                command.CommandText = @"INSERT INTO restaurants VALUES (2, 'Milia', 'milia@gmail.com', '0728441127', 'milia', 10, 'https://scontent.fotp3-1.fna.fbcdn.net/v/t1.0-9/21740526_772983509568833_452600469297350631_n.jpg?_nc_cat=111&_nc_ht=scontent.fotp3-1.fna&oh=6521c06accafb5ce6353e073a499a4d5&oe=5CCBCB2B', CURRENT_TIMESTAMP);";
                command.ExecuteNonQuery();
            }
            catch(Exception) {
                Console.WriteLine("Duplicate");
            }

            try {
                command.CommandText = @"INSERT INTO restaurants VALUES (3, 'Star', 'star@gmail.com', '0728441128', 'star', 5, 'https://scontent.fotp3-1.fna.fbcdn.net/v/t1.0-9/11059322_827093123994184_7802084377421279853_n.jpg?_nc_cat=111&_nc_ht=scontent.fotp3-1.fna&oh=94c2eec4edfa453ae2433970ada6b0e5&oe=5CCE622C', CURRENT_TIMESTAMP);";
                command.ExecuteNonQuery();
            }
            catch(Exception) {
                Console.WriteLine("Duplicate");
            }

            try {
                command.CommandText = @"INSERT INTO restaurants VALUES (4, 'Dominos', 'dominos@gmail.com', '0728441129', 'dominos', 6, 'https://scontent.fotp3-3.fna.fbcdn.net/v/t1.0-9/48365942_1974458319257672_7851144288023871488_n.png?_nc_cat=104&_nc_ht=scontent.fotp3-3.fna&oh=83876346bab9ca8a919a98bbef3fa79f&oe=5CBD71F2', CURRENT_TIMESTAMP);";
                command.ExecuteNonQuery();
            }
            catch(Exception) {
                Console.WriteLine("Duplicate");
            }

            try {
                command.CommandText = @"INSERT INTO restaurants VALUES (5, 'PizzaHut', 'pizzahut@gmail.com', '0728441130', 'pizzahut', 3, 'https://scontent.fotp3-2.fna.fbcdn.net/v/t1.0-9/18527715_1639913159355482_2840008991308881657_n.png?_nc_cat=106&_nc_ht=scontent.fotp3-2.fna&oh=b47872d06366d27a8faa5282d08f799f&oe=5CC3EC40', CURRENT_TIMESTAMP);";
                command.ExecuteNonQuery();
            }
            catch(Exception) {
                Console.WriteLine("Duplicate");
            }

            try {
                command.CommandText = @"INSERT INTO restaurants VALUES (6, 'Panini', 'panini@gmail.com', '0728441131', 'Panini', 10, 'https://scontent.fotp3-1.fna.fbcdn.net/v/t1.0-9/21077723_1750708831638372_6518113221077813021_n.jpg?_nc_cat=108&_nc_ht=scontent.fotp3-1.fna&oh=227b80ed2ec125fa3e4cabb01ea966d8&oe=5CCD7951', CURRENT_TIMESTAMP);";
                command.ExecuteNonQuery();
            }
            catch(Exception) {
                Console.WriteLine("Duplicate");
            }

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

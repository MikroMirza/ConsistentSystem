using System;
using System.IO;
using System.Data.SQLite;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ServiceModel.Description;

namespace WcfSensorService
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class SensorService : ISensorService
    {
        readonly string _dbPath;
        readonly object _alignLock = new object();
        bool _isAligning = false;

        public SensorService(string dbPath)
        {
            _dbPath = dbPath;
            CreateDb();
        }

        private SQLiteConnection GetDatabaseConnection()
        {
            return new SQLiteConnection($"Data Source={_dbPath};Version=3;");
        }

        private void CreateDb()
        {
            if (!File.Exists(_dbPath))
            {
                SQLiteConnection.CreateFile(_dbPath);
                using (var c = GetDatabaseConnection())
                {
                    c.Open();
                    using (var cmd = c.CreateCommand())
                    {
                        cmd.CommandText = "CREATE TABLE readings(id INTEGER PRIMARY KEY AUTOINCREMENT, ts DATETIME DEFAULT CURRENT_TIMESTAMP, value REAL);";
                        cmd.ExecuteNonQuery();
                    }
                }
                
            }
        }

        public double GetLatest()
        {
            //Wait if aligning
            lock (_alignLock)
            {
                while (_isAligning) Monitor.Wait(_alignLock);
            }

            using (var c = GetDatabaseConnection())
            {
                c.Open();
                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText = "SELECT value FROM readings ORDER BY id DESC LIMIT 1";
                    var r = cmd.ExecuteScalar();
                    return r == null ? double.NaN : Convert.ToDouble(r);
                }
            }
        }

        public void SetLatest(double value)
        {
            lock (_alignLock)
            {
                _isAligning = true;
            }

            try
            {
                using (var c = GetDatabaseConnection())
                {
                    c.Open();
                    using (var cmd = c.CreateCommand())
                    {
                        cmd.CommandText = "INSERT INTO readings(value) VALUES(@v);";
                        cmd.Parameters.AddWithValue("@v", value);
                        cmd.ExecuteNonQuery();
                        Console.WriteLine($"[{_dbPath}] poravnjanje! nova temperatura = {value}");
                    }
                }
            }
            finally
            {
                lock (_alignLock)
                {
                    _isAligning = false;
                    Monitor.PulseAll(_alignLock);
                }
            }
        }

        public void StartGenerating()
        {
            var rnd = new Random((int)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(rnd.Next(1000, 10000)); // 1-10 seconds
                    double temp = Math.Round(15 + rnd.NextDouble() * 20, 2);
                    using (var c = GetDatabaseConnection())
                    {
                        c.Open();
                        using (var cmd = new SQLiteCommand("INSERT INTO readings(value) VALUES(@v);", c))
                        {
                            cmd.Parameters.AddWithValue("@v", temp);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    Console.WriteLine($"[{_dbPath}] nova temperatura = {temp}");
                }
            });
        }
    }

    public class Program
    {
        static void Main()
        {
            Console.WriteLine("Pokrećem 3 senzora u paralelnim threadovima...");

            // Run 3 service threads
            for (int i = 0; i < 3; i++)
            {
                int port = 9001 + i;
                string dbName = $"sensor-{i + 1}.db";

                int copy = i;
                new Thread(() => StartSensorInstance(port, dbName)).Start();
                Thread.Sleep(1000);
            }

            Console.WriteLine("Svi senzori aktivni! Pritisni ENTER za izlaz...");
            Console.ReadLine();
        }

        static void StartSensorInstance(int port, string dbName)
        {
            var baseAddress = new Uri($"http://localhost:{port}/SensorService");
            var service = new SensorService(dbName);
            var host = new ServiceHost(service, baseAddress);

            host.AddServiceEndpoint(typeof(ISensorService), new BasicHttpBinding(), "");

            var smb = new ServiceMetadataBehavior { HttpGetEnabled = true };
            host.Description.Behaviors.Add(smb);

            // MEX endpoint
            host.AddServiceEndpoint(
                ServiceMetadataBehavior.MexContractName,
                MetadataExchangeBindings.CreateMexHttpBinding(),
                "mex"
            );

            host.Open();

            Console.WriteLine($"[Sensor-{port}] sluša na {baseAddress}");
            service.StartGenerating();

            // Block the thread until shutdown
            Thread.Sleep(Timeout.Infinite);
        }
    }
}

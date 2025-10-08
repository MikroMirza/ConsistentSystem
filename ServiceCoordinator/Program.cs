using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceCoordinator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Kreiramo proxy-je prema servisima
            var sensor1 = new ServiceReference1.SensorServiceClient();
            var sensor2 = new ServiceReference2.SensorServiceClient();
            var sensor3 = new ServiceReference3.SensorServiceClient();

            Console.WriteLine("Koordinator pokrenut. Svakih 60s ažurira temperaturu na prosek.\n");

            while (true)
            {
                Task<double>[] latestTempTasks = { sensor1.GetLatestAsync(), sensor2.GetLatestAsync(), sensor3.GetLatestAsync()};
                double[] temps = await Task.WhenAll(latestTempTasks);

                double avg = temps.Average();
                Console.WriteLine($"[Koordinator] Prosek poslednjih merenja: {avg:F2}°C");


                Task[] setTempTasks = { sensor1.SetLatestAsync(avg), sensor2.SetLatestAsync(avg), sensor3.SetLatestAsync(avg) };
                await Task.WhenAll(setTempTasks);
                Console.WriteLine("[Koordinator] Temperatura svih senzora ažurirana na prosek.\n");

                Thread.Sleep(60000);
            }
        }

    }
}

using System;

namespace ServiceClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var client1 = new ServiceReference1.SensorServiceClient();
            var client2 = new ServiceReference2.SensorServiceClient();
            var client3 = new ServiceReference3.SensorServiceClient();

            Console.WriteLine("Klijent pokrenut. Pritisnite ENTER da vidite najnovije informacije. Napisite exit da ugasite klijenta.");
            while (true)
            {
                var line = Console.ReadLine();
                if (line == "exit") break;

                try
                {
                    double[] latest =
                    {
                        client1.GetLatest(),
                        client2.GetLatest(),
                        client3.GetLatest()
                    };

                    double avg = (latest[0] + latest[1] + latest[2]) / 3.0;
                    Console.WriteLine($"Senzor 1: {latest[0]}, Senzor 2: {latest[1]}, Senzor 3: {latest[2]}, Prosek: {avg}");

                    int inRange = 0;
                    foreach (double d in latest) if (Math.Abs(d - avg) <= 5.0) inRange++;

                    if (inRange >= 2)
                        Console.WriteLine($"OK, bar 2 senzora su u intervalu ±5 od proseka");
                    else
                    {
                        Console.WriteLine($"PROBLEM, manje od 2 senzora su u intervalu od ±5 od proseka");
                        client1.SetLatest(avg);
                        client2.SetLatest(avg);
                        client3.SetLatest(avg);
                        Console.WriteLine($"Zahtev za poravnanje poslat");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Greska: " + ex.Message);
                }
            }
        }
    }
}

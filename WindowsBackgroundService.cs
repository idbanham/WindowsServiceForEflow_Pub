using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;

namespace App.WindowsService;

public sealed class WindowsBackgroundService : BackgroundService
{
    private readonly JokeService _jokeService;
    private readonly ILogger<WindowsBackgroundService> _logger;
    private static DeviceClient deviceClient;
    //Use a connection string here for local testing
    //private readonly static string connectionString = "<Connection String>";
    private string connectionString = null;

    public WindowsBackgroundService(
        JokeService jokeService,
        ILogger<WindowsBackgroundService> logger) =>
        (_jokeService, _logger) = (jokeService, logger);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("IoT Hub C# Simulated Cave Device. Ctrl-C to exit.\n");
        _logger.LogWarning("Starting IoT Hub C# Simulated Cave Device.\n");

        //Get Connection String from Env
        string[] arguments = Environment.GetCommandLineArgs();
        connectionString = arguments[1];
        _logger.LogInformation("Connection String = " + arguments[1]);

        //InstallCACert();

        // Connect to the IoT hub using the MQTT protocol
        if (connectionString != null)
        {
            deviceClient = DeviceClient.CreateFromConnectionString(connectionString, TransportType.Mqtt);
            SendDeviceToCloudMessagesAsync();
            //Console.ReadLine();
        }
        else
        {
            Console.WriteLine("Connection String Null");
            _logger.LogWarning("Connection String Null");
            stoppingToken.Equals(true);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            string joke = _jokeService.GetJoke();
            _logger.LogWarning(joke);
            
            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
        }
    }

    static void InstallCACert()
    {
        string trustedCACertPath = "c:\\certs\\certs\\azure-iot-test-only.root.ca.cert.pem";
        if (!string.IsNullOrWhiteSpace(trustedCACertPath))
        {
            Console.WriteLine("User configured CA certificate path: {0}", trustedCACertPath);
            if (!File.Exists(trustedCACertPath))
            {
                // cannot proceed further without a proper cert file
                Console.WriteLine("Certificate file not found: {0}", trustedCACertPath);
                //_logger.LogWarning("Certificate file not found: {0}", trustedCACertPath);
                throw new InvalidOperationException("Invalid certificate file.");
            }
            else
            {
                Console.WriteLine("Attempting to install CA certificate: {0}", trustedCACertPath);
                X509Store store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
                try
                {
                    store.Open(OpenFlags.ReadWrite);
                    store.Add(new X509Certificate2(X509Certificate.CreateFromCertFile(trustedCACertPath)));
                }
                catch (Exception a)
                {
                    Console.WriteLine("Exception = " + a.Message);
                    throw;
                }
                
                Console.WriteLine("Successfully added certificate: {0}", trustedCACertPath);
                //_logger.LogInformation("Successfully added certificate: {0}", trustedCACertPath);
                store.Close();
            }
        }
        else
        {
            Console.WriteLine("trustedCACertPath was not set or null, not installing any CA certificate");

        }
    }

    // Async method to send simulated telemetry
    private static async void SendDeviceToCloudMessagesAsync()
    {
        // Create an instance of our sensor 
        var sensor = new EnvironmentSensor();

        while (true)
        {
            // read data from the sensor
            var currentTemperature = sensor.ReadTemperature();
            var currentHumidity = sensor.ReadHumidity();

            var messageString = CreateMessageString(currentTemperature, currentHumidity);
            var message = new Message(Encoding.ASCII.GetBytes(messageString));

            // Add a custom application property to the message.
            // An IoT hub can filter on these properties without access to the message body.
            message.Properties.Add("temperatureAlert", (currentTemperature > 30) ? "true" : "false");

            // Send the telemetry message
            await deviceClient.SendEventAsync(message);
            Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);

            await Task.Delay(1000);
        }
    }

    private static string CreateMessageString(double temperature, double humidity)
    {
        // Create an anonymous object that matches the data structure we wish to send
        var telemetryDataPoint = new
        {
            temperature = temperature,
            humidity = humidity
        };

        // Create a JSON string from the anonymous object
        return JsonConvert.SerializeObject(telemetryDataPoint);
    }

    /// <summary>
    /// This class represents a sensor 
    /// real-world sensors would contain code to initialize
    /// the device or devices and maintain internal state
    /// a real-world example can be found here: https://bit.ly/IoT-BME280
    /// </summary>
    internal class EnvironmentSensor
    {
        // Initial telemetry values
        double minTemperature = 20;
        double minHumidity = 60;
        Random rand = new Random();

        internal EnvironmentSensor()
        {
            // device initialization could occur here
        }

        internal double ReadTemperature()
        {
            return minTemperature + rand.NextDouble() * 15;
        }

        internal double ReadHumidity()
        {
            return minHumidity + rand.NextDouble() * 20;
        }
    }
}



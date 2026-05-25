using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Windows;

namespace TelemetryApp.Wpf
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _httpClient = new();
        private readonly ILogger<MainWindow> _logger;

        public MainWindow(ILogger<MainWindow> logger)
        {
            InitializeComponent();
            _logger = logger;

            _logger.LogInformation("MainWindow created");
        }

        private void CreateError_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                throw new InvalidOperationException("Test error from WPF client");
            }
            catch (Exception ex)
            {
                LogOutput($"ERROR: {ex.Message}");
                _logger.LogError(ex, "Request failed");
            }
        }

        private async void MakeRequestGetWeatherforecast_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var url = "https://localhost:5073/weatherforecast";

                LogOutput("Sending request GetWeatherforecast...");
                _logger.LogInformation("Sending request GetWeatherforecast...");

                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                LogOutput($"Response: {content}");
            }
            catch (Exception ex)
            {
                LogOutput($"Request failed: {ex.Message}");
                _logger.LogError(ex, "Request failed");
            }
        }
        private async void MakeRequestGetException_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var url = "https://localhost:5073/exception";

                LogOutput("Sending request GetException...");

                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                LogOutput($"Response: {content}");
            }
            catch (Exception ex)
            {
                LogOutput($"Request failed: {ex.Message}");
                _logger.LogError(ex, "Request failed");
            }
        }

        private void LogOutput(string message)
        {
            OutputBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            OutputBox.ScrollToEnd();
        }
    }
}
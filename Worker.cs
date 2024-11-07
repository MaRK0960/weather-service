using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using System.Net;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Text;
using weather_service.Models;

namespace weather_service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                Weather? weather = await GetWeather(stoppingToken);

                if (weather == null)
                {
                    _logger.LogError("Failed to get weather forcast");
                    return;
                }

                Day day = weather.forecast.forecastday[0].day;

                TableServiceClient tableServiceClient = new("***REMOVED***");
                Response<TableItem> table = await tableServiceClient.CreateTableIfNotExistsAsync("Emails", stoppingToken);

                TableClient tableClient = tableServiceClient.GetTableClient(table.Value.Name);

                List<string> emails = await tableClient.QueryAsync<Email>(maxPerPage: 1000, cancellationToken: stoppingToken)
                    .Select(e => e.EmailAddress)
                    .ToListAsync(cancellationToken: stoppingToken)
                    .ConfigureAwait(false);

                await SendEmail(emails,
                    "7'tfa Weather Notification",
                    $"Now {weather.current.temp_c:0.0}\u00B0C\n" +
                    $"Today {day.maxtemp_c:0.0}\u00B0C/{day.mintemp_c:0.0}\u00B0C\n" +
                    "May your 7'tfa stay eternally healthy!");

                Environment.Exit(0);
            }
            catch (Exception x)
            {
                _logger.LogCritical(x, "Error in ExecuteAsync");
            }
        }

        private async Task<Weather?> GetWeather(CancellationToken stoppingToken)
        {
            using HttpClient httpClient = new();

            HttpResponseMessage httpResponse = await httpClient.GetAsync("***REMOVED***", stoppingToken);

            return await httpResponse.Content.ReadFromJsonAsync<Weather>(cancellationToken: stoppingToken);
        }

        private async Task SendEmail(List<string> toAddresses, string subject, string body)
        {
            try
            {
                MailMessage mail = new()
                {
                    BodyEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8,
                    From = new MailAddress("***REMOVED***"),
                    Subject = subject,
                    Body = body
                };

                foreach (string email in toAddresses)
                {
                    mail.Bcc.Add(email);
                }

                mail.Headers.Add("Content-Type", "text/html; charset=utf-8");

                SmtpClient smtpClient = new("smtp.azurecomm.net", 587)
                {
                    Credentials = new NetworkCredential("***REMOVED***", "***REMOVED***"),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };

                await smtpClient.SendMailAsync(mail);
            }
            catch (Exception x)
            {
                _logger.LogCritical(x, "Error in SendEmail");
            }
        }
    }
}

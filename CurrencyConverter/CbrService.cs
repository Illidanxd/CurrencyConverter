using System.Text.Json;

namespace CurrencyConverter;

public class CbrService
{
    private readonly HttpClient _httpClient = new HttpClient();

    public CbrService()
    {
        // Притворяемся браузером, чтобы сайт не блокировал запросы
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }

    public async Task<CbrResponse?> GetRatesForDateAsync(DateTime date)
    {
        // Если выбрана сегодняшняя дата (или будущее), берем актуальный файл
        if (date.Date >= DateTime.Today)
        {
            string url = "https://www.cbr-xml-daily.ru/daily_json.js";
            var data = await FetchData(url);
            if (data != null) return data;
        }

        // Поиск в архиве (двигаемся назад до 7 дней, если выходные)
        for (int i = 0; i < 7; i++)
        {
            var checkDate = date.AddDays(-i);
            string url = $"https://www.cbr-xml-daily.ru/archive/{checkDate:yyyy/MM/dd}/daily_json.js";

            var data = await FetchData(url);
            if (data != null)
            {
                // Если нашли в архиве, оставляем дату, которую вернул сайт
                return data;
            }
        }
        return null;
    }

    private async Task<CbrResponse?> FetchData(string url)
    {
        try
        {
            var response = await _httpClient.GetStringAsync(url);
            var data = JsonSerializer.Deserialize<CbrResponse>(response);

            if (data != null && data.Valute != null && data.Valute.Count > 0)
            {
                return data;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ошибка загрузки {url}: {ex.Message}");
        }
        return null;
    }
}
using System.Text.Json.Serialization;

namespace CurrencyConverter;

// Класс для одной валюты
public class CurrencyModel
{
    [JsonPropertyName("ID")]
    public string Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; }

    [JsonPropertyName("Value")]
    public double Value { get; set; }

    [JsonPropertyName("Nominal")]
    public int Nominal { get; set; }

    [JsonPropertyName("CharCode")]
    public string CharCode { get; set; }

    // Вычисляемый курс за 1 единицу валюты
    public double RatePerUnit => Value / Nominal;

    // Красивое отображение для выпадающего списка
    public string DisplayName => $"{Name} ({CharCode})";
}

// Класс для всего ответа от сервера
public class CbrResponse
{
    [JsonPropertyName("Date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("Valute")]
    public Dictionary<string, CurrencyModel> Valute { get; set; }
}
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace CurrencyConverter;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly CbrService _cbrService = new();
    private CbrResponse? _currentRates;

    // Флаг для защиты от бесконечного цикла при пересчете
    private bool _isUpdating = false;

    public ObservableCollection<CurrencyModel> Currencies { get; } = new();

    // Выбранная дата
    private DateTime _selectedDate = DateTime.Today;
    public DateTime SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (_selectedDate != value)
            {
                _selectedDate = value;
                OnPropertyChanged();
                LoadDataCommand.Execute(null); // Перезагружаем данные при смене даты
            }
        }
    }

    // Текст подсказки (на какую дату нашли курс)
    private string _rateDateLabel = "Загрузка...";
    public string RateDateLabel
    {
        get => _rateDateLabel;
        set { _rateDateLabel = value; OnPropertyChanged(); }
    }

    // Первая валюта
    private CurrencyModel? _currencyFrom;
    public CurrencyModel? CurrencyFrom
    {
        get => _currencyFrom;
        set
        {
            _currencyFrom = value;
            OnPropertyChanged();
            RecalculateFrom();
        }
    }

    // Сумма "Из"
    private double _amountFrom = 1;
    public double AmountFrom
    {
        get => _amountFrom;
        set
        {
            _amountFrom = value;
            OnPropertyChanged();
            RecalculateFrom();
        }
    }

    // Вторая валюта
    private CurrencyModel? _currencyTo;
    public CurrencyModel? CurrencyTo
    {
        get => _currencyTo;
        set
        {
            _currencyTo = value;
            OnPropertyChanged();
            RecalculateTo();
        }
    }

    // Сумма "В"
    private double _amountTo;
    public double AmountTo
    {
        get => _amountTo;
        set
        {
            _amountTo = value;
            OnPropertyChanged();
            RecalculateTo();
        }
    }

    public ICommand LoadDataCommand { get; }

    public MainViewModel()
    {
        LoadDataCommand = new Command(async () => await LoadDataAsync());

        // Запускаем загрузку при старте
        LoadDataCommand.Execute(null);
    }

    private async Task LoadDataAsync()
    {
        try
        {
            // Читаем встроенный файл
            var assembly = typeof(MainViewModel).Assembly;
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(r => r.EndsWith("daily_json.js"));

            if (resourceName == null)
            {
                RateDateLabel = "Файл не найден!";
                return;
            }

            using var stream = assembly.GetManifestResourceStream(resourceName);
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();

            var data = System.Text.Json.JsonSerializer.Deserialize<CbrResponse>(json);

            if (data?.Valute != null)
            {
                // Сохраняем текущие курсы в переменную, иначе пересчет не сработает!
                _currentRates = data;

                // Очищаем и заполняем коллекцию заново
                Currencies.Clear();
                Currencies.Add(new CurrencyModel { CharCode = "RUB", Name = "Российский рубль", Value = 1, Nominal = 1 });

                foreach (var val in data.Valute.Values)
                {
                    Currencies.Add(val);
                }

                // Обновляем текст даты
                RateDateLabel = $"Курс на {data.Date:dd MMMM yyyy}";

                // Устанавливаем валюты по умолчанию (USD и EUR).
                // При установке CurrencyFrom сработает сеттер и автоматически запустит пересчет.
                CurrencyFrom = Currencies.FirstOrDefault(c => c.CharCode == "USD") ?? Currencies[0];
                CurrencyTo = Currencies.FirstOrDefault(c => c.CharCode == "EUR") ?? Currencies[0];

                // Сбрасываем сумму на 1, чтобы сразу увидеть результат
                AmountFrom = 1;
            }
        }
        catch (Exception ex)
        {
            RateDateLabel = "ОШИБКА ЧТЕНИЯ";
            System.Diagnostics.Debug.WriteLine($"ОШИБКА: {ex.Message}");
        }
    }

    // Пересчет "Из" -> "В"
    private void RecalculateFrom()
    {
        // Если идет обновление, курсы не загружены или валюты не выбраны — выходим
        if (_isUpdating || _currentRates == null || CurrencyFrom == null || CurrencyTo == null) return;

        // Включаем блокировку, чтобы изменение AmountTo не вызвало обратный пересчет
        _isUpdating = true;

        // Защита от деления на ноль, если у валюты вдруг нулевой номинал
        if (CurrencyTo.RatePerUnit == 0)
        {
            AmountTo = 0;
        }
        else
        {
            double inRubles = AmountFrom * CurrencyFrom.RatePerUnit;
            AmountTo = Math.Round(inRubles / CurrencyTo.RatePerUnit, 2);
        }

        // Выключаем блокировку
        _isUpdating = false;
    }

    // Пересчет "В" -> "Из"
    private void RecalculateTo()
    {
        if (_isUpdating || _currentRates == null || CurrencyFrom == null || CurrencyTo == null) return;

        _isUpdating = true;

        if (CurrencyFrom.RatePerUnit == 0)
        {
            AmountFrom = 0;
        }
        else
        {
            double inRubles = AmountTo * CurrencyTo.RatePerUnit;
            AmountFrom = Math.Round(inRubles / CurrencyFrom.RatePerUnit, 2);
        }

        _isUpdating = false;
    }

    // Реализация INotifyPropertyChanged
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
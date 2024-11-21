using System;
using System.Collections.Generic;
using System.Linq;

// Перелік статусів рейсу
public enum FlightStatus
{
    Очікується, Посадка, Відправлено, Затримано, Скасовано
}
// Клас для опису рейсу
public record Flight(string Destination, bool IsVIP)
{
    // Поточний статус рейсу, за замовчуванням "Очікується"
    public FlightStatus Status { get; private set; } = FlightStatus.Очікується;
    // Подія для повідомлення про зміну статусу рейсу
    public event Action<Flight, FlightStatus>? StatusChanged;
    // Дозволені переходи між статусами
    private static readonly Dictionary<FlightStatus, List<FlightStatus>> StatusTransitions = new()
    {
        { FlightStatus.Очікується, new List<FlightStatus> { FlightStatus.Посадка, FlightStatus.Затримано,  FlightStatus.Скасовано } },
        { FlightStatus.Посадка, new List<FlightStatus> { FlightStatus.Відправлено, FlightStatus.Затримано, FlightStatus.Скасовано } },
        { FlightStatus.Відправлено, new List<FlightStatus>() }, // Завершальний статус
        { FlightStatus.Затримано, new List<FlightStatus> { FlightStatus.Очікується, FlightStatus.Посадка, FlightStatus.Скасовано } },
        { FlightStatus.Скасовано, new List<FlightStatus>() } // Завершальний статус
    };
    // Метод для зміни статусу рейсу
    public void ChangeStatus(FlightStatus newStatus)
    {
        // Перевірка на допустимість переходу між статусами
        if (!StatusTransitions[Status].Contains(newStatus))
        {
            Console.WriteLine($"Неможливий перехід зі статусу '{Status}' до статусу '{newStatus}'.");
            return;
        }
        // Зміна статусу та виклик подій
        if (Status != newStatus)
        {
            Status = newStatus;
            StatusChanged?.Invoke(this, newStatus);
        }
    }
}
// Клас для опису пасажира
public record Passenger(string Name)
{
    // Метод для повідомлення пасажира про зміну статусу рейсу
    public void Notify(Flight flight, FlightStatus status)
    {
        // Повідомлення пасажира залежно від статусу рейсу
        string message = status switch
        {
            FlightStatus.Очікується => $"Пасажир {Name}! Ваш рейс до {flight.Destination} очікується. Будьте готові.",
            FlightStatus.Посадка => $"Пасажир {Name}! Почалася посадка на рейс до {flight.Destination}. Прямуйте до літака.",
            FlightStatus.Відправлено => $"Пасажир {Name}! Ваш рейс до {flight.Destination} відправлено. Відчувайте себе комфортно!",
            FlightStatus.Затримано => $"Пасажир {Name}! Ваш рейс до {flight.Destination} затримано. Будь ласка, чекайте подальших інструкцій.",
            FlightStatus.Скасовано => $"Пасажир {Name}! На жаль, ваш рейс до {flight.Destination} скасовано. Зверніться до інформаційної стійки.",
            _ => $"Пасажир {Name}: Статус рейсу до {flight.Destination} оновлено."
        };
        Console.ForegroundColor = status switch
        {
            FlightStatus.Очікується => ConsoleColor.Green,
            FlightStatus.Посадка => ConsoleColor.Blue,
            FlightStatus.Відправлено => ConsoleColor.Cyan,
            FlightStatus.Затримано => ConsoleColor.Yellow,
            FlightStatus.Скасовано => ConsoleColor.Red,
            _ => ConsoleColor.White
        };
        Console.WriteLine(message);
        Console.ResetColor();
    }
}
// Клас для опису персоналу аеропорту
public record Staff
{
    // Метод для повідомлення персоналу про зміну статусу рейсу
    public void Notify(Flight flight, FlightStatus status)
    {
        // Повідомлення персоналу залежно від статусу рейсу
        string message = status switch
        {
            FlightStatus.Очікується => $"Персонал: Підготовка до рейсу до {flight.Destination}. Проінформуйте пасажирів",
            FlightStatus.Посадка => $"Персонал: Почалася посадка на рейс до {flight.Destination}. Організуйте процес посадки.",
            FlightStatus.Відправлено => $"Персонал: Рейс до {flight.Destination} відправлено. Обслуговайте пасажирів",
            FlightStatus.Затримано => $"Персонал: Рейс до {flight.Destination} затримано. Оновіть розклад і сповістіть пасажирів.",
            FlightStatus.Скасовано => $"Персонал: Рейс до {flight.Destination} скасовано. Сповістіть пасажирів і організуйте альтернативи.",
            _ => $"Персонал: Оновлення статусу рейсу до {flight.Destination}."
        };
        Console.ForegroundColor = status switch
        {
            FlightStatus.Очікується => ConsoleColor.Green,
            FlightStatus.Посадка => ConsoleColor.Blue,
            FlightStatus.Відправлено => ConsoleColor.Cyan,
            FlightStatus.Затримано => ConsoleColor.Yellow,
            FlightStatus.Скасовано => ConsoleColor.Red,
            _ => ConsoleColor.White
        };
        Console.WriteLine(message);
        Console.ResetColor();
    }
}
public class Airport
{
    // Список рейсів
    private List<Flight> Flights { get; } = new();
    // Словник для збереження пасажирів, прив'язаних до конкретних рейсів
    private Dictionary<Flight, List<Passenger>> FlightPassengers { get; } = new();
    // Персонал аеропорту
    private Staff AirportStaff { get; } = new();
    // Статистика завершених, затриманих і скасованих рейсів
    private int CompletedFlights { get; set; }
    private int DelayedFlights { get; set; }
    private int CancelledFlights { get; set; }
    // Перевірка, чи є активні рейси
    public bool HasFlights()
    {
        return Flights.Any();
    }
    // Додавання нового рейсу
    public void AddFlight(string? destination, bool isVIP)
    {
        if (string.IsNullOrWhiteSpace(destination))
        {
            Console.WriteLine("Помилка: Назва напрямку не може бути порожньою. Спробуйте ще раз.");
            return;
        }
        var flight = new Flight(destination, isVIP);
        flight.StatusChanged += NotifyPassengersAndStaff;
        flight.StatusChanged += UpdateStatistics;

        Flights.Add(flight);
        FlightPassengers[flight] = new List<Passenger>();
        Console.WriteLine($"Рейс до {destination} додано.");
    }
    // Реєстрація пасажира на рейс
    public void RegisterPassenger(string passengerName, int flightIndex)
    {
        if (!Flights.Any())
        {
            Console.WriteLine("Немає створених рейсів. Спочатку додайте рейс.");
            return;
        }
        if (flightIndex < 0 || flightIndex >= Flights.Count)
        {
            Console.WriteLine("Невірний вибір рейсу.");
            return;
        }
        var flight = Flights[flightIndex];
        if (flight.Status == FlightStatus.Відправлено || flight.Status == FlightStatus.Скасовано)
        {
            Console.WriteLine($"Реєстрація неможлива: рейс до {flight.Destination} вже {flight.Status.ToString().ToLower()}.");
            return;
        }
        if (!FlightPassengers.ContainsKey(flight))
        {
            FlightPassengers[flight] = new List<Passenger>();
        }
        var passenger = new Passenger(passengerName);
        FlightPassengers[flight].Add(passenger);
        flight.StatusChanged += passenger.Notify; // Підписка пасажира на події рейсу

        Console.WriteLine($"Пасажир {passenger.Name} зареєстрований на рейс до {flight.Destination}.");
    }
    public void ChangeFlightStatus(int flightIndex, FlightStatus newStatus)
    {
        // Перевіряємо, чи є рейси в списку Flights. Якщо рейсів немає, повідомляємо користувача і завершуємо метод.
        if (!Flights.Any())
        {
            Console.WriteLine("Немає створених рейсів. Спочатку додайте рейс.");
            return;
        }
        // Перевіряємо, чи введений індекс рейсу знаходиться в допустимому діапазоні.
        // Якщо індекс менше 0 або більше кількості рейсів, повідомляємо про помилку і завершуємо метод.
        if (flightIndex < 0 || flightIndex >= Flights.Count)
        {
            Console.WriteLine("Невірний вибір рейсу.");
            return;
        }

        // Викликаємо метод ChangeStatus для вибраного рейсу, передаючи новий статус.
        // Цей метод перевіряє допустимість переходу між статусами і генерує події для сповіщення.
        Flights[flightIndex].ChangeStatus(newStatus);
    }
    private void NotifyPassengersAndStaff(Flight flight, FlightStatus status)
    {
        // Сортуємо список рейсів так, щоб VIP-рейси мали вищий пріоритет.
        var sortedFlights = Flights
            .OrderByDescending(f => f.IsVIP) // VIP-рейси йдуть першими
            .ToList();
        // Проходимося по відсортованих рейсах.
        foreach (var currentFlight in sortedFlights)
        {
            if (currentFlight == flight) // Обробляємо лише рейс, статус якого змінився
            {
                // Виводимо інформацію про зміну статусу рейсу.
                Console.WriteLine($"\nСповіщення про рейс до {currentFlight.Destination}: статус змінено на {status}.");
                // Сповіщаємо персонал аеропорту про зміну статусу.
                AirportStaff.Notify(currentFlight, status);
                // Якщо для рейсу зареєстровані пасажири, сповіщаємо їх.
                if (FlightPassengers.TryGetValue(currentFlight, out var passengers))
                {
                    foreach (var passenger in passengers)
                    {
                        passenger.Notify(currentFlight, status);
                    }
                }
                // Якщо рейс завершено (відправлено або скасовано), очищаємо всі події для нього.
                if (status == FlightStatus.Відправлено || status == FlightStatus.Скасовано)
                {
                    // Видаляємо обробники подій NotifyPassengersAndStaff і UpdateStatistics.
                    currentFlight.StatusChanged -= NotifyPassengersAndStaff;
                    currentFlight.StatusChanged -= UpdateStatistics;

                    // Видаляємо обробники подій для кожного пасажира, зареєстрованого на рейс.
                    if (FlightPassengers.TryGetValue(currentFlight, out var passengerList))
                    {
                        foreach (var passenger in passengerList)
                        {
                            currentFlight.StatusChanged -= passenger.Notify;
                        }
                        // Видаляємо інформацію про пасажирів з FlightPassengers.
                        FlightPassengers.Remove(currentFlight);
                    }
                }

                break; // Перериваємо цикл, адже обробляємо лише один рейс
            }
        }
    }
    private void UpdateStatistics(Flight flight, FlightStatus status)
    {
        // Оновлюємо статистику залежно від нового статусу рейсу.
        switch (status)
        {
            case FlightStatus.Відправлено:
                CompletedFlights++; // Збільшуємо лічильник завершених рейсів.
                break;
            case FlightStatus.Затримано:
                DelayedFlights++; // Збільшуємо лічильник затриманих рейсів.
                break;
            case FlightStatus.Скасовано:   // Збільшуємо лічильник скасованих рейсів.
                CancelledFlights++;
                break;
        }
    }
    public void DisplayStatistics()
    {
        // Виводимо статистику рейсів у зручному форматі.
        Console.WriteLine("\nСтатистика рейсів:");
        Console.ForegroundColor = ConsoleColor.Green; // Завершені рейси - зелений колір.
        Console.WriteLine($"Завершених рейсів: {CompletedFlights}");
        Console.ForegroundColor = ConsoleColor.Yellow;  // Затримані рейси - жовтий колір.
        Console.WriteLine($"Затриманих рейсів: {DelayedFlights}");
        Console.ForegroundColor = ConsoleColor.Red;   // Скасовані рейси - червоний колір.
        Console.WriteLine($"Скасованих рейсів: {CancelledFlights}");
        Console.ResetColor();
    }

    public void DisplayFlights()
    {
        // Виводимо список рейсів для перегляду.
        Console.WriteLine("\nСписок рейсів:");
        // Сортуємо рейси: спочатку VIP, потім за алфавітом напрямків.
        var sortedFlights = Flights
            .OrderByDescending(f => f.IsVIP)
            .ThenBy(f => f.Destination)
            .ToList();
        // Проходимо по відсортованому списку і виводимо інформацію про кожен рейс.
        for (int i = 0; i < sortedFlights.Count; i++)
        {
            var flight = sortedFlights[i];
            Console.WriteLine($"{i + 1}. {flight.Destination} - {flight.Status} {(flight.IsVIP ? "(VIP)" : "")}");
        }
    }

}
public static class Program
{
    public static void Main()
    {
        // Налаштовуємо консоль на підтримку української мови (UTF-8).
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        // Створюємо екземпляр класу Airport для управління рейсами.
        var airport = new Airport();
        // Основний цикл програми, який обробляє взаємодію з користувачем.
        while (true)
        {
            // Виводимо заголовок і меню з доступними командами.
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\nСистема моніторингу та обслуговування рейсів в аеропорту\n");
            Console.WriteLine("Меню:");
            Console.WriteLine("1. Додати рейс");
            Console.WriteLine("2. Зареєструвати пасажира");
            Console.WriteLine("3. Змінити статус рейсу");
            Console.WriteLine("4. Показати рейси");
            Console.WriteLine("5. Показати статистику");
            Console.WriteLine("0. Завершити симуляцію");
            Console.ResetColor();
            Console.Write("Ваш вибір: ");
            var choice = Console.ReadLine();
            // Зчитуємо вибір користувача.
            switch (choice)
            {
                case "1":
                    string? destination;
                    do
                    {
                        Console.Write("Введіть напрямок рейсу: ");
                        destination = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(destination))
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Помилка: Назва напрямку не може бути порожньою.");
                            Console.ResetColor();
                        }
                    } while (string.IsNullOrWhiteSpace(destination));

                    Console.Write("Це VIP-рейс? (так - 1/ні - 2): ");
                    var input = Console.ReadLine()?.Trim().ToLower();
                    var isVIP = input == "так" || input == "1";

                    Console.ForegroundColor = ConsoleColor.Green;
                    airport.AddFlight(destination, isVIP);
                    Console.ResetColor();
                    break;

                case "2":

                    if (!airport.HasFlights())
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Немає створених рейсів. Спочатку додайте рейс.");
                        Console.ResetColor();
                        break;
                    }
                    airport.DisplayFlights();
                    string passengerName;
                    while (true)
                    {
                        Console.Write("Введіть ім'я пасажира: ");
                        passengerName = Console.ReadLine()!;

                        if (string.IsNullOrWhiteSpace(passengerName))
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Помилка: Ім'я не може бути порожнім. Спробуйте ще раз.");
                            Console.ResetColor();
                            continue;
                        }

                        if (passengerName.Any(char.IsDigit))
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Помилка: Ім'я не може містити чисел. Спробуйте ще раз.");
                            Console.ResetColor();
                            continue;
                        }

                        break;
                    }

                    Console.Write("Виберіть номер рейсу: ");
                    if (int.TryParse(Console.ReadLine(), out var flightIndex1))
                    {
                        try
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            airport.RegisterPassenger(passengerName, flightIndex1 - 1);
                            Console.ResetColor();
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Помилка: Невірний номер рейсу. Спробуйте ще раз.");
                            Console.ResetColor();
                            continue;
                        }
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Помилка: Невірний формат номера рейсу.");
                        Console.ResetColor();
                        continue;
                    }
                    break;

                case "3":
                    if (!airport.HasFlights())
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Немає створених рейсів. Спочатку додайте рейс.");
                        Console.ResetColor();
                        break;
                    }
                    airport.DisplayFlights();
                    Console.Write("Виберіть номер рейсу: ");
                    if (int.TryParse(Console.ReadLine(), out var flightIndex2))
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Доступні статуси:");
                        foreach (var status in Enum.GetValues<FlightStatus>())
                        {
                            Console.WriteLine($"{(int)status}. {status}");
                        }
                        Console.ResetColor();

                        Console.Write("Виберіть новий статус: ");
                        if (int.TryParse(Console.ReadLine(), out var statusIndex) && Enum.IsDefined(typeof(FlightStatus), statusIndex))
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            airport.ChangeFlightStatus(flightIndex2 - 1, (FlightStatus)statusIndex);
                            Console.ResetColor();
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Невірний вибір статусу.");
                            Console.ResetColor();
                        }
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Невірний номер рейсу.");
                        Console.ResetColor();
                    }
                    break;

                case "4":
                    if (!airport.HasFlights())
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Немає створених рейсів. Спочатку додайте рейс.");
                        Console.ResetColor();
                        break;
                    }
                    airport.DisplayFlights();
                    break;

                case "5":
                    airport.DisplayStatistics();
                    break;

                case "0":
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Симуляцію завершено.");
                    Console.ResetColor();
                    return;

                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Невірний вибір. Спробуйте ще раз.");
                    Console.ResetColor();
                    break;
            }
        }
    }
}

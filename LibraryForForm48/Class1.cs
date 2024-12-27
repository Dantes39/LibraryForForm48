using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using System.Diagnostics;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace LibraryForForm48
{
    // Интерфейс представляющий объект использующие в моей форме
    public interface IFormObject
    {
        string DisplayName { get; }
        string SerialNumber { get; set; }
        string ToString();
        string GetInfo();
    }

    // Интерфейс представляющий объект использующий память
    public interface IMemoryDevice
    {
        int Capacity { get; set; } // Размер памяти в ГБ
        int BusyMemory { get; set; }
        int CacheMemory { get; set; }

        void FileSaving(int capacityFile); // Метод сохранения файла на устройство
        void FileDelete(int capacityFile); // Метод удаления файла на устройстве
        void CleanCache(); // Метод очистки кеша на устройстве
    }


    public class Computer : IFormObject
    {
        public string DisplayName { get; set; }
        public string Name { get; set; }
        public string SerialNumber { get; set; }
        public CPU Cpu { get; set; }
        public RAM Ram { get; set; }
        public HDD Hdd { get; set; }
        public GPU Gpu { get; set; }
        public Motherboard Motherboard { get; set; }
        public PowerSupply PowerSupply { get; set; }

        // Добавляем кулер как свойство
        public Cooler Cooler { get; set; }

        // Конструктор класса Computer
        public Computer(string displayName, string serialNumber, CPU cpu, RAM ram, HDD hdd, GPU gpu, Motherboard motherboard, PowerSupply powerSupply)
        {
            DisplayName = displayName;
            SerialNumber = serialNumber;
            Cpu = cpu;
            Ram = ram;
            Gpu = gpu;
            Hdd = hdd;
            Motherboard = motherboard;
            PowerSupply = powerSupply;

            // Создаем кулер с передачей устройств
            Cooler = new Cooler("CoolerMaster", "Hyper 212", serialNumber + "_Cooler", cpu, gpu, ram);
        }

        public string GetInfo()
        {
            var info = new StringBuilder();
            info.AppendLine($"Имя: {Name}");
            info.AppendLine($"Серийный номер: {SerialNumber}");
            info.AppendLine($"CPU: {Cpu.DisplayName}");
            info.AppendLine($"RAM: {Ram.DisplayName}");
            if (Hdd != null) info.AppendLine($"HDD: {Hdd.DisplayName}");
            if (Gpu != null) info.AppendLine($"GPU: {Gpu.DisplayName}");
            info.AppendLine($"Материнская плата: {Motherboard.DisplayName}");
            info.AppendLine($"Блок питания: {PowerSupply.DisplayName}");
            info.AppendLine($"Кулер: {Cooler.DisplayName}, Охлаждающая мощность: {Cooler.CoolingPower}W");
            return info.ToString();
        }

        public new string ToString()
        {
            return GetInfo();
        }

        // Метод для сохранения в JSON
        public static void SaveToJson<T>(List<T> objects, string filePath)
        {
            string json = JsonConvert.SerializeObject(objects, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        // Метод для загрузки из JSON
        public static List<Computer> LoadFromJson<T>(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Файл {filePath} не найден.");
            }

            string json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<List<Computer>>(json);
        }
    }



    // Базовый класс для всех устройств
    public abstract class Device : IFormObject
    {
        // Общие поля для всех устройств
        public string Brand { get; set; } // Производитель
        public string Model { get; set; } // Модель
        public string SerialNumber { get; set; } // Серийный номер
        public string Condition { get; set; } // Состояние
        public string DisplayName => $"{Brand} {Model}";

        // Словарь для хранения дополнительных полей (например, фото и года выпуска)
        private Dictionary<string, string> properties = new Dictionary<string, string>();

        // Индексатор для доступа к свойствам через словарь
        public string this[string key]
        {
            get => properties.ContainsKey(key) ? properties[key] : null;
            set => properties[key] = value;
        }

        // Абстрактный метод для получения информации о конкретном устройстве
        public abstract string GetInfo();
        public abstract override string ToString();

        // Инстансный метод для отображения года выпуска
        public string YearOfRelease
        {
            get => this["YearOfRelease"];
            set => this["YearOfRelease"] = value;
        }

        // Инстансный метод для отображения пути к фото
        public string PhotoPath
        {
            get => this["PhotoPath"];
            set => this["PhotoPath"] = value;
        }

        // Метод для сохранения списка устройств в JSON
        public static void SaveToJson<T>(List<T> devices, string filePath)
        {
            try
            {
                string json = JsonConvert.SerializeObject(devices, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при сохранении в JSON: " + ex.Message);
            }
        }

        // Метод для загрузки списка устройств из JSON
        public static List<T> LoadFromJson<T>(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                List<T> devices = JsonConvert.DeserializeObject<List<T>>(json);
                return devices;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при загрузке из JSON: " + ex.Message);
                return new List<T>(); // Возвращаем пустой список в случае ошибки
            }
        }
    }



    // Класс для процессора
    public class CPU : Device
    {
        public int CoreCount { get; } // Количество ядер
        public int ThreadCount { get; } // Количество потоков
        public double BaseClock { get; } // Базовая частота в ГГц
        public double MaxClock { get; private set; } // Максимальная частота в ГГц
        public string SocketType { get; } // Тип сокета

        public DateTime GuaranteeEndDate { get; set; } // Дата окончания гарантии

        // Конструктор
        public CPU(string brand, string model, int coreCount, int threadCount, double baseClock, double maxClock,
                   string socketType, string condition, DateTime guaranteeEndDate, string serialNumber)
        {
            Brand = brand;
            Model = model;
            CoreCount = coreCount;
            ThreadCount = threadCount;
            BaseClock = baseClock;
            MaxClock = maxClock;
            SocketType = socketType;
            Condition = condition;
            GuaranteeEndDate = guaranteeEndDate;
            SerialNumber = serialNumber;
        }

        // Переопределение абстрактного метода из Device
        public override string GetInfo()
        {
            return $"CPU: {Brand} {Model}, Ядер: {CoreCount}, Потоков: {ThreadCount}, " +
                   $"Базовая частота: {BaseClock}GHz, Макс частота: {MaxClock}GHz, " +
                   $"Сокет: {SocketType}, Состояние: {Condition}, " +
                   $"Гарантия до: {GuaranteeEndDate.ToShortDateString()}";
        }

        public override string ToString()
        {
            return GetInfo();
        }

        // Метод для корректировки максимальной частоты процессора
        public static void AdjustMaxClock(ref CPU cpu)
        {
            if (cpu.Condition == "Бывший в употреблении")
            {
                cpu.MaxClock -= 0.5; // Уменьшаем на 0.5 ГГц
            }
            else if (cpu.Condition == "Новое")
            {
                cpu.MaxClock += 0.5; // Увеличиваем на 0.5 ГГц
            }
        }

        // Метод для вычисления мощности процессора
        public static void CalculateCpuPower(CPU cpu, out double power, out string powerCategory)
        {
            power = (cpu.CoreCount * cpu.ThreadCount) * cpu.MaxClock;

            if (power > 20000)
            {
                powerCategory = "Высокая мощность";
            }
            else if (power > 10000)
            {
                powerCategory = "Средняя мощность";
            }
            else
            {
                powerCategory = "Низкая мощность";
            }
        }

    }

    // Класс для оперативной памяти
    // Класс для оперативной памяти
    public class RAM : Device, IMemoryDevice
    {
        public int Capacity { get; set; } // Объем в ГБ
        public int Frequency { get; set; } // Частота в МГц
        public string Type { get; set; } // Тип (например, DDR4, DDR5)
        public string Cpu { get; set; } // Тип поддерживаемого процессора
        public int BusyMemory { get; set; }
        public int CacheMemory { get; set; }

        // Конструктор для RAM
        public RAM(string brand, string model, int capacity, int frequency, string type, string cpu,
                   string condition, string serialNumber)
        {
            Brand = brand;
            Model = model;
            Capacity = capacity;
            Frequency = frequency;
            Type = type;
            Cpu = cpu;
            Condition = condition;
            SerialNumber = serialNumber;
            Random rn = new Random();
            CacheMemory = rn.Next(0, Capacity / 10);
            BusyMemory = CacheMemory;
        }

        public RAM() { }

        // Переопределение метода GetInfo
        public override string GetInfo()
        {
            return $"RAM: {Brand} {Model}, Объем: {Capacity}GB, Частота: {Frequency}MHz, " +
                   $"Тип: {Type}, Поддерживаемый CPU: {Cpu}, Состояние: {Condition}";
        }

        public override string ToString()
        {
            return GetInfo();
        }

        public void FileSaving(int capacityFile) // Метод сохранения файла на устройство
        {
            if (BusyMemory < Capacity && Capacity - BusyMemory >= capacityFile)
            {
                BusyMemory += capacityFile;
                if (BusyMemory < Capacity)
                {
                    int availableSpace = Capacity - BusyMemory;
                    if (availableSpace > 0) // Проверяем, что деление будет корректным
                    {
                        Random rn = new Random();
                        int randomValue = rn.Next(0, availableSpace / 10);
                        CacheMemory += randomValue;
                        BusyMemory += randomValue;
                    }
                }
            }
        }


        public void FileDelete(int capacityFile)
        {
            if (capacityFile <= BusyMemory)
            {
                BusyMemory -= capacityFile;
                if (BusyMemory > 0 && CacheMemory > 0)
                {
                    Random rn = new Random();
                    int cache = rn.Next(0, BusyMemory / 10);
                    CacheMemory -= cache;
                    BusyMemory -= cache;
                }
            }
        }

        public void CleanCache() // Метод очистки кеша на устройстве
        {
            BusyMemory -= CacheMemory;
            CacheMemory = 0;
        }
    }

    // Класс для жесткого диска (HDD)
    // Класс для жесткого диска (HDD)
    public class HDD : Device, IMemoryDevice
    {
        public int Capacity { get; set; } // Объем в ГБ
        public string Type { get; set; } // Тип (например, SSD или HDD)
        public int RotationSpeed { get; set; } // Скорость вращения в об/мин
        public string Interface { get; set; } // Интерфейс (например, SATA, NVMe)
        public string WarrantyPeriod { get; set; } // Гарантийный срок
        public int BusyMemory { get; set; }
        public int CacheMemory { get; set; }

        // Нестатический конструктор
        public HDD(string brand, string model, int capacity, string type, int rotationSpeed, string interfaceType,
                   string condition, string serialNumber, string warrantyPeriod)
        {
            Brand = brand;
            Model = model;
            Capacity = capacity;
            Type = type;
            RotationSpeed = rotationSpeed;
            Interface = interfaceType;
            Condition = condition;
            SerialNumber = serialNumber;
            WarrantyPeriod = warrantyPeriod;
            Random rn = new Random();
            CacheMemory = rn.Next(0, Capacity / 10);
            BusyMemory = CacheMemory;
        }

        // Конструктор без параметров
        public HDD() { }

        // Переопределение метода GetInfo
        public override string GetInfo()
        {
            return $"HDD: {Brand} {Model}, Объем: {Capacity}GB, Тип: {Type}, " +
                   $"Скорость вращения: {RotationSpeed} об/мин, Интерфейс: {Interface}, " +
                   $"Состояние: {Condition}, Гарантия: {WarrantyPeriod}, Серийный номер: {SerialNumber}";
        }

        public override string ToString()
        {
            return GetInfo();
        }

        public void FileSaving(int capacityFile) // Метод сохранения файла на устройство
        {
            if (BusyMemory < Capacity && Capacity - BusyMemory >= capacityFile)
            {
                BusyMemory += capacityFile;
                if (BusyMemory < Capacity)
                {
                    int availableSpace = Capacity - BusyMemory;
                    if (availableSpace > 0) // Проверяем, что деление будет корректным
                    {
                        Random rn = new Random();
                        int randomValue = rn.Next(0, availableSpace / 10);
                        CacheMemory += randomValue;
                        BusyMemory += randomValue;
                    }
                }
            }
        }


        public void FileDelete(int capacityFile)
        {
            if (capacityFile <= BusyMemory)
            {
                BusyMemory -= capacityFile;
                if (BusyMemory > 0 && CacheMemory > 0)
                {
                    Random rn = new Random();
                    int cache = rn.Next(0, BusyMemory / 10);
                    CacheMemory -= cache;
                    BusyMemory -= cache;
                }
            }
        }

        public void CleanCache() // Метод очистки кеша на устройстве
        {
            BusyMemory -= CacheMemory;
            CacheMemory = 0;
        }
    }

    // Класс для видеокарты (GPU)
    public class GPU : Device
    {
        public int MemorySize { get; set; } // Объем видеопамяти в ГБ
        public string MemoryType { get; set; } // Тип видеопамяти (например, GDDR6)
        public int CoreClock { get; set; } // Частота ядра в МГц
        public int BoostClock { get; set; } // Частота буста в МГц
        public string Interface { get; set; } // Интерфейс подключения (например, PCIe 4.0)

        public GPU(string brand, string model, int memorySize, string memoryType, int coreClock, int boostClock,
                   string interfaceType, string condition, string serialNumber)
        {
            Brand = brand;
            Model = model;
            MemorySize = memorySize;
            MemoryType = memoryType;
            CoreClock = coreClock;
            BoostClock = boostClock;
            Interface = interfaceType;
            Condition = condition;
            SerialNumber = serialNumber;
        }

        public GPU() { }

        public override string GetInfo()
        {
            return $"GPU: {Brand} {Model}, Память: {MemorySize}GB {MemoryType}, " +
                   $"Частота ядра: {CoreClock}MHz, Boost: {BoostClock}MHz, Интерфейс: {Interface}, " +
                   $"Состояние: {Condition}, Серийный номер: {SerialNumber}";
        }

        public override string ToString()
        {
            return GetInfo();
        }
    }

    // Класс для материнской платы
    public class Motherboard : Device
    {
        public string SupportedCPU { get; set; }
        public string SocketType { get; set; } // Тип сокета
        public string Chipset { get; set; } // Чипсет
        public int RAMSlots { get; set; } // Количество слотов для оперативной памяти
        public int MaxRAM { get; set; } // Максимальный поддерживаемый объем оперативной памяти (в ГБ)
        public string FormFactor { get; set; } // Форм-фактор (например, ATX, Micro-ATX)

        public Motherboard(string brand, string model, string socketType, string chipset, int ramSlots,
                           int maxRam, string formFactor, string condition, string serialNumber)
        {
            Brand = brand;
            Model = model;
            SocketType = socketType;
            Chipset = chipset;
            RAMSlots = ramSlots;
            MaxRAM = maxRam;
            FormFactor = formFactor;
            Condition = condition;
            SerialNumber = serialNumber;
        }

        public Motherboard() { }

        public override string GetInfo()
        {
            return $"Motherboard: {Brand} {Model}, Поддерживаемые процессоры: {SupportedCPU}, Сокет: {SocketType}, Чипсет: {Chipset}, " +
                   $"Слотов RAM: {RAMSlots}, Макс RAM: {MaxRAM}GB, Форм-фактор: {FormFactor}, " +
                   $"Состояние: {Condition}, Серийный номер: {SerialNumber}";
        }

        public override string ToString()
        {
            return GetInfo();
        }
    }

    // Класс для блока питания (PSU)
    public class PowerSupply : Device
    {
        public int Wattage { get; set; } // Мощность в ваттах
        public string EfficiencyRating { get; set; } // Класс энергоэффективности (например, 80+ Gold)
        public string FormFactor { get; set; } // Форм-фактор (например, ATX, SFX)
        public bool Modular { get; set; } // Модульный ли блок питания

        public PowerSupply(string brand, string model, int wattage, string efficiencyRating, string formFactor,
                           bool modular, string condition, string serialNumber)
        {
            Brand = brand;
            Model = model;
            Wattage = wattage;
            EfficiencyRating = efficiencyRating;
            FormFactor = formFactor;
            Modular = modular;
            Condition = condition;
            SerialNumber = serialNumber;
        }

        public PowerSupply() { }

        public override string GetInfo()
        {
            string modularity = Modular ? "Модульный" : "Немодульный";
            return $"PSU: {Brand} {Model}, Мощность: {Wattage}W, Энергоэффективность: {EfficiencyRating}, " +
                   $"Форм-фактор: {FormFactor}, {modularity}, Состояние: {Condition}, Серийный номер: {SerialNumber}";
        }

        public override string ToString()
        {
            return GetInfo();
        }
    }

    public class Cooler : Device
    {
        public string Manufacturer { get; set; }


        // Устройства, влияющие на мощность кулера
        public CPU Cpu { get; set; }
        public GPU Gpu { get; set; }
        public RAM Ram { get; set; }

        // Конструктор, в который передаются устройства для расчета мощности
        public Cooler(string manufacturer, string model, string serialNumber, CPU cpu, GPU gpu, RAM ram)
        {
            Manufacturer = manufacturer;
            Model = model;
            SerialNumber = serialNumber;
            Cpu = cpu;
            Gpu = gpu;
            Ram = ram;
        }

        // Свойство для вычисления мощности кулера
        public double CoolingPower
        {
            get
            {
                double totalPower = 0;

                // Для процессора
                if (Cpu != null)
                {
                    // Рассчитываем мощность как частота процессора умноженная на количество ядер
                    totalPower += Cpu.BaseClock * Cpu.CoreCount * 10; // Коэффициент 10 для оценки мощности
                }

                // Для видеокарты
                if (Gpu != null)
                {
                    // Рассчитываем мощность как частота ядра видеокарты умноженная на количество ядер
                    totalPower += Gpu.CoreClock * 0.1; // Коэффициент 0.1 для видеокарты
                }

                // Для оперативной памяти
                if (Ram != null)
                {
                    // Для RAM можно использовать её частоту, умноженную на объем
                    totalPower += Ram.Frequency * Ram.Capacity * 0.05; // Коэффициент 0.05 для RAM
                }

                // Возвращаем охлаждающую мощность как 80% от общей потребляемой мощности
                return totalPower * 0.8;
            }
        }

        public override string GetInfo()
        {
            var info = new StringBuilder();
            info.AppendLine($"Производитель: {Manufacturer}");
            info.AppendLine($"Модель: {Model}");
            info.AppendLine($"Серийный номер: {SerialNumber}");
            info.AppendLine($"Охлаждающая мощность: {CoolingPower}W");
            return info.ToString();
        }

        public override string ToString()
        {
            return GetInfo();
        }
    }


}

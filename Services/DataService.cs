using Newtonsoft.Json;
using MobileECommerceAPI.Models;

namespace MobileECommerceAPI.Services
{
    public class DataService
    {
        private readonly string _dataPath;
        private readonly ILogger<DataService> _logger;

        public DataService(IConfiguration configuration, ILogger<DataService> logger)
        {
            _dataPath = configuration["DataStorage:JsonFilePath"] ?? "Data";
            _logger = logger;
            EnsureDataDirectoryExists();
        }

        private void EnsureDataDirectoryExists()
        {
            if (!Directory.Exists(_dataPath))
            {
                Directory.CreateDirectory(_dataPath);
            }
        }

        public async Task<List<T>> LoadDataAsync<T>(string fileName) where T : class
        {
            try
            {
                var filePath = Path.Combine(_dataPath, $"{fileName}.json");
                if (!File.Exists(filePath))
                {
                    return new List<T>();
                }

                var json = await File.ReadAllTextAsync(filePath);
                return JsonConvert.DeserializeObject<List<T>>(json) ?? new List<T>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading data from {fileName}.json");
                return new List<T>();
            }
        }

        public async Task<T?> LoadSingleDataAsync<T>(string fileName) where T : class
        {
            try
            {
                var filePath = Path.Combine(_dataPath, $"{fileName}.json");
                if (!File.Exists(filePath))
                {
                    return null;
                }

                var json = await File.ReadAllTextAsync(filePath);
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading single data from {fileName}.json");
                return null;
            }
        }

        public async Task SaveDataAsync<T>(List<T> data, string fileName) where T : class
        {
            try
            {
                var filePath = Path.Combine(_dataPath, $"{fileName}.json");
                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving data to {fileName}.json");
                throw;
            }
        }

        public async Task SaveSingleDataAsync<T>(T data, string fileName) where T : class
        {
            try
            {
                var filePath = Path.Combine(_dataPath, $"{fileName}.json");
                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving single data to {fileName}.json");
                throw;
            }
        }

        public async Task<int> GetNextIdAsync<T>(string fileName, Func<T, int> idSelector) where T : class
        {
            var data = await LoadDataAsync<T>(fileName);
            return data.Any() ? data.Max(idSelector) + 1 : 1;
        }

        public async Task<List<User>> LoadUsersAsync()
        {
            return await LoadDataAsync<User>("users");
        }

        public async Task SaveUsersAsync(List<User> users)
        {
            await SaveDataAsync(users, "users");
        }
    }
}
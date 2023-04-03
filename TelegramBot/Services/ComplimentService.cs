using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TelegramBot.Abstract;

namespace TelegramBot.Services
{
    public class ComplimentService : IComplimentService
    {
        public const string RootPath = "data";
        private const string FilePath = "data.json";
        private string fullPath = Path.Combine(RootPath, FilePath);
        private Queue<string> _compliments = new Queue<string>();

        public ComplimentService()
        {
            LoadData();
        }

        public void AddCompliment(string compliment)
        {
            _compliments.Enqueue(compliment);
            SaveData();
        }

        public IEnumerable<string> GetAllCompliments()
        {
            return _compliments;
        }

        public string? GetCompliment()
        {
            if (_compliments.TryDequeue(out var compliment))
            {
                SaveData();
                return compliment;
            }

            return null;
        }

        public bool RemoveLastAdded()
        {
            var newSize = _compliments.Count - 1;
            if (newSize > 0)
            {
                _compliments = new Queue<string>(_compliments.Take(newSize));
                SaveData();
                return true;
            }

            return false;
        }
        private void SaveData()
        {
            File.WriteAllText(fullPath, JsonConvert.SerializeObject(_compliments));
        }

        private void LoadData()
        {
            if (File.Exists(fullPath))
            {
                var array = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(FilePath));
                foreach (var item in array)
                {
                    _compliments.Enqueue(item);
                }
            }
        }

        public int GetComplimentCount()
        {
            return _compliments.Count;
        }
    }
}

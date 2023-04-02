using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using TelegramBot.Abstract;

namespace TelegramBot.Services
{
    public class ComplimentService : IComplimentService
    {
        private const string FilePath = "data.json";
        private readonly Queue<string> _compliments = new Queue<string>();

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

        private void SaveData()
        {
            File.WriteAllText(FilePath, JsonConvert.SerializeObject(_compliments));
        }

        private void LoadData()
        {
            if (File.Exists(FilePath))
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

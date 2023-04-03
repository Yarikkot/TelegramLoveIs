using System.Collections.Generic;

namespace TelegramBot.Abstract
{
    public interface IComplimentService
    {
        bool RemoveLastAdded();

        string? GetCompliment();

        int GetComplimentCount();

        void AddCompliment(string compliment);

        IEnumerable<string> GetAllCompliments();
    }
}

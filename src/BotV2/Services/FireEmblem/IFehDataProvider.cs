using System.Collections.Generic;
using System.Threading.Tasks;

namespace BotV2.Services.FireEmblem
{
    public interface IFehDataProvider
    {
        Task<IEnumerable<KeyValuePair<string, string>>> GetCharacter(string query);

        Task<IEnumerable<KeyValuePair<string, string>>> GetSkill(string query);

        Task<IEnumerable<KeyValuePair<string, string>>> GetWeapon(string query);

        Task<IEnumerable<KeyValuePair<string, string>>> GetSeal(string query);

        Task<IEnumerable<KeyValuePair<string, string>>> GetBuilding(string query);

        Task<IEnumerable<KeyValuePair<string, string>>> GetVoiceActor(string query);

        void Reload();
    }
}
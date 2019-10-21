﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace BotV2.Services.FireEmblem
{
    public interface IFehDataProvider
    {
        Task<IEnumerable<KeyValuePair<string, string>>> GetCharacter(string query);

        Task<IEnumerable<KeyValuePair<string, string>>> GetSkill(string query);

        Task<IEnumerable<KeyValuePair<string, string>>> GetWeapon(string query);
    }
}
using DSharpPlus.Entities;
using PassBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PassBot.Services.Interfaces
{
    public interface ISpreadsheetService
    {
        Task<MemoryStream> GenerateUserReport(List<UserProfileWithPoints> users);
    }
}

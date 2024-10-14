using ClosedXML.Excel;
using Microsoft.Extensions.Configuration;
using PassBot.Models;
using PassBot.Services.Interfaces;
using System.IO;

namespace PassBot.Services
{
    public class SpreadsheetService : ISpreadsheetService
    {
        private readonly string _connectionString;

        public SpreadsheetService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<MemoryStream> GenerateUserReport(List<UserProfileWithPoints> users)
        {
            var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("User Report");

            worksheet.Cell(1, 1).Value = "Discord Id";
            worksheet.Cell(1, 2).Value = "Discord Username";
            worksheet.Cell(1, 3).Value = "Email";
            worksheet.Cell(1, 4).Value = "Ethereum Address";
            worksheet.Cell(1, 5).Value = "Points Balance";

            int currentRow = 2;
            foreach (var user in users)
            {
                worksheet.Cell(currentRow, 1).Value = user.DiscordId;
                worksheet.Cell(currentRow, 2).Value = user.DiscordUsername ?? "Not set";
                worksheet.Cell(currentRow, 3).Value = user.Email ?? "Not set";
                worksheet.Cell(currentRow, 4).Value = user.WalletAddress ?? "Not set";
                worksheet.Cell(currentRow, 5).Value = user.Points;
                currentRow++;
            }

            worksheet.Columns().AdjustToContents();

            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return stream;
        }
    }
}

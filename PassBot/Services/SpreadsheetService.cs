using ClosedXML.Excel;
using Microsoft.Extensions.Configuration;
using PassBot.Models;
using PassBot.Services.Interfaces;

namespace PassBot.Services
{
    public class SpreadsheetService : ISpreadsheetService
    {
        private readonly string _connectionString;

        public SpreadsheetService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<MemoryStream> GeneratePointsReportAsync(List<UserProfileWithPoints> users)
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

        public async Task<MemoryStream> GenerateUserReportAsync(List<UserPointsLog> logs)
        {
            var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("User Points Log");

            worksheet.Cell(1, 1).Value = "Discord Id";
            worksheet.Cell(1, 2).Value = "Discord Username";
            worksheet.Cell(1, 3).Value = "Points";
            worksheet.Cell(1, 4).Value = "Assigned By";
            worksheet.Cell(1, 5).Value = "Assigned Date";
            worksheet.Cell(1, 6).Value = "Removed By";
            worksheet.Cell(1, 7).Value = "Removed At";
            worksheet.Cell(1, 8).Value = "Message";

            int currentRow = 2;
            foreach (var log in logs)
            {
                worksheet.Cell(currentRow, 1).Value = log.DiscordId;
                worksheet.Cell(currentRow, 2).Value = log.DiscordUsername ?? "Not set";
                worksheet.Cell(currentRow, 3).Value = log.Points;
                worksheet.Cell(currentRow, 4).Value = log.AssignerUsername ?? "Unknown";
                worksheet.Cell(currentRow, 5).Value = log.InsertedAt.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cell(currentRow, 6).Value = log.RemovedBy ?? "";
                worksheet.Cell(currentRow, 7).Value = log.RemovedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                worksheet.Cell(currentRow, 8).Value = log.Message ?? "";
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

﻿using PassBot.Models;

namespace PassBot.Services.Interfaces
{
    public interface ISpreadsheetService
    {
        Task<MemoryStream> GeneratePointsReportAsync(List<UserProfileWithPoints> users);
        Task<MemoryStream> GeneratePointsReportCSVUploadAsync(List<UserProfileWithPoints> users);
        Task<MemoryStream> GenerateUserReportAsync(List<UserPointsLog> logs);
        Task<MemoryStream> GenerateDatabaseBackupAsync(DBBackup backup);
        Task<DBBackup> GetFullBackupAsync();
    }
}

using ClosedXML.Excel;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using NanoidDotNet;
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

        
        public async Task<MemoryStream> GeneratePointsReportCSVUploadAsync(List<UserProfileWithPoints> users)
        {

            //NOTE: I made this 3 days ago incase we submit a few days after the month has started.
            DateTime now = DateTime.Now.AddDays(-3);

            var stream = new MemoryStream();
            using (var writer = new StreamWriter(stream, leaveOpen: true))
            {
                // Write the header row
                writer.WriteLine("Discord Id,Discord Username,Email,Wallet Address,X Account,Points Balance,Reference ID,Month");

                // Write the user data rows
                foreach (var user in users)
                {
                    var discordId = user.DiscordId ?? "Not set";
                    var discordUsername = user.DiscordUsername ?? "Not set";
                    var email = user.Email ?? "Not set";
                    var walletAddress = user.WalletAddress ?? "Not set";
                    var xAccount = user.XAccount ?? "Not set";
                    var points = user.Points;
                    var refId = Nanoid.Generate(size: 12);

                    // Write a CSV row
                    writer.WriteLine($"{discordId},{discordUsername},{email},{walletAddress},{xAccount},{points},{refId},{now.ToString("MMM-yy")}");
                }
            }

            // Reset the stream position to the beginning
            stream.Position = 0;
            return stream;
        }

        public async Task<MemoryStream> GeneratePointsReportAsync(List<UserProfileWithPoints> users)
        {
            var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("User Report");

            worksheet.Cell(1, 1).Value = "Discord Id";
            worksheet.Cell(1, 2).Value = "Discord Username";
            worksheet.Cell(1, 3).Value = "Email";
            worksheet.Cell(1, 4).Value = "Wallet Address";
            worksheet.Cell(1, 5).Value = "X Account";
            worksheet.Cell(1, 6).Value = "Points Balance";

            int currentRow = 2;
            foreach (var user in users)
            {
                worksheet.Cell(currentRow, 1).Value = user.DiscordId;
                worksheet.Cell(currentRow, 2).Value = user.DiscordUsername ?? "Not set";
                worksheet.Cell(currentRow, 3).Value = user.Email ?? "Not set";
                worksheet.Cell(currentRow, 4).Value = user.WalletAddress ?? "Not set";
                worksheet.Cell(currentRow, 5).Value = user.XAccount ?? "Not set";
                worksheet.Cell(currentRow, 6).Value = user.Points;
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

        public async Task<MemoryStream> GenerateDatabaseBackupAsync(DBBackup backup)
        {
            var workbook = new XLWorkbook();

            // Generic method to add worksheets dynamically
            void AddWorksheet<T>(string sheetName, List<T> data)
            {
                var worksheet = workbook.Worksheets.Add(sheetName);
                var properties = typeof(T).GetProperties();

                // Add headers
                for (int col = 0; col < properties.Length; col++)
                {
                    worksheet.Cell(1, col + 1).Value = properties[col].Name;
                }

                // Add data
                for (int row = 0; row < data.Count; row++)
                {
                    for (int col = 0; col < properties.Length; col++)
                    {
                        var value = properties[col].GetValue(data[row]);
                        // Dynamically handle nulls and types
                        if (value == null)
                        {
                            worksheet.Cell(row + 2, col + 1).Value = string.Empty;
                        }
                        else if (value is DateTime dateTimeValue)
                        {
                            worksheet.Cell(row + 2, col + 1).Value = dateTimeValue.ToString("yyyy-MM-dd HH:mm:ss");
                        }
                        else if (value is bool boolValue)
                        {
                            worksheet.Cell(row + 2, col + 1).Value = boolValue ? "True" : "False";
                        }
                        else
                        {
                            worksheet.Cell(row + 2, col + 1).Value = value.ToString();
                        }
                    }
                }

                worksheet.Columns().AdjustToContents();
            }

            // Add each table as a worksheet
            AddWorksheet("CheckInLog", backup.checkInLogs);
            AddWorksheet("Item", backup.items);
            AddWorksheet("Log", backup.logs);
            AddWorksheet("ProfileChangeLog", backup.profileChangeLogs);
            AddWorksheet("Redemption", backup.redemptions);
            AddWorksheet("UserPoints", backup.userPoints);
            AddWorksheet("UserPointsLog", backup.userPointsTableLogs);
            AddWorksheet("UserProfile", backup.userProfiles);

            // Save the workbook to a memory stream
            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return stream;
        }

        public async Task<DBBackup> GetFullBackupAsync()
        {
            DBBackup backup = new DBBackup();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Fetch CheckInLog data
                backup.checkInLogs = new List<CheckInLog>();
                using (SqlCommand checkInLogCommand = new SqlCommand("SELECT * FROM CheckInLog", connection))
                {
                    using (SqlDataReader reader = await checkInLogCommand.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            backup.checkInLogs.Add(new CheckInLog
                            {
                                Id = (int)reader["Id"],
                                DiscordId = reader["DiscordId"].ToString(),
                                LastCheckIn = (DateTime)reader["LastCheckIn"],
                                DiscordUsername = reader["DiscordUsername"].ToString(),
                                CheckInIterator = (int)reader["CheckInIterator"]
                            });
                        }
                    }
                }

                // Fetch Item data
                backup.items = new List<Item>();
                using (SqlCommand itemCommand = new SqlCommand("SELECT * FROM Item", connection))
                {
                    using (SqlDataReader reader = await itemCommand.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            backup.items.Add(new Item
                            {
                                Id = (int)reader["Id"],
                                Name = reader["Name"].ToString(),
                                CreatedOn = (DateTime)reader["CreatedOn"],
                                CreatedBy = reader["CreatedBy"].ToString(),
                                Cost = (long)reader["Cost"],
                                ExpiresOn = reader["ExpiresOn"] as DateTime?
                            });
                        }
                    }
                }

                // Fetch Log data
                backup.logs = new List<Log>();
                using (SqlCommand logCommand = new SqlCommand("SELECT * FROM log", connection))
                {
                    using (SqlDataReader reader = await logCommand.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            backup.logs.Add(new Log
                            {
                                Id = (int)reader["Id"],
                                DiscordId = reader["DiscordId"].ToString(),
                                DiscordUsername = reader["DiscordUsername"].ToString(),
                                Command = reader["Command"].ToString(),
                                Message = reader["Message"].ToString(),
                                DateTime = (DateTime)reader["DateTime"]
                            });
                        }
                    }
                }

                // Repeat this pattern for other tables...

                // Fetch ProfileChangeLog
                backup.profileChangeLogs = new List<ProfileChangeLog>();
                using (SqlCommand profileChangeLogCommand = new SqlCommand("SELECT * FROM ProfileChangeLog", connection))
                {
                    using (SqlDataReader reader = await profileChangeLogCommand.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            backup.profileChangeLogs.Add(new ProfileChangeLog
                            {
                                Id = (int)reader["Id"],
                                DiscordId = reader["DiscordId"].ToString(),
                                DiscordUsername = reader["DiscordUsername"].ToString(),
                                ChangedItem = reader["ChangedItem"].ToString(),
                                ChangedTime = (DateTime)reader["ChangedTime"],
                                ChangedTo = reader["ChangedTo"]?.ToString()
                            });
                        }
                    }
                }

                // Fetch Redemption
                backup.redemptions = new List<Redemption>();
                using (SqlCommand redemptionCommand = new SqlCommand("SELECT * FROM Redemption", connection))
                {
                    using (SqlDataReader reader = await redemptionCommand.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            backup.redemptions.Add(new Redemption
                            {
                                Id = (int)reader["Id"],
                                ItemId = (int)reader["ItemId"],
                                DiscordId = reader["DiscordId"].ToString(),
                                DiscordUsername = reader["DiscordUsername"].ToString(),
                                ClaimedOn = (DateTime)reader["ClaimedOn"],
                                SentBy = reader["SentBy"]?.ToString(),
                                SentOn = reader["SentOn"] as DateTime?,
                                Spent = (long)reader["Spent"],
                                RedemptionId = reader["RedemptionId"]?.ToString()
                            });
                        }
                    }
                }

                // Fetch UserPoints
                backup.userPoints = new List<UserPoints>();
                using (SqlCommand userPointsCommand = new SqlCommand("SELECT * FROM UserPoints", connection))
                {
                    using (SqlDataReader reader = await userPointsCommand.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            backup.userPoints.Add(new UserPoints
                            {
                                Id = (int)reader["Id"],
                                DiscordId = reader["DiscordId"].ToString(),
                                DiscordUsername = reader["DiscordUsername"].ToString(),
                                Points = (int)reader["Points"],
                                LastUpdated = (DateTime)reader["LastUpdated"]
                            });
                        }
                    }
                }

                // Fetch UserPointsTableLog
                backup.userPointsTableLogs = new List<UserPointsLog>();
                using (SqlCommand userPointsTableLogCommand = new SqlCommand("SELECT * FROM UserPointsTableLog", connection))
                {
                    using (SqlDataReader reader = await userPointsTableLogCommand.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            backup.userPointsTableLogs.Add(new UserPointsLog
                            {
                                Id = (int)reader["Id"],
                                DiscordId = reader["DiscordId"].ToString(),
                                DiscordUsername = reader["DiscordUsername"].ToString(),
                                AssignerId = reader["AssignerId"].ToString(),
                                AssignerUsername = reader["AssignerUsername"].ToString(),
                                Points = (int)reader["Points"],
                                InsertedAt = (DateTime)reader["InsertedAt"],
                                RemovedAt = reader["RemovedAt"] as DateTime?,
                                RemovedBy = reader["RemovedBy"]?.ToString(),
                                Message = reader["Message"]?.ToString()
                            });
                        }
                    }
                }

                // Fetch UserProfile
                backup.userProfiles = new List<UserProfile>();
                using (SqlCommand userProfileCommand = new SqlCommand("SELECT * FROM UserProfile", connection))
                {
                    using (SqlDataReader reader = await userProfileCommand.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            backup.userProfiles.Add(new UserProfile
                            {
                                Id = (int)reader["Id"],
                                DiscordId = reader["DiscordId"].ToString(),
                                DiscordUsername = reader["DiscordUsername"].ToString(),
                                Email = reader["Email"]?.ToString(),
                                WalletAddress = reader["WalletAddress"]?.ToString(),
                                XAccount = reader["XAccount"]?.ToString()
                            });
                        }
                    }
                }
            }

            return backup;
        }




    }
}

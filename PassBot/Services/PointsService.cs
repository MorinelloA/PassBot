using DSharpPlus.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PassBot.Models;
using PassBot.Services.Interfaces;
using PassBot.Utilities;
using System.Data;
using System.Runtime.CompilerServices;

namespace PassBot.Services
{
    public class PointsService : IPointsService
    {
        private readonly string _connectionString;
        private readonly long _checkInPoints;
        private readonly int _checkInTimes;
        private readonly Dictionary<PointCategory, long> _pointCategories;

        public PointsService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _checkInPoints = configuration.GetValue<long>("CheckInPoints");
            _checkInTimes = configuration.GetValue<int>("CheckInTimes");

            _pointCategories = configuration.GetSection("PointCategories")
               .Get<Dictionary<string, long>>()
               .OrderBy(kvp => kvp.Key) // Alphabetize the keys
               .ToDictionary(
                   kvp => Enum.Parse<PointCategory>(kvp.Key),
                   kvp => kvp.Value
               );
        }

        public async Task<long> GetPointsToAssignAsync(long? pointsToAdd, string category)
        {
            if (pointsToAdd.HasValue)
                return pointsToAdd.Value;

            if (Enum.TryParse(category, out PointCategory categoryEnum))
                return _pointCategories.GetValueOrDefault(categoryEnum, 0);

            return 0; // Invalid input
        }

        public async Task UpdatePointsAsync(DiscordUser assigner, DiscordUser user, long points, string message)
        {
            var discordId = user.Id.ToString();
            var discordUsername = user.Discriminator == "0" ? user.Username : $"{user.Username}#{user.Discriminator}";

            var assignerId = assigner.Id.ToString();
            var assignerUsername = assigner.Discriminator == "0" ? assigner.Username : $"{assigner.Username}#{assigner.Discriminator}";

            await AddPointsAsync(discordId, discordUsername, points);
            await LogPointsAssignmentAsync(discordId, discordUsername, assignerId, assignerUsername, points, message);
        }

        public async Task AddPointsAsync(string discordId, string discordUsername, long points)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
                    IF EXISTS (SELECT 1 FROM UserPoints WHERE DiscordId = @DiscordId)
                    BEGIN
                        UPDATE UserPoints SET Points = Points + @Points, LastUpdated = GETUTCDATE(), DiscordUsername = @DiscordUsername
                        WHERE DiscordId = @DiscordId;
                    END
                    ELSE
                    BEGIN
                        INSERT INTO UserPoints (DiscordId, DiscordUsername, Points, LastUpdated)
                        VALUES (@DiscordId, @DiscordUsername, @Points, GETUTCDATE());
                    END";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DiscordId", discordId);
                    command.Parameters.AddWithValue("@DiscordUsername", discordUsername);
                    command.Parameters.AddWithValue("@Points", points);

                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<MonthlyKPIHelper> GetMonthlyKPIDataAsync(int month, int year)
        {
            MonthlyKPIHelper kpis = new MonthlyKPIHelper();
            kpis.month = month;
            kpis.year = year;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"SELECT 
                    COALESCE(
                        (SELECT COUNT(DISTINCT u.[DiscordId])
                            FROM [PassApp].[dbo].[UserPointsTableLog] u), 0
                    ) AS totalUsers,
                    COALESCE(COUNT(DISTINCT upLog.[DiscordId]), 0) AS activeUsers,
                    COALESCE(SUM(upLog.[Points]), 0) AS pointsDistributed,
                    COALESCE(COUNT(DISTINCT upLog.[Message]), 0) AS actions,
                    COALESCE(COUNT(DISTINCT CASE 
                        WHEN upLog.[Message] LIKE '%Poll%' THEN upLog.[Message]
                        ELSE NULL
                    END), 0) AS polls,
                    COALESCE(COUNT(CASE 
                        WHEN upLog.[Message] LIKE '%Poll%' THEN upLog.[Message]
                        ELSE NULL
                    END), 0) AS pollResponses
                FROM [PassApp].[dbo].[UserPointsTableLog] AS upLog
                WHERE MONTH(upLog.[InsertedAt]) = @Month
                    AND YEAR(upLog.[InsertedAt]) = @Year
                    AND upLog.[Message] NOT LIKE '%Zealy%';";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Month", month);
                    command.Parameters.AddWithValue("@Year", year);
                    await connection.OpenAsync();

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            kpis.totalUsers = reader["totalUsers"] != DBNull.Value ? Convert.ToInt32(reader["totalUsers"]) : 0;
                            kpis.activeUsers = reader["activeUsers"] != DBNull.Value ? Convert.ToInt32(reader["activeUsers"]) : 0;
                            kpis.pointsDistributed = reader["pointsDistributed"] != DBNull.Value ? Convert.ToInt32(reader["pointsDistributed"]) : 0;
                            kpis.actions = reader["actions"] != DBNull.Value ? Convert.ToInt32(reader["actions"]) : 0;
                            kpis.polls = reader["polls"] != DBNull.Value ? Convert.ToInt32(reader["polls"]) : 0;
                            kpis.pollResponses = reader["pollResponses"] != DBNull.Value ? Convert.ToInt32(reader["pollResponses"]) : 0;

                            // Calculate the averageNumOfPollResponses
                            kpis.averageNumOfPollResponses = kpis.polls > 0
                                ? (decimal)kpis.pollResponses / kpis.polls
                                : 0;
                        }
                    }
                }
            }
            return kpis;
        }

        public async Task<long> GetUserPointsAsync(string discordId)
        {
            long points = 0;
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT Points FROM UserPoints WHERE DiscordId = @DiscordId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DiscordId", discordId);
                    await connection.OpenAsync();

                    var result = await command.ExecuteScalarAsync();
                    if (result != null)
                    {
                        points = Convert.ToInt64(result);
                    }
                }
            }
            return points;
        }

        public async Task<long> GetUserTransferredPointsAsync(string discordId)
        {
            long points = 0;
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT TransferredPoints FROM UserPoints WHERE DiscordId = @DiscordId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DiscordId", discordId);
                    await connection.OpenAsync();

                    var result = await command.ExecuteScalarAsync();
                    if (result != null)
                    {
                        points = Convert.ToInt64(result);
                    }
                }
            }
            return points;
        }

        public async Task LogPointsAssignmentAsync(string discordId, string discordUsername, string assignerId, string assignerUsername, long points, string message = null)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
                    INSERT INTO UserPointsTableLog (DiscordId, DiscordUsername, AssignerId, AssignerUsername, Points, InsertedAt, Message)
                    VALUES (@DiscordId, @DiscordUsername, @AssignerId, @AssignerUsername, @Points, GETUTCDATE(), @Message);";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Add parameters to prevent SQL injection
                    command.Parameters.AddWithValue("@DiscordId", discordId);
                    command.Parameters.AddWithValue("@DiscordUsername", discordUsername);
                    command.Parameters.AddWithValue("@AssignerId", assignerId);
                    command.Parameters.AddWithValue("@AssignerUsername", assignerUsername);
                    command.Parameters.AddWithValue("@Points", points);
                    command.Parameters.AddWithValue("@Message", string.IsNullOrEmpty(message) ? (object)DBNull.Value : message);

                    // Open the connection
                    await connection.OpenAsync();

                    // Execute the query
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        //Returns whether the user earned points or not
        public async Task<CheckInHelper> CheckInUserAsync(string discordId, string discordUsername, CheckInLog? checkInLog)
        {
            CheckInHelper helper = new CheckInHelper();
            helper.overallNeededCheckins = _checkInTimes;
            helper.pointsCanEarn = _checkInPoints;

            if (_checkInTimes <= 1 || (checkInLog != null && _checkInTimes <= checkInLog.CheckInIterator + 1))
            {
                // Add points for check-in
                await AddPointsAsync(discordId, discordUsername, _checkInPoints);

                // Log points assignment
                await LogPointsAssignmentAsync(discordId, discordUsername, discordId, discordUsername, _checkInPoints, "Check-In");

                await ResetCheckInTimeAsync(discordId, discordUsername);

                helper.didEarnPoints = true;
                helper.pointsEarned = _checkInPoints;
                helper.currentIterator = 0;
                helper.checkinsToPoints = _checkInTimes;
            }
            else
            {
                // Update the check-in time for the user
                await UpdateCheckInTimeAsync(discordId, discordUsername);

                helper.didEarnPoints = false;
                helper.pointsEarned = 0;
                helper.currentIterator = checkInLog == null ? 1 : checkInLog.CheckInIterator + 1;
                helper.checkinsToPoints = _checkInTimes - helper.currentIterator;
            }

            return helper;
        }


        public async Task<CheckInLog?> GetLastCheckInAsync(string discordId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"SELECT Id, DiscordId, LastCheckIn, DiscordUsername, CheckInIterator FROM CheckInLog WHERE DiscordId = @DiscordId";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DiscordId", discordId);
                    await connection.OpenAsync();

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var lastCheckIn = new CheckInLog
                            {
                                Id = Convert.ToInt64(reader["Id"]),
                                DiscordId = reader["DiscordId"].ToString(),
                                LastCheckIn = Convert.ToDateTime(reader["LastCheckIn"]),
                                DiscordUsername = reader["DiscordUsername"]?.ToString(),
                                CheckInIterator = Convert.ToInt32(reader["CheckInIterator"])
                            };

                            if (lastCheckIn != null && DateTime.UtcNow < lastCheckIn.LastCheckIn.AddHours(23))
                            {
                                lastCheckIn.IsAllowed = false;
                                // Calculate remaining time until they can check in again
                                lastCheckIn.RemainingTime = lastCheckIn.LastCheckIn.AddHours(23) - DateTime.UtcNow;
                            }
                            else
                            {
                                lastCheckIn.IsAllowed = true;
                            }

                            return lastCheckIn;
                        }
                        return null;
                    }
                }
            }
        }

        public async Task<List<UserPointsLog>> GetUserPointsLogByDiscordIdAsync(string discordId, bool includeRemoved = true)
        {
            var logs = new List<UserPointsLog>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                // SQL query with conditional filtering based on 'includeRemoved'
                string query = @"
                    SELECT DiscordId, DiscordUsername, Points, AssignerId, AssignerUsername, InsertedAt, RemovedBy, RemovedAt, Message
                    FROM UserPointsTableLog
                    WHERE DiscordId = @DiscordId";

                // Add condition to filter out removed records if 'includeRemoved' is false
                if (!includeRemoved)
                {
                    query += " AND RemovedAt IS NULL";
                }

                query += " ORDER BY InsertedAt DESC";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DiscordId", discordId);

                    await connection.OpenAsync();

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var log = new UserPointsLog
                            {
                                DiscordId = reader["DiscordId"].ToString(),
                                DiscordUsername = reader["DiscordUsername"]?.ToString(),
                                Points = Convert.ToInt32(reader["Points"]),
                                AssignerId = reader["AssignerId"].ToString(),
                                AssignerUsername = reader["AssignerUsername"]?.ToString(),
                                InsertedAt = Convert.ToDateTime(reader["InsertedAt"]),
                                RemovedBy = reader["RemovedBy"]?.ToString(),
                                RemovedAt = reader["RemovedAt"] as DateTime?,
                                Message = reader["Message"]?.ToString(),
                            };
                            logs.Add(log);
                        }
                    }
                }
            }

            return logs;
        }

        public async Task ResetCheckInTimeAsync(string discordId, string discordUsername)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
            
                UPDATE CheckInLog
                SET LastCheckIn = GETUTCDATE(), DiscordUsername = @DiscordUsername, CheckInIterator = 0
                WHERE DiscordId = @DiscordId;";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DiscordId", discordId);
                    command.Parameters.AddWithValue("@DiscordUsername", discordUsername);
                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task UpdateCheckInTimeAsync(string discordId, string discordUsername)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
            IF EXISTS (SELECT 1 FROM CheckInLog WHERE DiscordId = @DiscordId)
            BEGIN
                UPDATE CheckInLog
                SET LastCheckIn = GETUTCDATE(), DiscordUsername = @DiscordUsername, CheckInIterator = CheckInIterator + 1
                WHERE DiscordId = @DiscordId;
            END
            ELSE
            BEGIN
                INSERT INTO CheckInLog (DiscordId, DiscordUsername, CheckInIterator, LastCheckIn)
                VALUES (@DiscordId, @DiscordUsername, 1, GETUTCDATE());
            END";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DiscordId", discordId);
                    command.Parameters.AddWithValue("@DiscordUsername", discordUsername);
                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task DeleteUserPointsOfUsersFromListAsync(List<UserProfileWithPoints> users, string removerDiscordId)
        {
            // Extract DiscordIds from the provided list
            var discordIds = users.Select(u => u.DiscordId).ToList();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                // Begin transaction
                await connection.OpenAsync();
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Step 1: Create a DataTable to represent the TVP
                        var discordIdTable = new DataTable();
                        discordIdTable.Columns.Add("DiscordId", typeof(string));

                        foreach (var id in discordIds)
                        {
                            discordIdTable.Rows.Add(id);
                        }

                        // Step 2: Create and execute the DELETE command
                        //This is redundant with Step #4 now
                        /*
                        string deleteQuery = @"
                    DELETE FROM UserPoints
                    WHERE DiscordId IN (SELECT DiscordId FROM @DiscordIdTable);";

                        using (SqlCommand deleteCmd = new SqlCommand(deleteQuery, connection, transaction))
                        {
                            deleteCmd.Parameters.Add(new SqlParameter("@DiscordIdTable", SqlDbType.Structured)
                            {
                                TypeName = "DiscordIdTable",
                                Value = discordIdTable
                            });

                            await deleteCmd.ExecuteNonQueryAsync();
                        }
                        */

                        // Step 3: Create and execute the UPDATE command
                        string updateLogQuery = @"
                    UPDATE UserPointsTableLog
                    SET RemovedAt = GETUTCDATE(),
                        RemovedBy = @RemoverDiscordId
                    WHERE DiscordId IN (SELECT DiscordId FROM @DiscordIdTable)
                      AND RemovedAt IS NULL;";

                        using (SqlCommand updateCmd = new SqlCommand(updateLogQuery, connection, transaction))
                        {
                            updateCmd.Parameters.AddWithValue("@RemoverDiscordId", removerDiscordId);
                            updateCmd.Parameters.Add(new SqlParameter("@DiscordIdTable", SqlDbType.Structured)
                            {
                                TypeName = "DiscordIdTable",
                                Value = discordIdTable
                            });

                            await updateCmd.ExecuteNonQueryAsync();
                        }
                                                
                        // Step 4: Reset User Points Table
                        string resetUserPointsQuery = @"EXEC [dbo].[resetUserPointTable];";

                        using (SqlCommand resetCmd = new SqlCommand(resetUserPointsQuery, connection, transaction))
                        {
                            await resetCmd.ExecuteNonQueryAsync();
                        }

                        // Commit the transaction
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        // Rollback transaction if something goes wrong
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task TruncateUserPointsTableAsync(string removerDiscordId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                // Begin transaction
                await connection.OpenAsync();
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Step 1: Truncate the UserPoints table
                        using (SqlCommand truncateCmd = new SqlCommand("TRUNCATE TABLE UserPoints", connection, transaction))
                        {
                            await truncateCmd.ExecuteNonQueryAsync();
                        }

                        // Step 2: Update UserPointsTableLog with RemovedAt and RemovedBy for all active records
                        string updateLogQuery = @"
                        UPDATE UserPointsTableLog
                        SET RemovedAt = GETUTCDATE(),
                            RemovedBy = @RemoverDiscordId
                        WHERE RemovedAt IS NULL";

                        using (SqlCommand updateCmd = new SqlCommand(updateLogQuery, connection, transaction))
                        {
                            updateCmd.Parameters.AddWithValue("@RemoverDiscordId", removerDiscordId);
                            await updateCmd.ExecuteNonQueryAsync();
                        }

                        // Commit the transaction
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        // Rollback transaction if something goes wrong
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

    }
}

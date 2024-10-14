using DSharpPlus.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PassBot.Models;
using PassBot.Services.Interfaces;

namespace PassBot.Services
{
    public class PointsService : IPointsService
    {
        private readonly string _connectionString;
        private readonly long _checkInPoints;
        private readonly Dictionary<PointCategory, long> _pointCategories;

        public PointsService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _checkInPoints = configuration.GetValue<long>("CheckInPoints");

            _pointCategories = configuration.GetSection("PointCategories")
               .Get<Dictionary<string, long>>()
               .OrderBy(kvp => kvp.Key) // Alphabetize the keys
               .ToDictionary(
                   kvp => Enum.Parse<PointCategory>(kvp.Key),
                   kvp => kvp.Value
               );
        }

        public async Task<long> GetPointsToAssign(long? pointsToAdd, string category)
        {
            if (pointsToAdd.HasValue)
                return pointsToAdd.Value;

            if (Enum.TryParse(category, out PointCategory categoryEnum))
                return _pointCategories.GetValueOrDefault(categoryEnum, 0);

            return 0; // Invalid input
        }

        public async Task UpdatePoints(DiscordUser assigner, DiscordUser user, long points, string message)
        {
            var discordId = user.Id.ToString();
            var discordUsername = user.Discriminator == "0" ? user.Username : $"{user.Username}#{user.Discriminator}";

            var assignerId = assigner.Id.ToString();
            var assignerUsername = assigner.Discriminator == "0" ? assigner.Username : $"{assigner.Username}#{assigner.Discriminator}";

            await AddPoints(discordId, discordUsername, points);
            await LogPointsAssignment(discordId, discordUsername, assignerId, assignerUsername, points, message);
        }

        public async Task AddPoints(string discordId, string discordUsername, long points)
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

        public async Task<long> GetUserPoints(string discordId)
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

        public async Task LogPointsAssignment(string discordId, string discordUsername, string assignerId, string assignerUsername, long points, string message = null)
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

        public async Task<(bool IsAllowed, TimeSpan? RemainingTime)> CanCheckIn(string discordId)
        {
            var lastCheckIn = await GetLastCheckIn(discordId);

            if (lastCheckIn != null && DateTime.UtcNow < lastCheckIn.Value.AddHours(23))
            {
                // Calculate remaining time until they can check in again
                var remainingTime = lastCheckIn.Value.AddHours(23) - DateTime.UtcNow;
                return (false, remainingTime);
            }

            return (true, null);
        }

        public async Task CheckInUser(string discordId, string discordUsername)
        {
            // Add points for check-in
            await AddPoints(discordId, discordUsername, _checkInPoints);

            // Log points assignment
            await LogPointsAssignment(discordId, discordUsername, discordId, discordUsername, _checkInPoints);

            // Update the check-in time for the user
            await UpdateCheckInTime(discordId, discordUsername);
        }

        public async Task<DateTime?> GetLastCheckIn(string discordId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT LastCheckIn FROM CheckInLog WHERE DiscordId = @DiscordId";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DiscordId", discordId);
                    await connection.OpenAsync();

                    var result = await command.ExecuteScalarAsync();
                    if (result != null)
                    {
                        return Convert.ToDateTime(result);
                    }
                    return null;
                }
            }
        }

        public async Task UpdateCheckInTime(string discordId, string discordUsername)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
            IF EXISTS (SELECT 1 FROM CheckInLog WHERE DiscordId = @DiscordId)
            BEGIN
                UPDATE CheckInLog
                SET LastCheckIn = GETUTCDATE(), DiscordUsername = @DiscordUsername
                WHERE DiscordId = @DiscordId;
            END
            ELSE
            BEGIN
                INSERT INTO CheckInLog (DiscordId, DiscordUsername, LastCheckIn)
                VALUES (@DiscordId, @DiscordUsername, GETUTCDATE());
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


        public async Task TruncateUserPointsTable(string removerDiscordId)
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

using DocumentFormat.OpenXml.Spreadsheet;
using DSharpPlus.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PassBot.Models;
using PassBot.Services.Interfaces;

namespace PassBot.Services
{
    public class ProfileService : IProfileService
    {
        private readonly string _connectionString;
        private readonly int _profileChangeCooldownDays;
        private readonly int _profileAdditionPoints;

        private readonly IPointsService _pointsService;

        public ProfileService(IConfiguration configuration, IPointsService pointsService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _profileChangeCooldownDays = configuration.GetValue<int>("ProfileChangeCooldownDays");
            _profileAdditionPoints = configuration.GetValue<int>("ProfileAdditionPoints");            
            _pointsService = pointsService;
        }

        public async Task<UserProfile> GetUserProfileAsync(string discordId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT DiscordId, DiscordUsername, Email, WalletAddress, XAccount FROM UserProfile WHERE DiscordId = @DiscordId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DiscordId", discordId);
                    await connection.OpenAsync();

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new UserProfile
                            {
                                DiscordId = reader["DiscordId"].ToString(),
                                DiscordUsername = reader["DiscordUsername"]?.ToString(),
                                Email = reader["Email"]?.ToString(),
                                WalletAddress = reader["WalletAddress"]?.ToString(),
                                XAccount = reader["XAccount"]?.ToString()
                            };
                        }
                        return null;
                    }
                }
            }
        }

        public async Task<bool> AssignPoints(DiscordUser user, string fieldToCheck)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = $@"
                    SELECT CASE 
                        WHEN NOT EXISTS (SELECT 1 FROM UserProfile WHERE DiscordId = @DiscordId) THEN 1
                        WHEN (SELECT {fieldToCheck} FROM UserProfile WHERE DiscordId = @DiscordId) IS NULL THEN 1
                        ELSE 0
                    END AS ShouldAssignPoints";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DiscordId", user.Id.ToString());

                    await connection.OpenAsync();
                    var result = await command.ExecuteScalarAsync();

                    return result != null && Convert.ToInt32(result) == 1;
                }
            }
        }


        public async Task SetEmailAsync(DiscordUser user, string email)
        {
            bool shouldAssignPoints = await AssignPoints(user, "Email");

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
            IF EXISTS (SELECT 1 FROM UserProfile WHERE DiscordId = @DiscordId)
            BEGIN
                UPDATE UserProfile
                SET Email = @Email, DiscordUsername = @DiscordUsername
                WHERE DiscordId = @DiscordId;
            END
            ELSE
            BEGIN
                INSERT INTO UserProfile (DiscordId, DiscordUsername, Email)
                VALUES (@DiscordId, @DiscordUsername, @Email);
            END";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DiscordId", user.Id.ToString());
                    command.Parameters.AddWithValue("@DiscordUsername", user.Username);
                    command.Parameters.AddWithValue("@Email", email);

                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();
                }
            }

            // Add entry to ProfileChangeLog
            var changeLog = new ProfileChangeLog
            {
                DiscordId = user.Id.ToString(),
                DiscordUsername = user.Username,
                ChangedItem = "Email",
                ChangedTo = email,
                ChangedTime = DateTime.UtcNow
            };

            await AddProfileChangeLogAsync(changeLog);

            // Award points if necessary
            if (shouldAssignPoints)
            {
                var assigner = user;
                string message = "Profile Email Set";
                long points = _profileAdditionPoints;

                await _pointsService.UpdatePointsAsync(assigner, user, points, message);
            }
        }


        public async Task SetWalletAddressAsync(DiscordUser user, string walletAddress)
        {
            bool shouldAssignPoints = await AssignPoints(user, "WalletAddress");

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
            IF EXISTS (SELECT 1 FROM UserProfile WHERE DiscordId = @DiscordId)
            BEGIN
                UPDATE UserProfile
                SET WalletAddress = @WalletAddress, DiscordUsername = @DiscordUsername
                WHERE DiscordId = @DiscordId;
            END
            ELSE
            BEGIN
                INSERT INTO UserProfile (DiscordId, DiscordUsername, WalletAddress)
                VALUES (@DiscordId, @DiscordUsername, @WalletAddress);
            END";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DiscordId", user.Id.ToString());
                    command.Parameters.AddWithValue("@DiscordUsername", user.Username);
                    command.Parameters.AddWithValue("@WalletAddress", walletAddress);

                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();
                }
            }

            var changeLog = new ProfileChangeLog
            {
                DiscordId = user.Id.ToString(),
                DiscordUsername = user.Username,
                ChangedItem = "Wallet Address",
                ChangedTo = walletAddress,
                ChangedTime = DateTime.UtcNow
            };

            await AddProfileChangeLogAsync(changeLog);

            // Award points if necessary
            if (shouldAssignPoints)
            {
                var assigner = user;
                string message = "Profile Wallet Set";
                long points = _profileAdditionPoints;

                await _pointsService.UpdatePointsAsync(assigner, user, points, message);
            }
        }



        public async Task<List<UserProfileWithPoints>> GetAllUserProfilesWithPointsAsync()
        {
            var profiles = new List<UserProfileWithPoints>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                // SQL query to get all users with either profiles, points, or both
                string query = @"
            SELECT 
                COALESCE(up.DiscordId, p.DiscordId) AS DiscordId,
                COALESCE(up.DiscordUsername, p.DiscordUsername) AS DiscordUsername,  -- Prioritize the first non-null username
                up.Email,
                up.WalletAddress,
                up.XAccount,
                COALESCE(p.Points, 0) AS Points
            FROM UserProfile up
            FULL OUTER JOIN UserPoints p ON up.DiscordId = p.DiscordId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var profile = new UserProfileWithPoints
                            {
                                DiscordId = reader["DiscordId"].ToString(),
                                DiscordUsername = reader["DiscordUsername"]?.ToString(),
                                Email = reader["Email"]?.ToString(),
                                WalletAddress = reader["WalletAddress"]?.ToString(),
                                XAccount = reader["XAccount"]?.ToString(),
                                Points = Convert.ToInt64(reader["Points"])
                            };

                            profiles.Add(profile);
                        }
                    }
                }
            }

            return profiles;
        }

        public async Task<UserProfileWithPoints> GetUserProfileWithPointsByDiscordIdAsync(string discordId)
        {
            UserProfileWithPoints profile = null;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT 
                        COALESCE(up.DiscordId, p.DiscordId) AS DiscordId,
                        COALESCE(up.DiscordUsername, p.DiscordUsername) AS DiscordUsername,  -- Prioritize the first non-null username
                        up.Email,
                        up.WalletAddress,
                        up.XAccount,
                        COALESCE(p.Points, 0) AS Points
                    FROM UserProfile up
                    FULL OUTER JOIN UserPoints p ON up.DiscordId = p.DiscordId
                    WHERE COALESCE(up.DiscordId, p.DiscordId) = @DiscordId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DiscordId", discordId);
                    await connection.OpenAsync();

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())  // Expect only one result, hence 'if' instead of 'while'
                        {
                            profile = new UserProfileWithPoints
                            {
                                DiscordId = reader["DiscordId"].ToString(),
                                DiscordUsername = reader["DiscordUsername"]?.ToString(),
                                Email = reader["Email"]?.ToString(),
                                WalletAddress = reader["WalletAddress"]?.ToString(),
                                XAccount = reader["XAccount"]?.ToString(),
                                Points = Convert.ToInt64(reader["Points"])
                            };
                        }
                    }
                }
            }

            return profile;  // Return null if no record found
        }

        public async Task AddProfileChangeLogAsync(ProfileChangeLog log)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
            INSERT INTO ProfileChangeLog (DiscordId, DiscordUsername, ChangedItem, ChangedTime, ChangedTo)
            VALUES (@DiscordId, @DiscordUsername, @ChangedItem, @ChangedTime, @ChangedTo)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DiscordId", log.DiscordId);
                    command.Parameters.AddWithValue("@DiscordUsername", log.DiscordUsername);
                    command.Parameters.AddWithValue("@ChangedItem", log.ChangedItem);
                    command.Parameters.AddWithValue("@ChangedTime", log.ChangedTime);
                    command.Parameters.AddWithValue("@ChangedTo", log.ChangedTo);

                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<TimeSpan?> GetTimeUntilNextProfileChangeAsync(string discordId, string item)
        {
            // Get the last change time for the given item
            var lastChange = await GetLastChangeTimeAsync(discordId, item);

            // If the user has never changed this item, they can update it immediately
            if (lastChange == null)
            {
                return null;
            }

            // Get the cooldown period from the config (Assuming _configuration is injected properly)
            int cooldownDays = _profileChangeCooldownDays;

            // Calculate the next allowed change date
            var nextAllowedChange = lastChange.Value.AddDays(cooldownDays);

            // If the cooldown has passed, they can update now (no waiting time)
            if (DateTime.UtcNow >= nextAllowedChange)
            {
                return null;
            }

            // Return the remaining time until the user can change the item
            return nextAllowedChange - DateTime.UtcNow;
        }

        public async Task<DateTime?> GetLastChangeTimeAsync(string discordId, string itemName)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT TOP 1 ChangedTime
                    FROM ProfileChangeLog
                    WHERE DiscordId = @DiscordId AND ChangedItem = @ItemName
                    ORDER BY ChangedTime DESC";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DiscordId", discordId);
                    command.Parameters.AddWithValue("@ItemName", itemName);

                    await connection.OpenAsync();

                    var result = await command.ExecuteScalarAsync();
                    if (result != null)
                    {
                        return Convert.ToDateTime(result);
                    }
                }
            }

            return null; // If no record exists, return null
        }

        public async Task SetXAccountAsync(DiscordUser user, string xAccount)
        {
            bool shouldAssignPoints = await AssignPoints(user, "XAccount");

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
            IF EXISTS (SELECT 1 FROM UserProfile WHERE DiscordId = @DiscordId)
            BEGIN
                UPDATE UserProfile
                SET XAccount = @XAccount, DiscordUsername = @DiscordUsername
                WHERE DiscordId = @DiscordId;
            END
            ELSE
            BEGIN
                INSERT INTO UserProfile (DiscordId, DiscordUsername, XAccount)
                VALUES (@DiscordId, @DiscordUsername, @XAccount);
            END";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DiscordId", user.Id.ToString());
                    command.Parameters.AddWithValue("@DiscordUsername", user.Username);
                    command.Parameters.AddWithValue("@XAccount", xAccount);

                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();
                }
            }

            var changeLog = new ProfileChangeLog
            {
                DiscordId = user.Id.ToString(),
                DiscordUsername = user.Username,
                ChangedItem = "X Account",
                ChangedTo = xAccount,
                ChangedTime = DateTime.UtcNow
            };

            await AddProfileChangeLogAsync(changeLog);

            // Award points if necessary
            if (shouldAssignPoints)
            {
                var assigner = user;
                string message = "Profile X Account Set";
                long points = _profileAdditionPoints;

                await _pointsService.UpdatePointsAsync(assigner, user, points, message);
            }
        }


    }
}

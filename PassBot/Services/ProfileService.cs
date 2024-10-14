using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PassBot.Models;
using PassBot.Services.Interfaces;

namespace PassBot.Services
{
    public class ProfileService : IProfileService
    {
        private readonly string _connectionString;

        public ProfileService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<UserProfile> GetUserProfile(string discordId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT DiscordId, DiscordUsername, Email, WalletAddress FROM UserProfile WHERE DiscordId = @DiscordId";

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
                                WalletAddress = reader["WalletAddress"]?.ToString()
                            };
                        }
                        return null;
                    }
                }
            }
        }

        public async Task SetEmail(DiscordUser user, string email)
        {
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
        }

        public async Task SetWalletAddress(DiscordUser user, string walletAddress)
        {
            

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
        }

        public async Task<List<UserProfileWithPoints>> GetAllUserProfilesWithPoints()
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
                                Points = Convert.ToInt64(reader["Points"])
                            };

                            profiles.Add(profile);
                        }
                    }
                }
            }

            return profiles;
        }

        public async Task<UserProfileWithPoints> GetUserProfileWithPointsByDiscordId(string discordId)
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
                                Points = Convert.ToInt64(reader["Points"])
                            };
                        }
                    }
                }
            }

            return profile;  // Return null if no record found
        }

    }
}

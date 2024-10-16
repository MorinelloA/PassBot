using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using NanoidDotNet;
using PassBot.Models;
using PassBot.Services.Interfaces;

namespace PassBot.Services
{
    public class RedemptionService : IRedemptionService
    {
        private readonly string _connectionString;

        public RedemptionService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // Method to check if an active item with the same name exists
        public async Task<bool> DoesActiveItemExistAsync(string name)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string checkQuery = @"
                SELECT COUNT(1) 
                FROM Item 
                WHERE Name = @Name 
                AND (ExpiresOn IS NULL OR ExpiresOn > GETUTCDATE())";

                using (SqlCommand command = new SqlCommand(checkQuery, connection))
                {
                    command.Parameters.AddWithValue("@Name", name);

                    await connection.OpenAsync();

                    var result = await command.ExecuteScalarAsync();

                    return Convert.ToInt32(result) > 0;
                }
            }
        }

        public async Task<Item> GetActiveItemByNameAsync(string name)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
            SELECT * FROM Item
            WHERE Name = @Name AND (ExpiresOn IS NULL OR ExpiresOn > GETUTCDATE())";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", name);

                    await connection.OpenAsync();
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new Item
                            {
                                Id = (int)reader["Id"],
                                Name = reader["Name"].ToString(),
                                Cost = (long)reader["Cost"],
                                CreatedOn = (DateTime)reader["CreatedOn"],
                                CreatedBy = reader["CreatedBy"].ToString(),
                                ExpiresOn = reader["ExpiresOn"] as DateTime?
                            };
                        }
                    }
                }
            }
            return null; // No active item found
        }

        public async Task ExpireItemAsync(int itemId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
            UPDATE Item
            SET ExpiresOn = GETUTCDATE()
            WHERE Id = @ItemId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ItemId", itemId);

                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<List<Item>> GetNonExpiredItemsAsync()
        {
            var items = new List<Item>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
            SELECT * FROM Item
            WHERE ExpiresOn IS NULL OR ExpiresOn > GETUTCDATE()
            ORDER BY Name ASC";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var item = new Item
                            {
                                Id = (int)reader["Id"],
                                Name = reader["Name"].ToString(),
                                Cost = (long)reader["Cost"],
                                CreatedOn = (DateTime)reader["CreatedOn"],
                                CreatedBy = reader["CreatedBy"].ToString(),
                                ExpiresOn = reader["ExpiresOn"] as DateTime?
                            };

                            items.Add(item);
                        }
                    }
                }
            }
            return items;
        }

        // Method to add a new item
        public async Task AddItemAsync(Item item)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string insertQuery = @"
                INSERT INTO Item (Name, Cost, CreatedOn, CreatedBy, ExpiresOn) 
                VALUES (@Name, @Cost, @CreatedOn, @CreatedBy, @ExpiresOn)";

                using (SqlCommand command = new SqlCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@Name", item.Name);
                    command.Parameters.AddWithValue("@Cost", item.Cost);
                    command.Parameters.AddWithValue("@CreatedOn", item.CreatedOn);
                    command.Parameters.AddWithValue("@CreatedBy", item.CreatedBy);

                    if (item.ExpiresOn.HasValue)
                    {
                        command.Parameters.AddWithValue("@ExpiresOn", item.ExpiresOn.Value);
                    }
                    else
                    {
                        command.Parameters.AddWithValue("@ExpiresOn", DBNull.Value);
                    }

                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task RedeemItemAsync(Item item, string discordId, string discordUsername, long spent)
        {
            // Generate a NanoID
            var redemptionId = Nanoid.Generate(size: 12);

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
                    INSERT INTO Redemption (RedemptionId, ItemId, DiscordId, DiscordUsername, ClaimedOn, Spent)
                    VALUES (@RedemptionId, @ItemId, @DiscordId, @DiscordUsername, GETUTCDATE(), @Spent)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@RedemptionId", redemptionId);
                    command.Parameters.AddWithValue("@ItemId", item.Id);
                    command.Parameters.AddWithValue("@DiscordId", discordId);
                    command.Parameters.AddWithValue("@DiscordUsername", discordUsername);
                    command.Parameters.AddWithValue("@Spent", spent);

                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<List<Redemption>> GetOpenRedemptionsAsync()
        {
            var redemptions = new List<Redemption>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
            SELECT r.Id, r.RedemptionId, r.ItemId, r.DiscordId, r.DiscordUsername, r.ClaimedOn, i.Name as ItemName
            FROM Redemption r
            JOIN Item i ON r.ItemId = i.Id
            WHERE r.SentOn IS NULL";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var redemption = new Redemption
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                RedemptionId = reader["RedemptionId"].ToString(),
                                ItemId = Convert.ToInt32(reader["ItemId"]),
                                DiscordId = reader["DiscordId"].ToString(),
                                DiscordUsername = reader["DiscordUsername"].ToString(),
                                ClaimedOn = Convert.ToDateTime(reader["ClaimedOn"]),
                                ItemName = reader["ItemName"].ToString()
                            };

                            redemptions.Add(redemption);
                        }
                    }
                }
            }

            return redemptions;
        }

        public async Task<List<Redemption>> GetUserRedemptionsAsync(string discordId)
        {
            var redemptions = new List<Redemption>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT r.Id, r.RedemptionId, r.ItemId, r.DiscordId, r.DiscordUsername, r.ClaimedOn, r.SentOn, i.Name AS ItemName
                    FROM Redemption r
                    JOIN Item i ON r.ItemId = i.Id
                    WHERE r.DiscordId = @DiscordId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DiscordId", discordId);

                    await connection.OpenAsync();
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var redemption = new Redemption
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                RedemptionId = reader["RedemptionId"].ToString(),
                                ItemName = reader["ItemName"].ToString(),
                                DiscordId = reader["DiscordId"].ToString(),
                                DiscordUsername = reader["DiscordUsername"].ToString(),
                                ClaimedOn = Convert.ToDateTime(reader["ClaimedOn"]),
                                SentOn = reader["SentOn"] as DateTime?
                            };
                            redemptions.Add(redemption);
                        }
                    }
                }
            }

            return redemptions;
        }

        public async Task<Redemption> GetOpenRedemptionByIdAsync(string redemptionId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT * FROM Redemption
                    WHERE RedemptionId = @RedemptionId
                    AND SentOn IS NULL";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@RedemptionId", redemptionId);
                    await connection.OpenAsync();

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new Redemption
                            {
                                Id = (int)reader["Id"],
                                ItemId = (int)reader["ItemId"],
                                DiscordId = reader["DiscordId"].ToString(),
                                DiscordUsername = reader["DiscordUsername"].ToString(),
                                ClaimedOn = (DateTime)reader["ClaimedOn"],
                                SentBy = reader["SentBy"] as string,
                                SentOn = reader["SentOn"] as DateTime?,
                                Spent = (long)reader["Spent"],
                                RedemptionId = reader["RedemptionId"].ToString()
                            };
                        }
                        return null;
                    }
                }
            }
        }

        public async Task CloseRedemptionAsync(int redemptionId, string sentByDiscordId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
                    UPDATE Redemption
                    SET SentBy = @SentByDiscordId, SentOn = GETUTCDATE()
                    WHERE Id = @RedemptionId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@SentByDiscordId", sentByDiscordId);
                    command.Parameters.AddWithValue("@RedemptionId", redemptionId);

                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}

using MySqlConnector;

namespace Vizsgaremek2026.Services
{
    public class ReviewService
    {
        private readonly string _connectionString;

        public ReviewService(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException("DefaultConnection connection string is required");
        }

        public async Task<List<(string Name, string Comment, int Rating, DateTime CreatedAt)>> GetReviewsAsync()
        {
            var reviews = new List<(string Name, string Comment, int Rating, DateTime CreatedAt)>();

            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            const string sql = "SELECT Name, Comment, Rating, CreatedAt FROM reviews ORDER BY Id DESC";
            await using var cmd = new MySqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                reviews.Add((reader.GetString(0), reader.GetString(1), reader.GetInt32(2), reader.GetDateTime(3)));
            }

            return reviews;
        }

        public async Task AddReviewAsync(string name, string comment, int rating)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new MySqlCommand("INSERT INTO reviews (Name, Comment, Rating) VALUES (@n, @c, @r)", conn);
            cmd.Parameters.AddWithValue("@n", name);
            cmd.Parameters.AddWithValue("@c", comment);
            cmd.Parameters.AddWithValue("@r", rating);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}

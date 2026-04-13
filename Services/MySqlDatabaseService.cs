using System.Data;
using System.Text.Json;
using MySqlConnector;
using Vizsgaremek2026.Models;

namespace Vizsgaremek2026.Services
{
    public interface IMySqlDatabaseService
    {
        Task<RentalItem?> GetRentalItemAsync(int id);
        Task<List<RentalItem>> GetAllRentalItemsAsync();
        Task<RentalOrder> CreateRentalOrderAsync(RentalOrder order);
        Task<bool> HasCapacityForRangeAsync(int rentalItemId, DateTime from, DateTime to, int requestedQty);
        Task<List<DateTime>> GetDepletedDaysAsync(int rentalItemId, DateTime from, DateTime to);
        Task<List<TimeSpan>> GetUnavailableHourSlotsAsync(int rentalItemId, DateTime day);
    }

    public class MySqlDatabaseService : IMySqlDatabaseService
    {
        private readonly string _connectionString;
        private static readonly TimeSpan[] SlotStarts = Enumerable.Range(6, 16).Select(h => new TimeSpan(h, 0, 0)).ToArray();

        public MySqlDatabaseService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException("DefaultConnection connection string is required");
        }

        private MySqlConnection CreateConnection() => new(_connectionString);

        public async Task<RentalItem?> GetRentalItemAsync(int id)
        {
            const string sql = @"
SELECT Id, Name, Category, Capacity, PricePerDay, Rating, Tags, Description, ImageUrl, IsActive, CreatedAt
FROM RentalItems
WHERE Id = @Id AND IsActive = 1
LIMIT 1;";

            await using var connection = CreateConnection();
            await connection.OpenAsync();

            await using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.Add("@Id", MySqlDbType.Int32).Value = id;

            await using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapRentalItem(reader) : null;
        }

        public async Task<List<RentalItem>> GetAllRentalItemsAsync()
        {
            const string sql = @"
SELECT Id, Name, Category, Capacity, PricePerDay, Rating, Tags, Description, ImageUrl, IsActive, CreatedAt
FROM RentalItems
WHERE IsActive = 1
ORDER BY Category, Name;";

            var result = new List<RentalItem>();

            await using var connection = CreateConnection();
            await connection.OpenAsync();

            await using var cmd = new MySqlCommand(sql, connection);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(MapRentalItem(reader));
            }

            return result;
        }

        public async Task<RentalOrder> CreateRentalOrderAsync(RentalOrder order)
        {
            const string insertOrderSql = @"
INSERT INTO RentalOrders (
    CustomerName, CustomerEmail, CustomerPhone, CustomerAddress,
    CustomerCity, CustomerPostalCode, VatNumber,
    Quantity, PricePerDay, TotalAmount,
    PaymentMethod, StripeSessionId, StripePaymentIntentId,
    Status, Notes, CreatedAt, UpdatedAt, PaidAt
)
VALUES (
    @CustomerName, @CustomerEmail, @CustomerPhone, @CustomerAddress,
    @CustomerCity, @CustomerPostalCode, @VatNumber,
    @Quantity, @PricePerDay, @TotalAmount,
    @PaymentMethod, @StripeSessionId, @StripePaymentIntentId,
    @Status, @Notes, @CreatedAt, @UpdatedAt, @PaidAt
);
SELECT LAST_INSERT_ID();";

            const string insertItemSql = @"
INSERT INTO RentalOrderItems (RentalOrderId, RentalItemId, Quantity, Price, RentalStartDate, RentalEndDate)
VALUES (@RentalOrderId, @RentalItemId, @Quantity, @Price, @StartDate, @EndDate);";

            const string insertPaymentSql = @"
INSERT INTO PaymentRecords (
    RentalOrderId, Amount, Vat, PaymentMethod, Status,
    StripeSessionId, StripePaymentIntentId, StripeChargeId,
    ProviderResponse, CreatedAt, CompletedAt
)
VALUES (
    @RentalOrderId, @Amount, @Vat, @PaymentMethod, @Status,
    @StripeSessionId, @StripePaymentIntentId, @StripeChargeId,
    @ProviderResponse, @CreatedAt, @CompletedAt
);";

            await using var connection = CreateConnection();
            await connection.OpenAsync();
            await using var tx = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted);

            try
            {
                await using (var orderCmd = new MySqlCommand(insertOrderSql, connection, tx))
                {
                    AddOrderParameters(orderCmd, order);
                    var idObj = await orderCmd.ExecuteScalarAsync();
                    order.Id = Convert.ToInt32(idObj);
                }

                foreach (var item in order.Items)
                {
                    await using var itemCmd = new MySqlCommand(insertItemSql, connection, tx);
                    itemCmd.Parameters.AddWithValue("@RentalOrderId", order.Id);
                    itemCmd.Parameters.AddWithValue("@RentalItemId", item.RentalItemId);
                    itemCmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                    itemCmd.Parameters.AddWithValue("@Price", item.Price);
                    itemCmd.Parameters.AddWithValue("@StartDate", item.RentalStartDate);
                    itemCmd.Parameters.AddWithValue("@EndDate", item.RentalEndDate);
                    await itemCmd.ExecuteNonQueryAsync();
                }

                await using (var paymentCmd = new MySqlCommand(insertPaymentSql, connection, tx))
                {
                    paymentCmd.Parameters.AddWithValue("@RentalOrderId", order.Id);
                    paymentCmd.Parameters.AddWithValue("@Amount", order.TotalAmount);
                    paymentCmd.Parameters.AddWithValue("@Vat", decimal.Round(order.TotalAmount * 0.27m, 2));
                    paymentCmd.Parameters.AddWithValue("@PaymentMethod", order.PaymentMethod);
                    paymentCmd.Parameters.AddWithValue("@Status", order.PaymentStatus ?? "pending");
                    paymentCmd.Parameters.AddWithValue("@StripeSessionId", (object?)order.StripeSessionId ?? DBNull.Value);
                    paymentCmd.Parameters.AddWithValue("@StripePaymentIntentId", (object?)order.StripePaymentIntentId ?? DBNull.Value);
                    paymentCmd.Parameters.AddWithValue("@StripeChargeId", (object?)order.StripeChargeId ?? DBNull.Value);
                    paymentCmd.Parameters.AddWithValue("@ProviderResponse", (object?)order.PaymentProviderResponse ?? DBNull.Value);
                    paymentCmd.Parameters.AddWithValue("@CreatedAt", order.CreatedAt);
                    paymentCmd.Parameters.AddWithValue("@CompletedAt", (object?)order.PaymentCompletedAt ?? DBNull.Value);
                    await paymentCmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                return order;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> HasCapacityForRangeAsync(int rentalItemId, DateTime from, DateTime to, int requestedQty)
        {
            if (requestedQty < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(requestedQty), "A mennyiség legalább 1 kell legyen.");
            }

            if (to <= from)
            {
                throw new ArgumentException("A végdátumnak későbbinek kell lennie a kezdő dátumnál.");
            }

            var item = await GetRentalItemAsync(rentalItemId)
                       ?? throw new InvalidOperationException($"A termék nem található: {rentalItemId}");

            const string sql = @"
SELECT COALESCE(SUM(roi.Quantity), 0)
FROM RentalOrderItems roi
INNER JOIN RentalOrders ro ON ro.Id = roi.RentalOrderId
WHERE roi.RentalItemId = @ItemId
  AND ro.Status IN ('pending', 'booked', 'paid', 'confirmed')
  AND roi.RentalStartDate < @RangeEnd
  AND roi.RentalEndDate > @RangeStart;";

            await using var connection = CreateConnection();
            await connection.OpenAsync();

            await using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@ItemId", rentalItemId);
            cmd.Parameters.AddWithValue("@RangeStart", from);
            cmd.Parameters.AddWithValue("@RangeEnd", to);

            var existingObj = await cmd.ExecuteScalarAsync();
            var alreadyUsed = existingObj is null || existingObj == DBNull.Value ? 0 : Convert.ToInt32(existingObj);

            return alreadyUsed + requestedQty <= item.Capacity;
        }

        public async Task<List<DateTime>> GetDepletedDaysAsync(int rentalItemId, DateTime from, DateTime to)
        {
            var rangeStart = from.Date;
            var rangeEnd = to.Date;

            if (rangeEnd < rangeStart)
            {
                throw new ArgumentException("A végdátum nem lehet korábbi a kezdő dátumnál.");
            }

            var item = await GetRentalItemAsync(rentalItemId)
                       ?? throw new InvalidOperationException($"A termék nem található: {rentalItemId}");

            var depletedDays = new List<DateTime>();

            for (var day = rangeStart; day <= rangeEnd; day = day.AddDays(1))
            {
                var hasAnyFreeSlot = false;
                foreach (var slot in SlotStarts)
                {
                    var slotStart = day.Add(slot);
                    var slotEnd = slotStart.AddHours(1);
                    if (await HasCapacityForRangeAsync(rentalItemId, slotStart, slotEnd, 1))
                    {
                        hasAnyFreeSlot = true;
                        break;
                    }
                }

                if (!hasAnyFreeSlot)
                {
                    depletedDays.Add(day);
                }
            }

            return depletedDays;
        }

        public async Task<List<TimeSpan>> GetUnavailableHourSlotsAsync(int rentalItemId, DateTime day)
        {
            var unavailableSlots = new List<TimeSpan>();
            var item = await GetRentalItemAsync(rentalItemId)
                       ?? throw new InvalidOperationException($"A termék nem található: {rentalItemId}");

            const string sql = @"
SELECT roi.RentalStartDate, SUM(roi.Quantity) AS UsedQty
FROM RentalOrderItems roi
INNER JOIN RentalOrders ro ON ro.Id = roi.RentalOrderId
WHERE roi.RentalItemId = @ItemId
  AND ro.Status IN ('pending', 'booked', 'paid', 'confirmed')
  AND roi.RentalStartDate >= @DayStart
  AND roi.RentalStartDate < @DayEnd
GROUP BY roi.RentalStartDate;";

            await using var connection = CreateConnection();
            await connection.OpenAsync();

            await using var cmd = new MySqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@ItemId", rentalItemId);
            cmd.Parameters.AddWithValue("@DayStart", day.Date);
            cmd.Parameters.AddWithValue("@DayEnd", day.Date.AddDays(1));

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var start = reader.GetDateTime(0);
                var usedQty = reader.GetInt32(1);

                if (usedQty >= item.Capacity)
                {
                    unavailableSlots.Add(start.TimeOfDay);
                }
            }

            return unavailableSlots;
        }

        private static RentalItem MapRentalItem(MySqlDataReader reader)
        {
            var tagsJson = reader["Tags"]?.ToString();
            string[] tags;

            if (string.IsNullOrWhiteSpace(tagsJson))
            {
                tags = [];
            }
            else
            {
                try
                {
                    tags = JsonSerializer.Deserialize<string[]>(tagsJson) ?? [];
                }
                catch
                {
                    tags = tagsJson.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                }
            }

            return new RentalItem
            {
                Id = Convert.ToInt32(reader["Id"]),
                Name = reader["Name"]?.ToString() ?? string.Empty,
                Category = reader["Category"]?.ToString() ?? string.Empty,
                Capacity = Convert.ToInt32(reader["Capacity"]),
                PricePerDay = Convert.ToDecimal(reader["PricePerDay"]),
                Rating = Convert.ToDouble(reader["Rating"]),
                Tags = tags,
                Description = reader["Description"]?.ToString() ?? string.Empty,
                ImageUrl = reader["ImageUrl"]?.ToString() ?? string.Empty,
                IsActive = Convert.ToBoolean(reader["IsActive"]),
                CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
            };
        }

        private static void AddOrderParameters(MySqlCommand cmd, RentalOrder order)
        {
            cmd.Parameters.AddWithValue("@CustomerName", order.CustomerName);
            cmd.Parameters.AddWithValue("@CustomerEmail", order.CustomerEmail);
            cmd.Parameters.AddWithValue("@CustomerPhone", order.CustomerPhone);
            cmd.Parameters.AddWithValue("@CustomerAddress", order.CustomerAddress);
            cmd.Parameters.AddWithValue("@CustomerCity", order.CustomerCity);
            cmd.Parameters.AddWithValue("@CustomerPostalCode", order.CustomerPostalCode);
            cmd.Parameters.AddWithValue("@VatNumber", order.VatNumber);
            cmd.Parameters.AddWithValue("@Quantity", order.Quantity);
            cmd.Parameters.AddWithValue("@PricePerDay", order.PricePerDay);
            cmd.Parameters.AddWithValue("@TotalAmount", order.TotalAmount);
            cmd.Parameters.AddWithValue("@PaymentMethod", order.PaymentMethod);
            cmd.Parameters.AddWithValue("@StripeSessionId", (object?)order.StripeSessionId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@StripePaymentIntentId", (object?)order.StripePaymentIntentId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Status", order.Status);
            cmd.Parameters.AddWithValue("@Notes", (object?)order.Notes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedAt", order.CreatedAt);
            cmd.Parameters.AddWithValue("@UpdatedAt", (object?)order.UpdatedAt ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PaidAt", (object?)order.PaidAt ?? DBNull.Value);
        }
    }
}

using Vizsgaremek2026.Models;

namespace Vizsgaremek2026.Services
{
    public interface IRentalService
    {
        Task<List<RentalItem>> GetAllRentalItemsAsync();
        Task<bool> IsAvailableAsync(int rentalItemId, DateTime startDate, DateTime endDate, int quantity = 1);
        Task<List<DateTime>> GetDepletedDaysAsync(int rentalItemId, DateTime startDate, DateTime endDate);
        Task<List<TimeSpan>> GetUnavailableHourSlotsAsync(int rentalItemId, DateTime day);
        Task<RentalOrder> CreateOrderAsync(CreateRentalOrderDto orderDto);
    }
    public class RentalService : IRentalService
    {
        private readonly IMySqlDatabaseService _databaseService;
        private readonly EmailService _emailService;

        public RentalService(IMySqlDatabaseService databaseService, EmailService emailService)
        {
            _databaseService = databaseService;
            _emailService = emailService;
        }

        public Task<List<RentalItem>> GetAllRentalItemsAsync()
            => _databaseService.GetAllRentalItemsAsync();

        public Task<bool> IsAvailableAsync(int rentalItemId, DateTime startDate, DateTime endDate, int quantity = 1)
            => _databaseService.HasCapacityForRangeAsync(rentalItemId, startDate, endDate, quantity);

        public Task<List<DateTime>> GetDepletedDaysAsync(int rentalItemId, DateTime startDate, DateTime endDate)
            => _databaseService.GetDepletedDaysAsync(rentalItemId, startDate, endDate);

        public Task<List<TimeSpan>> GetUnavailableHourSlotsAsync(int rentalItemId, DateTime day)
            => _databaseService.GetUnavailableHourSlotsAsync(rentalItemId, day);

        public async Task<RentalOrder> CreateOrderAsync(CreateRentalOrderDto orderDto)
        {
            if (orderDto.Items.Count == 0)
            {
                throw new ArgumentException("Nincs kiválasztott termék.");
            }

            decimal totalAmount = 0m;

            foreach (var item in orderDto.Items)
            {
                if (item.Quantity < 1)
                {
                    throw new ArgumentException("A foglalási darabszám legalább 1 fő kell legyen.");
                }

                var start = item.RentalStartDate == default ? orderDto.RentalStartDate : item.RentalStartDate;
                var end = item.RentalEndDate == default ? orderDto.RentalEndDate : item.RentalEndDate;

                if (end <= start)
                {
                    throw new ArgumentException("Érvénytelen idősáv: a végdátum nem lehet korábbi vagy azonos a kezdéssel.");
                }

                if (start.Date != end.Date || end - start != TimeSpan.FromHours(1))
                {
                    throw new ArgumentException("A foglalás kizárólag 1 órás lehet ugyanazon a napon.");
                }

                var rentalItem = await _databaseService.GetRentalItemAsync(item.RentalItemId)
                    ?? throw new ArgumentException($"A(z) {item.RentalItemId} azonosítójú termék nem található.");

                var isAvailable = await _databaseService.HasCapacityForRangeAsync(item.RentalItemId, start, end, item.Quantity);
                if (!isAvailable)
                {
                    throw new InvalidOperationException($"{rentalItem.Name} nem elérhető a választott időpontban.");
                }

                totalAmount += rentalItem.PricePerDay * item.Quantity;

                item.Name = rentalItem.Name;
                item.Price = rentalItem.PricePerDay * item.Quantity;
                item.RentalStartDate = start;
                item.RentalEndDate = end;
            }

            var order = new RentalOrder
            {
                CustomerName = orderDto.CustomerName,
                CustomerEmail = orderDto.CustomerEmail,
                CustomerPhone = orderDto.CustomerPhone,
                CustomerAddress = orderDto.CustomerAddress ?? string.Empty,
                CustomerCity = orderDto.CustomerCity,
                CustomerPostalCode = orderDto.CustomerPostalCode,
                VatNumber = orderDto.VatNumber,
                RentalStartDate = orderDto.RentalStartDate,
                RentalEndDate = orderDto.RentalEndDate,
                Quantity = orderDto.Items.Sum(i => i.Quantity),
                PricePerDay = orderDto.Items.Count == 0 ? 0 : totalAmount / orderDto.Items.Count,
                TotalAmount = totalAmount,
                PaymentMethod = string.IsNullOrWhiteSpace(orderDto.PaymentMethod) ? "manual" : orderDto.PaymentMethod,
                PaymentStatus = "pending",
                Status = "pending",
                Notes = orderDto.Notes,
                CreatedAt = DateTime.UtcNow,
                Items = orderDto.Items
            };

            var createdOrder = await _databaseService.CreateRentalOrderAsync(order);
            await _emailService.SendOrderPlacedEmails(createdOrder);
            return createdOrder;
        }
    }

    public class CreateRentalOrderDto
    {
        public List<RentalOrderItem> Items { get; set; } = new();
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string? CustomerAddress { get; set; }
        public string CustomerCity { get; set; } = string.Empty;
        public string CustomerPostalCode { get; set; } = string.Empty;
        public string VatNumber { get; set; } = string.Empty;
        public DateTime RentalStartDate { get; set; }
        public DateTime RentalEndDate { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
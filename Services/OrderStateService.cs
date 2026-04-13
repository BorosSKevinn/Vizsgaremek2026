using Vizsgaremek2026.Models;

namespace Vizsgaremek2026.Services
{
    public interface IOrderStateService
    {
        Task<BillingDetailsModel?> GetBillingDetailsAsync();
        Task SetBillingDetailsAsync(BillingDetailsModel? billingDetails);
        Task<List<OrderItem>> GetOrderItemsAsync();
        Task SetOrderItemsAsync(List<OrderItem> items);
        Task AddRentalItemAsync(int rentalItemId, string name, decimal pricePerDay, DateTime startDate, DateTime endDate, int quantity = 1);
        Task ClearOrderAsync();
        Task<decimal> GetTotalAsync();
    }

    public class OrderStateService : IOrderStateService
    {
        private readonly SemaphoreSlim _gate = new(1, 1);
        private BillingDetailsModel? _billingDetails;
        private List<OrderItem> _items = new();

        public async Task<BillingDetailsModel?> GetBillingDetailsAsync()
        {
            await _gate.WaitAsync();
            try
            {
                return _billingDetails is null
                    ? null
                    : new BillingDetailsModel
                    {
                        FullName = _billingDetails.FullName,
                        Email = _billingDetails.Email,
                        Phone = _billingDetails.Phone,
                        PostalCode = _billingDetails.PostalCode,
                        City = _billingDetails.City,
                        Address = _billingDetails.Address,
                        AddressLine2 = _billingDetails.AddressLine2,
                        VatNumber = _billingDetails.VatNumber,
                        PaymentMethod = _billingDetails.PaymentMethod
                    };
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task SetBillingDetailsAsync(BillingDetailsModel? billingDetails)
        {
            await _gate.WaitAsync();
            try
            {
                _billingDetails = billingDetails;
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task<List<OrderItem>> GetOrderItemsAsync()
        {
            await _gate.WaitAsync();
            try
            {
                return _items.Select(i => new OrderItem
                {
                    RentalItemId = i.RentalItemId,
                    Name = i.Name,
                    Quantity = i.Quantity,
                    Price = i.Price,
                    RentalStartDate = i.RentalStartDate,
                    RentalEndDate = i.RentalEndDate
                }).ToList();
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task SetOrderItemsAsync(List<OrderItem> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            await _gate.WaitAsync();
            try
            {
                _items = items;
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task AddRentalItemAsync(int rentalItemId, string name, decimal pricePerDay, DateTime startDate, DateTime endDate, int quantity = 1)
        {
            if (quantity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(quantity), "A mennyiség legalább 1 kell legyen.");
            }

            var from = startDate;
            var to = endDate;
            if (to <= from)
            {
                throw new ArgumentException("A végdátum nem lehet korábbi a kezdő dátumnál.");
            }

            var totalPrice = pricePerDay * quantity;

            await _gate.WaitAsync();
            try
            {
                _items.Add(new OrderItem
                {
                    RentalItemId = rentalItemId,
                    Name = name,
                    Quantity = quantity,
                    Price = totalPrice,
                    RentalStartDate = from,
                    RentalEndDate = to
                });
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task ClearOrderAsync()
        {
            await _gate.WaitAsync();
            try
            {
                _items.Clear();
                _billingDetails = null;
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task<decimal> GetTotalAsync()
        {
            await _gate.WaitAsync();
            try
            {
                return _items.Sum(i => i.Price);
            }
            finally
            {
                _gate.Release();
            }
        }
    }
}

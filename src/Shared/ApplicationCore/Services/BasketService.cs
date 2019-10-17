using ApplicationCore.Interfaces;
using System.Threading.Tasks;
using System.Collections.Generic;
using ApplicationCore.Specifications;
using ApplicationCore.Entities;
using System.Linq;
using Ardalis.GuardClauses;
using ApplicationCore.DTOs;
using ApplicationCore.Entities.BasketAggregate;
using System;

namespace ApplicationCore.Services
{
    public class BasketService : IBasketService
    {
        private readonly IAsyncRepository<Basket> _basketRepository;
        private readonly IAppLogger<BasketService> _logger;

        public BasketService(IAsyncRepository<Basket> basketRepository,
            IAppLogger<BasketService> logger)
        {
            _basketRepository = basketRepository;
            _logger = logger;
        }

        public async Task<int> GetBasketItemCountAsync(string userName)
        {
            if (!string.IsNullOrEmpty(userName))
            {
                Guard.Against.NullOrEmpty(userName, nameof(userName));
                var basketSpec = new BasketWithItemsSpecification(userName);
                var basket = (await _basketRepository.ListAsync(basketSpec)).LastOrDefault();
                if (basket == null)
                {
                    _logger.LogInformation($"No basket found for {userName}");
                    return 0;
                }
                int count = basket.Items.Sum(i => i.Quantity);
                _logger.LogInformation($"Basket for {userName} has {count} items.");
                return count;
            }
            return 0;
        }

        public async Task AddItemToBasket(int basketId, int catalogItemId, decimal price, int quantity, int? option1 = null, int? option2 = null, int? option3 = null, string customizeName = null, string customizeSide = null, bool addToExistingItem = false)
        {
            var basket = await _basketRepository.GetByIdAsync(basketId);            

            basket.AddItem(catalogItemId, price, quantity, option1, option2, option3, customizeName, customizeSide, addToExistingItem);

            await _basketRepository.UpdateAsync(basket);
        }

        public async Task DeleteBasketAsync(int basketId)
        {
            var basket = await _basketRepository.GetByIdAsync(basketId);

            await _basketRepository.DeleteAsync(basket);
        }          

        public async Task SetQuantities(int basketId, Dictionary<string, int> quantities)
        {
            Guard.Against.Null(quantities, nameof(quantities));
            var basket = await _basketRepository.GetByIdAsync(basketId);
            Guard.Against.NullBasket(basketId, basket);
            foreach (var item in basket.Items)
            {
                if (quantities.TryGetValue(item.Id.ToString(), out var quantity))
                {
                    _logger.LogInformation($"Updating quantity of item ID:{item.Id} to {quantity}.");
                    item.Quantity = quantity;
                    item.UpdatedDate = DateTime.Now;
                }
            }
            await _basketRepository.UpdateAsync(basket);
        }
        
        public async Task DeleteItem(int basketId, int itemIndex)
        {
            var basket = await _basketRepository.GetByIdAsync(basketId);
            if (basket != null)
                basket.RemoveItem(itemIndex);
            await _basketRepository.UpdateAsync(basket);
        }

        public async Task TransferBasketAsync(string anonymousId, string userName, bool isGuest)
        {
            Guard.Against.NullOrEmpty(anonymousId, nameof(anonymousId));
            Guard.Against.NullOrEmpty(userName, nameof(userName));

            var basketSpec = new BasketWithItemsSpecification(anonymousId);
            var basket = (await _basketRepository.ListAsync(basketSpec)).LastOrDefault();
            if (basket == null) return;

            if (!isGuest && basket.Items?.Count > 0)
            {
                //Delete previous baskets
                var basketSpecUser = new BasketWithItemsSpecification(userName);
                var listBasket = await _basketRepository.ListAsync(basketSpecUser);
                foreach (var item in listBasket)
                {
                    await _basketRepository.DeleteAsync(item);
                }
            }
            basket.BuyerId = userName;
            basket.UpdatedDate = DateTime.Now;
            await _basketRepository.UpdateAsync(basket);
        }
    }
}

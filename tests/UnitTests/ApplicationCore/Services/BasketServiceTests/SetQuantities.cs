﻿using ApplicationCore.Exceptions;
using ApplicationCore.Interfaces;
using ApplicationCore.Services;
using ApplicationCore.Entities.BasketAggregate;
using Moq;
using System;
using Xunit;
using System.Threading.Tasks;
using ApplicationCore.Entities;

namespace UnitTests.ApplicationCore.Services.BasketServiceTests
{
    public class SetQuantities
    {
        private readonly int _invalidId = -1;
        private readonly Mock<IAsyncRepository<Basket>> _mockBasketRepo;
        private readonly Mock<IAsyncRepository<CatalogItem>> _mockCatalogRepo;

        public SetQuantities()
        {
            _mockBasketRepo = new Mock<IAsyncRepository<Basket>>();
            _mockCatalogRepo = new Mock<IAsyncRepository<CatalogItem>>();
        }

        [Fact]
        public async Task ThrowsGivenInvalidBasketId()
        {
            var basketService = new BasketService(_mockBasketRepo.Object, null);

            await Assert.ThrowsAsync<BasketNotFoundException>(async () =>
                await basketService.SetQuantities(_invalidId, new System.Collections.Generic.Dictionary<string, int>()));
        }

        [Fact]
        public async Task ThrowsGivenNullQuantities()
        {
            var basketService = new BasketService(null, null);

            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await basketService.SetQuantities(123, null));
        }

    }
}

﻿using ApplicationCore.DTOs;
using ApplicationCore.Entities;
using ApplicationCore.Entities.OrderAggregate;
using ApplicationCore.Interfaces;
using ApplicationCore.Specifications;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationCore.Services
{
    public class InvoiceTestService : IInvoiceService
    {

        public InvoiceTestService()
        {            
        }

        public Task<(string AccessToken, string RefreshToken)> GenerateNewAccessTokenAsync(SageApplicationType applicationType, string code)
        {
            return Task.FromResult((Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
        }

        public Task<byte[]> GetPDFInvoiceAsync(SageApplicationType applicationType, long invoiceId)
        {
            byte[] bytes = null;
            return Task.FromResult(bytes);
        }

        public Task<byte[]> GetPDFReceiptAsync(SageApplicationType applicationType, long invoiceId, long paymentId)
        {
            byte[] bytes = null;
            return Task.FromResult(bytes);
        }

        public Task<SageResponseDTO> RegisterInvoiceAsync(SageApplicationType applicationType, Order order)
        {
            return Task.FromResult(new SageResponseDTO
            {
                InvoiceId = 99999,
                InvoiceNumber = "FT/99999",
                Message = "Success"                
            });
        }

        public Task<SageResponseDTO> RegisterPaymentAsync(SageApplicationType applicationType, long salesInvoiceId, decimal total, PaymentType paymentType)
        {
            return Task.FromResult(new SageResponseDTO
            {
                InvoiceId = 99999,
                InvoiceNumber = "FT/99999",
                Message = "Success",
                PaymentId = 99999               
            });

        }
    }
}

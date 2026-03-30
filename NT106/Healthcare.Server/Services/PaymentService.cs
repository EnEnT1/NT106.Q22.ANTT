using System;
using Microsoft.AspNetCore.Http;

namespace Healthcare.Server.Services
{
    public class PaymentService
    {
        private readonly string _vnpayHashSecret;

        public PaymentService()
        {
            _vnpayHashSecret = Environment.GetEnvironmentVariable("VNPAY_HASH_SECRET")
                               ?? throw new Exception("Thiếu VNPAY_HASH_SECRET trong file .env!");
        }

        public bool ValidateVnPaySignature(IQueryCollection queryParams)
        {
            return true;
        }
    }
}
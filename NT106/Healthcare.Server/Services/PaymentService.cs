using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Healthcare.Server.Services
{
    public class PaymentService
    {
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
        private readonly string _vnpayTmnCode;
        private readonly string _vnpayHashSecret;
        private readonly string _vnpayUrl;
        private readonly string _vnpayReturnUrl;

        public PaymentService(Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _configuration = configuration;
            _vnpayTmnCode = _configuration["VnPay:TmnCode"] ?? "66G4ROY1";
            _vnpayHashSecret = _configuration["VnPay:HashSecret"] ?? "71ZYZXHKPH9171NGNV7550Y22ADTXXOM";
            _vnpayUrl = _configuration["VnPay:Url"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            _vnpayReturnUrl = _configuration["VnPay:ReturnUrl"] ?? "http://localhost:5246/api/payment/vnpay-return";
        }

        public string CreatePaymentUrl(string appointmentId, double amount, string ipAddress)
        {
            if (string.IsNullOrEmpty(ipAddress) || ipAddress.Contains("::1") || ipAddress == "127.0.0.1")
            {
                ipAddress = "113.160.92.202";
            }
            long amountInVnPayFormat = (long)(amount * 100);
            var vnp_Params = new SortedList<string, string>
            {
                { "vnp_Version", "2.1.0" },
                { "vnp_Command", "pay" },
                { "vnp_TmnCode", _vnpayTmnCode },
                { "vnp_Amount", amountInVnPayFormat.ToString() },
                { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") },
                { "vnp_CurrCode", "VND" },
                { "vnp_IpAddr", ipAddress },
                { "vnp_Locale", "vn" },
                { "vnp_OrderInfo", $"Thanh_toan_vien_phi_cho_lich_{appointmentId}" },
                { "vnp_OrderType", "other" },
                { "vnp_ReturnUrl", _vnpayReturnUrl },
                { "vnp_TxnRef", appointmentId }
            };
            var signData = string.Join("&", vnp_Params.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var vnp_SecureHash = HmacSHA512(_vnpayHashSecret, signData);

            return $"{_vnpayUrl}?{signData}&vnp_SecureHash={vnp_SecureHash}";
        }

        public bool ValidateVnPaySignature(IQueryCollection queryParams)
        {
            string vnp_SecureHash = queryParams["vnp_SecureHash"].ToString();
            if (string.IsNullOrEmpty(vnp_SecureHash)) return false;
            var parametersDictionary = queryParams.ToDictionary(k => k.Key, v => v.Value.ToString());
            var sortedParams = parametersDictionary
                .Where(kvp => kvp.Key.StartsWith("vnp_")
                           && kvp.Key != "vnp_SecureHash"
                           && kvp.Key != "vnp_SecureHashType")
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}");

            string signData = string.Join("&", sortedParams);
            string checkHash = HmacSHA512(_vnpayHashSecret, signData);
            return checkHash.Equals(vnp_SecureHash, StringComparison.InvariantCultureIgnoreCase);
        }

        private string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);

            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }
            return hash.ToString();
        }
    }
}
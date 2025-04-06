using GoParkAPI.DTO;
using GoParkAPI.Models;
using GoParkAPI.Providers;
using MailKit.Search;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Numerics;
using System.Text;

namespace GoParkAPI.Services
{
    public class LinePayService
    {
        private readonly string channelId = "1657306405";
        private readonly string channelSecretKey = "720c8af4d271ce2fef3535b8821d9e8e";
        private readonly string linePayBaseApiUrl = "https://sandbox-api-pay.line.me";

        private readonly HttpClient _client;
        private readonly JsonProvider _jsonProvider;
        private readonly EasyParkContext _context;

        public LinePayService(HttpClient client, JsonProvider jsonProvider, EasyParkContext context)
        {
            _client = client;
            _jsonProvider = jsonProvider;
            _context = context;
        }

        private void AddLinePayHeaders(HttpRequestMessage request, string nonce, string signature)
        {
            request.Headers.Add("X-LINE-ChannelId", channelId);
            request.Headers.Add("X-LINE-Authorization-Nonce", nonce);
            request.Headers.Add("X-LINE-Authorization", signature);
        }


        public async Task<PaymentResponseDto> SendPaymentRequest(PaymentRequestDto dto)
        {
            var json = _jsonProvider.Serialize(dto);
            var nonce = Guid.NewGuid().ToString();
            var requestUrl = "/v3/payments/request";
            var signature = SignatureProvider.HMACSHA256(
                channelSecretKey, channelSecretKey + requestUrl + json + nonce);

            var request = new HttpRequestMessage(HttpMethod.Post, linePayBaseApiUrl + requestUrl)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            AddLinePayHeaders(request, nonce, signature);

            var response = await _client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"LinePay API Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var paymentResponse = _jsonProvider.Deserialize<PaymentResponseDto>(responseContent);

            // 返回解析的回應資料
            return paymentResponse;
        }





        //-------------------------------------------------------------------------------------------------------------------


        // 取得 transactionId 後進行確認交易
        public async Task<PaymentConfirmResponseDto> ConfirmPayment(string transactionId, string orderId, PaymentConfirmDto dto) //加上 OrderId 去找資料
        {
            var json = _jsonProvider.Serialize(dto);

            var nonce = Guid.NewGuid().ToString();
            var requestUrl = string.Format("/v3/payments/{0}/confirm", transactionId);
            var signature = SignatureProvider.HMACSHA256(channelSecretKey, channelSecretKey + requestUrl + json + nonce);

            var request = new HttpRequestMessage(HttpMethod.Post, String.Format(linePayBaseApiUrl + requestUrl, transactionId))
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            AddLinePayHeaders(request, nonce, signature);

            var response = await _client.SendAsync(request);
            var responseDto = _jsonProvider.Deserialize<PaymentConfirmResponseDto>(await response.Content.ReadAsStringAsync());
            return responseDto;
        }




        public async Task TransactionCancel(string transactionId)
        {
            Console.WriteLine($"訂單 {transactionId} 已取消");
            await Task.CompletedTask;
        }

    }
}

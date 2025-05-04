using Mango.Web.Models;
using Mango.Web.Service.IService;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using static Mango.Web.Utility.SD;

namespace Mango.Web.Service
{
    public class BaseService : IBaseService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITokenProvider _tokenProvider;

        public BaseService(IHttpClientFactory httpClientFactory, ITokenProvider tokenProvider)
        {
            _httpClientFactory = httpClientFactory;
            _tokenProvider = tokenProvider;
        }

        public async Task<ResponseDto?> SendAsync(RequestDto requestDto, bool withBearer = true)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();

                var message = new HttpRequestMessage
                {
                    RequestUri = new Uri(requestDto.Url),
                    Method = requestDto.ApiType switch
                    {
                        ApiType.POST => HttpMethod.Post,
                        ApiType.PUT => HttpMethod.Put,
                        ApiType.DELETE => HttpMethod.Delete,
                        _ => HttpMethod.Get
                    }
                };

                message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                if (withBearer)
                {
                    var token = _tokenProvider.GetToken();
                    if (!string.IsNullOrEmpty(token))
                    {
                        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    }
                }

                // Handle request body
                if (requestDto.Data != null)
                {
                    if (requestDto.ContentType == ContentType.MultipartFormData)
                    {
                        var content = new MultipartFormDataContent();

                        foreach (var prop in requestDto.Data.GetType().GetProperties())
                        {
                            var value = prop.GetValue(requestDto.Data);
                            if (value is FormFile file)
                            {
                                content.Add(new StreamContent(file.OpenReadStream()), prop.Name, file.FileName);
                            }
                        }

                        message.Content = content;
                    }
                    else
                    {
                        string jsonData = JsonConvert.SerializeObject(requestDto.Data);
                        message.Content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                    }
                }

                var response = await client.SendAsync(message);

                var apiContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new ResponseDto
                    {
                        IsSuccess = false,
                        Message = response.StatusCode switch
                        {
                            HttpStatusCode.NotFound => "Not Found",
                            HttpStatusCode.Forbidden => "Access Denied",
                            HttpStatusCode.Unauthorized => "Unauthorized",
                            HttpStatusCode.InternalServerError => "Internal Server Error",
                            _ => $"Error: {response.StatusCode}"
                        }
                    };
                }

                return JsonConvert.DeserializeObject<ResponseDto>(apiContent);
            }
            catch (Exception ex)
            {
                return new ResponseDto
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }
    }
}

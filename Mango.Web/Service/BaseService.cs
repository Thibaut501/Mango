
using Mango.Web.Models;
using Mango.Web.Service.IService;
using Newtonsoft.Json;
using static Mango.Web.Utility.SD;
using System.Text;
using System.Net.Http.Headers;

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
            HttpClient client = _httpClientFactory.CreateClient("MangoAPI");
            HttpRequestMessage message = new()
            {
                Headers = { { "Accept", "application/json" } },
                RequestUri = new Uri(requestDto.Url)
            };

            // Add token if required
            if (withBearer)
            {
                var token = _tokenProvider.GetToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            }

            // Add content if any
            if (requestDto.Data != null)
            {
                message.Content = new StringContent(
                    JsonConvert.SerializeObject(requestDto.Data),
                    Encoding.UTF8,
                    "application/json");
            }

            // Determine the HTTP method
            message.Method = requestDto.ApiType switch
            {
                ApiType.POST => HttpMethod.Post,
                ApiType.DELETE => HttpMethod.Delete,
                ApiType.PUT => HttpMethod.Put,
                _ => HttpMethod.Get,
            };

            // Send the request
            HttpResponseMessage apiResponse = await client.SendAsync(message);

            // Handle response
            switch (apiResponse.StatusCode)
            {
                case System.Net.HttpStatusCode.NotFound:
                    return new ResponseDto { IsSuccess = false, Message = "Not Found" };
                case System.Net.HttpStatusCode.Forbidden:
                    var forbiddenContent = await apiResponse.Content.ReadAsStringAsync();
                    // Optionally log forbiddenContent
                    return new ResponseDto { IsSuccess = false, Message = "Access Denied" };
                case System.Net.HttpStatusCode.Unauthorized:
                    var unauthorizedContent = await apiResponse.Content.ReadAsStringAsync();
                    // Optionally log unauthorizedContent
                    return new ResponseDto { IsSuccess = false, Message = "Unauthorized" };
                case System.Net.HttpStatusCode.InternalServerError:
                    var errorContent = await apiResponse.Content.ReadAsStringAsync();
                    // Optionally log errorContent
                    return new ResponseDto { IsSuccess = false, Message = "Internal Server Error" };
                default:
                    var apiContent = await apiResponse.Content.ReadAsStringAsync();
                    var apiResponseDto = JsonConvert.DeserializeObject<ResponseDto>(apiContent);
                    return apiResponseDto;
            }
        }
        catch (Exception ex)
        {
            // Optionally log the exception
            return new ResponseDto
            {
                Message = $"An error occurred: {ex.Message}",
                IsSuccess = false
            };
        }
    }
}

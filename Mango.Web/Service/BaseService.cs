using Mango.Web.Models;
using Mango.Web.Service.IService;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using static Mango.Web.Utility.SD;
using Microsoft.AspNetCore.Http; // For IFormFile
using System.Globalization; // For invariant conversions

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
                // Use named client to benefit from dev cert bypass
                var client = _httpClientFactory.CreateClient("Default");

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

                // Build request content depending on ContentType
                if (requestDto.Data != null)
                {
                    if (requestDto.ContentType == ContentType.MultipartFormData)
                    {
                        var multipart = new MultipartFormDataContent();

                        var props = requestDto.Data.GetType().GetProperties();
                        foreach (var prop in props)
                        {
                            var value = prop.GetValue(requestDto.Data);
                            if (value == null) continue;

                                   if (value is IFormFile file)
                            {
                                // Add file content
                                var fileContent = new StreamContent(file.OpenReadStream());
                                fileContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
                                multipart.Add(fileContent, prop.Name, file.FileName);
                            }
                            else if (value is IEnumerable<IFormFile> files)
                            {
                                foreach (var f in files)
                                {
                                    if (f == null) continue;
                                    var fileContent = new StreamContent(f.OpenReadStream());
                                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(f.ContentType) ? "application/octet-stream" : f.ContentType);
                                    multipart.Add(fileContent, prop.Name, f.FileName);
                                }
                            }
                            else
                            {
                                // Skip empty-string optional fields (prevents ImageUrl="" from tripping API validation)
                                if (value is string s && string.IsNullOrWhiteSpace(s))
                                {
                                    continue;
                                }
                                // Convert other simple properties to strings
                                string stringValue = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                                multipart.Add(new StringContent(stringValue), prop.Name);
                            }
                        }

                        message.Content = multipart;
                    }
                    else // default JSON
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
                            HttpStatusCode.BadRequest => apiContent,
                            HttpStatusCode.InternalServerError => "Internal Server Error",
                            _ => $"Error: {response.StatusCode}"
                        }
                    };
                }

                return JsonConvert.DeserializeObject<ResponseDto>(apiContent);
            }
            catch (HttpRequestException ex)
            {
                return new ResponseDto
                {
                    IsSuccess = false,
                    Message = $"Network error: {ex.Message}"
                };
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

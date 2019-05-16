using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace api.Services
{
    public class AzureService
    {
        private readonly ILogger<AzureService> logger;

        public AzureService(ILogger<AzureService> logger)
        {
            this.logger = logger;
        }
        
        public async Task<TextAnalyticsResponseDocument> AnalyseTextAsync(string text)
        {
            var httpClient = new HttpClient();
            
            httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "5d2da20682f64ee9a4b7a3b4d14b0107");

            var request = new
            {
                documents = new[] 
                {
                    new {
                        language = "en",
                        id = "1",
                        text
                    }
                }
            };
            
            var response = await httpClient.PostAsync(
                "https://westeurope.api.cognitive.microsoft.com/text/analytics/v2.0/entities",
                new JsonContent(request)
            );

            if (!response.IsSuccessStatusCode)
            {
                var errorResponseString = await response.Content.ReadAsStringAsync();
                var errorResponse = JsonConvert.DeserializeObject<TextAnalyticsErrorResponse>(errorResponseString);
                throw new Exception(errorResponse.InnerError.Message);
            }

            var responseString = await response.Content.ReadAsStringAsync();

            var serialisedResponse = JsonConvert.DeserializeObject<TextAnalyticsResponse>(responseString);

            if (serialisedResponse.Errors.Length > 0)
            {
                throw new Exception(serialisedResponse.Errors.First().Message);
            }

            return serialisedResponse.Documents.First();
        }
        
        public async Task<BingEntitySearchResponseEntities> GetBingEntityAsync(string entityName)
        {
            var httpClient = new HttpClient();
            
            httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "f256a10bf91e423f9cabaae0217c0d32");
            
            var query = $"?q={entityName}&mkt=en-US";
            var response = await httpClient.GetAsync("https://api.cognitive.microsoft.com/bing/v7.0/entities" + query);

            if (!response.IsSuccessStatusCode)
            {
                var errorResponseString = await response.Content.ReadAsStringAsync();
                logger.LogError(errorResponseString);
                return null;
            }

            var responseString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<BingEntitySearchResponse>(responseString).Entities;
        }
    }

    public class TextAnalyticsErrorResponse
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public TextAnalyticsInnerError InnerError { get; set; }
    }

    public class TextAnalyticsInnerError
    {
        public string Code { get; set; }
        public string Message { get; set; }
    }

    public class BingEntitySearchResponse
    {
        public BingEntitySearchResponseEntities Entities { get; set; }
    }

    public class BingEntitySearchResponseEntities
    {
        public BingEntitySearchResponseEntityValue[] Value { get; set; }
    }

    public class BingEntitySearchResponseEntityValue
    {
        public string Id { get; set; }
        public string BingId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public BingEntitySearchResponseEntityPresentation EntityPresentationInfo { get; set; }  
        public BingEntitySearchResponseEntityValueImage Image { get; set; }        
    }

    public class BingEntitySearchResponseEntityPresentation
    {
        public string EntityScenario { get; set; }
        public string EntityTypeDisplayHint { get; set; }
        public string[] EntityTypeHints { get; set; }
    }

    public class BingEntitySearchResponseEntityValueImage
    {
        public string Name { get; set; }
        public string HostPageUrl { get; set; }
    }

    public class TextAnalyticsResponse
    {
        public TextAnalyticsResponseDocument[] Documents { get; set; }
        public TextAnalyticsResponseError[] Errors { get; set; }
    }

    public class TextAnalyticsResponseError
    {
        public string Id { get; set; }
        public string Message { get; set; }
    }

    public class TextAnalyticsResponseDocument
    {
        public string Id { get; set; }
        public TextAnalyticsResponseEntity[] Entities { get; set; }
    }

    public class TextAnalyticsResponseEntity
    {
        public string Name { get; set; }
        public TextAnalyticsResponseEntityMatch[] Matches { get; set; }
    }

    public class TextAnalyticsResponseEntityMatch
    {
        public string Text { get; set; }
        public int Offset { get; set; }
        public int Length { get; set; }
    }
}
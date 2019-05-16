using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace api.Controllers
{
    [Route("api/entities")]
    [ApiController]
    public class EntityInfoController : ControllerBase
    {
        private readonly AzureService azureService;
        private readonly ILogger<EntityInfoController> logger;

        private static string[] locationEntityTypes = {
            "City",
            "Country",
            "Place",
            "Person",
            "Organization"
        };

        private static string[] entityTypeWhitelist = new [] {
            "Person",
            "Organization"
        }.Union(locationEntityTypes).ToArray();

        public EntityInfoController(AzureService azureService, ILogger<EntityInfoController> logger)
        {
            this.azureService = azureService;
            this.logger = logger;
        }
        
        // POST api/entities
        [HttpPost]
        public async Task<ActionResult<EntityInfoResponse>> Post([FromBody]EntityInfoRequest request)
        {
            try
            {
                // Run Text Analysis once to get all the entities
                var textAnalysisResult = await azureService.AnalyseTextAsync(request.Text);

                // Run all the Bing search tasks concurrently
                var entitySearchTasks = textAnalysisResult
                    .Entities
                    .Select(e => azureService.GetBingEntityAsync(e.Name))
                    .ToArray();

                Task.WaitAll(entitySearchTasks);

                // Process the Bing responses
                var entities = ProcessResponses(entitySearchTasks, textAnalysisResult);

                return new EntityInfoResponse
                {
                    Text = request.Text,
                    Entities = entities
                        .OrderBy(e => e.Location.Matches.First().Offset)
                        .ToArray()
                };

            }
            catch (Exception ex)
            {
                logger.LogError("Internal Server Error", ex);
                
                return StatusCode(StatusCodes.Status500InternalServerError, new EntityInfoErrorResponse
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Message = ex.Message
                });
            }
        }

        private List<EntityInfo> ProcessResponses(Task<BingEntitySearchResponseEntities>[] tasks, TextAnalyticsResponseDocument textAnalysisResult)
        {
            var entities = new List<EntityInfo>();

            foreach (var task in tasks)
            {
                var bingEntity = task.Result;

                if (bingEntity == null || bingEntity.Value.Length == 0)
                {
                    continue;
                }

                if (!bingEntity.Value[0].EntityPresentationInfo.EntityTypeHints.Any(e => entityTypeWhitelist.Contains(e)))
                {
                    // Filter out the entity types we don't want
                    continue;
                }

                UpdateImageIfLocation(bingEntity);

                entities.Add(new EntityInfo
                {
                    Location = textAnalysisResult.Entities.First(e => e.Name.Equals(bingEntity.Value[0].Name)),
                    Data = bingEntity.Value[0]
                });
            }

            return entities;
        }

        private void UpdateImageIfLocation(BingEntitySearchResponseEntities bingEntity)
        {
            if (!bingEntity.Value[0].EntityPresentationInfo.EntityTypeHints.Any(e => locationEntityTypes.Contains(e)))
            {
                return;
            }

            var image = bingEntity.Value[0].Image;
            
            image.HostPageUrl =
                $"https://dev.virtualearth.net/REST/v1/Imagery/Map/CanvasLight/{UrlEncoder.Default.Encode(bingEntity.Value[0].Name)}?mapSize=500,400&key=AmxOih6m90zBSnPD_jH-HEXIt4M5SxkZZyB4p1FOxFzGLrLTD61bsI94g01aJPCn";
            
        }
    }
   
    
    public class EntityInfoRequest
    {
        public string Text { get; set; }
    }

    public class EntityInfoResponse
    {
        public string Text { get; set; }
        public EntityInfo[] Entities { get; set; }
    }

    public class EntityInfo
    {
        public TextAnalyticsResponseEntity Location { get; set; }
        public BingEntitySearchResponseEntityValue Data { get; set; }
    }

    public class EntityInfoErrorResponse
    {
        public string Message { get; set; }
        public int Status { get; set; }
    }
}
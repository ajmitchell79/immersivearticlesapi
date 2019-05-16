using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/entities")]
    [ApiController]
    public class EntityInfoController : ControllerBase
    {
        private readonly AzureService azureService;

        private string[] entityTypeWhiteList = new []
        {
            "City",
            "Country",
            "Person",
            "Organization"
        };

        public EntityInfoController(AzureService azureService)
        {
            this.azureService = azureService;
        }
        
        // POST api/TextAnalytics
        [HttpPost]
        public async Task<ActionResult<EntityInfoResponse>> Post([FromBody]EntityInfoRequest request)
        {
            try
            {
                var result = await azureService.AnalyseTextAsync(request.Text);

                var entities = new List<EntityInfo>();

                foreach (var entity in result.Entities)
                {
                    var bingEntity = await azureService.GetBingEntityAsync(entity.Name);

                    if (bingEntity?.Value.Length == 0)
                    {
                        continue;
                    }
                    
                    if (bingEntity?.Value[0].EntityPresentationInfo.EntityTypeHints)
                    {
                        entities.Add(new EntityInfo
                        {
                            Location = entity,
                            Data = bingEntity.Value[0]
                        });
                    }
                }

                return new EntityInfoResponse
                {
                    Text = request.Text,
                    Entities = entities.ToArray()
                };

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new EntityInfoErrorResponse
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Message = ex.Message
                });
            }
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
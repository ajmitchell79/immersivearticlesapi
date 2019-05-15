using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using api.Services;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/entities")]
    [ApiController]
    public class EntityInfoController : ControllerBase
    {
        // POST api/TextAnalytics
        [HttpPost]
        public async Task<EntityInfoResponse> Post([FromBody]EntityInfoRequest request)
        {
            var azureService = new AzureService();

            var result = await azureService.AnalyseTextAsync(request.Text);

            var entities = new List<BingEntitySearchResponseEntityValue>();
            foreach (var entity in result.Entities)
            {
                var bingEntity = await azureService.GetBingEntityAsync(entity.Name);
                entities.Add(bingEntity.Value[0]);
            }

            return new EntityInfoResponse
            {
                Text = request.Text,
                Entities = entities.ToArray()
            };
            
        }
    }
    
    public class EntityInfoRequest
    {
        public string Text { get; set; }
    }

    public class EntityInfoResponse
    {
        public string Text { get; set; }
        public BingEntitySearchResponseEntityValue[] Entities { get; set; }
    }
}
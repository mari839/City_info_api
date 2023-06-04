using AutoMapper;
using CityInfo.API.Entities;
using CityInfo.API.Models;
using CityInfo.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;

namespace CityInfo.API.Controllers
{
    [Route("api/v{version:apiVersion}/cities/{cityId}/pointsofinterest")]
    [Authorize (Policy= "MustBeFromAntwerp")]
    [ApiVersion("2.0")]
    [ApiController]
    public class PointsOfInterestController : ControllerBase
    {
        private readonly ILogger<PointsOfInterestController> _logger; 
        private readonly IMailService _mailService;
        private readonly ICityInfoRepository _cityInfoRepository;
        private readonly IMapper _mapper;

        public PointsOfInterestController(ILogger<PointsOfInterestController> logger, IMailService mailService, ICityInfoRepository cityInfoRepository, IMapper mapper) 
        {
            _logger = logger?? throw new ArgumentNullException(nameof(logger));
            _mailService = mailService?? throw new ArgumentNullException(nameof(mailService));
            _cityInfoRepository = cityInfoRepository ?? throw new ArgumentNullException(nameof(cityInfoRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }
        [HttpGet] // signify that this action can be routed to when the http method is get
        public async Task<ActionResult<IEnumerable<PointOfInterestDto>>> GetPointsOfInterest(int cityId)
        {
            var cityName = User.Claims.FirstOrDefault(c => c.Type == "city")?.Value;

            if (!await _cityInfoRepository.CityNameMatchesCityId(cityName, cityId))
            {
                return Forbid(); //403 status code, user is authentificated but doesnt have an access
            }

            if(!await _cityInfoRepository.cityExistsAsync(cityId))
            {
                _logger.LogInformation($"City with id {cityId} wasn't found when accessing points of interest.");
                return NotFound();
            }
            var pointOfInterestForCity = await _cityInfoRepository.GetPointsOfInterestForCityAsync(cityId);

            return Ok(_mapper.Map<IEnumerable<PointOfInterestDto>>(pointOfInterestForCity));
        }
        [HttpGet("{pointOfInterestId}", Name = "GetPointOfInterest")] //we can call it by name in createdAtRoute method
        public async Task<ActionResult<PointOfInterestDto>> GetPointOfInterest(int cityId, int pointOfInterestId)
        {
            if (!await _cityInfoRepository.cityExistsAsync(cityId))
            {
                _logger.LogInformation($"City with id {cityId} wasn't found when accessing points of interest.");
                return NotFound();
            }
            var pointOfInterest = await _cityInfoRepository.GetPointOfInterestForCityAsync(cityId, pointOfInterestId);
            if(pointOfInterest == null)
            {
                return NotFound();
            }
            return Ok(_mapper.Map<PointOfInterestDto>(pointOfInterest)); 
        }
        [HttpPost]
        //why use PointOfInterestForCreationDto
        public async Task<ActionResult<PointOfInterestDto>> CreatePointOfInterest(int cityId, [FromBody] PointOfInterestForCreationDto pointOfInterest) //since using AppController frombody is not necessary, it will be assumed itself
        {
            //check if city exists
            if (!await _cityInfoRepository.cityExistsAsync(cityId))
            {
                return NotFound();
            }
            //if exist we map PointOfInterestForCreationDto pointOfInterest in our entity pointsOfInterest.
            var finalPointOfinterest = _mapper.Map<PointOfInterest>(pointOfInterest); //returns PointOfInterest list entiti

            await _cityInfoRepository.AddPointOfInterestForCityAsync(cityId, finalPointOfinterest);

            await _cityInfoRepository.SaveChangesAsync(); //saves changes in entity to database

            var createdPointsOfInterestToReturn = _mapper.Map<PointOfInterestDto>(finalPointOfinterest);

            return CreatedAtRoute("GetPointOfInterest", new {cityId = cityId,pointOfInterestId = createdPointsOfInterestToReturn.Id},createdPointsOfInterestToReturn);
        }

        [HttpPut("{pointofinterestid}")] //?
        public async Task<ActionResult> UpdatePointOfInterestAsync(int cityId, int pointOfInterestId, PointOfInterestForUpdateDto pointOfInterest)
        {
            if (!await _cityInfoRepository.cityExistsAsync(cityId))
            {
                return NotFound();
            }

            var pointOfInterestEntity = await _cityInfoRepository.GetPointOfInterestForCityAsync(cityId, pointOfInterestId);
            if(pointOfInterestEntity == null)
            {
                return NotFound();
            }

            _mapper.Map(pointOfInterest, pointOfInterestEntity); //will override values from source in destinashion

            await _cityInfoRepository.SaveChangesAsync();
            return NoContent();
        }

        [HttpPatch("{pointofinterestid}")]
        public async Task<ActionResult> PartiallyUpdatePointOfInterest(int cityId, int pointOfInterestId, JsonPatchDocument<PointOfInterestForUpdateDto> patchDocument)
        {
            if (!await _cityInfoRepository.cityExistsAsync(cityId))
            {
                return NotFound();
            }

            var pointOfInterestEntity = await _cityInfoRepository.GetPointOfInterestForCityAsync(cityId, pointOfInterestId);
            if (pointOfInterestEntity == null)
            {
                return NotFound();
            }

            var pointOfInterestToPatch = _mapper.Map<PointOfInterestForUpdateDto>(pointOfInterestEntity);

            patchDocument.ApplyTo(pointOfInterestToPatch, ModelState);
            
            if(!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if(!TryValidateModel(pointOfInterestToPatch))
            {
                return BadRequest(ModelState);
            }
            _mapper.Map(pointOfInterestToPatch, pointOfInterestEntity); //why are we passing back to entities
            await _cityInfoRepository.SaveChangesAsync();
            return NoContent();
        }
        [HttpDelete("{pointofinterestid}")]
        public async Task<ActionResult> DeletePointOfInterest(int cityId, int pointOfInterestId)
        {
            if (!await _cityInfoRepository.cityExistsAsync(cityId))
            {
                return NotFound();
            }

            var pointOfInterestEntity = await _cityInfoRepository.GetPointOfInterestForCityAsync(cityId, pointOfInterestId);
            if (pointOfInterestEntity == null)
            {
                return NotFound();
            }

            _cityInfoRepository.DeletePointOfInterest(pointOfInterestEntity);
            await _cityInfoRepository.SaveChangesAsync(); 

            _mailService.Send("Point of interest deleted.", $"Point of interest {pointOfInterestEntity.Name} with id {pointOfInterestEntity.Id}");
            
            return NoContent();
            
        }
    }
}

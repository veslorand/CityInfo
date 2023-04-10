using CityInfo.API.Models;
using CityInfo.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace CityInfo.API.Controllers
{
    [Route("api/cities/{cityId}/pointsofinterest")]
    [ApiController]
    public class PointsOfInterestController : ControllerBase
    {
        private readonly ILogger<PointsOfInterestController> _logger;
        private readonly IMailService _mailService;
        private readonly CitiesDataStore _citiesDataStore;

        public PointsOfInterestController(ILogger<PointsOfInterestController> logger, IMailService mailService, CitiesDataStore citiesDataStore)
        {
            _logger = logger ?? throw new ArgumentException(nameof(logger));
            _mailService = mailService ?? throw new ArgumentException(nameof(mailService));
            _citiesDataStore = citiesDataStore ?? throw new ArgumentException(nameof(citiesDataStore));
        }
        [HttpGet]
        public ActionResult<IEnumerable<PointOfInterestDto>> GetPointsOfInterest(int cityId)
        {
            try
            {
                var city = _citiesDataStore.Cities.FirstOrDefault(city => city.Id == cityId);
                if (city == null)
                {
                    _logger.LogInformation($"City not found: {cityId}");
                    return NotFound();
                }
                return Ok(city.PointsOfInterest);
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"OMGGGGG: {cityId}", ex);
                return StatusCode(500, "Problem while handling your request");
            }
        }

        [HttpGet("{pointofinterestid}", Name = "GetPointOfInterest")]
        public ActionResult<PointOfInterestDto> GetPointOfInterest(int cityId, int pointofinterestid)
        {
            var city = _citiesDataStore.Cities.FirstOrDefault(city => city.Id == cityId);
            if (city == null || city.PointsOfInterest.FirstOrDefault(x => x.Id == pointofinterestid) == default)
            {
                return NotFound();
            }

            var pointOfInterest = city.PointsOfInterest.FirstOrDefault(x => x.Id == pointofinterestid);
            if (pointOfInterest == null)
            {
                return NotFound();
            }

            return Ok(pointOfInterest);
        }

        [HttpPost]
        public ActionResult<PointOfInterestDto> CreatePointOfInterest(int cityId, PointOfInterestForCreationDto pointOfInterest)
        {
            var city = _citiesDataStore.Cities.FirstOrDefault(x => cityId == x.Id);
            if (city == null)
            {
                return NotFound();
            }

            var maxPointOfInterestId = _citiesDataStore.Cities.SelectMany(x => x.PointsOfInterest).Max(p => p.Id);

            var finalPointOfInterest = new PointOfInterestDto()
            {
                Id = ++maxPointOfInterestId,
                Name = pointOfInterest.Name,
                Description = pointOfInterest.Description,
            };

            city.PointsOfInterest.Add(finalPointOfInterest);

            return CreatedAtRoute("GetPointOfInterest",
                new
                {
                    cityId = cityId,
                    pointOfInterestId = finalPointOfInterest.Id,
                },
                finalPointOfInterest
                );
        }

        [HttpPut("{pointofinterestid}")]
        public ActionResult<PointOfInterestDto> UpdatePointOfInterest(int cityId, int pointOfInterestId, PointOfInterestForUpdateDto pointOfInterest)
        {
            var city = _citiesDataStore.Cities.FirstOrDefault(x => cityId == x.Id);
            if (city == null)
            {
                return NotFound();
            }

            var pointOfInterestFromStore = city.PointsOfInterest.FirstOrDefault(c => c.Id == pointOfInterestId);
            if (pointOfInterestFromStore == null)
            {
                return NotFound();
            }

            pointOfInterestFromStore.Name = pointOfInterest.Name;
            pointOfInterestFromStore.Description = pointOfInterest.Description;

            return NoContent();
        }

        [HttpPatch("{pointofinterestid}")]
        public ActionResult<PointOfInterestDto> PartiallyUpdatePointOfInterest(
            int cityId, int pointOfInterestId, JsonPatchDocument<PointOfInterestForUpdateDto> patchDocument)
        {
            var city = _citiesDataStore.Cities.FirstOrDefault(x => cityId == x.Id);
            if (city == null)
            {
                return NotFound();
            }

            var pointOfInterestFromStore = city.PointsOfInterest.FirstOrDefault(c => c.Id == pointOfInterestId);
            if (pointOfInterestFromStore == null)
            {
                return NotFound();
            }

            var pointOfInterestToPatch = new PointOfInterestForUpdateDto()
            {
                Name = pointOfInterestFromStore.Name,
                Description = pointOfInterestFromStore.Description,
            };

            patchDocument.ApplyTo(pointOfInterestToPatch, ModelState);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!TryValidateModel(pointOfInterestToPatch))
            {
                return BadRequest(ModelState);
            }

            pointOfInterestFromStore.Name = pointOfInterestToPatch.Name;
            pointOfInterestFromStore.Description = pointOfInterestToPatch.Description;

            return NoContent();
        }

        [HttpDelete("{pointofinterestid}")]
        public ActionResult<PointOfInterestDto> DeletePointOfInterest(int cityId, int pointOfInterestId)
        {
            var city = _citiesDataStore.Cities.FirstOrDefault(x => cityId == x.Id);
            if (city == null)
            {
                return NotFound();
            }

            var pointOfInterestFromStore = city.PointsOfInterest.FirstOrDefault(c => c.Id == pointOfInterestId);
            if (pointOfInterestFromStore == null)
            {
                return NotFound();
            }

            city.PointsOfInterest.Remove(pointOfInterestFromStore);
            _mailService.Send(
                "Point of interest deleted.",
                $"Point of interest {pointOfInterestFromStore.Name} with id {pointOfInterestFromStore.Id} was deleted.");
            return NoContent();
        }
    }
}

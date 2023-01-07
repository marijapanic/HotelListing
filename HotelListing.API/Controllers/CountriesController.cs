using AutoMapper;
using HotelListing.API.Core.Contracts;
using HotelListing.API.Core.Exceptions;
using HotelListing.API.Core.Models;
using HotelListing.API.Core.Models.Country;
using HotelListing.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;

namespace HotelListing.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CountriesController : ControllerBase
    {
        private readonly ICountriesRepository _countriesRepository;
        private readonly ILogger<CountriesController> _logger;
        private readonly IMapper _mapper;

        public CountriesController(IMapper mapper, ICountriesRepository countryRepository, ILogger<CountriesController> logger)
        {
            this._mapper = mapper;
            this._countriesRepository = countryRepository;
            this._logger = logger;
        }

        // GET: api/Countries
        [HttpGet("GetAll")]
        [EnableQuery]
        public async Task<ActionResult<IEnumerable<GetCountryDto>>> GetCountries()
        {
            var countries = await _countriesRepository.GetAllAsync<GetCountryDto>();

            return Ok(countries);
        }

        // GET: api/Countries/?StartIndex=0&pagesize=25&PageNumber=1
        [HttpGet]
        public async Task<ActionResult<PagedResult<GetCountryDto>>> GetPagedCountries([FromQuery] QueryParameters queryParameters)
        {
            var pagedCountriesResult = await _countriesRepository.GetAllAsync<GetCountryDto>(queryParameters);

            return Ok(pagedCountriesResult);
        }

        // GET: api/Countries/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CountryDto>> GetCountry(int id)
        {
            var country = await _countriesRepository.GetDetails(id);

            return Ok(country);
        }

        // PUT: api/Countries/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutCountry(int id, UpdateCountryDto updateCountry)
        {
            if (id != updateCountry.Id)
            {
                return BadRequest();
            }

            try
            {
                await _countriesRepository.UpdateAsync(id, updateCountry);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await CountryExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Countries
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<CountryDto>> PostCountry(CreateCountryDto createCountry)
        {
            var country = await _countriesRepository.AddSync<CreateCountryDto, GetCountryDto>(createCountry);

            return CreatedAtAction(nameof(GetCountry), new { id = country.Id }, country);
        }

        // DELETE: api/Countries/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> DeleteCountry(int id)
        {
            await _countriesRepository.DeleteAsync(id);

            return NoContent();
        }

        private async Task<bool> CountryExists(int id)
        {
            return await _countriesRepository.Exists(id);
        }
    }
}

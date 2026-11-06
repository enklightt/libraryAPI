using Asp.Versioning;
using LibraryAPI.Infrastructure;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace LibraryAPI.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/genres")]
    public class GenresController : ControllerBase
    {
        private readonly IGenreService _genreService;
        private readonly RequestCacheStatus _cacheStatus;

        public GenresController(IGenreService genreService, RequestCacheStatus cacheStatus)
        {
            _genreService = genreService;
            _cacheStatus = cacheStatus;
        }

        [HttpGet]
        public async Task<IActionResult> GetGenres()
        {
            var genres = await _genreService.GetGenresAsync();
            Response.Headers["X-Cache"] = _cacheStatus.CacheHit == true ? "HIT" : "MISS";
            return Ok(genres);
        }
    }
}

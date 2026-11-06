using Asp.Versioning;
using LibraryAPI.Infrastructure;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace LibraryAPI.Controllers
{
    /// <summary>Provides the available book genres.</summary>
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

        /// <summary>Lists all genres.</summary>
        /// <response code="200">The genre identifiers and names.</response>
        /// <remarks>Example request: <code>GET /api/v1/genres</code>. Example response: <code>[{"id":"genre-guid","name":"History"}]</code></remarks>
        [HttpGet]
        public async Task<IActionResult> GetGenres()
        {
            var genres = await _genreService.GetGenresAsync();
            Response.Headers["X-Cache"] = _cacheStatus.CacheHit == true ? "HIT" : "MISS";
            return Ok(genres);
        }
    }
}

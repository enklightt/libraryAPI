using LibraryAPI.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryAPI.Controllers
{
    /// <summary>Provides the available book genres.</summary>
    [ApiController]
    [Route("api/v1/genres")]
    public class GenresController : ControllerBase
    {
        private readonly AppDbContext _context;

        public GenresController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>Lists all genres.</summary>
        /// <response code="200">The genre identifiers and names.</response>
        /// <remarks>Example request: <code>GET /api/v1/genres</code>. Example response: <code>[{"id":"genre-guid","name":"History"}]</code></remarks>
        [HttpGet]
        public async Task<IActionResult> GetGenres()
        {
            var genres = await _context.Genres
                .Select(g => new
                {
                    g.Id,
                    g.Name
                })
                .ToListAsync();

            return Ok(genres);
        }
    }
}

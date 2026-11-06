using Asp.Versioning;
using LibraryAPI.DTOs;
using LibraryAPI.Infrastructure;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace LibraryAPI.Controllers;

/// <summary>
/// Друга версія API книг із уніфікованою структурою відповіді (data + meta).
/// </summary>
[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/books")]
[Produces(MediaTypeNames.Application.Json)]
public class BooksV2Controller : ControllerBase
{
    private readonly IBookService _bookService;
    private readonly RequestCacheStatus _cacheStatus;

    public BooksV2Controller(IBookService bookService, RequestCacheStatus cacheStatus)
    {
        _bookService = bookService;
        _cacheStatus = cacheStatus;
    }

    /// <summary>
    /// Повертає посторінковий список книг у форматі v2.
    /// </summary>
    /// <response code="200">Список книг успішно отримано.</response>
    /// <response code="400">Некоректні параметри посторінкового відображення.</response>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> GetBooks(
        [FromQuery, System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)] int page = 1,
        [FromQuery, System.ComponentModel.DataAnnotations.Range(1, 100)] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? genreId = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortOrder = "asc")
    {
        var result = await _bookService.GetAllAsync(page, pageSize, search, genreId, sortBy, sortOrder);
        Response.Headers["X-Cache"] = _cacheStatus.CacheHit == true ? "HIT" : "MISS";

        return Ok(new
        {
            data = result.Items,
            meta = new
            {
                result.Page,
                result.PageSize,
                result.TotalCount,
                result.TotalPages,
                ApiVersion = "2.0"
            }
        });
    }
}

using LibraryAPI.DTOs;

namespace LibraryAPI.Services;

public interface IGenreService
{
    Task<IReadOnlyList<GenreDto>> GetGenresAsync();
}

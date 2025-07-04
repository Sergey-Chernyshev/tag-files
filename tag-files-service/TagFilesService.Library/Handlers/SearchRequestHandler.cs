using System.Linq.Dynamic.Core;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TagFilesService.Infrastructure;
using TagFilesService.Library.Contracts;
using TagFilesService.Model;

namespace TagFilesService.Library.Handlers;

public class SearchRequestHandler(AppDbContext dbContext)
    : IRequestHandler<SearchRequest, PaginatedList<LibraryItemDto>>
{
    public async Task<PaginatedList<LibraryItemDto>> Handle(SearchRequest request, CancellationToken cancellationToken)
    {
        PaginatedList<LibraryItem> searchResults = await Search(request);
        List<LibraryItemDto> itemsDto = searchResults.Items
            .Select(LibraryItemDto.FromModel)
            .ToList();
        return new(itemsDto, searchResults.TotalItems, searchResults.PageIndex, searchResults.TotalPages);
    }

    private async Task<PaginatedList<LibraryItem>> Search(SearchRequest request)
    {
        IQueryable<LibraryItem> queryable = dbContext.LibraryItems
            .Include(x => x.Tags);

        if (!string.IsNullOrWhiteSpace(request.TagQuery))
        {
            string dynamicQuery = TagQueryConverter.ConvertToDynamicQuery(request.TagQuery);
            queryable = queryable
                .Where(dynamicQuery);
        }

        if (request.ItemType is not null)
        {
            queryable = queryable.Where(x => x.FileType == request.ItemType);
        }

        queryable = queryable
            .OrderByDescending(x => x.UploadedOn);
        return await QueryablePaginatedList<LibraryItem>.CreateAsync(queryable, request.PageIndex, request.PageSize);
    }
}
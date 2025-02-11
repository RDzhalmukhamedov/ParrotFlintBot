using System.Collections.Generic;

namespace ParrotFlintBot.RSSReader.Dto;

internal record GfResponseDto(IEnumerable<GfUpdateDto> PagedItems, short TotalItemCount);

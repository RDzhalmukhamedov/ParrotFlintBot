using System;

namespace ParrotFlintBot.RSSReader.Dto;

internal record GfRequestDto(long ProjectID, int? LowestFetchedSequenceNumber, string SearchTerm, DateTime? DateFrom, DateTime? DateTo, bool OnlyUserRelevant, int ProjectUpdateUserContext);

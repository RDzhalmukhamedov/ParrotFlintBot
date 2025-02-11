using System;

namespace ParrotFlintBot.RSSReader.Dto;

internal record GfUpdateDto(string Abstract, object Content, bool HasImage, string ImageUrl, int targetAudience, object targetUserGroupIDs, string projectUpdateUrl, object bulkNotificationID, DateTime? createdAt, string imageFileName, bool IsPinned, bool IsPublished, int LikesCount, object PollID, long ProjectId, long ProjectUpdateId, DateTime? publishedAt, int sequenceNumber, object status, string Title, DateTime? updatedAt);

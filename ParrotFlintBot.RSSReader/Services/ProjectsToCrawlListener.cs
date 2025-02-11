using CodeHollow.FeedReader;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParrotFlintBot.RabbitMQ;
using ParrotFlintBot.RSSReader.Dto;
using ParrotFlintBot.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ParrotFlintBot.RSSReader.Services;

public class ProjectsToCrawlListener : RabbitMQListener
{
    private static string _fakeUserAgent = "Mozilla/5.0 (Windows NT 10.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/111.0.0.0 Safari/537.36";
    private readonly ILogger<ProjectsToCrawlListener> _logger;
    private readonly RabbitMQPublisher _publisher;
    private readonly RabbitMQConfiguration _rabbitConfig;
    private readonly string _updatesRouteKey;

    public ProjectsToCrawlListener(RabbitMQPublisher publisher, IOptions<RabbitMQConfiguration> config,
        ILogger<ProjectsToCrawlListener> logger)
        : base(config, logger, RouteKeyNames.ProjectsToSimpleCrawl, nameof(ProjectsToCrawlListener))
    {
        _publisher = publisher;
        _logger = logger;
        _rabbitConfig = config.Value;

        config.Value.PublisherRouteKeys.TryGetValue(RouteKeyNames.NewCrawledUpdates, out var route);
        _updatesRouteKey = string.IsNullOrWhiteSpace(route) ? RouteKeyNames.NewCrawledUpdates : route;
    }

    protected override async Task<bool> ProcessMessage(string message, CancellationToken stoppingToken)
    {
        try
        {
            var projectsInfo = JsonSerializer.Deserialize<List<ProjectInfo>>(message);
            if (projectsInfo is null)
            {
                return false;
            }

            var result = new List<ProjectInfo>();
            foreach (var projectInfo in projectsInfo)
            {
                ProjectInfo update = null;
                if (projectInfo.Link.Contains("kickstarter", StringComparison.InvariantCultureIgnoreCase))
                {
                    update = await CrawlKs(projectInfo, stoppingToken);
                }
                else
                {
                    update = await CrawlGf(projectInfo, stoppingToken);
                }

                if (update is not null) result.Add(update);
            }

            _publisher.PushMessage(_updatesRouteKey, result, _rabbitConfig.MessageTTL);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError("Processing message for routeKey: {RouteKey}, failed with exception: {Exception}", RouteKey, ex);
            return false;
        }
    }

    private async Task<ProjectInfo> CrawlKs(ProjectInfo projectInfo, CancellationToken stoppingToken)
    {
        try
        {
            // TODO Use user-agent generator (later)
            var feed = await FeedReader.ReadAsync(projectInfo.Link, cancellationToken: stoppingToken, userAgent: _fakeUserAgent);

            if (feed.Items.Count > 0)
            {
                var lastCrawledUpdate = feed.Items.FirstOrDefault(i => i.Id.Contains($"{projectInfo.LastUpdateId}"), null);
                if (lastCrawledUpdate is null)
                {
                    projectInfo.NeedFullCrawl = true;
                }
                else
                {
                    var countChange = feed.Items.IndexOf(lastCrawledUpdate);
                    if (countChange == 0)
                    {
                        return null;
                    }
                    projectInfo.NeedFullCrawl = false;
                    projectInfo.LastUpdateTitle = lastCrawledUpdate.Title;
                    projectInfo.PrevUpdatesCount = projectInfo.UpdatesCount;
                    projectInfo.UpdatesCount = (short)(projectInfo.UpdatesCount + countChange);
                    projectInfo.Link = lastCrawledUpdate.Link;
                    projectInfo.LastUpdateId = long.Parse(projectInfo.Link.Split('/').Last());
                }
            }
            else
            {
                projectInfo.NeedFullCrawl = true;
            }

            return projectInfo;
        }
        catch (Exception ex)
        {
            Logger.LogError("Processing message for project: {ProjectName}, failed with exception: {Exception}",
                projectInfo.ProjectName, ex);

            return null;
        }
    }

    private async Task<ProjectInfo> CrawlGf(ProjectInfo projectInfo, CancellationToken stoppingToken)
    {
        try
        {
            using (var client = new HttpClient())
            {
                // TODO Use user-agent generator (later)
                client.DefaultRequestHeaders.UserAgent.ParseAdd(_fakeUserAgent);

                var request = new GfRequestDto(projectInfo.ProjectIdOnSite.Value, null, "", null, null, false, 1);
                var response = await client.PostAsJsonAsync<GfRequestDto>(projectInfo.Link, request, stoppingToken);
                var updatesInfo = JsonSerializer.Deserialize<GfResponseDto>(await response.Content.ReadAsStringAsync());

                if (updatesInfo is not null && updatesInfo.TotalItemCount > projectInfo.UpdatesCount)
                {
                    var lastUpdate = updatesInfo.PagedItems.First();
                    projectInfo.NeedFullCrawl = false;
                    projectInfo.PrevUpdatesCount = projectInfo.UpdatesCount;
                    projectInfo.UpdatesCount = updatesInfo.TotalItemCount;
                    projectInfo.LastUpdateId = lastUpdate.sequenceNumber;
                    projectInfo.Link = $"https://gamefound.com{lastUpdate.projectUpdateUrl}";

                    return projectInfo;
                }

                return null;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError("Processing message for project: {ProjectName}, failed with exception: {Exception}",
                projectInfo.ProjectName, ex);

            return null;
        }
    }
}
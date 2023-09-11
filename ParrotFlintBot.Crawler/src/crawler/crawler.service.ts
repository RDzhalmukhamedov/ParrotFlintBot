import { Injectable, Logger } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { ActorRun, ApifyClient } from 'apify-client';
import { AppSettings } from 'src/shared/app-settings.interface';
import { ProjectInfo } from 'src/shared/project-info.interface';
import { ActorInput } from './actor-input.interface';
import { RequestOptions } from './request-options.interface';

@Injectable()
export class CrawlerService {
  private readonly logger = new Logger(CrawlerService.name);
  private readonly crawler: ApifyClient;

  constructor(private readonly configService: ConfigService<AppSettings>) {
    const apifyToken = this.configService.get('APIFY_TOKEN', { infer: true });
    if (!!apifyToken) {
      this.crawler = new ApifyClient({ token: apifyToken });
    } else {
      this.logger.warn('Apify client token not defined');
    }
  }

  public async crawlProjectsUpdates(projectsToCrawl: ProjectInfo[]): Promise<ProjectInfo[]> {
    if (!this.crawler) {
      this.logger.warn('Crawler client not initialized, crawling will be stopped');
      return [];
    }

    const crawledUpdates = await Promise.all([
      this.runActor('APIFY_KS_ACTOR_ID', projectsToCrawl, 'kickstarter'),
      this.runActor('APIFY_GF_ACTOR_ID', projectsToCrawl, 'gamefound'),
    ]);
    return crawledUpdates.flat();
  }

  private async runActor(
    actorIdKey: keyof AppSettings,
    projectsToCrawl: ProjectInfo[],
    label: string,
  ): Promise<ProjectInfo[]> {
    const actorId = this.configService.get(actorIdKey, { infer: true }) as string;
    if (!actorId) {
      this.logger.warn('Apify actor id not defined, crawling will be skipped', { label: label });
      return [];
    }

    const filteredProjects = projectsToCrawl.filter((project) => project.Link.includes(label));
    if (!filteredProjects || filteredProjects.length == 0) return [];
    const input = await this.initActorInput(filteredProjects, label);

    this.logger.log(`Started crawling new updates for ${input.projectsToCrawl.length} projects`, { label: label });
    try {
      const maxRuns = this.configService.get('APIFY_MAX_RUNS', 3, { infer: true });
      let runsCount = 0;
      let run: ActorRun | undefined = undefined;
      let requestHandled: number | undefined = 0;
      while (!requestHandled && runsCount < maxRuns) {
        runsCount++;
        run = await this.crawler.actor(actorId).call(JSON.stringify(input), {
          contentType: 'application/json',
          build: 'latest',
        });
        requestHandled = run?.usage?.REQUEST_QUEUE_READS;
      }
      if (!run || requestHandled == 0) {
        this.logger.log(
          `Some issue during actor execution. Run failed or none of requests was handled (${requestHandled})`,
        );
        return [];
      }
      const { items } = await this.crawler.dataset<ProjectInfo>(run.defaultDatasetId).listItems();
      return items;
    } catch (ex) {
      this.logger.error('Some error during crawler run', ex);
    }
    return [];
  }

  private async initActorInput(projectsToCrawl: ProjectInfo[], label: string): Promise<ActorInput> {
    const maxRetriesCount = this.configService.get('APIFY_MAX_RETRIES', 10, { infer: true });
    const result: ActorInput = {
      projectsToCrawl: [],
      maxRequestRetries: +maxRetriesCount,
    };
    this.logger.log(`Started initializing request list for ${projectsToCrawl.length} projects`);
    try {
      const requests = projectsToCrawl.map((project: ProjectInfo): RequestOptions => {
        return {
          url: project.Link,
          userData: {
            label: label,
            project: project,
          },
        };
      });
      result.projectsToCrawl = requests;
    } catch (ex) {
      this.logger.error('Some error during initialize actor input', ex);
    }
    return result;
  }
}

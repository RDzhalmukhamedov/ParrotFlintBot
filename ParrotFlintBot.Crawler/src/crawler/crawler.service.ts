import { Injectable, Logger } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { ApifyClient } from 'apify-client';
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

    const actorId = this.configService.get('APIFY_ACTOR_ID', { infer: true });
    if (!actorId) {
      this.logger.warn('Apify actor id not defined, crawling will be stopped');
      return [];
    }

    const actorInput = await this.initActorInput(projectsToCrawl);
    this.logger.log(`Started crawling new updates for ${projectsToCrawl.length} projects`);
    try {
      const { defaultDatasetId } = await this.crawler.actor(actorId).call(JSON.stringify(actorInput), {
        contentType: 'application/json',
        build: 'latest',
      });
      const { items } = await this.crawler.dataset<ProjectInfo>(defaultDatasetId).listItems();
      return items;
    } catch (ex) {
      this.logger.error('Some error during crawler run', ex);
    }
    return [];
  }

  private async initActorInput(projectsToCrawl: ProjectInfo[]): Promise<ActorInput> {
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
            label: 'ks',
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

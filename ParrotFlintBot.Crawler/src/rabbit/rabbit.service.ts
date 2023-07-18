import { AmqpConnection, Nack } from '@golevelup/nestjs-rabbitmq';
import { Injectable, Logger } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { CrawlerService } from 'src/crawler/crawler.service';
import { AppSettings } from 'src/shared/app-settings.interface';
import { ProjectInfo } from 'src/shared/project-info.interface';
import { RabbitListener } from './rabbit.decorator';

@Injectable()
export class RabbitService {
  private readonly logger = new Logger(RabbitService.name);

  constructor(
    private readonly crawler: CrawlerService,
    private readonly amqpConnection: AmqpConnection,
    private readonly configService: ConfigService<AppSettings>,
  ) {}

  @RabbitListener()
  public async handleProjectsToCrawl(message: string): Promise<void | Nack> {
    try {
      const projectsToCrawl: ProjectInfo[] = JSON.parse(message);
      this.logger.log(`Received message with ${projectsToCrawl.length} projects to crawl`);

      const newUpdates = await this.crawler.crawlProjectsUpdates(projectsToCrawl);
      await this.sendUpdates(newUpdates);
    } catch (ex) {
      this.logger.error('Some error during handle rabbit message', ex);
      return new Nack();
    }
  }

  async sendUpdates(crawledUpdates: ProjectInfo[]): Promise<void> {
    this.logger.log(`Sending message with updates for ${crawledUpdates.length} crawled projects`);
    await this.amqpConnection.publish(
      'message',
      this.configService.get('ROUTE_KEY_TO_PUBLISH', 'NewCrawledUpdates', { infer: true }),
      crawledUpdates,
      { expiration: `${this.configService.get('RABBIT_MESSAGE_TTL', 82800, { infer: true }) * 1000}` },
    );
  }
}

import { Module } from '@nestjs/common';
import { CrawlerService } from './crawler.service';

@Module({
  imports: [],
  controllers: [],
  providers: [CrawlerService],
  exports: [CrawlerService],
})
export class CrawlerModule {}

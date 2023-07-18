import { RabbitMQModule } from '@golevelup/nestjs-rabbitmq';
import { Module } from '@nestjs/common';
import { ConfigModule, ConfigService } from '@nestjs/config';
import { CrawlerModule } from 'src/crawler/crawler.module';
import { AppSettings } from 'src/shared/app-settings.interface';
import { RabbitService } from './rabbit.service';

@Module({
  imports: [
    RabbitMQModule.forRootAsync(RabbitMQModule, {
      imports: [ConfigModule],
      inject: [ConfigService],
      useFactory: (config: ConfigService<AppSettings>) => ({
        exchanges: [
          {
            name: 'message',
            type: 'direct',
            createExchangeIfNotExists: true,
            options: {
              durable: true,
            },
          },
        ],
        uri:
          `amqp://${config.get('RABBIT_USERNAME', { infer: true })}:${config.get('RABBIT_PASSWORD', { infer: true })}` +
          `@${config.get('RABBIT_HOST', { infer: true })}/${config.get('RABBIT_USERNAME', { infer: true })}`,
        channels: {
          'new-crawled-updates-publisher': {
            default: true,
          },
          'project-to-crawl-listener': {},
        },
        connectionInitOptions: { wait: false },
      }),
    }),
    CrawlerModule,
  ],
  controllers: [],
  providers: [RabbitService],
})
export class RabbitModule {}

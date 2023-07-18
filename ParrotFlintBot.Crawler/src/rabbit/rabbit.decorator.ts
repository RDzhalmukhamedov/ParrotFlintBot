import { RabbitSubscribe } from '@golevelup/nestjs-rabbitmq';
import { Logger } from '@nestjs/common';
import { Channel } from 'amqp-connection-manager';
import * as dotenv from 'dotenv';

export function RabbitListener(): ReturnType<typeof RabbitSubscribe> {
  const logger = new Logger(RabbitListener.name);
  dotenv.config();

  const exchange = 'message';
  const routingKey = process.env.ROUTE_KEY_TO_LISTEN;
  const queue = process.env.ROUTE_KEY_TO_LISTEN;

  return RabbitSubscribe({
    exchange,
    routingKey,
    queue,
    queueOptions: {
      durable: true,
      exclusive: false,
      channel: 'project-to-crawl-listener',
    },
    createQueueIfNotExists: true,
    errorHandler: (channel: Channel, msg: any, error: Error) => {
      logger.error(error);
      channel.reject(msg, false);
    },
  });
}

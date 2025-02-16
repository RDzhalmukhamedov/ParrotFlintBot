import { ConfigModule } from '@nestjs/config';
import { RabbitModule } from './rabbit/rabbit.module';
import { Module } from '@nestjs/common';
import { HealthModule } from './health/health.module';
import { AppController } from './app.controller';

@Module({
  imports: [
    RabbitModule,
    ConfigModule.forRoot({
      isGlobal: true,
    }),
    HealthModule,
  ],
  controllers: [AppController],
})
export class AppModule {}

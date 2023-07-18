import { ConfigModule } from '@nestjs/config';
import { RabbitModule } from './rabbit/rabbit.module';
import { Module } from '@nestjs/common';

@Module({
  imports: [
    RabbitModule,
    ConfigModule.forRoot({
      isGlobal: true,
    }),
  ],
})
export class AppModule {}

import { Controller, Get } from '@nestjs/common';

@Controller('app')
export class AppController {
  @Get('index')
  getHello(): string {
    return "It's Crawler";
  }
}

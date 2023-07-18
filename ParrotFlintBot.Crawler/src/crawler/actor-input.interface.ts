import { RequestOptions } from './request-options.interface';

export interface ActorInput {
  projectsToCrawl: RequestOptions[];
  maxRequestRetries: number;
}

export interface AppSettings {
  ROUTE_KEY_TO_LISTEN: string;
  ROUTE_KEY_TO_PUBLISH: string;
  RABBIT_HOST: string;
  RABBIT_PORT: number;
  RABBIT_USERNAME: string;
  RABBIT_PASSWORD: string;
  RABBIT_MESSAGE_TTL: number;
  APIFY_TOKEN: string;
  APIFY_KS_ACTOR_ID: string;
  APIFY_GF_ACTOR_ID: string;
  APIFY_MAX_RETRIES: number;
}

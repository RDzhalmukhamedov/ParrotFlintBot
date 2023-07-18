import { ProjectInfo } from 'src/shared/project-info.interface';

export interface RequestOptions {
  url: string;
  userData: {
    label: string;
    project: ProjectInfo;
  };
}

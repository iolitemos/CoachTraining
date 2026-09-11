export interface CompetitionMatch {
  competitionMatchId: number;
  name: string;
  province: string;
  startDate: string;
  endDate: string;
}

export type CompetitionMatchRequest = Omit<CompetitionMatch, 'competitionMatchId'>;

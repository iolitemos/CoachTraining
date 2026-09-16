export interface CompetitionMatch {
  competitionMatchId: number;
  name: string;
  province: string;
  startDate: string;
  endDate: string;
  coaches: CompetitionMatchCoach[];
}

export interface CompetitionMatchCoach {
  coachId: number;
  fullName: string;
  nickname: string | null;
}

export type CompetitionMatchRequest = Omit<CompetitionMatch, 'competitionMatchId' | 'coaches'> & { coachIds: number[] };

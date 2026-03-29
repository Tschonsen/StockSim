/**
 * Career progression titles based on portfolio performance.
 * Displayed in TopBar next to player name.
 */
export interface CareerTitle {
  id: string;
  title: string;
  minEquity: number;      // Minimum portfolio equity to qualify
  minTrades: number;      // Minimum total trades
  minDays: number;        // Minimum days played
  color: string;          // Badge color
  icon: string;           // Unicode icon
}

export const CAREER_TITLES: CareerTitle[] = [
  { id: 'intern',        title: 'Intern',           minEquity: 0,         minTrades: 0,   minDays: 0,   color: '#6B7280', icon: '\u{1F4CB}' },
  { id: 'junior',        title: 'Junior Trader',    minEquity: 10_000,    minTrades: 5,   minDays: 3,   color: '#9CA3AF', icon: '\u{1F4C8}' },
  { id: 'trader',        title: 'Trader',           minEquity: 50_000,    minTrades: 20,  minDays: 10,  color: '#60A5FA', icon: '\u{1F4B9}' },
  { id: 'senior',        title: 'Senior Trader',    minEquity: 100_000,   minTrades: 50,  minDays: 30,  color: '#8B5CF6', icon: '\u{2B50}' },
  { id: 'vp',            title: 'VP Trading',       minEquity: 250_000,   minTrades: 100, minDays: 60,  color: '#F59E0B', icon: '\u{1F31F}' },
  { id: 'director',      title: 'Director',         minEquity: 500_000,   minTrades: 200, minDays: 90,  color: '#F97316', icon: '\u{1F525}' },
  { id: 'fund_manager',  title: 'Fund Manager',     minEquity: 1_000_000, minTrades: 300, minDays: 120, color: '#10B981', icon: '\u{1F4B0}' },
  { id: 'legend',        title: 'Legend',            minEquity: 5_000_000, minTrades: 500, minDays: 252, color: '#EF4444', icon: '\u{1F451}' },
];

/**
 * Calculate the current career title based on stats.
 * Player qualifies for the highest title where ALL conditions are met.
 */
export function getCareerTitle(equity: number, trades: number, days: number): CareerTitle {
  let best = CAREER_TITLES[0];
  for (const t of CAREER_TITLES) {
    if (equity >= t.minEquity && trades >= t.minTrades && days >= t.minDays) {
      best = t;
    }
  }
  return best;
}

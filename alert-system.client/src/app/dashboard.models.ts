export interface AlertRule {
  id: string;
  name: string;
  isActive: boolean;
  matchExpression: string;
  notificationChannels: string[];
  description?: string | null;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
}

export interface AlertRuleDraft {
  id: string | null;
  name: string;
  isActive: boolean;
  matchExpression: string;
  notificationChannelsText: string;
  description: string;
}

export interface WorldEvent {
  id: string;
  eventType: string;
  source: string;
  payloadJson: string;
  occurredAtUtc: string;
  metadata: Record<string, string>;
}

export interface WorldEventDraft {
  eventType: string;
  source: string;
  payloadJson: string;
  metadataJson: string;
}

export interface NotificationLog {
  id: string;
  alertRuleId: string;
  worldEventId: string;
  providerName: string;
  channelName: string;
  succeeded: boolean;
  errorMessage?: string | null;
  attemptedAtUtc: string;
  completedAtUtc?: string | null;
}

export interface SimulationGenerateResponse {
  alertRulesCreated: number;
  worldEventsPublished: number;
  alertRuleIds: string[];
  worldEventIds: string[];
}

export interface DashboardFeedEntry {
  id: string;
  title: string;
  detail: string;
  timestamp: string;
  tone: 'success' | 'info' | 'warning';
}

export interface SimulationDraft {
  alertRules: number;
  worldEvents: number;
}

export const createEmptyAlertRuleDraft = (): AlertRuleDraft => ({
  id: null,
  name: '',
  isActive: true,
  matchExpression: '',
  notificationChannelsText: 'Email, Slack',
  description: ''
});

export const createEmptyWorldEventDraft = (): WorldEventDraft => ({
  eventType: 'BreakingNews',
  source: 'Reuters',
  payloadJson: '{\n  "title": "Demo event",\n  "severity": "High"\n}',
  metadataJson: '{\n  "region": "EU",\n  "priority": "P1"\n}'
});

export const createEmptySimulationDraft = (): SimulationDraft => ({
  alertRules: 3,
  worldEvents: 6
});
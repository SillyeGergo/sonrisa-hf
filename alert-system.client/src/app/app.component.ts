import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import {
  AlertRule,
  AlertRuleDraft,
  DashboardFeedEntry,
  NotificationLog,
  SimulationDraft,
  WorldEvent,
  WorldEventDraft,
  createEmptyAlertRuleDraft,
  createEmptySimulationDraft,
  createEmptyWorldEventDraft
} from './dashboard.models';
import { DashboardApiService } from './dashboard-api.service';

const MAX_FEED_ITEMS = 8;

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './app.component.html',
})
export class AppComponent implements OnInit {
  private readonly api = inject(DashboardApiService);

  readonly title = 'World Event Alerts';
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly alertRules = signal<AlertRule[]>([]);
  readonly notificationLogs = signal<NotificationLog[]>([]);
  readonly eventFeed = signal<DashboardFeedEntry[]>([]);
  readonly alertRuleDraft = signal<AlertRuleDraft>(createEmptyAlertRuleDraft());
  readonly worldEventDraft = signal<WorldEventDraft>(createEmptyWorldEventDraft());
  readonly simulationDraft = signal<SimulationDraft>(createEmptySimulationDraft());

  readonly activeRuleCount = computed(() => this.alertRules().filter(rule => rule.isActive).length);
  readonly notificationSuccessCount = computed(() => this.notificationLogs().filter(log => log.succeeded).length);
  readonly notificationFailureCount = computed(() => this.notificationLogs().filter(log => !log.succeeded).length);
  readonly eventFeedCount = computed(() => this.eventFeed().length);

  async ngOnInit(): Promise<void> {
    await this.refreshDashboard();
  }

  async refreshDashboard(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const [alertRules, notificationLogs] = await Promise.all([
        this.api.getAlertRules(),
        this.api.getNotificationLogs(12)
      ]);

      this.alertRules.set(alertRules);
      this.notificationLogs.set(notificationLogs);

      this.pushFeedEntry({
        title: 'Dashboard refreshed',
        detail: `Loaded ${alertRules.length} alert rules and ${notificationLogs.length} notification logs.`,
        tone: 'info'
      });
    } catch (error) {
      this.error.set(this.toErrorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  async saveAlertRule(): Promise<void> {
    const draft = this.alertRuleDraft();
    this.error.set(null);

    const payload = this.toAlertRulePayload(draft);

    try {
      const saved = draft.id ? await this.api.updateAlertRule(draft.id, payload) : await this.api.createAlertRule(payload);
      this.alertRules.update(currentRules => this.mergeAlertRule(currentRules, saved));
      this.pushFeedEntry({
        title: draft.id ? 'Alert rule updated' : 'Alert rule created',
        detail: `${saved.name} now targets ${saved.notificationChannels.join(', ') || 'no channels'}.`,
        tone: 'success'
      });
      this.resetAlertRuleDraft();
    } catch (error) {
      this.error.set(this.toErrorMessage(error));
    }
  }

  editAlertRule(alertRule: AlertRule): void {
    this.alertRuleDraft.set({
      id: alertRule.id,
      name: alertRule.name,
      isActive: alertRule.isActive,
      matchExpression: alertRule.matchExpression,
      notificationChannelsText: alertRule.notificationChannels.join(', '),
      description: alertRule.description ?? ''
    });
  }

  async deleteAlertRule(alertRuleId: string): Promise<void> {
    const alertRule = this.alertRules().find(rule => rule.id === alertRuleId);
    this.error.set(null);

    try {
      await this.api.deleteAlertRule(alertRuleId);
      this.alertRules.update(currentRules => currentRules.filter(rule => rule.id !== alertRuleId));
      this.pushFeedEntry({
        title: 'Alert rule deleted',
        detail: alertRule ? `${alertRule.name} was removed.` : `Rule ${alertRuleId} was removed.`,
        tone: 'warning'
      });
      if (this.alertRuleDraft().id === alertRuleId) {
        this.resetAlertRuleDraft();
      }
    } catch (error) {
      this.error.set(this.toErrorMessage(error));
    }
  }

  async toggleAlertRule(alertRule: AlertRule): Promise<void> {
    this.error.set(null);

    try {
      const updatedRule = await this.api.updateAlertRule(alertRule.id, {
        ...alertRule,
        isActive: !alertRule.isActive
      });

      this.alertRules.update(currentRules => this.mergeAlertRule(currentRules, updatedRule));
      this.pushFeedEntry({
        title: updatedRule.isActive ? 'Alert rule activated' : 'Alert rule paused',
        detail: updatedRule.name,
        tone: 'info'
      });
    } catch (error) {
      this.error.set(this.toErrorMessage(error));
    }
  }

  async ingestWorldEvent(): Promise<void> {
    const draft = this.worldEventDraft();
    this.error.set(null);

    try {
      const worldEvent = this.toWorldEventPayload(draft);
      const response = await this.api.ingestWorldEvent(worldEvent);

      this.pushFeedEntry({
        title: 'World event ingested',
        detail: `${worldEvent.eventType} from ${worldEvent.source} was accepted as ${response.id}.`,
        tone: 'success'
      });

      this.resetWorldEventDraft();
      await this.refreshDashboard();
    } catch (error) {
      this.error.set(this.toErrorMessage(error));
    }
  }

  async generateSimulation(): Promise<void> {
    const draft = this.simulationDraft();
    this.error.set(null);

    try {
      const response = await this.api.generateSimulation(draft.alertRules, draft.worldEvents);

      this.pushFeedEntry({
        title: 'Simulation generated',
        detail: `${response.alertRulesCreated} rules and ${response.worldEventsPublished} events were seeded.`,
        tone: 'success'
      });

      await this.refreshDashboard();
    } catch (error) {
      this.error.set(this.toErrorMessage(error));
    }
  }

  updateAlertRuleDraft(patch: Partial<AlertRuleDraft>): void {
    this.alertRuleDraft.update(current => ({ ...current, ...patch }));
  }

  updateWorldEventDraft(patch: Partial<WorldEventDraft>): void {
    this.worldEventDraft.update(current => ({ ...current, ...patch }));
  }

  updateSimulationDraft(patch: Partial<SimulationDraft>): void {
    this.simulationDraft.update(current => ({ ...current, ...patch }));
  }

  resetAlertRuleDraft(): void {
    this.alertRuleDraft.set(createEmptyAlertRuleDraft());
  }

  resetWorldEventDraft(): void {
    this.worldEventDraft.set(createEmptyWorldEventDraft());
  }

  private pushFeedEntry(entry: Omit<DashboardFeedEntry, 'id' | 'timestamp'>): void {
    const feedEntry: DashboardFeedEntry = {
      id: crypto.randomUUID(),
      timestamp: new Date().toISOString(),
      ...entry
    };

    this.eventFeed.update(currentEntries => [feedEntry, ...currentEntries].slice(0, MAX_FEED_ITEMS));
  }

  private mergeAlertRule(currentRules: AlertRule[], savedRule: AlertRule): AlertRule[] {
    const existingIndex = currentRules.findIndex(rule => rule.id === savedRule.id);

    if (existingIndex === -1) {
      return [savedRule, ...currentRules];
    }

    return currentRules.map(rule => (rule.id === savedRule.id ? savedRule : rule));
  }

  private toAlertRulePayload(draft: AlertRuleDraft): AlertRule {
    return {
      id: draft.id ?? crypto.randomUUID(),
      name: draft.name.trim(),
      isActive: draft.isActive,
      matchExpression: draft.matchExpression.trim(),
      notificationChannels: draft.notificationChannelsText
        .split(',')
        .map(channel => channel.trim())
        .filter(Boolean),
      description: draft.description.trim() || null,
      createdAtUtc: new Date().toISOString(),
      updatedAtUtc: new Date().toISOString()
    };
  }

  private toWorldEventPayload(draft: WorldEventDraft): WorldEvent {
    let metadata: Record<string, string> = {};

    if (draft.metadataJson.trim()) {
      metadata = JSON.parse(draft.metadataJson) as Record<string, string>;
    }

    return {
      id: crypto.randomUUID(),
      eventType: draft.eventType.trim(),
      source: draft.source.trim(),
      payloadJson: draft.payloadJson.trim(),
      occurredAtUtc: new Date().toISOString(),
      metadata
    };
  }

  private toErrorMessage(error: unknown): string {
    if (error instanceof Error) {
      return error.message;
    }

    return 'An unexpected error occurred.';
  }
}
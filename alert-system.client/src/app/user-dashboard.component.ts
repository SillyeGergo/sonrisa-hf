import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import {
  AlertRule,
  AlertRuleDraft,
  NotificationLog,
  createEmptyAlertRuleDraft
} from './dashboard.models';
import { DashboardApiService } from './dashboard-api.service';

const AVAILABLE_CHANNELS = ['Email', 'Slack'];

@Component({
  selector: 'app-user-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './user-dashboard.component.html'
})
export class UserDashboardComponent implements OnInit {
  private readonly api = inject(DashboardApiService);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly alertRules = signal<AlertRule[]>([]);
  readonly notificationLogs = signal<NotificationLog[]>([]);
  readonly selectedNotification = signal<NotificationLog | null>(null);
  readonly alertRuleDraft = signal<AlertRuleDraft>(createEmptyAlertRuleDraft());

  readonly activeRuleCount = computed(() => this.alertRules().filter(rule => rule.isActive).length);
  readonly recentAlertCount = computed(() => this.notificationLogs().length);

  readonly availableChannels = AVAILABLE_CHANNELS;

  async ngOnInit(): Promise<void> {
    await this.refreshDashboard();
  }

  async refreshDashboard(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const [alertRules, notificationLogs] = await Promise.all([
        this.api.getAlertRules(),
        this.api.getNotificationLogs(8)
      ]);

      this.alertRules.set(alertRules);
      this.notificationLogs.set(notificationLogs);
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
      notificationChannels: [...alertRule.notificationChannels],
      description: alertRule.description ?? ''
    });
  }

  async deleteAlertRule(alertRuleId: string): Promise<void> {
    this.error.set(null);

    try {
      await this.api.deleteAlertRule(alertRuleId);
      this.alertRules.update(currentRules => currentRules.filter(rule => rule.id !== alertRuleId));
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
    } catch (error) {
      this.error.set(this.toErrorMessage(error));
    }
  }

  updateAlertRuleDraft(patch: Partial<AlertRuleDraft>): void {
    this.alertRuleDraft.update(current => ({ ...current, ...patch }));
  }

  toggleNotificationChannel(channelName: string): void {
    this.alertRuleDraft.update(current => {
      const selectedChannels = current.notificationChannels.includes(channelName)
        ? current.notificationChannels.filter(existingChannel => existingChannel !== channelName)
        : [...current.notificationChannels, channelName];

      return { ...current, notificationChannels: selectedChannels };
    });
  }

  isNotificationChannelSelected(channelName: string): boolean {
    return this.alertRuleDraft().notificationChannels.includes(channelName);
  }

  getPayloadFields(notificationLog: NotificationLog): Array<{ label: string; value: string }> {
    const payload = this.parsePayload(notificationLog.payloadJson);
    const preferredKeys = ['title', 'severity'];
    const remainingEntries = Object.entries(payload).filter(([key]) => !preferredKeys.includes(key));

    const fields = preferredKeys
      .filter(key => key in payload)
      .map(key => ({ label: this.toDisplayLabel(key), value: this.toDisplayValue(payload[key]) }));

    for (const [key, value] of remainingEntries) {
      fields.push({ label: this.toDisplayLabel(key), value: this.toDisplayValue(value) });
    }

    if (fields.length === 0) {
      return [{ label: 'Payload', value: notificationLog.payloadJson || 'No payload available' }];
    }

    return fields;
  }

  openNotificationDetails(notificationLog: NotificationLog): void {
    this.selectedNotification.set(notificationLog);
  }

  closeNotificationDetails(): void {
    this.selectedNotification.set(null);
  }

  resetAlertRuleDraft(): void {
    this.alertRuleDraft.set(createEmptyAlertRuleDraft());
  }

  private parsePayload(payloadJson: string): Record<string, unknown> {
    if (!payloadJson.trim()) {
      return {};
    }

    try {
      const parsed = JSON.parse(payloadJson) as unknown;

      if (parsed && typeof parsed === 'object' && !Array.isArray(parsed)) {
        return parsed as Record<string, unknown>;
      }
    } catch {
      return {};
    }

    return {};
  }

  private toDisplayLabel(key: string): string {
    return key
      .replace(/[_-]+/g, ' ')
      .replace(/\b\w/g, match => match.toUpperCase());
  }

  private toDisplayValue(value: unknown): string {
    if (value === null || value === undefined) {
      return '—';
    }

    if (typeof value === 'string') {
      return value;
    }

    if (typeof value === 'number' || typeof value === 'boolean') {
      return String(value);
    }

    return JSON.stringify(value, null, 2);
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
      notificationChannels: [...draft.notificationChannels],
      description: draft.description.trim() || null,
      createdAtUtc: new Date().toISOString(),
      updatedAtUtc: new Date().toISOString()
    };
  }

  private toErrorMessage(error: unknown): string {
    if (error instanceof Error) {
      return error.message;
    }

    return 'An unexpected error occurred.';
  }
}

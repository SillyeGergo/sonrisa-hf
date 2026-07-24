import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import {
  NotificationLog,
  WorldEvent,
  WorldEventDraft,
  createEmptyWorldEventDraft
} from './dashboard.models';
import { DashboardApiService } from './dashboard-api.service';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-dashboard.component.html'
})
export class AdminDashboardComponent implements OnInit {
  private readonly api = inject(DashboardApiService);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly notificationLogs = signal<NotificationLog[]>([]);
  readonly worldEventDraft = signal<WorldEventDraft>(createEmptyWorldEventDraft());
  readonly worldEventGenerationCount = signal(6);

  readonly notificationSuccessCount = computed(() => this.notificationLogs().filter(log => log.succeeded).length);
  readonly notificationFailureCount = computed(() => this.notificationLogs().filter(log => !log.succeeded).length);

  async ngOnInit(): Promise<void> {
    await this.refreshDashboard();
  }

  async refreshDashboard(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const notificationLogs = await this.api.getNotificationLogs(12);

      this.notificationLogs.set(notificationLogs);
    } catch (error) {
      this.error.set(this.toErrorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  async ingestWorldEvent(): Promise<void> {
    const draft = this.worldEventDraft();
    this.error.set(null);

    try {
      const worldEvent = this.toWorldEventPayload(draft);
      await this.api.ingestWorldEvent(worldEvent);

      this.resetWorldEventDraft();
      await this.refreshDashboard();
    } catch (error) {
      this.error.set(this.toErrorMessage(error));
    }
  }

  async generateWorldEvents(): Promise<void> {
    this.error.set(null);

    try {
      await this.api.generateSimulation(0, this.worldEventGenerationCount());
      await this.refreshDashboard();
    } catch (error) {
      this.error.set(this.toErrorMessage(error));
    }
  }

  updateWorldEventDraft(patch: Partial<WorldEventDraft>): void {
    this.worldEventDraft.update(current => ({ ...current, ...patch }));
  }

  updateWorldEventGenerationCount(count: number): void {
    this.worldEventGenerationCount.set(Number.isFinite(count) && count >= 0 ? count : 0);
  }

  resetWorldEventDraft(): void {
    this.worldEventDraft.set(createEmptyWorldEventDraft());
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

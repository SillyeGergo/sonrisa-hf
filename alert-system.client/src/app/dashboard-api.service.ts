import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AlertRule, NotificationLog, SimulationGenerateResponse, WorldEvent } from './dashboard.models';

@Injectable({ providedIn: 'root' })
export class DashboardApiService {
  private readonly http = inject(HttpClient);

  getAlertRules(): Promise<AlertRule[]> {
    return firstValueFrom(this.http.get<AlertRule[]>('/api/alert-rules'));
  }

  createAlertRule(alertRule: AlertRule): Promise<AlertRule> {
    return firstValueFrom(this.http.post<AlertRule>('/api/alert-rules', alertRule));
  }

  updateAlertRule(alertRuleId: string, alertRule: AlertRule): Promise<AlertRule> {
    return firstValueFrom(this.http.put<AlertRule>(`/api/alert-rules/${alertRuleId}`, alertRule));
  }

  deleteAlertRule(alertRuleId: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/alert-rules/${alertRuleId}`));
  }

  getNotificationLogs(take = 12): Promise<NotificationLog[]> {
    return firstValueFrom(this.http.get<NotificationLog[]>(`/api/notification-logs?take=${take}`));
  }

  ingestWorldEvent(worldEvent: WorldEvent): Promise<{ id: string }> {
    return firstValueFrom(this.http.post<{ id: string }>('/api/world-events', worldEvent));
  }

  generateSimulation(alertRules: number, worldEvents: number): Promise<SimulationGenerateResponse> {
    return firstValueFrom(
      this.http.post<SimulationGenerateResponse>(
        `/api/simulation/generate?alertRules=${alertRules}&worldEvents=${worldEvents}`,
        {}
      )
    );
  }
}
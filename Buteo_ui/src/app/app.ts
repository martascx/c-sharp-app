import { Component, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';

type Page = 'Dashboard' | 'Hosts' | 'Security Events' | 'Incidents' | 'Detection Rules';
type Severity = 'Critical' | 'High' | 'Medium' | 'Low';
type IncidentStatus = 'New' | 'Investigating' | 'Resolved';

interface Host {
  hostID: number; hostname: string; ipAddress: string; osType: string;
  hostStatus: boolean; region: string; lastSeen: string;
}
interface SecurityEvent {
  securityEventID: number; eventType: string; hostID: number;
  ipAddress: string; timestamp: string; summary: string;
}
interface Incident {
  incidentId: number; title: string; description: string; severity: Severity;
  status: IncidentStatus; host: string; timestamp: string; rule: string;
}
interface DetectionRule {
  id: number; name: string; description: string; eventType: string;
  threshold: string; severity: Severity; enabled: boolean;
}

@Component({
  selector: 'app-root',
  standalone: true,
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  private readonly http = inject(HttpClient);
  private readonly severityNames: Severity[] = ['Low', 'Medium', 'High', 'Critical'];

  readonly user = signal('');
  readonly loginError = signal('');

  private incidentsLoaded = false;

  constructor() {
    const saved = sessionStorage.getItem('user');
    if (saved) this.start(saved);
  }

  login(name: string, password: string): void {
    this.http.post<{ name: string }>('/api/auth/login', { name, password }).subscribe({
      next: result => {
        this.loginError.set('');
        sessionStorage.setItem('user', result.name);
        this.start(result.name);
      },
      error: () => this.loginError.set('Invalid credentials.'),
    });
  }

  private start(name: string): void {
    this.user.set(name);
    this.loadData();
    setInterval(() => this.loadData(), 5000); // de 5 em 5 segundos dá refresh para atualizar os dados da dashboard
  }

  private fmt(value: string): string {
    return value.replace('T', ' ').slice(0, 16);
  }

  private loadData(): void {
    this.loadIncidents();
    this.http.get<any[]>('/api/hosts').subscribe(list => this.hosts.set(list.map(h => ({ ...h, lastSeen: this.fmt(h.timestamp) }))));
    this.http.get<any[]>('/api/events').subscribe(list => this.events.set(list.map(e => ({ ...e, timestamp: this.fmt(e.timestamp), summary: e.rawPayload }))));
  }

  private loadIncidents(): void {
    const statuses = new Map(this.incidents().map(i => [i.incidentId, i.status]));
    this.http.get<any[]>('/api/incidents').subscribe(list => {
      const loaded: Incident[] = list.map(i => ({
        incidentId: i.incidentId,
        title: i.title,
        description: i.incidentDescription ?? '',
        severity: this.severityNames[i.severity - 1],
        status: statuses.get(i.incidentId) ?? i.status,
        host: `Event #${i.securityEventID}`,
        timestamp: this.fmt(i.timestamp),
        rule: `Rule #${i.detectionRuleID}`,
      }));
      const added = loaded.filter(i => !statuses.has(i.incidentId));
      if (this.incidentsLoaded && added.length) this.showToast(`New incident detected: ${added[0].title}`);
      this.incidentsLoaded = true;
      this.incidents.set(loaded);
    });
  }

  readonly pages: Page[] = ['Dashboard', 'Hosts', 'Security Events', 'Incidents', 'Detection Rules'];
  readonly activePage = signal<Page>('Dashboard');
  readonly searchTerm = signal('');
  readonly severityFilter = signal('All');
  readonly toast = signal('');

  readonly hosts = signal<Host[]>([]);

  readonly events = signal<SecurityEvent[]>([]);

  readonly incidents = signal<Incident[]>([]);

  readonly rules = signal<DetectionRule[]>([
    { id: 1, name: 'Brute-force pattern', description: 'Repeated failed authentication attempts from the same source.', eventType: 'AUTH_FAIL', threshold: '5 attempts / 5 min', severity: 'Critical', enabled: true },
    { id: 2, name: 'Privilege escalation', description: 'Detects unexpected administrative permission changes.', eventType: 'PRIVILEGE_ESCALATION', threshold: 'Any occurrence', severity: 'Critical', enabled: true },
    { id: 3, name: 'Large transaction', description: 'Flags transactions above the configured amount.', eventType: 'BANK_TX', threshold: '€10,000', severity: 'High', enabled: true },
    { id: 4, name: 'Host availability', description: 'Detects monitored hosts that stop reporting.', eventType: 'HOST_STATUS', threshold: '15 min inactive', severity: 'Medium', enabled: true },
  ]);

  readonly severityCounts = computed(() => {
    const open = this.incidents().filter(i => i.status !== 'Resolved');
    return {
      Critical: open.filter(i => i.severity === 'Critical').length,
      High: open.filter(i => i.severity === 'High').length,
      Medium: open.filter(i => i.severity === 'Medium').length,
      Low: open.filter(i => i.severity === 'Low').length,
    };
  });
  readonly donutBackground = computed(() => {
    const counts = this.severityCounts();
    const values = [counts.Critical, counts.High, counts.Medium, counts.Low];
    const total = values.reduce((a, b) => a + b, 0);
    if (!total) return '#edf0f5';
    const colors = ['#d75d61', '#e5a34c', '#5a9ce1', '#a6b1bf'];
    let start = 0;
    const stops = values.map((v, i) => {
      const end = start + (v / total) * 100;
      const stop = `${colors[i]} ${start}% ${end}%`;
      start = end;
      return stop;
    });
    return `conic-gradient(${stops.join(', ')})`;
  });
  readonly openIncidents = computed(() => this.incidents().filter(i => i.status !== 'Resolved').length);
  readonly criticalIncidents = computed(() => this.incidents().filter(i => i.severity === 'Critical' && i.status !== 'Resolved').length);
  readonly onlineHosts = computed(() => this.hosts().filter(h => h.hostStatus).length);
  readonly filteredIncidents = computed(() => this.incidents().filter(i => {
    const matchesSearch = `${i.title} ${i.host} ${i.rule}`.toLowerCase().includes(this.searchTerm().toLowerCase());
    const matchesSeverity = this.severityFilter() === 'All' || i.severity === this.severityFilter();
    return matchesSearch && matchesSeverity;
  }));
  readonly filteredHosts = computed(() => this.hosts().filter(h => `${h.hostname} ${h.ipAddress} ${h.region}`.toLowerCase().includes(this.searchTerm().toLowerCase())));
  readonly filteredEvents = computed(() => this.events().filter(e => `${e.eventType} ${e.ipAddress} ${e.summary}`.toLowerCase().includes(this.searchTerm().toLowerCase())));

  navigate(page: Page): void { this.activePage.set(page); this.searchTerm.set(''); this.severityFilter.set('All'); }
  setSearch(value: string): void { this.searchTerm.set(value); }
  setSeverity(value: string): void { this.severityFilter.set(value); }
  updateIncident(id: number, status: IncidentStatus): void {
    this.incidents.update(items => items.map(item => item.incidentId === id ? { ...item, status } : item));
    this.showToast(`Incident #${id} marked as ${status.toLowerCase()}.`);
  }
  deleteIncident(id: number): void {
    this.http.delete(`/api/incidents/${id}`).subscribe(() => {
      this.incidents.update(items => items.filter(item => item.incidentId !== id));
      this.showToast(`Incident #${id} deleted.`);
    });
  }
  toggleRule(id: number): void {
    this.rules.update(items => items.map(rule => rule.id === id ? { ...rule, enabled: !rule.enabled } : rule));
    const rule = this.rules().find(item => item.id === id);
    this.showToast(`${rule?.name ?? 'Rule'} ${rule?.enabled ? 'enabled' : 'disabled'}.`);
  }
  showToast(message: string): void {
    this.toast.set(message);
    setTimeout(() => this.toast.set(''), 2600);
  }
  hostName(id: number): string { return this.hosts().find(h => h.hostID === id)?.hostname ?? `HOST-${id}`; }
  severityClass(severity: string): string { return severity.toLowerCase(); }
}

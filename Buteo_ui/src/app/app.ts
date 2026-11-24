import { Component, computed, signal } from '@angular/core';

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
  readonly pages: Page[] = ['Dashboard', 'Hosts', 'Security Events', 'Incidents', 'Detection Rules'];
  readonly activePage = signal<Page>('Dashboard');
  readonly searchTerm = signal('');
  readonly severityFilter = signal('All');
  readonly toast = signal('');

  readonly hosts = signal<Host[]>([
    { hostID: 1, hostname: 'BANK-CORE-01', ipAddress: '10.20.1.10', osType: 'Windows Server', hostStatus: true, region: 'Lisbon', lastSeen: 'Just now' },
    { hostID: 2, hostname: 'BANK-AUTH-01', ipAddress: '10.20.1.11', osType: 'Ubuntu Linux', hostStatus: true, region: 'Lisbon', lastSeen: '1 min ago' },
    { hostID: 3, hostname: 'BANK-DB-01', ipAddress: '10.20.2.20', osType: 'Windows Server', hostStatus: true, region: 'Porto', lastSeen: '2 min ago' },
    { hostID: 4, hostname: 'ATM-GW-04', ipAddress: '10.20.4.14', osType: 'Ubuntu Linux', hostStatus: false, region: 'Braga', lastSeen: '18 min ago' },
    { hostID: 5, hostname: 'BANK-WEB-02', ipAddress: '10.20.1.22', osType: 'Ubuntu Linux', hostStatus: true, region: 'Lisbon', lastSeen: 'Just now' },
    { hostID: 6, hostname: 'SWIFT-GW-01', ipAddress: '10.20.5.10', osType: 'Windows Server', hostStatus: true, region: 'Porto', lastSeen: '4 min ago' },
  ]);

  readonly events = signal<SecurityEvent[]>([
    { securityEventID: 1048, eventType: 'AUTH_FAIL', hostID: 2, ipAddress: '185.220.101.14', timestamp: '14:32:08', summary: 'Repeated authentication failures detected' },
    { securityEventID: 1047, eventType: 'BANK_TX', hostID: 1, ipAddress: '10.20.1.10', timestamp: '14:30:51', summary: 'High-value transfer submitted' },
    { securityEventID: 1046, eventType: 'PRIVILEGE_ESCALATION', hostID: 3, ipAddress: '10.20.2.20', timestamp: '14:28:17', summary: 'Unexpected permission change' },
    { securityEventID: 1045, eventType: 'HOST_HEARTBEAT', hostID: 5, ipAddress: '10.20.1.22', timestamp: '14:25:03', summary: 'Host heartbeat received' },
    { securityEventID: 1044, eventType: 'AUTH_FAIL', hostID: 2, ipAddress: '185.220.101.14', timestamp: '14:21:42', summary: 'Authentication attempt rejected' },
    { securityEventID: 1043, eventType: 'HOST_STATUS', hostID: 4, ipAddress: '10.20.4.14', timestamp: '14:18:11', summary: 'Host stopped reporting' },
  ]);

  readonly incidents = signal<Incident[]>([
    { incidentId: 2308, title: 'Repeated authentication failures', description: 'Multiple failed sign-in attempts from an external IP address.', severity: 'Critical', status: 'New', host: 'BANK-AUTH-01', timestamp: '14:32', rule: 'AUTH-001 · Brute-force pattern' },
    { incidentId: 2307, title: 'High-value bank transaction', description: 'A transaction exceeded the configured monitoring threshold.', severity: 'High', status: 'Investigating', host: 'BANK-CORE-01', timestamp: '14:30', rule: 'TX-003 · Large transaction' },
    { incidentId: 2306, title: 'Privilege change detected', description: 'An unexpected administrative permission change was recorded.', severity: 'Critical', status: 'Investigating', host: 'BANK-DB-01', timestamp: '14:28', rule: 'IAM-002 · Privilege escalation' },
    { incidentId: 2305, title: 'Host heartbeat missing', description: 'The host has not reported telemetry within the expected interval.', severity: 'Medium', status: 'New', host: 'ATM-GW-04', timestamp: '14:18', rule: 'HOST-004 · Host availability' },
    { incidentId: 2304, title: 'Unusual login location', description: 'Authentication originated from an uncommon network location.', severity: 'Medium', status: 'Resolved', host: 'BANK-AUTH-01', timestamp: '13:54', rule: 'AUTH-006 · Location anomaly' },
  ]);

  readonly rules = signal<DetectionRule[]>([
    { id: 1, name: 'Brute-force pattern', description: 'Repeated failed authentication attempts from the same source.', eventType: 'AUTH_FAIL', threshold: '5 attempts / 5 min', severity: 'Critical', enabled: true },
    { id: 2, name: 'Privilege escalation', description: 'Detects unexpected administrative permission changes.', eventType: 'PRIVILEGE_ESCALATION', threshold: 'Any occurrence', severity: 'Critical', enabled: true },
    { id: 3, name: 'Large transaction', description: 'Flags transactions above the configured amount.', eventType: 'BANK_TX', threshold: '€10,000', severity: 'High', enabled: true },
    { id: 4, name: 'Host availability', description: 'Detects monitored hosts that stop reporting.', eventType: 'HOST_STATUS', threshold: '15 min inactive', severity: 'Medium', enabled: true },
  ]);

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

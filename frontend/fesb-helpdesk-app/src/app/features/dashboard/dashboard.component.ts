import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { TicketsService } from '../../core/services/tickets.service';
import { AuthService } from '../../core/services/auth.service';
import { TicketListItem, TicketStats } from '../../core/models/models';

const RECENT_TICKETS_LIMIT = 5;

const WEEKDAYS_HR = [
  'Nedjelja',
  'Ponedjeljak',
  'Utorak',
  'Srijeda',
  'Četvrtak',
  'Petak',
  'Subota'
];

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink, DatePipe],
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent {
  private tickets = inject(TicketsService);
  private auth = inject(AuthService);

  loading = signal(true);
  stats = signal<TicketStats | null>(null);
  recent = signal<TicketListItem[]>([]);

  user = this.auth.user;

  welcomeSubtitle = computed(() => {
    const role = this.auth.role();

    if (role === 'student') {
      return 'Ovdje možete pregledati i slati upite referadi ili nastavnicima.';
    }
    if (role === 'referada') {
      return 'Pregled i upravljanje upitima pristiglim referadi.';
    }
    if (role === 'nastavnik') {
      return 'Pregled upita dodijeljenih vašem korisničkom računu.';
    }
    if (role === 'admin') {
      return 'Administracija sustava, upita i kategorija.';
    }
    return '';
  });

  canCreate = computed(() => this.auth.hasRole('student'));

  constructor() {
    this.loadStats();
    this.loadRecentTickets();
  }

  statusClass(status: string): string {
    if (status === 'Novo') {
      return 'status--novo';
    }
    if (status === 'U obradi') {
      return 'status--u-obradi';
    }
    if (status === 'Riješeno') {
      return 'status--rijeseno';
    }
    return '';
  }

  padNum(value: number): string {
    return String(value).padStart(2, '0');
  }

  todayLabel(): string {
    const now = new Date();
    const dayName = WEEKDAYS_HR[now.getDay()];
    const day = String(now.getDate()).padStart(2, '0');
    const month = String(now.getMonth() + 1).padStart(2, '0');
    const year = now.getFullYear();

    return `${dayName}, ${day}.${month}.${year}`;
  }

  private loadStats(): void {
    this.tickets.stats().subscribe({
      next: (response) => this.stats.set(response),
      error: () => {}
    });
  }

  private loadRecentTickets(): void {
    this.tickets.list().subscribe({
      next: (allTickets) => {
        const mostRecent = allTickets.slice(0, RECENT_TICKETS_LIMIT);
        this.recent.set(mostRecent);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }
}

import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { TicketsService } from '../../core/services/tickets.service';
import { CategoriesService } from '../../core/services/categories.service';
import { AuthService } from '../../core/services/auth.service';
import { Category, TicketListItem } from '../../core/models/models';

@Component({
  selector: 'app-ticket-list',
  standalone: true,
  imports: [FormsModule, DatePipe, RouterLink],
  templateUrl: './ticket-list.component.html'
})
export class TicketListComponent {
  private ticketsApi = inject(TicketsService);
  private categoriesApi = inject(CategoriesService);
  private router = inject(Router);
  private auth = inject(AuthService);

  loading = signal(true);
  tickets = signal<TicketListItem[]>([]);
  categories = signal<Category[]>([]);

  statusFilter = signal<string>('');
  categoryFilter = signal<number | null>(null);
  searchTerm = signal<string>('');

  canCreate = computed(() => this.auth.hasRole('student'));

  pageTitle = computed(() => {
    const role = this.auth.role();

    if (role === 'student') {
      return 'Moji upiti';
    }
    if (role === 'referada') {
      return 'Upiti referade';
    }
    if (role === 'nastavnik') {
      return 'Upiti nastavniku';
    }
    if (role === 'admin') {
      return 'Svi upiti';
    }
    return 'Upiti';
  });

  filtered = computed(() => {
    const searchText = this.searchTerm().trim().toLowerCase();

    if (!searchText) {
      return this.tickets();
    }

    return this.tickets().filter((ticket) => {
      const title = ticket.title.toLowerCase();
      const categoryName = ticket.categoryName.toLowerCase();
      const studentName = ticket.studentName.toLowerCase();
      const studentEmail = ticket.studentEmail.toLowerCase();

      return (
        title.includes(searchText) ||
        categoryName.includes(searchText) ||
        studentName.includes(searchText) ||
        studentEmail.includes(searchText)
      );
    });
  });

  constructor() {
    this.load();
    this.loadCategories();
  }

  load(): void {
    this.loading.set(true);

    const filter = {
      status: this.statusFilter() || undefined,
      categoryId: this.categoryFilter() ?? undefined
    };

    this.ticketsApi.list(filter).subscribe({
      next: (list) => {
        this.tickets.set(list);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  resetFilters(): void {
    this.statusFilter.set('');
    this.categoryFilter.set(null);
    this.searchTerm.set('');
    this.load();
  }

  openTicket(id: number): void {
    this.router.navigate(['/upiti', id]);
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

  recipientLabel(ticket: TicketListItem): string {
    if (ticket.recipientType === 'nastavnik') {
      return `Nastavnik: ${ticket.recipientEmail}`;
    }
    return 'Referada';
  }

  private loadCategories(): void {
    this.categoriesApi.list().subscribe({
      next: (list) => this.categories.set(list),
      error: () => {}
    });
  }
}

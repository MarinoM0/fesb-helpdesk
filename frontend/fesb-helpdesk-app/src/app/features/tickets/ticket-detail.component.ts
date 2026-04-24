import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { TicketsService } from '../../core/services/tickets.service';
import { CategoriesService } from '../../core/services/categories.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { Category, TicketDetail, TicketStatus } from '../../core/models/models';

const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

@Component({
  selector: 'app-ticket-detail',
  standalone: true,
  imports: [ReactiveFormsModule, DatePipe, RouterLink],
  templateUrl: './ticket-detail.component.html'
})
export class TicketDetailComponent {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private ticketsApi = inject(TicketsService);
  private categoriesApi = inject(CategoriesService);
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private toast = inject(ToastService);

  loading = signal(true);
  saving = signal(false);
  ticket = signal<TicketDetail | null>(null);
  categories = signal<Category[]>([]);

  statuses: TicketStatus[] = ['Novo', 'U obradi', 'Riješeno'];

  replyForm = this.fb.nonNullable.group({
    message: ['', [Validators.required, Validators.minLength(1)]]
  });

  assignForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]]
  });

  reassignForm = this.fb.nonNullable.group({
    recipientType: this.fb.nonNullable.control<'referada' | 'nastavnik'>('referada', [Validators.required]),
    email: ['']
  });

  role = this.auth.role;

  canReply = computed(() => {
    const ticket = this.ticket();
    const role = this.role();

    if (!ticket || !role) {
      return false;
    }

    if (role === 'student') {
      return ticket.status !== 'Riješeno';
    }

    return true;
  });

  canChangeStatus = computed(() => {
    const role = this.role();
    return role === 'referada' || role === 'nastavnik' || role === 'admin';
  });

  canChangeCategory = computed(() => {
    const role = this.role();
    return role === 'referada' || role === 'admin';
  });

  canAssignNastavnik = computed(() => {
    const role = this.role();
    const ticket = this.ticket();
    return role === 'referada' && ticket?.recipientType === 'referada';
  });

  canManageAsAdmin = computed(() => this.role() === 'admin');

  reassignRecipient = toSignal(this.reassignForm.controls.recipientType.valueChanges, {
    initialValue: this.reassignForm.controls.recipientType.value
  });

  constructor() {
    const ticketId = Number(this.route.snapshot.paramMap.get('id'));
    this.load(ticketId);
    this.loadCategories();
  }

  submitReply(): void {
    const ticket = this.ticket();
    if (!ticket) {
      return;
    }

    if (this.replyForm.invalid) {
      this.replyForm.markAllAsTouched();
      return;
    }

    const message = this.replyForm.controls.message.value;
    this.saving.set(true);

    this.ticketsApi.reply(ticket.id, message).subscribe({
      next: () => {
        this.replyForm.reset({ message: '' });
        this.saving.set(false);
        this.toast.success('Odgovor je poslan.');
        this.load(ticket.id);
      },
      error: () => {
        this.saving.set(false);
      }
    });
  }

  changeStatus(status: TicketStatus): void {
    const ticket = this.ticket();
    if (!ticket || ticket.status === status) {
      return;
    }

    this.ticketsApi.updateStatus(ticket.id, status).subscribe({
      next: () => {
        this.toast.success('Status ažuriran.');
        this.load(ticket.id);
      }
    });
  }

  changeCategory(categoryId: number): void {
    const ticket = this.ticket();
    if (!ticket || ticket.categoryId === categoryId) {
      return;
    }

    this.ticketsApi.updateCategory(ticket.id, categoryId).subscribe({
      next: () => {
        this.toast.success('Kategorija ažurirana.');
        this.load(ticket.id);
      }
    });
  }

  assignNastavnik(): void {
    const ticket = this.ticket();
    if (!ticket) {
      return;
    }

    if (this.assignForm.invalid) {
      this.assignForm.markAllAsTouched();
      return;
    }

    const email = this.assignForm.controls.email.value;

    this.ticketsApi.assignNastavnik(ticket.id, email).subscribe({
      next: () => {
        this.toast.success('Upit je dodijeljen nastavniku.');
        this.assignForm.reset({ email: '' });
        this.load(ticket.id);
      }
    });
  }

  reassignTicket(): void {
    const ticket = this.ticket();
    if (!ticket) {
      return;
    }

    const recipientType = this.reassignForm.controls.recipientType.value;
    const rawEmail = (this.reassignForm.controls.email.value ?? '').trim();

    if (recipientType === 'nastavnik') {
      const emailIsValid = !!rawEmail && EMAIL_REGEX.test(rawEmail);
      if (!emailIsValid) {
        this.reassignForm.controls.email.markAsTouched();
        this.toast.error('Unesi ispravnu email adresu nastavnika.');
        return;
      }
    }

    const finalEmail = recipientType === 'nastavnik' ? rawEmail.toLowerCase() : undefined;

    this.ticketsApi.reassign(ticket.id, recipientType, finalEmail).subscribe({
      next: () => {
        const successMessage = recipientType === 'nastavnik'
          ? 'Upit preusmjeren nastavniku.'
          : 'Upit preusmjeren referadi.';
        this.toast.success(successMessage);
        this.reassignForm.reset({ recipientType: 'referada', email: '' });
        this.load(ticket.id);
      },
      error: (err) => {
        const message = this.buildReassignErrorMessage(err);
        this.toast.error(message);
      }
    });
  }

  deleteTicket(): void {
    const ticket = this.ticket();
    if (!ticket) {
      return;
    }

    const userConfirmed = confirm(`Obrisati upit "${ticket.title}"? Ova akcija ne može se poništiti.`);
    if (!userConfirmed) {
      return;
    }

    this.ticketsApi.remove(ticket.id).subscribe({
      next: () => {
        this.toast.success('Upit je obrisan.');
        this.router.navigate(['/upiti']);
      },
      error: (err) => {
        const message = this.buildDeleteErrorMessage(err);
        this.toast.error(message);
      }
    });
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

  authorLabel(role: string): string {
    if (role === 'student') {
      return 'Student';
    }
    if (role === 'referada') {
      return 'Referada';
    }
    if (role === 'nastavnik') {
      return 'Nastavnik';
    }
    if (role === 'admin') {
      return 'Admin';
    }
    return role;
  }

  private load(id: number): void {
    this.loading.set(true);

    this.ticketsApi.get(id).subscribe({
      next: (ticket) => {
        this.ticket.set(ticket);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.router.navigate(['/upiti']);
      }
    });
  }

  private loadCategories(): void {
    this.categoriesApi.list().subscribe({
      next: (list) => this.categories.set(list),
      error: () => {}
    });
  }

  private buildReassignErrorMessage(err: any): string {
    if (err?.error?.message) {
      return err.error.message;
    }

    if (err?.status === 404) {
      return 'Endpoint nije pronađen — restartaj backend da učita novu rutu.';
    }

    if (err?.status === 403) {
      return 'Nemaš dozvolu za preusmjeravanje.';
    }

    return 'Preusmjeravanje nije uspjelo.';
  }

  private buildDeleteErrorMessage(err: any): string {
    if (err?.error?.message) {
      return err.error.message;
    }

    if (err?.status === 404 || err?.status === 405) {
      return 'Endpoint nije pronađen — restartaj backend da učita novu rutu.';
    }

    if (err?.status === 403) {
      return 'Nemaš dozvolu za brisanje.';
    }

    if (err?.status === 500) {
      return 'Greška na serveru pri brisanju.';
    }

    return 'Brisanje nije uspjelo.';
  }
}

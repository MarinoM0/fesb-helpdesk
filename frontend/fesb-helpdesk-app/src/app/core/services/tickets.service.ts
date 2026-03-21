import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  CreateTicketRequest,
  TicketDetail,
  TicketListItem,
  TicketReply,
  TicketStats,
  TicketStatus
} from '../models/models';

export interface TicketListFilter {
  status?: string;
  categoryId?: number;
  q?: string;
}

@Injectable({ providedIn: 'root' })
export class TicketsService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/tickets`;

  list(filter: TicketListFilter = {}) {
    let params = new HttpParams();

    if (filter.status) {
      params = params.set('status', filter.status);
    }

    if (filter.categoryId) {
      params = params.set('categoryId', filter.categoryId);
    }

    if (filter.q) {
      params = params.set('q', filter.q);
    }

    return this.http.get<TicketListItem[]>(this.baseUrl, { params });
  }

  stats() {
    return this.http.get<TicketStats>(`${this.baseUrl}/stats`);
  }

  get(id: number) {
    return this.http.get<TicketDetail>(`${this.baseUrl}/${id}`);
  }

  create(body: CreateTicketRequest) {
    return this.http.post<TicketDetail>(this.baseUrl, body);
  }

  reply(id: number, message: string) {
    const url = `${this.baseUrl}/${id}/replies`;
    return this.http.post<TicketReply>(url, { message });
  }

  updateStatus(id: number, status: TicketStatus) {
    const url = `${this.baseUrl}/${id}/status`;
    return this.http.put<void>(url, { status });
  }

  updateCategory(id: number, categoryId: number) {
    const url = `${this.baseUrl}/${id}/category`;
    return this.http.put<void>(url, { categoryId });
  }

  assignNastavnik(id: number, nastavnikEmail: string) {
    const url = `${this.baseUrl}/${id}/assign-nastavnik`;
    return this.http.put<void>(url, { nastavnikEmail });
  }

  reassign(id: number, recipientType: 'referada' | 'nastavnik', recipientEmail?: string) {
    const url = `${this.baseUrl}/${id}/reassign`;
    const body = {
      recipientType,
      recipientEmail: recipientEmail ?? null
    };
    return this.http.put<void>(url, body);
  }

  remove(id: number) {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}

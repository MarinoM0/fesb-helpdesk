import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Category } from '../models/models';

@Injectable({ providedIn: 'root' })
export class CategoriesService {
  private http = inject(HttpClient);
  private url = `${environment.apiUrl}/categories`;

  list() {
    return this.http.get<Category[]>(this.url);
  }

  create(body: { name: string; description?: string | null }) {
    return this.http.post<Category>(this.url, body);
  }

  update(id: number, body: { name: string; description?: string | null }) {
    return this.http.put<Category>(`${this.url}/${id}`, body);
  }

  remove(id: number) {
    return this.http.delete<void>(`${this.url}/${id}`);
  }
}

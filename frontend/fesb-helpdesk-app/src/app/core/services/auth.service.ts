import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest, RegisterRequest, User, UserRole } from '../models/models';

const TOKEN_KEY = 'fesb_helpdesk_token';
const USER_KEY = 'fesb_helpdesk_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  private readonly userSignal = signal<User | null>(this.loadStoredUser());
  readonly user = this.userSignal.asReadonly();
  readonly isLoggedIn = computed(() => this.userSignal() !== null);
  readonly role = computed(() => this.userSignal()?.role ?? null);

  login(body: LoginRequest) {
    const url = `${environment.apiUrl}/auth/login`;
    return this.http.post<AuthResponse>(url, body).pipe(
      tap((response) => this.storeAuth(response))
    );
  }

  register(body: RegisterRequest) {
    const url = `${environment.apiUrl}/auth/register`;
    return this.http.post<AuthResponse>(url, body).pipe(
      tap((response) => this.storeAuth(response))
    );
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this.userSignal.set(null);
    this.router.navigate(['/prijava']);
  }

  token(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  hasRole(...roles: UserRole[]): boolean {
    const currentRole = this.userSignal()?.role;
    if (currentRole === undefined) {
      return false;
    }
    return roles.includes(currentRole);
  }

  private storeAuth(response: AuthResponse): void {
    localStorage.setItem(TOKEN_KEY, response.token);
    localStorage.setItem(USER_KEY, JSON.stringify(response.user));
    this.userSignal.set(response.user);
  }

  private loadStoredUser(): User | null {
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) {
      return null;
    }
    try {
      return JSON.parse(raw) as User;
    } catch {
      return null;
    }
  }
}

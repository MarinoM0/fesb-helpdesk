import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { ToastService } from '../services/toast.service';

const AUTH_ENDPOINT_REGEX = /\/auth\/(login|register)\b/;

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const toast = inject(ToastService);

  const token = auth.token();
  const requestWithAuth = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  const isAuthEndpoint = AUTH_ENDPOINT_REGEX.test(req.url);

  return next(requestWithAuth).pipe(
    catchError((error: HttpErrorResponse) => {
      if (isAuthEndpoint) {
        return throwError(() => error);
      }

      handleGlobalError(error, auth, router, toast);
      return throwError(() => error);
    })
  );
};

function handleGlobalError(
  error: HttpErrorResponse,
  auth: AuthService,
  router: Router,
  toast: ToastService
): void {
  const status = error?.status ?? 0;

  if (status === 401) {
    auth.logout();
    router.navigate(['/prijava']);
    toast.error('Vaša prijava je istekla. Prijavite se ponovno.');
    return;
  }

  if (status === 403) {
    toast.error('Nemate ovlasti za ovu akciju.');
    return;
  }

  if (status >= 500) {
    toast.error('Došlo je do greške na poslužitelju.');
    return;
  }

  const serverMessage = error?.error?.message;
  if (serverMessage) {
    toast.error(serverMessage);
  }
}

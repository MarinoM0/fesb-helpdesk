import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './auth.scss'
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);
  private toast = inject(ToastService);

  loading = signal(false);

  form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  submit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    const credentials = this.form.getRawValue();

    this.auth.login(credentials).subscribe({
      next: () => {
        this.loading.set(false);
        this.toast.success('Uspješna prijava.');
        this.router.navigate(['/nadzorna-ploca']);
      },
      error: (err) => {
        this.loading.set(false);
        const message = this.buildErrorMessage(err);
        this.toast.error(message);
      }
    });
  }

  hasError(field: 'email' | 'password', error: string) {
    const control = this.form.get(field);
    if (!control) {
      return false;
    }
    return control.touched && control.hasError(error);
  }

  private buildErrorMessage(err: any): string {
    const status = err?.status;

    if (status === 401 || status === 400) {
      return 'Pogrešan email ili lozinka.';
    }

    if (status === 0) {
      return 'Poslužitelj nije dostupan. Provjeri internetsku vezu.';
    }

    return err?.error?.message ?? 'Prijava nije uspjela. Pokušaj ponovno.';
  }
}

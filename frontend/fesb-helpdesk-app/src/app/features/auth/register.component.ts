import { Component, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';

function fesbEmailValidator(control: AbstractControl): ValidationErrors | null {
  const rawValue = (control.value ?? '').toString().trim().toLowerCase();

  if (!rawValue) {
    return null;
  }

  if (rawValue.endsWith('@fesb.hr')) {
    return null;
  }

  return { notFesb: true };
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './auth.scss'
})
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);
  private toast = inject(ToastService);

  loading = signal(false);

  form = this.fb.nonNullable.group({
    firstName: ['', [Validators.required, Validators.minLength(2)]],
    lastName: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email, fesbEmailValidator]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  submit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    const formValue = this.form.getRawValue();

    this.auth.register(formValue).subscribe({
      next: () => {
        this.loading.set(false);
        this.toast.success('Registracija uspješna.');
        this.router.navigate(['/nadzorna-ploca']);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  hasError(field: string, error: string) {
    const control = this.form.get(field);
    if (!control) {
      return false;
    }
    return control.touched && control.hasError(error);
  }
}

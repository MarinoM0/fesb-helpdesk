import { Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TicketsService } from '../../core/services/tickets.service';
import { ToastService } from '../../core/services/toast.service';

const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

function emailIfProvided(control: AbstractControl): ValidationErrors | null {
  const value = (control.value ?? '').toString().trim();

  if (!value) {
    return null;
  }

  if (EMAIL_REGEX.test(value)) {
    return null;
  }

  return { email: true };
}

@Component({
  selector: 'app-ticket-create',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './ticket-create.component.html'
})
export class TicketCreateComponent {
  private fb = inject(FormBuilder);
  private ticketsApi = inject(TicketsService);
  private router = inject(Router);
  private toast = inject(ToastService);

  loading = signal(false);

  form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(200)]],
    description: ['', [Validators.required, Validators.minLength(10)]],
    recipientType: ['referada' as 'referada' | 'nastavnik', [Validators.required]],
    recipientEmail: ['', [emailIfProvided]]
  });

  recipientType = toSignal(this.form.controls.recipientType.valueChanges, {
    initialValue: this.form.controls.recipientType.value
  });

  constructor() {
    this.form.controls.recipientType.valueChanges.subscribe((newValue) => {
      this.updateRecipientEmailValidators(newValue);
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const formValue = this.form.getRawValue();
    const isNastavnik = formValue.recipientType === 'nastavnik';

    const payload = {
      title: formValue.title,
      description: formValue.description,
      recipientType: formValue.recipientType,
      recipientEmail: isNastavnik ? formValue.recipientEmail : null
    };

    this.loading.set(true);
    this.ticketsApi.create(payload).subscribe({
      next: (createdTicket) => {
        this.loading.set(false);
        this.toast.success('Upit je uspješno poslan.');
        this.router.navigate(['/upiti', createdTicket.id]);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  hasError(field: string, error: string): boolean {
    const control = this.form.get(field);
    if (!control) {
      return false;
    }
    return control.touched && control.hasError(error);
  }

  private updateRecipientEmailValidators(recipientType: 'referada' | 'nastavnik'): void {
    const emailControl = this.form.controls.recipientEmail;

    if (recipientType === 'nastavnik') {
      emailControl.setValidators([Validators.required, emailIfProvided]);
    } else {
      emailControl.setValidators([emailIfProvided]);
      emailControl.setValue('');
    }

    emailControl.updateValueAndValidity();
  }
}

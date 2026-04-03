import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CategoriesService } from '../../core/services/categories.service';
import { ToastService } from '../../core/services/toast.service';
import { Category } from '../../core/models/models';

const PROTECTED_CATEGORY_NAME = 'ostalo';

@Component({
  selector: 'app-categories-admin',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './categories-admin.component.html'
})
export class CategoriesAdminComponent {
  private categoriesApi = inject(CategoriesService);
  private fb = inject(FormBuilder);
  private toast = inject(ToastService);

  loading = signal(true);
  saving = signal(false);
  categories = signal<Category[]>([]);
  editingId = signal<number | null>(null);

  form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
    description: ['', [Validators.maxLength(500)]]
  });

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.categoriesApi.list().subscribe({
      next: (list) => {
        this.categories.set(list);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  startEdit(category: Category): void {
    this.editingId.set(category.id);
    this.form.reset({
      name: category.name,
      description: category.description ?? ''
    });
  }

  cancelEdit(): void {
    this.editingId.set(null);
    this.form.reset({ name: '', description: '' });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const body = {
      name: this.form.controls.name.value.trim(),
      description: this.form.controls.description.value?.trim() || null
    };

    this.saving.set(true);
    const editingId = this.editingId();
    const isEditing = editingId !== null;

    const request$ = isEditing
      ? this.categoriesApi.update(editingId, body)
      : this.categoriesApi.create(body);

    request$.subscribe({
      next: () => {
        this.saving.set(false);
        const successMessage = isEditing
          ? 'Kategorija ažurirana.'
          : 'Kategorija dodana.';
        this.toast.success(successMessage);
        this.cancelEdit();
        this.load();
      },
      error: () => {
        this.saving.set(false);
      }
    });
  }

  remove(category: Category): void {
    const userConfirmed = confirm(`Obrisati kategoriju "${category.name}"?`);
    if (!userConfirmed) {
      return;
    }

    this.categoriesApi.remove(category.id).subscribe({
      next: () => {
        this.toast.success('Kategorija obrisana.');
        this.load();
      }
    });
  }

  hasError(field: 'name' | 'description', error: string): boolean {
    const control = this.form.get(field);
    if (!control) {
      return false;
    }
    return control.touched && control.hasError(error);
  }

  isProtected(category: Category): boolean {
    return category.name.toLowerCase() === PROTECTED_CATEGORY_NAME;
  }
}

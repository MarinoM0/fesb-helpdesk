import { Injectable, signal } from '@angular/core';

export interface ToastMessage {
  id: number;
  text: string;
  type: 'success' | 'error' | 'info';
}

const AUTO_DISMISS_MS = 4000;

@Injectable({ providedIn: 'root' })
export class ToastService {
  private nextId = 0;
  readonly messages = signal<ToastMessage[]>([]);

  success(text: string): void {
    this.push(text, 'success');
  }

  error(text: string): void {
    this.push(text, 'error');
  }

  info(text: string): void {
    this.push(text, 'info');
  }

  dismiss(id: number): void {
    this.messages.update((list) => list.filter((m) => m.id !== id));
  }

  private push(text: string, type: ToastMessage['type']): void {
    this.nextId = this.nextId + 1;
    const id = this.nextId;
    const newMessage: ToastMessage = { id, text, type };

    this.messages.update((list) => [...list, newMessage]);
    setTimeout(() => this.dismiss(id), AUTO_DISMISS_MS);
  }
}

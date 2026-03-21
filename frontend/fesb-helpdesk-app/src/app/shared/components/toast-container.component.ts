import { Component, inject } from '@angular/core';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  template: `
    <div class="toast-container">
      @for (msg of toast.messages(); track msg.id) {
        <div class="toast" [class]="'toast--' + msg.type" (click)="toast.dismiss(msg.id)">
          <span class="toast__dot" aria-hidden="true"></span>
          <span class="toast__body">{{ msg.text }}</span>
          <span class="toast__close" aria-hidden="true">×</span>
        </div>
      }
    </div>
  `,
  styles: [`
    .toast-container {
      position: fixed;
      top: 1.5rem;
      right: 1.5rem;
      display: flex;
      flex-direction: column;
      gap: 0.55rem;
      z-index: 1000;
    }
    @media (max-width: 720px) {
      .toast-container {
        top: 0.75rem;
        right: 0.75rem;
        left: 0.75rem;
      }
    }
    .toast {
      display: grid;
      grid-template-columns: auto 1fr auto;
      align-items: center;
      gap: 0.75rem;
      padding: 0.8rem 0.95rem 0.8rem 1rem;
      border-radius: 12px;
      background: #18181b;
      color: #f8f6f1;
      min-width: 280px;
      max-width: 420px;
      font-family: 'Geist', ui-sans-serif, sans-serif;
      font-weight: 500;
      font-size: 0.84rem;
      letter-spacing: -0.008em;
      cursor: pointer;
      border: 1px solid rgba(255, 255, 255, 0.06);
      box-shadow:
        0 1px 2px rgba(0, 0, 0, 0.1),
        0 16px 40px rgba(0, 0, 0, 0.22),
        inset 0 1px 0 rgba(255, 255, 255, 0.06);
      animation: toastIn 0.45s cubic-bezier(0.16, 1, 0.3, 1);
      transition: transform 0.18s ease, box-shadow 0.18s ease;
    }
    .toast:hover {
      transform: translateY(-1px);
      box-shadow:
        0 2px 4px rgba(0, 0, 0, 0.12),
        0 22px 48px rgba(0, 0, 0, 0.26),
        inset 0 1px 0 rgba(255, 255, 255, 0.06);
    }
    .toast__dot {
      width: 8px;
      height: 8px;
      border-radius: 50%;
      background: currentColor;
      flex-shrink: 0;
      box-shadow: 0 0 0 3px color-mix(in srgb, currentColor 15%, transparent);
    }
    .toast__body { min-width: 0; color: #f8f6f1; line-height: 1.4; }
    .toast__close {
      font-family: 'Geist Mono', ui-monospace, monospace;
      font-size: 1rem;
      line-height: 1;
      opacity: 0.45;
      padding: 0 0.2rem;
      transition: opacity 0.15s ease;
    }
    .toast:hover .toast__close { opacity: 0.8; }
    .toast--success { color: #4ade80; }
    .toast--error { color: #f87171; }
    .toast--info { color: #60a5fa; }
    @keyframes toastIn {
      from { transform: translateX(32px) scale(0.96); opacity: 0; }
      to { transform: translateX(0) scale(1); opacity: 1; }
    }
    @media (max-width: 720px) {
      .toast { min-width: 0; max-width: none; width: 100%; }
    }
  `]
})
export class ToastContainerComponent {
  toast = inject(ToastService);
}

import { Component, computed, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs/operators';
import { AuthService } from '../core/services/auth.service';
import { UserRole } from '../core/models/models';

interface NavItem {
  label: string;
  path: string;
  icon: string;
  roles: UserRole[];
}

const NAV_ITEMS: NavItem[] = [
  {
    label: 'Nadzorna ploča',
    path: '/nadzorna-ploca',
    icon: 'fa-solid fa-gauge-high',
    roles: ['student', 'referada', 'nastavnik', 'admin']
  },
  {
    label: 'Moji upiti',
    path: '/upiti',
    icon: 'fa-solid fa-ticket',
    roles: ['student']
  },
  {
    label: 'Novi upit',
    path: '/upiti/novi',
    icon: 'fa-solid fa-pen-to-square',
    roles: ['student']
  },
  {
    label: 'Upiti referade',
    path: '/upiti',
    icon: 'fa-solid fa-inbox',
    roles: ['referada']
  },
  {
    label: 'Moji upiti',
    path: '/upiti',
    icon: 'fa-solid fa-ticket',
    roles: ['nastavnik']
  },
  {
    label: 'Svi upiti',
    path: '/upiti',
    icon: 'fa-solid fa-list-check',
    roles: ['admin']
  },
  {
    label: 'Kategorije',
    path: '/administracija/kategorije',
    icon: 'fa-solid fa-tags',
    roles: ['admin']
  }
];

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss'
})
export class ShellComponent {
  private auth = inject(AuthService);
  private router = inject(Router);

  user = this.auth.user;
  mobileNavOpen = signal(false);
  navigation: NavItem[] = NAV_ITEMS;

  roleLabel = computed(() => {
    const role = this.auth.role();

    if (role === 'student') {
      return 'Student';
    }
    if (role === 'referada') {
      return 'Referada';
    }
    if (role === 'nastavnik') {
      return 'Nastavnik';
    }
    if (role === 'admin') {
      return 'Administrator';
    }
    return '';
  });

  visibleNav = computed(() => {
    const role = this.auth.role();
    if (!role) {
      return [];
    }
    return this.navigation.filter((item) => item.roles.includes(role));
  });

  constructor() {
    this.closeNavOnRouteChange();
  }

  toggleNav(): void {
    this.mobileNavOpen.update((isOpen) => !isOpen);
  }

  closeNav(): void {
    this.mobileNavOpen.set(false);
  }

  logout(): void {
    this.auth.logout();
  }

  private closeNavOnRouteChange(): void {
    this.router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe(() => this.mobileNavOpen.set(false));
  }
}

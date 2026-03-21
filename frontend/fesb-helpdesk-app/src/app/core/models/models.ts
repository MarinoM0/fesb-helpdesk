export type UserRole = 'student' | 'referada' | 'nastavnik' | 'admin';
export type TicketStatus = 'Novo' | 'U obradi' | 'Riješeno';
export type RecipientType = 'referada' | 'nastavnik';

export interface User {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  role: UserRole;
}

export interface AuthResponse {
  token: string;
  user: User;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

export interface Category {
  id: number;
  name: string;
  description?: string | null;
}

export interface TicketListItem {
  id: number;
  title: string;
  status: TicketStatus;
  recipientType: RecipientType;
  recipientEmail?: string | null;
  categoryName: string;
  studentName: string;
  studentEmail: string;
  createdAt: string;
  updatedAt: string;
}

export interface TicketReply {
  id: number;
  authorId: number;
  authorName: string;
  authorRole: UserRole;
  message: string;
  createdAt: string;
}

export interface TicketDetail extends TicketListItem {
  categoryId: number;
  description: string;
  replies: TicketReply[];
}

export interface CreateTicketRequest {
  title: string;
  description: string;
  recipientType: RecipientType;
  recipientEmail?: string | null;
}

export interface TicketStats {
  total: number;
  novo: number;
  uObradi: number;
  rijeseno: number;
}

// Core Types matching Backend DTOs

export type ResourceTypeCode = 'OPEN_WORKSPACE' | 'CLOSED_OFFICE' | 'MEETING_ROOM';
export type ReservationStatus = 'CONFIRMED' | 'CHECKED_IN' | 'CHECKED_OUT' | 'CANCELLED' | 'COMPLETED' | 'NOT_CHECKED_IN' | 'REJECTED';
export type BusinessProfileCode = 'COLLABORATOR' | 'ASSOCIATE' | 'LEADER' | 'DIRECTOR' | 'PARTNER';
export type ApplicationRoleCode = 'USER' | 'ROOM_ADMIN' | 'SUPPORT' | 'GLOBAL_ADMIN';
export type NotificationType = 'RESERVATION_CREATED' | 'RESERVATION_MODIFIED' | 'RESERVATION_CANCELLED' | 'RESERVATION_REMINDER';
export type NotificationStatus = 'PENDING' | 'SENT' | 'FAILED' | 'CANCELLED';
export type CheckInMethod = 'QR';

export interface ResourceType {
  code: ResourceTypeCode;
  name: string;
  qrRequired: boolean;
  checkinRequired: boolean;
  active: boolean;
}

export interface Location {
  id: string;
  code: string;
  name: string;
  city: string;
  country: string;
  timezone: string;
  active: boolean;
}

export interface Floor {
  id: string;
  locationId: string;
  floorNumber: number;
  code: string;
  name: string;
  active: boolean;
}

export interface Zone {
  id: string;
  floorId: string;
  code: string;
  name: string;
  active: boolean;
}

export interface Resource {
  id: string;
  code: string;
  name: string;
  resourceTypeCode: ResourceTypeCode;
  resourceType?: ResourceType;
  locationId: string;
  location?: Location;
  floorId: string;
  floor?: Floor;
  zoneId?: string;
  zone?: Zone;
  capacity: number;
  publicQrId?: string;
  qrVersion: number;
  active: boolean;
  reservable: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface User {
  id: string;
  entraObjectId: string;
  email: string;
  displayName: string;
  jobTitle?: string;
  department?: string;
  active: boolean;
  lastLoginAt?: string;
  roles: ApplicationRoleCode[];
  businessProfiles: BusinessProfileCode[];
  permissions?: Record<string, boolean>;
}

export interface BusinessProfile {
  code: BusinessProfileCode;
  name: string;
  active: boolean;
}

export interface ApplicationRole {
  code: ApplicationRoleCode;
  name: string;
  description?: string;
  active: boolean;
}

export interface ResourceAccessPolicy {
  id: string;
  resourceTypeCode: ResourceTypeCode;
  businessProfileCode: BusinessProfileCode;
  canView: boolean;
  canReserve: boolean;
  canModifyOwn: boolean;
  active: boolean;
}

export interface ReservationException {
  id: string;
  userId: string;
  maximumFutureActiveReservations: number;
  appliesToResourceTypeCode?: ResourceTypeCode;
  validFrom: string;
  expiresAt: string;
  reason: string;
  active: boolean;
  createdByUserId: string;
  createdAt: string;
  updatedAt: string;
}

export interface Reservation {
  id: string;
  resourceId: string;
  resource?: Resource;
  userId: string;
  user?: User;
  createdByUserId: string;
  reservationDate: string;
  startTime: string;
  endTime: string;
  status: ReservationStatus;
  title?: string;
  description?: string;
  attendeeCount?: number;
  supportChangeReason?: string;
  checkedInAt?: string;
  checkedOutAt?: string;
  cancelledAt?: string;
  cancelledByUserId?: string;
  cancellationReason?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CheckIn {
  id: string;
  reservationId: string;
  resourceId: string;
  userId: string;
  method: CheckInMethod;
  scannedPublicQrId: string;
  checkedInAt: string;
  ipAddress?: string;
  userAgent?: string;
}

export interface NotificationOutbox {
  id: string;
  reservationId?: string;
  recipientUserId: string;
  recipientEmail: string;
  type: NotificationType;
  subject: string;
  body: string;
  scheduledAt: string;
  sentAt?: string;
  status: NotificationStatus;
  retryCount: number;
  lastError?: string;
}

export interface AuditLog {
  id: string;
  actorUserId?: string;
  action: string;
  entityName: string;
  entityId?: string;
  beforeValue?: Record<string, any>;
  afterValue?: Record<string, any>;
  reason?: string;
  ipAddress?: string;
  userAgent?: string;
  correlationId?: string;
  createdAt: string;
}

export interface AppSettings {
  id: string;
  maximumFutureActiveReservations: number;
  maximumAdvanceDays?: number;
  minimumDurationMinutes: number;
  latestEndTime: string;
  reminderMinutesBefore: number;
  allowCrossDayBooking: boolean;
  showOccupantNameToUsers: boolean;
}

// API Request/Response Types

export interface CreateReservationRequest {
  resourceId: string;
  reservationDate: string;
  startTime: string;
  endTime: string;
  title?: string;
  description?: string;
  attendeeCount?: number;
}

export interface ModifyReservationRequest {
  reservationDate?: string;
  startTime?: string;
  endTime?: string;
  title?: string;
  description?: string;
  attendeeCount?: number;
  supportChangeReason?: string;
}

export interface AvailabilitySearchParams {
  resourceTypeCode?: ResourceTypeCode;
  floorId?: string;
  zoneId?: string;
  date: string;
  startTime: string;
  endTime: string;
  capacity?: number;
}

export interface AvailabilityResult {
  resourceId: string;
  resourceCode: string;
  resourceName: string;
  resourceTypeCode: ResourceTypeCode;
  floorId: string;
  floorName: string;
  zoneId?: string;
  zoneName?: string;
  capacity: number;
  available: boolean;
}

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface ReservationQueryParams {
  page?: number;
  pageSize?: number;
  status?: ReservationStatus;
  resourceTypeCode?: ResourceTypeCode;
  floorId?: string;
  dateFrom?: string;
  dateTo?: string;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}

export interface ResourceQueryParams {
  page?: number;
  pageSize?: number;
  resourceTypeCode?: ResourceTypeCode;
  floorId?: string;
  zoneId?: string;
  active?: boolean;
  reservable?: boolean;
  search?: string;
}

export interface AuditLogQueryParams {
  page?: number;
  pageSize?: number;
  actorUserId?: string;
  action?: string;
  entityName?: string;
  entityId?: string;
  dateFrom?: string;
  dateTo?: string;
}

// UI State Types

export interface ResourceCardProps {
  resource: Resource;
  onReserve?: (resource: Resource) => void;
  onViewDetails?: (resource: Resource) => void;
  showAvailability?: boolean;
  availability?: AvailabilityResult;
}

export interface ReservationCardProps {
  reservation: Reservation;
  onModify?: (reservation: Reservation) => void;
  onCancel?: (reservation: Reservation) => void;
  onCheckIn?: (reservation: Reservation) => void;
  showActions?: boolean;
  compact?: boolean;
}

export interface TimeSlot {
  start: string;
  end: string;
  available: boolean;
  resourceId?: string;
}

export interface CalendarDay {
  date: Date;
  isCurrentMonth: boolean;
  isToday: boolean;
  isSelected: boolean;
  isDisabled: boolean;
  hasReservations: boolean;
  reservationCount: number;
}

// Form Types

export interface ReservationFormData {
  resourceId: string;
  reservationDate: Date | null;
  startTime: string;
  endTime: string;
  title: string;
  description: string;
  attendeeCount: number | null;
}

export interface ProfileFormData {
  displayName: string;
  jobTitle: string;
  department: string;
  email: string;
}

// Theme Types

export type ThemeMode = 'light' | 'dark';

export interface ThemeContextType {
  mode: ThemeMode;
  toggleTheme: () => void;
  setMode: (mode: ThemeMode) => void;
}
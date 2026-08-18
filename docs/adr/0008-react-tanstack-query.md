# ADR-0008: React with TanStack Query for State Management

## Status
Accepted

## Context
Frontend state management needs:
- Server state (reservations, resources, availability, user profile)
- Client state (auth user, permissions, theme, UI modals)
- Form state (reservation creation, filters)
- Caching, background refetching, optimistic updates
- TypeScript integration
- Minimal boilerplate

## Decision
**TanStack Query (React Query) v5** for server state + **React Context** for client state.

### State Classification

| State Type | Examples | Solution |
|------------|----------|----------|
| **Server State** | Reservations, Resources, Availability, User Profile, Audit Logs | TanStack Query |
| **Client State** | Auth User, Roles/Permissions, Theme, Sidebar Open, Toasts | React Context |
| **Form State** | Reservation Form, Filters, Search | React Hook Form + Zod |
| **URL State** | Current page, filters in URL, deep links | React Router (search params) |

### TanStack Query Configuration

```tsx
// queryClient.ts
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 1000 * 60 * 2,      // 2 min fresh
      gcTime: 1000 * 60 * 10,        // 10 min cache
      retry: (failureCount, error) => {
        if (error instanceof ApiError && error.status === 401) return false;
        return failureCount < 3;
      },
      refetchOnWindowFocus: false,
      refetchOnReconnect: true,
    },
  },
});
```

### Query Keys (Structured)

```tsx
// queryKeys.ts
export const queryKeys = {
  resources: {
    all: ['resources'] as const,
    list: (filters: ResourceFilters) => [...queryKeys.resources.all, 'list', filters] as const,
    availability: (params: AvailabilityParams) => 
      [...queryKeys.resources.all, 'availability', params] as const,
    detail: (id: string) => [...queryKeys.resources.all, 'detail', id] as const,
  },
  reservations: {
    all: ['reservations'] as const,
    mine: (params: MyReservationsParams) => 
      [...queryKeys.reservations.all, 'mine', params] as const,
    detail: (id: string) => [...queryKeys.reservations.all, 'detail', id] as const,
  },
  auth: {
    me: ['auth', 'me'] as const,
    permissions: ['auth', 'permissions'] as const,
  },
};
```

### Usage Patterns

```tsx
// Query - Server State
const { data: resources, isLoading } = useQuery({
  queryKey: queryKeys.resources.list(filters),
  queryFn: () => api.resources.list(filters),
});

// Mutation - Optimistic Update
const mutation = useMutation({
  mutationFn: api.reservations.create,
  onMutate: async (newReservation) => {
    await queryClient.cancelQueries({ queryKey: queryKeys.reservations.mine() });
    const previous = queryClient.getQueryData(queryKeys.reservations.mine());
    queryClient.setQueryData(queryKeys.reservations.mine(), (old) => 
      [...old, { ...newReservation, status: 'CONFIRMED', id: 'temp-' + Date.now() }]
    );
    return { previous };
  },
  onError: (err, vars, context) => {
    queryClient.setQueryData(queryKeys.reservations.mine(), context.previous);
  },
  onSettled: () => {
    queryClient.invalidateQueries({ queryKey: queryKeys.reservations.mine() });
  },
});
```

### React Context for Client State

```tsx
// AuthContext.tsx
interface AuthState {
  user: User | null;
  permissions: string[];
  roles: string[];
  login: (redirectUrl?: string) => void;
  logout: () => void;
  hasRole: (role: string) => boolean;
  canReserve: (resourceType: ResourceType) => boolean;
}

// ThemeContext.tsx
interface ThemeState {
  mode: 'light' | 'dark';
  toggleTheme: () => void;
}
```

## Consequences

### Positive
- **Server State Excellence**: Caching, deduping, background updates, optimistic UI
- **Type Safety**: Query keys typed, inference from queryFn
- **DevTools**: Built-in debugging, cache inspection
- **No Boilerplate**: No Redux actions/reducers/selectors
- **Bundle Size**: ~13KB (vs Redux Toolkit ~25KB + RTK Query)

### Negative
- **Learning Curve**: Stale-while-revalidate mental model
- **Over-fetching Risk**: Must design query keys carefully
- **Testing**: Requires `QueryClientProvider` wrapper in tests

### Neutral
- React Context for low-frequency global state (auth, theme)
- No global store for UI state (modals, sidebars) - local state preferred

## Alternatives Considered

1. **Redux Toolkit + RTK Query**
   - Rejected: More boilerplate, larger bundle, same capabilities

2. **Zustand / Jotai / Recoil**
   - Rejected: Great for client state, poor for server state (no caching/deduping)

3. **SWR (Vercel)**
   - Rejected: Similar to TanStack Query, but less features (mutations, devtools)

4. **Apollo Client / Urql**
   - Rejected: GraphQL-specific, overkill for REST API

## References
- [TanStack Query v5 Docs](https://tanstack.com/query/latest)
- [Server State vs Client State](https://tkdodo.eu/blog/client-state-and-server-state)
- [React Query Essentials](https://tkdodo.eu/blog/react-query-essentials)
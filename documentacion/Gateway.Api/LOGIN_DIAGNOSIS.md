# Login flow diagnosis

## Observed flow
- **Login view**: `src/Gateway.Blazor/Pages/Login.razor` uses `<EditForm>` with `HandleLogin` to call `AuthService.LoginAsync`, then attempts to navigate to `/Home` after authentication state updates.
- **Frontend auth service**: `src/Gateway.Blazor/Services/AuthService.cs` posts credentials to `/api/auth/login`, parses the JSON, stores the token/user in `localStorage`, and sets `_currentUser` for `IsAuthenticated()`.
- **Backend endpoint**: `src/Gateway.Api/Program.cs` maps `POST /api/auth/login` to `AuthService.LoginAsync`, which returns a `LoginResponse` containing a JWT token and `UserDto` (enum `UserRole` for `Role`).

## Likely failure point
- The backend serializes `UserDto.Role` as a numeric enum value (default `System.Text.Json` behavior). The frontend strictly calls `roleElement.GetString()` when parsing the login response. If the response contains a number, `GetString()` throws an `InvalidOperationException`, preventing `_currentUser` from being set and leaving `IsAuthenticated()` false, so navigation to `/Home` never happens.

## Minimal fix options (choose one)
1. **Backend**: serialize enums as strings by adding `JsonStringEnumConverter` to API JSON options so `role` arrives as text.
2. **Frontend**: tolerate numeric roles by checking `roleElement.ValueKind` and converting numbers with `roleElement.GetInt32().ToString()` before building `UserInfo`.

Both options are low-impact; pick the side you prefer to keep enum/string alignment consistent with other endpoints.

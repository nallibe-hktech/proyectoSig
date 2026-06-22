# A3 INNUVA Nóminas — OAuth 2.0 Authorization Code Flow

## Overview

This document explains how to test the Wolters Kluwer OAuth 2.0 integration for A3 INNUVA Nóminas synchronization.

### Architecture

The system implements **Authorization Code Flow with PKCE** (RFC 7636) for security:

```
┌─────────────────────────────────────────────────────────────┐
│ USER (Browser)                                              │
└────────────┬────────────────────────────────────────────────┘
             │ 1. Click "Authorize with Wolters Kluwer"
             │
┌────────────▼────────────────────────────────────────────────┐
│ Frontend (Angular)                                          │
│ - Calls GET /api/a3-innuva-nominas/oauth/authorize-url     │
│ - Gets authorize URL                                        │
│ - Opens in browser (user authorizes)                        │
└────────────┬────────────────────────────────────────────────┘
             │ 2. User logs in & grants permission
             │
┌────────────▼────────────────────────────────────────────────┐
│ Wolters Kluwer OAuth Provider                              │
│ https://login.wolterskluwer.eu/auth/core/connect/authorize │
│ - User authorizes                                           │
│ - Redirects to callback with authorization code            │
└────────────┬────────────────────────────────────────────────┘
             │ 3. Redirect with code
             │
┌────────────▼────────────────────────────────────────────────┐
│ Frontend Callback Handler                                   │
│ http://localhost:4200/a3-innuva/oauth-callback             │
│ - Extracts code from URL                                    │
│ - Calls POST /api/a3-innuva-nominas/oauth/callback?code=X  │
└────────────┬────────────────────────────────────────────────┘
             │ 4. Send code to backend
             │
┌────────────▼────────────────────────────────────────────────┐
│ Backend (ASP.NET) — POST /oauth/callback                    │
│ - Exchanges code for tokens                                 │
│ - Stores access_token + refresh_token in database           │
│ - Returns 200 OK                                            │
└────────────┬────────────────────────────────────────────────┘
             │ 5. Tokens saved
             │
┌────────────▼────────────────────────────────────────────────┐
│ User can now sync companies & payrolls                      │
│ - POST /api/a3-innuva-nominas/sync/companies               │
│ - POST /api/a3-innuva-nominas/sync/payrolls                │
│ (service automatically uses stored access_token)            │
└─────────────────────────────────────────────────────────────┘
```

---

## Step-by-Step Testing

### Step 1: Start Backend

```bash
cd C:\Projects\workspaces\SIG-es\backend
dotnet run
# Listens on http://localhost:5180
```

### Step 2: Apply Database Migration

Make sure the new `A3InnuvaOAuthToken` table is created:

```bash
# Inside backend directory
dotnet ef database update
```

This creates the `a3_innuva_o_auth_token` table in PostgreSQL.

### Step 3: Start Frontend

```bash
cd C:\Projects\workspaces\SIG-es\frontend
npm start
# Listens on http://localhost:4200
```

### Step 4: Get Authorization URL

**Option A: Via API directly (Postman / curl)**

```bash
curl -X GET "http://localhost:5180/api/a3-innuva-nominas/oauth/authorize-url" \
  -H "Content-Type: application/json"
```

Response:
```json
{
  "authorizeUrl": "https://login.wolterskluwer.eu/auth/core/connect/authorize?client_id=WK.ES.API.a3innuvaNomina.47472&response_type=code&redirect_uri=http%3A%2F%2Flocalhost%3A4200%2Fa3-innuva%2Foauth-callback&scope=offline_access%2Bopenid%2BIDInfo%2BWK.ES.A3EquipoContex&state=xyz&nonce=abc&code_challenge=DEF&code_challenge_method=S256",
  "message": "Abre este URL en tu navegador para autorizar el acceso a Wolters Kluwer"
}
```

**Option B: Via Frontend UI**

Navigate to `http://localhost:4200/a3-innuva` → Click "Authorize with Wolters Kluwer" button (when implemented).

### Step 5: Authorize at Wolters Kluwer

1. Copy the `authorizeUrl` from the API response
2. Paste into browser address bar
3. Log in with Wolters Kluwer credentials:
   - Username: (provided by WK support)
   - Password: (provided by WK support)
4. Grant permission to the app
5. Browser redirects to: `http://localhost:4200/a3-innuva/oauth-callback?code=XYZ&state=ABC`

### Step 6: Exchange Code for Tokens

The frontend callback handler (to be implemented) should automatically call:

```bash
curl -X POST "http://localhost:5180/api/a3-innuva-nominas/oauth/callback?code=XYZ&redirectUri=http%3A%2F%2Flocalhost%3A4200%2Fa3-innuva%2Foauth-callback" \
  -H "Content-Type: application/json"
```

Or manually test in Postman:

```
POST http://localhost:5180/api/a3-innuva-nominas/oauth/callback
Query params:
  - code: (from redirect URL)
  - redirectUri: http://localhost:4200/a3-innuva/oauth-callback
```

Response:
```json
{
  "message": "✅ Tokens obtenidos. Puedes sincronizar empresas y nóminas ahora."
}
```

This stores the tokens in the `a3_innuva_o_auth_token` table.

### Step 7: Sync Companies (Production)

Now you can sync companies with the stored access token:

```bash
curl -X POST "http://localhost:5180/api/a3-innuva-nominas/sync/companies" \
  -H "Authorization: Bearer JWT_TOKEN" \
  -H "Content-Type: application/json"
```

Or through the frontend A3 INNUVA dashboard → Click "Sync Companies".

### Step 8: Sync Companies (Test Table - Safe)

To test WITHOUT affecting production data, use the test endpoints:

```bash
curl -X POST "http://localhost:5180/api/a3-innuva-nominas/test/sync/companies" \
  -H "Content-Type: application/json"
```

This writes to `staging_a3innuva_companies_test` table.

### Step 9: View Results

**Production companies:**
```bash
curl -X GET "http://localhost:5180/api/a3-innuva-nominas/companies?page=1&pageSize=25"
```

**Test companies:**
```bash
curl -X GET "http://localhost:5180/api/a3-innuva-nominas/test/companies?page=1&pageSize=25"
```

---

## Token Management

### Automatic Token Refresh

The `WoltersKluwerOAuthService` automatically:

1. **Checks cache**: Returns cached token if valid (within 5 min of expiry)
2. **Checks database**: If not in cache, loads from DB
3. **Auto-refreshes**: If access token expired, uses refresh token to get a new one
4. **Updates cache**: Fresh token is cached for 5 minutes

### Manual Token Refresh

If you need to manually refresh:

```bash
curl -X POST "http://localhost:5180/api/a3-innuva-nominas/oauth/refresh" \
  -H "Authorization: Bearer JWT_TOKEN" \
  -H "Content-Type: application/json"
```

Response:
```json
{
  "message": "✅ Token refrescado"
}
```

### View Token Status

Query the database:

```sql
SELECT 
  id,
  access_token,
  refresh_token,
  access_token_expires_at,
  refresh_token_expires_at,
  last_sync_at,
  is_valid,
  created_at,
  updated_at
FROM a3_innuva_o_auth_token
ORDER BY updated_at DESC
LIMIT 1;
```

---

## Error Handling

### "No OAuth token found in database"

**Cause**: You haven't completed the authorization flow yet.

**Fix**: 
1. Call `GET /oauth/authorize-url`
2. Open URL in browser
3. Authorize at Wolters Kluwer
4. Call `POST /oauth/callback` with the code

### "Access token expired and cannot be refreshed"

**Cause**: Refresh token also expired (30 days, sliding window).

**Fix**: Re-authorize by completing the flow again.

### "OAuth Error 401: unauthorized_client"

**Cause**: Client credentials are wrong or OAuth config mismatch.

**Check**:
- `Integrations:A3InnuvaNominas:ClientId` in appsettings.json
- `Integrations:A3InnuvaNominas:ClientSecret` in appsettings.json
- Verify credentials match Wolters Kluwer account

### "No access_token in response"

**Cause**: Wolters Kluwer server returned incomplete response.

**Fix**: 
- Check network logs in browser DevTools
- Verify authorization code wasn't already used (single-use)
- Ensure redirect_uri matches exactly

---

## Troubleshooting Checklist

- [ ] Backend is running (`dotnet run` on port 5180)
- [ ] PostgreSQL is running with SIG database
- [ ] Migration applied: `a3_innuva_o_auth_token` table exists
- [ ] Wolters Kluwer credentials are valid (ClientId, ClientSecret)
- [ ] `appsettings.json` has correct `Integrations:A3InnuvaNominas:*` values
- [ ] Authorization code is fresh (< 10 minutes old, not reused)
- [ ] Redirect URI matches exactly: `http://localhost:4200/a3-innuva/oauth-callback`
- [ ] Frontend callback handler is implemented (or use API directly)

---

## Implementation Status

| Component | Status | Notes |
|-----------|--------|-------|
| Backend OAuth Service | ✅ Done | Authorization Code Flow with PKCE |
| Database (A3InnuvaOAuthToken) | ✅ Done | Migration created |
| Controller Endpoints | ✅ Done | GET /authorize-url, POST /callback, POST /refresh |
| Token Caching | ✅ Done | Memory cache + DB persistence |
| Sync Endpoints | ✅ Done | Prod + Test tables |
| Frontend Callback Handler | ⏳ TODO | Needs implementation in Angular |
| Frontend Auth Button | ⏳ TODO | Needs implementation in A3 INNUVA component |

---

## Notes

- **Sliding Window Refresh**: Refresh token is valid for 30 days from issuance (sliding window). Each refresh resets the timer.
- **PKCE**: Authorization Code Flow uses PKCE (Proof Key Code Exchange) for added security. No manual implementation needed; handled by the service.
- **Out-of-Band Flow**: Alternative: User authorizes manually via Postman, gets code, and provides it via API (for headless testing).
- **Token Expiry**: Access tokens typically expire in 3600 seconds (1 hour); automatically refreshed 60 seconds before expiry.

---

## Next Steps

1. **Frontend**: Implement OAuth callback handler in `a3-innuva.component.ts`
2. **Frontend**: Add "Authorize" button that calls `GET /oauth/authorize-url` and opens URL
3. **Frontend**: Add status indicator showing if authorized (check token in DB)
4. **Testing**: Run end-to-end flow with real Wolters Kluwer credentials
5. **Production**: Deploy to staging environment with real API credentials


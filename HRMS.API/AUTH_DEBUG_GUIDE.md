# JWT Token Debug Guide

## Issue: "Organization ID not found in token"

The application is throwing `UnauthorizedAccessException` with message "Organization ID not found in token" when calling dashboard APIs.

## Demo User Credentials

Based on the seed data in `04_seed_data.sql`, test with these credentials:

- **Email**: `admin@demo.company.com`
- **Password**: `password` (the BCrypt hash indicates this is the password)
- **Organization ID**: `00000000-0000-0000-0000-000000000001`

Other demo users:
- `hr@demo.company.com` / `password`
- `john.doe@demo.company.com` / `password`
- `jane.smith@demo.company.com` / `password`
- `mike.wilson@demo.company.com` / `password`

## Debug Steps

### 1. Test Authentication
```bash
POST /api/auth/login
{
  "email": "admin@demo.company.com",
  "password": "password"
}
```

### 2. Check JWT Claims
After getting a token, call:
```bash
GET /api/auth/debug-claims
Authorization: Bearer YOUR_TOKEN_HERE
```

This will show all claims in your JWT token.

### 3. Test Dashboard API
```bash
GET /api/dashboard/attendance-stats
Authorization: Bearer YOUR_TOKEN_HERE
```

## Debug Logs Added

The application now logs:
1. **AuthService**: User details during authentication
2. **AuthService**: All JWT claims when generating tokens
3. **Controllers**: All claims received when parsing tokens

## Common Issues

1. **Empty OrganizationId**: If user.OrganizationId is Guid.Empty, token generation will fail
2. **Database mapping**: Authentication function might not return organization_id properly
3. **Claim name mismatch**: Frontend/backend might use different claim names

## Expected Fix

The debug logs will show exactly what's happening:
- If OrganizationId is empty during token generation
- If claims are missing from the token
- If claim parsing is failing in controllers

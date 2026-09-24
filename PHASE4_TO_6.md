# Phases 4–6 mobile architecture

`AuthenticatedHttpHandler` attaches access tokens, performs one serialized refresh after a 401, rotates secure tokens, retries the original request once, and clears rejected sessions. `ApiSessionService` exposes active-session listing, device revocation, and logout-all workflows.

Authentication, session transport, sync scheduling, CGM protocol handling, reporting, diagnostics, and presentation view models now have separate service boundaries. `ApplicationErrorHandler` centralizes diagnostic capture and safe user messages. Production startup requires an HTTPS API endpoint.

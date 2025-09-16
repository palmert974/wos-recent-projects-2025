- [Session-Based Auth: Step-by-Step Flow (No Code)](#session-based-auth-step-by-step-flow-no-code)
  - [0) Big Picture](#0-big-picture)
  - [1) Vocabulary & Assumptions](#1-vocabulary-assumptions)
  - [2) Prerequisites & Setup](#2-prerequisites-setup)
  - [3) Registration Flow (Create Account)](#3-registration-flow-create-account)
    - [A) Show the Registration Form (GET)](#a-show-the-registration-form-get)
    - [B) Submit the Registration Form (POST)](#b-submit-the-registration-form-post)
  - [4) Login Flow (Sign In)](#4-login-flow-sign-in)
    - [A) Show the Login Form (GET)](#a-show-the-login-form-get)
    - [B) Submit the Login Form (POST)](#b-submit-the-login-form-post)
  - [5) Protected Routes (Auth Guard)](#5-protected-routes-auth-guard)
  - [6) Logout Flow (Sign Out)](#6-logout-flow-sign-out)
    - [A) Confirm Logout (GET)](#a-confirm-logout-get)
    - [B) Perform Logout (POST)](#b-perform-logout-post)
  - [7) UX & Error-Handling Patterns](#7-ux-error-handling-patterns)
  - [8) Security Essentials (Non-Negotiables)](#8-security-essentials-non-negotiables)
  - [9) Testing Checklist](#9-testing-checklist)
  - [10) Common Pitfalls to Avoid](#10-common-pitfalls-to-avoid)
  - [11) What to Name Things (Guidance, Not Rules)](#11-what-to-name-things-guidance-not-rules)

# Session-Based Auth: Step-by-Step Flow (No Code)

## 0) Big Picture

- Users create accounts (register).
- Users sign in (login).
- The server stores a single piece of data in session (e.g., the user’s ID) to remember who is logged in.
- Protected pages check the session first; if no user is in session, redirect to login.
- Users can sign out (logout), which clears the session.

---

## 1) Vocabulary & Assumptions

- **Session**: Server-managed storage tied to the user’s browser via a cookie. Used to remember the logged-in user’s ID.
- **Session Key**: A string like `"userId"` used to store the logged-in user’s ID.
- **Password Hashing**: Store only a secure hash of the password (never the raw password).
- **Validation**: Enforce required fields, formats, and constraints (e.g., unique email).

---

## 2) Prerequisites & Setup

- A **User** record with at least: username, email (unique), and a hashed password field.
- A **Password Service** capable of hashing and verifying passwords (e.g., BCrypt/Argon2).
- Session is enabled and configured in the app.
- Views for: Register (GET), Login (GET), Logout Confirm (GET), and any protected pages.

---

## 3) Registration Flow (Create Account)

### A) Show the Registration Form (GET)

1. Render a blank registration form view model (username, email, password, confirm password).
2. Include space for field-level error messages and a general summary.

### B) Submit the Registration Form (POST)

1. **Normalize Input**
   - Trim whitespace on all fields.
   - Lowercase the email.
   - Keep passwords exactly as entered except trimming trailing/leading spaces if you choose.
2. **Validate**
   - Required: username, email, password, confirm password.
   - Email format is valid.
   - Password meets your policy (length, complexity).
   - Confirm password matches.
   - If validation fails: re-render the form with errors and the user’s previous inputs (except password fields if you prefer).
3. **Enforce Uniqueness**
   - Check that the email is not already taken.
   - If taken, add a model error to the email field and re-render.
4. **Hash the Password**
   - Hash the password using a secure algorithm.
5. **Create the User Record**
   - Save username, normalized email, and password hash to the database.
6. **Start a Session (Log In Immediately)**
   - Store the new user’s ID in session under your session key (e.g., `"userId"`).
7. **Redirect to a Post-Login Destination**
   - Example: the main “wall” or dashboard.

---

## 4) Login Flow (Sign In)

### A) Show the Login Form (GET)

1. Render a login form with fields for email and password.
2. Support an optional query parameter or view model field for non-sensitive error messages (e.g., “invalid credentials”).

### B) Submit the Login Form (POST)

1. **Normalize Input**
   - Trim whitespace.
   - Lowercase the email.
2. **Validate**
   - Required: email and password.
   - If validation fails: re-render the form with errors.
3. **Find the User**
   - Look up by normalized email.
   - If not found: show a generic “invalid credentials” outcome (do not reveal whether the email exists).
4. **Verify the Password**
   - Use the password service to compare the raw password with the stored hash.
   - If mismatch: show the same generic “invalid credentials” outcome.
5. **Start a Session**
   - Store the user’s ID in session under your session key.
6. **Redirect**
   - If a “return to” destination was captured earlier (see protected routes below), send them there.
   - Otherwise, send them to the main post-login page.

---

## 5) Protected Routes (Auth Guard)

1. **Before Handling the Request**
   - Check the session for the user ID using your session key.
2. **If Not Present**
   - Capture the original URL (so you can return the user after login).
   - Redirect the user to the login page, optionally passing a non-sensitive reason (e.g., “please sign in”).
3. **If Present**
   - Allow the request to proceed.
   - Optionally, load the user from the database to show a friendly welcome message.

---

## 6) Logout Flow (Sign Out)

### A) Confirm Logout (GET)

1. Show a simple confirmation page (e.g., “Are you sure you want to sign out?”).
2. Include a safe, intentional action to proceed (e.g., a POST with CSRF protection).

### B) Perform Logout (POST)

1. Clear the session entirely (or remove only the user ID).
2. Redirect to a public page with a friendly confirmation message (e.g., “You’ve signed out.”).

---

## 7) UX & Error-Handling Patterns

- **Field-Level Errors**: Attach messages specifically to the problem fields (e.g., invalid email format).
- **Global Errors**: Use a summary area for cross-field issues (e.g., “That email is already in use”).
- **Neutral Messaging**: For login failures, avoid revealing which part failed (email vs. password).
- **Status Indicators**: Consider lightweight query parameters (e.g., `?message=logout-successful`) for non-sensitive feedback.

---

## 8) Security Essentials (Non-Negotiables)

- **Hash All Passwords**: Never store plain-text passwords.
- **Normalize Inputs**: Trim and lowercase emails before checks and queries.
- **Validate Rigorously**: Use server-side validation even if you have client-side checks.
- **CSRF Protection**: Require anti-forgery tokens on all state-changing form posts (register, login, logout).
- **Least Disclosure**: Use generic error messages for login failure.
- **Session Hygiene**:
  - Use secure cookies (on by default HTTPS only).
  - Restrict cookie access from JavaScript (on by default).
- **Email Uniqueness**: Enforce it at both the application and database layers if possible.

---

## 9) Testing Checklist

- **Registration**
  - Rejects invalid inputs and shows clear messages.
  - Rejects duplicate emails.
  - Creates a user and starts a session on success.
- **Login**
  - Rejects invalid inputs.
  - Fails gracefully with wrong email or password (generic message).
  - Starts a session on success and redirects appropriately.
- **Protected Pages**
  - Redirect when not logged in.
  - Allow access when logged in.
  - Preserve and use return-to URLs correctly.
- **Logout**
  - Shows a confirmation step.
  - Clears the session and redirects with a success message.
- **Session & Cookies**
  - Session key is consistent everywhere.
  - Session ends when expected (manual logout or timeout).

---

## 10) Common Pitfalls to Avoid

- Forgetting to normalize emails before uniqueness checks.
- Returning detailed login errors that leak whether an email exists.
- Skipping CSRF protection on auth-related POSTs.
- Storing anything sensitive besides the user’s ID in session.
- Not clearing the session fully on logout.
- Inconsistent session key names across controllers or views.

---

## 11) What to Name Things (Guidance, Not Rules)

- Session key like: `"userId"` (short, clear, consistent).
- Routes like: `/account/register`, `/account/login`, `/account/logout`.
- Post-login landing page: a clear destination (e.g., “All Posts” or a dashboard).

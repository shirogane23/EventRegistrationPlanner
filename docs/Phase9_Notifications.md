# Phase 9: Notifications

Phase 9 adds the notification boundary required by EventFlow without coupling
the API to an external email provider. `LoggingNotificationService` is the
version-one adapter: it logs queued notification content and can later be
replaced by an email implementation without changing controllers or business
rules.

Notifications are queued after the related database save succeeds:

- Automatic confirmation and organizer approval queue confirmation details.
- Pending registrations never queue confirmation notifications.
- Attendee cancellation, rejection, and organizer cancellation queue a
  cancellation notification.
- Postponement, rescheduling, and event cancellation queue updates for all
  Pending and Confirmed registrations.

Email delivery is not guaranteed in this phase. There is no retry dashboard or
delivery-history module. If a provider is added later, provider failures must
be logged explicitly and must not turn a successful registration or lifecycle
transition into a false failure response.

To observe notifications during development, run the API and inspect its
console logs while exercising registration and lifecycle endpoints.

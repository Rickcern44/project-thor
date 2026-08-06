## ADDED Requirements

### Requirement: Two user roles
The system SHALL support exactly two roles — Admin and Player — and SHALL enforce role-based permissions on every action.

#### Scenario: Player attempts an admin-only action
- **WHEN** a Player attempts to add or remove another player, waive a charge, mark a payment, or create a game
- **THEN** the system denies the action and the underlying data is unchanged

#### Scenario: Admin performs admin actions
- **WHEN** an Admin performs roster, game, or payment management actions
- **THEN** the system permits the action

### Requirement: Admin-invite-only account creation
The system SHALL create Player accounts only via an Admin-issued invitation and SHALL NOT offer open self-registration.

#### Scenario: Admin invites a player
- **WHEN** an Admin issues an invite to a player's contact address
- **THEN** the system creates a pending account linked to that player's roster record and sends the invite

#### Scenario: Stranger tries to self-register
- **WHEN** someone who has not been invited attempts to create an account
- **THEN** the system refuses and no account is created

### Requirement: Invitation links to roster record
The system SHALL link an accepted invitation to the invited player's existing roster record so that prior payment history and balance carry over to the account.

#### Scenario: Player accepts invite
- **WHEN** an invited player accepts the invitation via their magic link
- **THEN** their account is activated and associated with their imported roster record, including any existing balance

### Requirement: Passwordless magic-link authentication
The system SHALL authenticate users via emailed magic links and SHALL NOT use passwords. The invitation itself SHALL serve as the first magic link.

#### Scenario: Player logs in
- **WHEN** a user requests to log in with their registered email
- **THEN** the system emails a magic link that authenticates them when followed

#### Scenario: No passwords stored
- **WHEN** a user account is created or activated
- **THEN** no password is set, stored, or required at any point

### Requirement: Authentication
The system SHALL authenticate users before granting access to any role-specific action, and SHALL scope Player self-service actions to that player's own account only.

#### Scenario: Player acts on another player's account
- **WHEN** an authenticated Player attempts to sign up or cancel on behalf of a different player
- **THEN** the system denies the action

#### Scenario: Unauthenticated access
- **WHEN** an unauthenticated visitor attempts any role-specific action
- **THEN** the system requires authentication first

# AlJohary Service Hub

A desktop point-of-sale and service-management system tailored for laptop &
printer repair workshops. **AlJohary Service Hub** unifies POS, repair &
maintenance tracking, spare-parts inventory, customer profiles, supplier
purchasing, and financial reporting in a single Windows-native application.

> **Cash/payment-only POS.** Every sale is paid in full at checkout. Credit sales
> and customer receivables (accounts-receivable / debt collection) are **not**
> supported; any related legacy database structures are de-scoped and retained for
> historical data only.

## Key Features

- **POS Workflow**: Streamlined sales process supporting cash and electronic payment methods (نقدي / محافظ إلكترونية / إنستا باي), paid in full at checkout.
- **Repair & Maintenance**: Track devices in for repair (laptops, printers, etc.), repair orders, parts consumed, status, and partial payments.
- **Spare Parts Inventory**: Manage parts, stock levels, purchase prices, and suppliers.
- **Customer Profiles**: Manage customer contact details and purchase history (no credit/outstanding balances).
- **Returns Handling**: Process full or partial returns with automatic stock adjustment and financial reconciliation.
- **Supplier Management**: Record purchases, supplier payments, and outstanding balances.
- **Expense Tracking**: Record and categorize daily business expenses.
- **Reporting**: Detailed daily and period summaries, profit analysis, and operational logs.
- **Data Safety**: Built-in backup and restore utilities with local SQLite persistence.
- **Security**: User authentication and role-based access for administrative tasks.

## Tech Stack

- **Language**: C#
- **Framework**: WPF (Windows Presentation Foundation)
- **Target**: .NET 10.0
- **Database**: SQLite
- **Architecture**: MVVM-style separation of concerns

## Project Structure

- **Domain**: Core business entities and repository interfaces.
- **Application**: Business logic, services, and application-specific interfaces.
- **Infrastructure**: Data persistence, migrations, printing services, and security implementations.
- **Presentation**: UI components, ViewModels, and visual resources.
- **Shared**: Common utilities, helpers, and constants used across the project.

## Getting Started

### Prerequisites

- .NET 10.0 SDK

### Setup

1. Clone the repository to your local machine.
2. Open `AlJohary.ServiceHub.sln` in Visual Studio 2022 or a compatible IDE.
3. Restore the NuGet packages.
4. Build the solution in Debug or Release mode.
5. Run the `AlJohary.ServiceHub` project.

## Default Credentials

The system initializes with a default administrator account:
- **Username**: `admin`
- **Initial Password**: `admin123`

> **Security:** Whenever the admin account still uses the default `admin123` password, the system **forces** a password change on the next admin login before proceeding. This applies to fresh installs and to any existing install still on the default credential. After the password is changed, `admin123` no longer works and the force-change prompt does not reappear. An existing install whose admin has **already** moved to a custom password is never re-prompted. This ensures no standing default credential exists in production.

## Database & Local Files

- The application uses a local SQLite database (`aljohary_service_hub.db`), which is initialized automatically on the first run.
- Runtime files, including the database (`.db`), logs, and backups, are excluded from the repository.
- Ensure the application has write permissions to its execution directory for data persistence.

## Notes

This system is optimized for the specific workflow of small to medium tech-service businesses (laptop and printer repair, spare-parts retail, and on-site maintenance). It focuses on day-to-day operational efficiency, accurate cash/payment tracking, and clear financial visibility.

### Run from Source (CLI)
1. Ensure the **.NET 10.0 SDK** is installed.
2. Open your terminal in the project directory.
3. Run `dotnet restore`.
4. Run `dotnet run --project AlJohary.ServiceHub.csproj`.

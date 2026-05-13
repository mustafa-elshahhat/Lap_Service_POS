# AutoParts POS

A desktop point-of-sale and inventory management system designed for auto parts shops. This application provides a unified interface to manage products, stock levels, sales transactions, customer credit, and financial reporting.

## Key Features

- **Inventory Management**: Track products, stock levels, purchase prices, and suppliers.
- **POS Workflow**: Streamlined sales process supporting both cash and credit transactions.
- **Customer Credit Tracking**: Manage customer profiles and track outstanding balances with payment history.
- **Returns Handling**: Process full or partial returns with automatic stock adjustment and financial reconciliation.
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
2. Open `CarPartsShopWPF.sln` in Visual Studio 2022 or a compatible IDE.
3. Restore the NuGet packages.
4. Build the solution in Debug or Release mode.
5. Run the `CarPartsShopWPF` project.

## Default Credentials

The system initializes with a default administrator account:
- **Username**: `admin`
- **Password**: `admin123`

*It is recommended to update the administrator password immediately after the first login.*

## Database & Local Files

- The application uses a local SQLite database, which is initialized automatically on the first run.
- Runtime files, including the database (`.db`), logs, and backups, are excluded from the repository.
- Ensure the application has write permissions to its execution directory for data persistence.

## Notes

This system is optimized for the specific workflow of small to medium auto parts retailers. It focuses on day-to-day operational efficiency, accurate credit tracking, and clear financial visibility.

### Run from Source (CLI)
1. Ensure the **.NET 10.0 SDK** is installed.
2. Open your terminal in the project directory.
3. Run dotnet restore.
4. Run dotnet run --project CarPartsShopWPF.csproj.

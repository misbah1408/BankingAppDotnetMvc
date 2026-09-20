# Banking Transaction & Loan Management System

Beginner/intermediate ASP.NET Core MVC + EF Core + SQL Server project based on the supplied case study.

## Stack
- .NET 8 / ASP.NET Core MVC
- Entity Framework Core 8
- SQL Server / LocalDB
- Cookie authentication + Claims-based role authorization
- Tailwind CDN for simple UI

## Roles
Customer, Teller, LoanOfficer, Auditor, Admin.

## Workflow
1. Customer registers; account is created and registration remains pending.
2. Teller approves customer. Password becomes first four letters of the customer's name (spaces removed, uppercased) + birth year.
3. Customer logs in and can view accounts, transaction history and loans. Deposit/withdrawal requests wait for Teller approval; transfers are immediate after balance validation.
4. Loan Officer reviews credit score/risk and approves/rejects loans. Approval disburses principal and creates monthly EMI schedule.
5. Customer pays pending EMIs from an active account. Balance remaining updates and loan closes when fully paid.
6. Auditor sees system audit events. Admin creates and activates/deactivates staff.

## Demo logins
admin@bank.local / 1990
teller@bank.local / 1992
officer@bank.local / 1991
auditor@bank.local / 1989

## Run
1. Install .NET 8 SDK and SQL Server LocalDB.
2. Open the .csproj in Visual Studio 2022 or run `dotnet restore`.
3. Run `dotnet run`.
4. The app creates the database automatically on first run.

For production, passwords should be hashed and migrations should replace EnsureCreated. This training project intentionally keeps authentication simple.

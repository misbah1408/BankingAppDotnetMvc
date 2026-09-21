using BankingLoanManagement.Data;
using BankingLoanManagement.Enums;
using BankingLoanManagement.Models;
using BankingLoanManagement.Services;
using BankingLoanManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace BankingLoanManagement.Controllers;

[Authorize(Roles = "Customer")]
public class CustomerController(ApplicationDbContext d, BankingService b, LoanService l, AuditService a) : Controller
{
    readonly ApplicationDbContext db = d;
    readonly BankingService bank = b;
    readonly LoanService loans = l;
    readonly AuditService audit = a;

    int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public async Task<IActionResult> Dashboard()
    {
        var c = await db.CustomerProfiles
            .Include(x => x.User)
            .Include(x => x.Accounts)
            .Include(x => x.Loans)
                .ThenInclude(x => x.RepaymentSchedules)
            .FirstAsync(x => x.UserId == UserId);

        ViewBag.Transactions = await db.Transactions
            .Include(x => x.Account)
            .Where(x => x.Account.CustomerProfileId == c.Id)
            .OrderByDescending(x => x.TransactionDate)
            .Take(10)
            .ToListAsync();

        return View(c);
    }
    [HttpPost]
    public async Task<IActionResult> Transaction(TransactionVm m)
    {
        var c = await db.CustomerProfiles.Include(x => x.Accounts).FirstAsync(x => x.UserId == UserId);
        if (!c.Accounts.Any(x => x.AccountNumber == m.AccountNumber))
        {
            TempData["Error"] = "Invalid account."; return RedirectToAction("Dashboard");
        }
        Transaction? t = null;
        if (m.Type == TransactionType.Transfer)
            t = await bank.TransferAsync(m.AccountNumber, m.ToAccountNumber!, m.Amount, User.Identity!.Name!);
        else
            t = await bank.DepositOrWithdrawAsync(m.AccountNumber, m.Amount, m.Type, User.Identity!.Name!, true);
        TempData[t == null ? "Error" : "Success"] = t == null ? "Transaction could not be processed." : m.Type == TransactionType.Transfer ? "Transfer completed." : "Request sent to Teller for approval.";
        return RedirectToAction("Dashboard");
    }
    public IActionResult ApplyLoan() => View(new LoanVm());
    [HttpPost]
    public async Task<IActionResult> ApplyLoan(LoanVm m)
    {
        if (m.PrincipalAmount <= 0 || m.TenureMonths <= 0)
        {
            ModelState.AddModelError("", "Enter valid loan amount and tenure.");
            return View(m);
        }
        var c = await db.CustomerProfiles.FirstAsync(x => x.UserId == UserId);
        var loan = new Loan
        {
            LoanNumber = "LN-" + Random.Shared.Next(100000, 999999),
            CustomerProfileId = c.Id,
            LoanType = m.LoanType,
            PrincipalAmount = m.PrincipalAmount,
            TenureMonths = m.TenureMonths,
            CollateralDetails = m.CollateralDetails,
        };

        if (m.Document != null)
        {
            using var memoryStream = new MemoryStream();
            await m.Document.CopyToAsync(memoryStream);
            loan.FileData = memoryStream.ToArray();
        }
        db.Loans.Add(loan);
        await db.SaveChangesAsync();
        await audit.LogAsync(User, "LOAN", "APPLY", $"Applied {m.LoanType} loan {loan.LoanNumber}", HttpContext);
        TempData["Success"] = "Loan application submitted.";
        return RedirectToAction("Dashboard");
    }
    [HttpPost]
    public async Task<IActionResult> PayEmi(int id)
    {
        var ok = await loans.PayEmiAsync(id);
        TempData[ok ? "Success" : "Error"] = ok ? "EMI paid successfully." : "Insufficient balance or EMI already processed.";
        return RedirectToAction("Dashboard");
    }
}

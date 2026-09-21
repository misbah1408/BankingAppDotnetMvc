using BankingLoanManagement.Data;
using BankingLoanManagement.Enums;
using BankingLoanManagement.Models;
using BankingLoanManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankingLoanManagement.Controllers
{
    [Authorize(Roles = "LoanOfficer")]
    public class LoanOfficerController(ApplicationDbContext d, LoanService s, AuditService a, GeminiAiService g) : Controller
    {
        readonly ApplicationDbContext db = d;
        readonly LoanService service = s;
        readonly AuditService audit = a;
        readonly GeminiAiService geminiService = g;

        public async Task<IActionResult> Index() => View(await db.Loans.Include(x => x.CustomerProfile).ThenInclude(x => x.User).Where(x => x.Status == LoanStatus.Applied).OrderBy(x => x.AppliedDate).ToListAsync());

        [HttpPost]
        public async Task<IActionResult> Approve(int id, decimal interestRate)
        {
            if (interestRate <= 0) interestRate = 10;
            await service.ApproveAsync(id, interestRate);
            await audit.LogAsync(User, "LOAN", "APPROVE", $"Approved loan #{id} at {interestRate}%", HttpContext);
            TempData["Success"] = "Loan approved, disbursed and EMI schedule generated.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            var l = await db.Loans.FindAsync(id);
            if (l != null)
            {
                l.Status = LoanStatus.Rejected;
                l.RejectionReason = reason;
                await db.SaveChangesAsync();
                await audit.LogAsync(User, "LOAN", "REJECT", $"Rejected loan #{id}", HttpContext);
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> GetLoanDetails(int id)
        {
            var loan = await db.Loans
                .Include(x => x.CustomerProfile)
                .ThenInclude(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (loan == null) return NotFound();

            return Json(new
            {
                id = loan.Id,
                applicantName = loan.CustomerProfile?.User?.Name ?? "N/A",
                loanType = loan.LoanType.ToString(),
                principalAmount = loan.PrincipalAmount,
                tenureMonths = loan.TenureMonths,
                creditScore = loan.CreditScore,
                collateralDetails = loan.CollateralDetails ?? "No collateral details supplied.",
                documentPath = loan.DocumentPath // Ensure your model stores document path/URL
            });
        }

        [HttpPost]
        public async Task<IActionResult> AnalyzeRisk([FromBody] RiskAnalysisRequest request)
        {
            var loan = await db.Loans
                .Include(x => x.CustomerProfile)
                .ThenInclude(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == request.LoanId);

            if (loan == null) return NotFound("Loan application not found.");

            var analysisResult = await geminiService.AnalyzeLoanRiskAsync(loan, request.ExtractedPdfText);
            return Json(analysisResult);
        }
    }

    public class RiskAnalysisRequest
    {
        public int LoanId { get; set; }
        public string ExtractedPdfText { get; set; } = string.Empty;
    }
}
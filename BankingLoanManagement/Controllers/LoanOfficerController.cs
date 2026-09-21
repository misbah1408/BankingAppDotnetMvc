using BankingLoanManagement.Data;
using BankingLoanManagement.Enums;
using BankingLoanManagement.Models;
using BankingLoanManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UglyToad.PdfPig;

namespace BankingLoanManagement.Controllers
{
    [Authorize(Roles = "LoanOfficer")]
    public class LoanOfficerController(
        ApplicationDbContext d,
        LoanService s,
        AuditService a,
        GeminiAiService g) : Controller
    {
        readonly ApplicationDbContext db = d;
        readonly LoanService service = s;
        readonly AuditService audit = a;
        readonly GeminiAiService geminiService = g;

        public async Task<IActionResult> Index()
        {
            var loans = await db.Loans
                .Include(x => x.CustomerProfile)
                .ThenInclude(x => x.User)
                .Where(x => x.Status == LoanStatus.Applied)
                .OrderBy(x => x.AppliedDate)
                .ToListAsync();

            return View(loans);
        }

        [HttpGet]
        public async Task<IActionResult> GetLoanDetails(int id)
        {
            var loan = await db.Loans
                .Include(x => x.CustomerProfile)
                .ThenInclude(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (loan == null)
                return NotFound();

            return Json(new
            {
                id = loan.Id,
                applicantName = loan.CustomerProfile?.User?.Name ?? "N/A",
                loanType = loan.LoanType.ToString(),
                principalAmount = loan.PrincipalAmount,
                tenureMonths = loan.TenureMonths,
                collateralDetails =
                    loan.CollateralDetails ?? "No collateral details supplied."
            });
        }

        [HttpPost]
        public async Task<IActionResult> AnalyzeLoan(int id)
        {
            Console.WriteLine(id);
            var loan = await db.Loans
                .Include(x => x.CustomerProfile)
                .ThenInclude(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (loan == null)
                return NotFound("Loan application not found.");

            if (loan.FileData == null || loan.FileData.Length == 0)
                return BadRequest("No loan document is available for analysis.");

            string pdfText;

            try
            {
                using var stream = new MemoryStream(loan.FileData);

                using var pdf = PdfDocument.Open(stream);

                var textBuilder = new System.Text.StringBuilder();

                foreach (var page in pdf.GetPages())
                {
                    textBuilder.AppendLine(page.Text);
                }

                pdfText = textBuilder.ToString();
            }
            catch
            {
                return BadRequest("Unable to read the stored PDF document.");
            }

            if (string.IsNullOrWhiteSpace(pdfText))
                return BadRequest("No readable text was found in the PDF.");

            var result = await geminiService.AnalyzeLoanAsync(
                loan,
                pdfText
            );

            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> Approve(int id, decimal interestRate)
        {
            if (interestRate <= 0)
                interestRate = 10;

            await service.ApproveAsync(id, interestRate);

            await audit.LogAsync(
                User,
                "LOAN",
                "APPROVE",
                $"Approved loan #{id} at {interestRate}%",
                HttpContext);

            TempData["Success"] =
                "Loan approved, disbursed and EMI schedule generated.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            var loan = await db.Loans.FindAsync(id);

            if (loan != null)
            {
                loan.Status = LoanStatus.Rejected;
                loan.RejectionReason = reason;

                await db.SaveChangesAsync();

                await audit.LogAsync(
                    User,
                    "LOAN",
                    "REJECT",
                    $"Rejected loan #{id}",
                    HttpContext);
            }

            return RedirectToAction("Index");
        }
    }
}
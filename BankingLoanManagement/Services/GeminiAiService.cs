using System.Text;
using Google.GenAI;
using Google.GenAI.Types;
using BankingLoanManagement.Models;

namespace BankingLoanManagement.Services
{
    public class GeminiAiService
    {
        private readonly string _apiKey;

        public GeminiAiService(IConfiguration configuration)
        {
            _apiKey = configuration["Gemini:ApiKey"]
                ?? throw new Exception("Gemini API Key is missing.");
        }

        public async Task<string> AnalyzeLoanAsync(Loan loan, string pdfText)
        {
            var applicantName = loan.CustomerProfile?.User?.Name ?? "Unknown";

            var prompt = $@"
You are an AI assistant helping a Loan Officer review a loan application.

IMPORTANT:
- Do not make the final approval or rejection decision.
- The Loan Officer makes the final decision.
- Analyze only the information provided.
- Do not invent missing information.
- If information is unavailable, write 'Not available'.
- Do NOT use Markdown formatting (no *, **, #, etc.).
- Provide a concise response formatted in clean HTML (using Tailwind CSS classes).
- Use exactly the following section format.

DOCUMENT TEXT:
{pdfText}

Provide a concise response formatted in clean HTML (using Tailwind CSS classes) app theme(dark)  containing:
LOAN APPLICATION ANALYSIS

1. APPLICANT INFORMATION
Name: {applicantName}
Loan Type: {loan.LoanType}
Loan Amount: ₹{loan.PrincipalAmount:N2}
Tenure: {loan.TenureMonths} months

2. INFORMATION FOUND IN DOCUMENT
[Summarize the important applicant and financial information found in the PDF.]

3. DOCUMENT VERIFICATION
[Compare the loan application information with the PDF information.
Mention any mismatch or inconsistency.]

4. COLLATERAL ANALYSIS
Collateral Details: {loan.CollateralDetails ?? "Not available"}
[Explain what the document says about collateral and identify missing or unverified information.]

5. KEY OBSERVATIONS
[Give 2-4 important observations.]

6. MISSING OR SUSPICIOUS INFORMATION
[Clearly list missing, inconsistent, or suspicious information.
If nothing is found, write 'No obvious issues found from the provided information.']

7. AI ASSISTANCE
Suggested Review Level: LOW / MEDIUM / HIGH
Reason: [Brief explanation]

FINAL NOTE
This analysis is an AI-assisted review only. The Loan Officer must verify the documents and make the final loan decision.
";

            try
            {
                // Initialize Google GenAI Client
                var client = new Client(apiKey: _apiKey);

                // Send content request using SDK
                var response = await client.Models.GenerateContentAsync(
                    model: "gemini-2.5-flash",
                    contents: prompt
                );

                // Access generated text directly from response property
                var resultText = response?.Text;

                if (string.IsNullOrWhiteSpace(resultText))
                {
                    return "<p>No response generated from Gemini API.</p>";
                }

                // Strip potential Markdown code blocks wrapping the HTML response
                resultText = resultText.Replace("```html", "").Replace("```", "").Trim();

                return resultText;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gemini API Error: {ex.Message}");
                return "<p class='text-red-400'>Failed to evaluate loan risk using AI Compiler. Please retry.</p>";
            }
        }
    }
}
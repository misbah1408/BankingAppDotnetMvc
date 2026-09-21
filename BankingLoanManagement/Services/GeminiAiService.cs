using System.Text;
using System.Text.Json;
using BankingLoanManagement.Models;

namespace BankingLoanManagement.Services
{
    public class GeminiAiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GeminiAiService(
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;

            _apiKey = configuration["Gemini:ApiKey"]
                ?? throw new Exception("Gemini API Key is missing.");
        }

        public async Task<string> AnalyzeLoanAsync(
            Loan loan,
            string pdfText)
        {
            var applicantName =
                loan.CustomerProfile?.User?.Name ?? "Unknown";

            var prompt = $@"
You are an AI assistant helping a Loan Officer review a loan application.

IMPORTANT:
- Do not make the final approval or rejection decision.
- The Loan Officer makes the final decision.
- Analyze only the information provided.
- Do not invent missing information.
- If information is unavailable, write 'Not available'.
- Do NOT use Markdown.
- Do NOT use *, **, #, bullet symbols, JSON, HTML, or code blocks.
- Return clean, readable plain text.
- Use exactly the following section format.

LOAN APPLICATION ANALYSIS

1. APPLICANT INFORMATION
Name: {loan.CustomerProfile?.User?.Name ?? "Not available"}
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

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = prompt
                            }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);

            using var content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

            var url =
                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";

            var response =
                await _httpClient.PostAsync(url, content);

            var responseBody =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return $"Gemini API Error: {responseBody}";
            }

            using var document =
                JsonDocument.Parse(responseBody);

            var result =
                document.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

            return result ??
                   "No analysis was generated.";
        }
    }
}
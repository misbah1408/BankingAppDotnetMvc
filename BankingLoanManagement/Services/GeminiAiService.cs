using System.Text;
using System.Text.Json;
using BankingLoanManagement.Models;

namespace BankingLoanManagement.Services
{
    public class GeminiAiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GeminiAiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Gemini:ApiKey"] ?? throw new ArgumentNullException("Gemini API Key missing.");
        }

        public async Task<string> AnalyzeLoanRiskAsync(Loan loan, string pdfText)
        {
            var prompt = $@"
You are an expert financial loan underwriter. Evaluate the risk score and compile details for the following application:

Applicant Name: {loan.CustomerProfile?.User?.Name}
Loan Type: {loan.LoanType}
Principal Amount: {loan.PrincipalAmount}
Tenure Months: {loan.TenureMonths}
Credit Score: {loan.CreditScore}
Collateral Details: {loan.CollateralDetails}
Extracted Document/PDF Text: {pdfText}

Provide a concise response formatted in clean HTML (using Tailwind CSS classes) containing:
1. **Risk Level**: (Low / Medium / High) with color-coded badges.
2. **Predicted Risk Score**: (Scale of 0 to 100, where 0 is safest and 100 is critical risk).
3. **Collateral Analysis**: Validation of collateral adequacy relative to loan amount.
4. **Key Risk Factors**: 2-3 bullet points outlining potential concerns.
5. **Final Underwriting Recommendation**: (Approve / Manual Review / Reject).";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";

            var response = await _httpClient.PostAsync(url, jsonContent);
            if (!response.IsSuccessStatusCode)
            {
                return "<p class='text-red-400'>Failed to evaluate loan risk using AI Compiler. Please retry.</p>";
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);

            var resultText = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return resultText ?? "<p>No response generated from Gemini API.</p>";
        }
    }
}
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace WMSApp.PrintManagement
{
    /// <summary>
    /// Downloads PDF reports from Oracle Fusion BI Publisher via SOAP
    /// </summary>
    public class FusionPdfDownloader
    {
        private readonly HttpClient _httpClient;

        public FusionPdfDownloader()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(120);
        }

        public async Task<PdfDownloadResult> DownloadGenericReportPdfAsync(
            string reportPath,
            string parameterName,
            string parameterValue,
            string instance,
            string username,
            string password)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[FusionPdfDownloader] Downloading report: {reportPath}");
                System.Diagnostics.Debug.WriteLine($"[FusionPdfDownloader] Parameter: {parameterName}={parameterValue}");

                string soapUrl = $"https://{instance}/xmlpserver/services/ExternalReportWSSService";

                string soapEnvelope = BuildSoapEnvelope(reportPath, parameterName, parameterValue, username, password);

                var request = new HttpRequestMessage(HttpMethod.Post, soapUrl);
                request.Content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
                request.Headers.Add("SOAPAction", "");

                var response = await _httpClient.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Parse SOAP response to extract base64 PDF
                    string base64Pdf = ExtractBase64FromSoapResponse(responseContent);

                    if (!string.IsNullOrEmpty(base64Pdf))
                    {
                        return new PdfDownloadResult
                        {
                            Success = true,
                            Base64Content = base64Pdf
                        };
                    }

                    return new PdfDownloadResult
                    {
                        Success = false,
                        ErrorMessage = "No PDF content in response"
                    };
                }

                return new PdfDownloadResult
                {
                    Success = false,
                    ErrorMessage = $"Request failed: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FusionPdfDownloader] Error: {ex.Message}");
                return new PdfDownloadResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<ReportDataResult> DownloadGenericReportAsync(
            string reportPath,
            string parameterName,
            string parameterValue,
            string instance,
            string username,
            string password)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[FusionPdfDownloader] Getting report data: {reportPath}");

                // For data extraction, request XML format instead of PDF
                string soapUrl = $"https://{instance}/xmlpserver/services/ExternalReportWSSService";

                string soapEnvelope = BuildSoapEnvelopeForData(reportPath, parameterName, parameterValue, username, password);

                var request = new HttpRequestMessage(HttpMethod.Post, soapUrl);
                request.Content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
                request.Headers.Add("SOAPAction", "");

                var response = await _httpClient.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    string base64Data = ExtractBase64FromSoapResponse(responseContent);

                    if (!string.IsNullOrEmpty(base64Data))
                    {
                        // Decode base64 to get XML data
                        byte[] dataBytes = Convert.FromBase64String(base64Data);
                        string xmlData = Encoding.UTF8.GetString(dataBytes);

                        return new ReportDataResult
                        {
                            Success = true,
                            XmlData = xmlData,
                            Base64Content = base64Data
                        };
                    }

                    return new ReportDataResult
                    {
                        Success = false,
                        ErrorMessage = "No data content in response"
                    };
                }

                return new ReportDataResult
                {
                    Success = false,
                    ErrorMessage = $"Request failed: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FusionPdfDownloader] Data Error: {ex.Message}");
                return new ReportDataResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private string BuildSoapEnvelope(string reportPath, string parameterName, string parameterValue, string username, string password)
        {
            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/""
               xmlns:pub=""http://xmlns.oracle.com/oxp/service/PublicReportService"">
    <soap:Header>
        <wsse:Security xmlns:wsse=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd"">
            <wsse:UsernameToken>
                <wsse:Username>{username}</wsse:Username>
                <wsse:Password>{password}</wsse:Password>
            </wsse:UsernameToken>
        </wsse:Security>
    </soap:Header>
    <soap:Body>
        <pub:runReport>
            <pub:reportRequest>
                <pub:reportAbsolutePath>{reportPath}</pub:reportAbsolutePath>
                <pub:parameterNameValues>
                    <pub:item>
                        <pub:name>{parameterName}</pub:name>
                        <pub:values>
                            <pub:item>{parameterValue}</pub:item>
                        </pub:values>
                    </pub:item>
                </pub:parameterNameValues>
                <pub:attributeFormat>pdf</pub:attributeFormat>
            </pub:reportRequest>
        </pub:runReport>
    </soap:Body>
</soap:Envelope>";
        }

        private string BuildSoapEnvelopeForData(string reportPath, string parameterName, string parameterValue, string username, string password)
        {
            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/""
               xmlns:pub=""http://xmlns.oracle.com/oxp/service/PublicReportService"">
    <soap:Header>
        <wsse:Security xmlns:wsse=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd"">
            <wsse:UsernameToken>
                <wsse:Username>{username}</wsse:Username>
                <wsse:Password>{password}</wsse:Password>
            </wsse:UsernameToken>
        </wsse:Security>
    </soap:Header>
    <soap:Body>
        <pub:runReport>
            <pub:reportRequest>
                <pub:reportAbsolutePath>{reportPath}</pub:reportAbsolutePath>
                <pub:parameterNameValues>
                    <pub:item>
                        <pub:name>{parameterName}</pub:name>
                        <pub:values>
                            <pub:item>{parameterValue}</pub:item>
                        </pub:values>
                    </pub:item>
                </pub:parameterNameValues>
                <pub:attributeFormat>xml</pub:attributeFormat>
            </pub:reportRequest>
        </pub:runReport>
    </soap:Body>
</soap:Envelope>";
        }

        private string ExtractBase64FromSoapResponse(string soapResponse)
        {
            try
            {
                var doc = XDocument.Parse(soapResponse);
                XNamespace pub = "http://xmlns.oracle.com/oxp/service/PublicReportService";

                var reportBytes = doc.Descendants(pub + "reportBytes").FirstOrDefault();
                if (reportBytes != null)
                {
                    return reportBytes.Value;
                }

                // Try alternative element names
                var reportData = doc.Descendants(pub + "reportData").FirstOrDefault();
                if (reportData != null)
                {
                    return reportData.Value;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FusionPdfDownloader] Parse Error: {ex.Message}");
            }

            return null;
        }
    }

    public class PdfDownloadResult
    {
        public bool Success { get; set; }
        public string Base64Content { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class ReportDataResult
    {
        public bool Success { get; set; }
        public string XmlData { get; set; }
        public string Base64Content { get; set; }
        public string ErrorMessage { get; set; }
    }
}

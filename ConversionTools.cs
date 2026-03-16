using System.ComponentModel;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace Aspose.HTML.Cloud.Mcp;

/// <summary>
/// MCP tools that expose Aspose.HTML Cloud URL-to-file conversion via the synchronous
/// REST endpoint  POST /v4.0/html/conversion/{from}-{to}/sync
/// </summary>
[McpServerToolType]
internal sealed class ConversionTools
{
    // Supported input formats whose URL-based conversion is well-tested
    private static readonly HashSet<string> SupportedInputFormats =
        new(StringComparer.OrdinalIgnoreCase) { "html", "mhtml", "xhtml", "epub", "svg", "md" };

    // Supported output formats
    private static readonly HashSet<string> SupportedOutputFormats =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "pdf", "xps", "docx", "doc", "jpeg", "png", "bmp", "gif", "tiff", "webp", "md", "mhtml", "svg"
        };

    private static readonly string ApiBase =
        Environment.GetEnvironmentVariable("ASPOSE_HTML_API_URL") ?? "https://api.aspose.cloud";

    private readonly AsposeHtmlAuth _auth;
    private readonly HttpClient _http;
    private readonly ILogger<ConversionTools> _logger;

    public ConversionTools(AsposeHtmlAuth auth, HttpClient http, ILogger<ConversionTools> logger)
    {
        _auth = auth;
        _http = http;
        _logger = logger;
    }

    /// <summary>
    /// Converts an HTML page (or MHTML / XHTML / EPUB / SVG / Markdown) referenced by a public URL
    /// to the requested output format and returns the converted file as a base-64 encoded string.
    /// </summary>
    [McpServerTool(Name = "convert_url_to_format")]
    [Description(
        "Converts a publicly accessible URL (HTML, MHTML, XHTML, EPUB, SVG, or Markdown page) " +
        "to a target document format using Aspose.HTML Cloud. " +
        "Returns the converted file encoded as a Base64 string together with metadata. " +
        "Supported input formats: html, mhtml, xhtml, epub, svg, md. " +
        "Supported output formats: pdf, xps, docx, doc, jpeg, png, bmp, gif, tiff, webp, md, mhtml, svg.")]
    public async Task<ConversionToolResult> ConvertUrlToFormat(
        [Description("Publicly accessible URL of the source document (e.g. https://example.com/page.html)")]
        string sourceUrl,
        [Description("Output format. Supported values: pdf, xps, docx, doc, jpeg, png, bmp, gif, tiff, webp, md, mhtml, svg")]
        string outputFormat,
        [Description("Input format of the source URL. Defaults to 'html'. Supported values: html, mhtml, xhtml, epub, svg, md")]
        string inputFormat = "html",
        CancellationToken cancellationToken = default)
    {
        inputFormat = inputFormat.ToLowerInvariant().Trim();
        outputFormat = outputFormat.ToLowerInvariant().Trim();

        _logger.LogInformation("convert_url_to_format called: sourceUrl={SourceUrl} inputFormat={InputFormat} outputFormat={OutputFormat}",
            sourceUrl, inputFormat, outputFormat);

        if (!SupportedInputFormats.Contains(inputFormat))
        {
            _logger.LogWarning("Rejected unsupported input format: {InputFormat}", inputFormat);
            return ConversionToolResult.Failure(
                $"Unsupported input format '{inputFormat}'. Supported: {string.Join(", ", SupportedInputFormats)}.");
        }

        if (!SupportedOutputFormats.Contains(outputFormat))
        {
            _logger.LogWarning("Rejected unsupported output format: {OutputFormat}", outputFormat);
            return ConversionToolResult.Failure(
                $"Unsupported output format '{outputFormat}'. Supported: {string.Join(", ", SupportedOutputFormats)}.");
        }

        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            _logger.LogWarning("Rejected invalid URL: {SourceUrl}", sourceUrl);
            return ConversionToolResult.Failure(
                $"'{sourceUrl}' is not a valid absolute HTTP/HTTPS URL.");
        }

        var token = await _auth.GetTokenAsync(cancellationToken);
        var endpoint = $"{ApiBase}/v4.0/html/conversion/{inputFormat}-{outputFormat}/sync";
        var requestBody = JsonSerializer.Serialize(new { InputPath = sourceUrl });

        _logger.LogInformation("POST {Endpoint}", endpoint);
        _logger.LogInformation("Request body: {RequestBody}", requestBody);

        using var requestContent = new StringContent(requestBody, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = requestContent;

        var sw = System.Diagnostics.Stopwatch.StartNew();
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
        sw.Stop();

        _logger.LogInformation("Response: {StatusCode} in {ElapsedMs}ms", (int)response.StatusCode, sw.ElapsedMilliseconds);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Conversion failed. Status={StatusCode} Body={ErrorBody}",
                (int)response.StatusCode, errorBody);
            return ConversionToolResult.Failure(
                $"Conversion API returned {(int)response.StatusCode} {response.StatusCode}: {errorBody}");
        }

        var fileBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        _logger.LogInformation("Conversion succeeded. Response size={Bytes} bytes", fileBytes.Length);

        var base64 = Convert.ToBase64String(fileBytes);

        return ConversionToolResult.Success(
            base64,
            outputFormat,
            fileBytes.Length,
            sourceUrl);
    }
}

/// <summary>Result returned by the <see cref="ConversionTools"/> MCP tools.</summary>
internal sealed class ConversionToolResult
{
    /// <summary>Whether the conversion succeeded.</summary>
    [JsonPropertyName("success")]
    public bool IsSuccess { get; init; }

    /// <summary>Human-readable status or error description.</summary>
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    /// <summary>Base64-encoded converted file content. Populated on success.</summary>
    [JsonPropertyName("fileBase64")]
    public string? FileBase64 { get; init; }

    /// <summary>Output format of the converted file (e.g. "pdf").</summary>
    [JsonPropertyName("outputFormat")]
    public string? OutputFormat { get; init; }

    /// <summary>Size in bytes of the converted file.</summary>
    [JsonPropertyName("fileSizeBytes")]
    public int? FileSizeBytes { get; init; }

    /// <summary>The original source URL that was converted.</summary>
    [JsonPropertyName("sourceUrl")]
    public string? SourceUrl { get; init; }

    internal static ConversionToolResult Success(string base64, string format, int sizeBytes, string sourceUrl) =>
        new()
        {
            IsSuccess = true,
            Message = $"Successfully converted to {format.ToUpperInvariant()} ({sizeBytes:N0} bytes).",
            FileBase64 = base64,
            OutputFormat = format,
            FileSizeBytes = sizeBytes,
            SourceUrl = sourceUrl
        };

    internal static ConversionToolResult Failure(string message) =>
        new() { IsSuccess = false, Message = message };
}

using System.ComponentModel;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace Aspose.HTML.Cloud.Mcp;

/// <summary>
/// MCP tools that expose Aspose.HTML Cloud conversion via the synchronous
/// REST endpoint  POST /v4.0/html/conversion/{from}-{to}/sync
/// </summary>
[McpServerToolType]
internal sealed class ConversionTools
{
    private static readonly HashSet<string> SupportedInputFormats =
        new(StringComparer.OrdinalIgnoreCase) { "html", "mhtml", "xhtml", "epub", "svg", "md" };

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

    // ?? convert_url_to_format ????????????????????????????????????????????????

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

        _logger.LogInformation("convert_url_to_format: sourceUrl={SourceUrl} {InputFormat}->{OutputFormat}",
            sourceUrl, inputFormat, outputFormat);

        var validation = ValidateFormats(inputFormat, outputFormat);
        if (validation != null) return validation;

        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return ConversionToolResult.Failure($"'{sourceUrl}' is not a valid absolute HTTP/HTTPS URL.");
        }

        var requestBody = JsonSerializer.Serialize(new { InputPath = sourceUrl });
        return await ExecuteConversion(inputFormat, outputFormat, requestBody, sourceUrl, cancellationToken);
    }

    // ?? convert_content_to_format ????????????????????????????????????????????

    /// <summary>
    /// Converts raw HTML (or SVG, Markdown, etc.) content provided as a string
    /// to a target document format using Aspose.HTML Cloud.
    /// Use this when you have the markup content directly rather than a URL or file.
    /// Returns the converted file encoded as a Base64 string together with metadata.
    /// Supported input formats: html, mhtml, xhtml, epub, svg, md.
    /// Supported output formats: pdf, xps, docx, doc, jpeg, png, bmp, gif, tiff, webp, md, mhtml, svg.
    /// </summary>
    [McpServerTool(Name = "convert_content_to_format")]
    [Description(
        "Converts raw HTML (or SVG, Markdown, etc.) content provided as a string " +
        "to a target document format using Aspose.HTML Cloud. " +
        "Use this when you have the markup content directly rather than a URL or file. " +
        "Returns the converted file encoded as a Base64 string together with metadata. " +
        "Supported input formats: html, mhtml, xhtml, epub, svg, md. " +
        "Supported output formats: pdf, xps, docx, doc, jpeg, png, bmp, gif, tiff, webp, md, mhtml, svg.")]
    public async Task<ConversionToolResult> ConvertContentToFormat(
        [Description("The markup content to convert (e.g. '<html><body><h1>Hello</h1></body></html>')")]
        string content,
        [Description("Output format. Supported values: pdf, xps, docx, doc, jpeg, png, bmp, gif, tiff, webp, md, mhtml, svg")]
        string outputFormat,
        [Description("Input format of the content. Defaults to 'html'. Supported values: html, mhtml, xhtml, epub, svg, md")]
        string inputFormat = "html",
        CancellationToken cancellationToken = default)
    {
        inputFormat = inputFormat.ToLowerInvariant().Trim();
        outputFormat = outputFormat.ToLowerInvariant().Trim();

        _logger.LogInformation("convert_content_to_format: contentLength={Length} {InputFormat}->{OutputFormat}",
            content.Length, inputFormat, outputFormat);

        var validation = ValidateFormats(inputFormat, outputFormat);
        if (validation != null) return validation;

        if (string.IsNullOrWhiteSpace(content))
        {
            return ConversionToolResult.Failure("Content must not be empty.");
        }

        var requestBody = JsonSerializer.Serialize(new { InputContent = content });
        return await ExecuteConversion(inputFormat, outputFormat, requestBody, "(content)", cancellationToken);
    }

    // ?? convert_base64_to_format ?????????????????????????????????????????????

    /// <summary>
    /// Converts a Base64-encoded document (HTML, SVG, Markdown, etc.)
    /// to a target document format using Aspose.HTML Cloud.
    /// Use this when the source document is available as a Base64 string.
    /// Returns the converted file encoded as a Base64 string together with metadata.
    /// Supported input formats: html, mhtml, xhtml, epub, svg, md.
    /// Supported output formats: pdf, xps, docx, doc, jpeg, png, bmp, gif, tiff, webp, md, mhtml, svg.
    /// </summary>
    [McpServerTool(Name = "convert_base64_to_format")]
    [Description(
        "Converts a Base64-encoded document (HTML, SVG, Markdown, etc.) " +
        "to a target document format using Aspose.HTML Cloud. " +
        "Use this when the source document is available as a Base64 string. " +
        "Returns the converted file encoded as a Base64 string together with metadata. " +
        "Supported input formats: html, mhtml, xhtml, epub, svg, md. " +
        "Supported output formats: pdf, xps, docx, doc, jpeg, png, bmp, gif, tiff, webp, md, mhtml, svg.")]
    public async Task<ConversionToolResult> ConvertBase64ToFormat(
        [Description("Base64-encoded source document content")]
        string inputBase64,
        [Description("Output format. Supported values: pdf, xps, docx, doc, jpeg, png, bmp, gif, tiff, webp, md, mhtml, svg")]
        string outputFormat,
        [Description("Input format of the content. Defaults to 'html'. Supported values: html, mhtml, xhtml, epub, svg, md")]
        string inputFormat = "html",
        CancellationToken cancellationToken = default)
    {
        inputFormat = inputFormat.ToLowerInvariant().Trim();
        outputFormat = outputFormat.ToLowerInvariant().Trim();

        _logger.LogInformation("convert_base64_to_format: base64Length={Length} {InputFormat}->{OutputFormat}",
            inputBase64.Length, inputFormat, outputFormat);

        var validation = ValidateFormats(inputFormat, outputFormat);
        if (validation != null) return validation;

        if (string.IsNullOrWhiteSpace(inputBase64))
        {
            return ConversionToolResult.Failure("Base64 input must not be empty.");
        }

        var requestBody = JsonSerializer.Serialize(new { InputBase64 = inputBase64 });
        return await ExecuteConversion(inputFormat, outputFormat, requestBody, "(base64)", cancellationToken);
    }

    // ?? convert_file_to_format ???????????????????????????????????????????????

    /// <summary>
    /// Converts a local file (HTML, MHTML, XHTML, EPUB, SVG, or Markdown)
    /// to a target document format using Aspose.HTML Cloud.
    /// The file is uploaded to the cloud first, then converted.
    /// Returns the converted file encoded as a Base64 string together with metadata.
    /// Supported input formats: html, mhtml, xhtml, epub, svg, md.
    /// Supported output formats: pdf, xps, docx, doc, jpeg, png, bmp, gif, tiff, webp, md, mhtml, svg.
    /// </summary>
    [McpServerTool(Name = "convert_file_to_format")]
    [Description(
        "Converts a local file (HTML, MHTML, XHTML, EPUB, SVG, or Markdown) " +
        "to a target document format using Aspose.HTML Cloud. " +
        "The file is uploaded to the cloud first, then converted. " +
        "Returns the converted file encoded as a Base64 string together with metadata. " +
        "Supported input formats: html, mhtml, xhtml, epub, svg, md. " +
        "Supported output formats: pdf, xps, docx, doc, jpeg, png, bmp, gif, tiff, webp, md, mhtml, svg.")]
    public async Task<ConversionToolResult> ConvertFileToFormat(
        [Description("Absolute path to the local file to convert (e.g. C:\\docs\\page.html or /home/user/page.html)")]
        string filePath,
        [Description("Output format. Supported values: pdf, xps, docx, doc, jpeg, png, bmp, gif, tiff, webp, md, mhtml, svg")]
        string outputFormat,
        [Description("Input format of the file. Defaults to 'html'. Supported values: html, mhtml, xhtml, epub, svg, md")]
        string inputFormat = "html",
        CancellationToken cancellationToken = default)
    {
        inputFormat = inputFormat.ToLowerInvariant().Trim();
        outputFormat = outputFormat.ToLowerInvariant().Trim();

        _logger.LogInformation("convert_file_to_format: filePath={FilePath} {InputFormat}->{OutputFormat}",
            filePath, inputFormat, outputFormat);

        var validation = ValidateFormats(inputFormat, outputFormat);
        if (validation != null) return validation;

        if (!File.Exists(filePath))
        {
            return ConversionToolResult.Failure($"File not found: '{filePath}'.");
        }

        // Step 1: Upload file to Aspose Cloud storage
        string storagePath;
        try
        {
            storagePath = await UploadFileAsync(filePath, cancellationToken);
            _logger.LogInformation("File uploaded to storage: {StoragePath}", storagePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File upload failed for {FilePath}", filePath);
            return ConversionToolResult.Failure($"File upload failed: {ex.Message}");
        }

        // Step 2: Convert using the storage path
        var requestBody = JsonSerializer.Serialize(new { InputPath = storagePath });
        return await ExecuteConversion(inputFormat, outputFormat, requestBody, filePath, cancellationToken);
    }

    // ?? Shared helpers ???????????????????????????????????????????????????????

    private ConversionToolResult? ValidateFormats(string inputFormat, string outputFormat)
    {
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

        return null;
    }

    private async Task<ConversionToolResult> ExecuteConversion(
        string inputFormat, string outputFormat, string requestBody, string source,
        CancellationToken cancellationToken)
    {
        var token = await _auth.GetTokenAsync(cancellationToken);
        var endpoint = $"{ApiBase}/v4.0/html/conversion/{inputFormat}-{outputFormat}/sync";

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
        return ConversionToolResult.Success(base64, outputFormat, fileBytes.Length, source);
    }

    private async Task<string> UploadFileAsync(string filePath, CancellationToken cancellationToken)
    {
        var token = await _auth.GetTokenAsync(cancellationToken);
        var endpoint = $"{ApiBase}/v4.0/html/file";

        using var content = new MultipartFormDataContent();
        await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "file", Path.GetFileName(filePath));

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = content;

        var response = await _http.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Upload failed: {(int)response.StatusCode} {response.StatusCode} - {responseBody}");
        }

        // Parse the upload response to get the storage path
        using var doc = JsonDocument.Parse(responseBody);
        var uploaded = doc.RootElement.GetProperty("uploaded");
        if (uploaded.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Upload succeeded but no file paths were returned.");
        }

        return uploaded[0].GetString()
            ?? throw new InvalidOperationException("Upload returned a null file path.");
    }
}

/// <summary>Result returned by the <see cref="ConversionTools"/> MCP tools.</summary>
internal sealed class ConversionToolResult
{
    [JsonPropertyName("success")]
    public bool IsSuccess { get; init; }

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("fileBase64")]
    public string? FileBase64 { get; init; }

    [JsonPropertyName("outputFormat")]
    public string? OutputFormat { get; init; }

    [JsonPropertyName("fileSizeBytes")]
    public int? FileSizeBytes { get; init; }

    [JsonPropertyName("source")]
    public string? Source { get; init; }

    internal static ConversionToolResult Success(string base64, string format, int sizeBytes, string source) =>
        new()
        {
            IsSuccess = true,
            Message = $"Successfully converted to {format.ToUpperInvariant()} ({sizeBytes:N0} bytes).",
            FileBase64 = base64,
            OutputFormat = format,
            FileSizeBytes = sizeBytes,
            Source = source
        };

    internal static ConversionToolResult Failure(string message) =>
        new() { IsSuccess = false, Message = message };
}

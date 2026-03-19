# Aspose.HTML Cloud MCP Server

A [Model Context Protocol (MCP)](https://modelcontextprotocol.io/) server that exposes **Aspose.HTML Cloud** document conversion as tools for AI assistants like Claude Desktop, Cursor, VS Code Copilot, and other MCP-compatible clients.

## What it does

Converts documents between formats via the [Aspose.HTML Cloud API](https://products.aspose.cloud/html/).

| Supported input formats | Supported output formats |
|---|---|
| `html`, `mhtml`, `xhtml`, `epub`, `svg`, `md` | `pdf`, `xps`, `docx`, `doc`, `jpeg`, `png`, `bmp`, `gif`, `tiff`, `webp`, `md`, `mhtml`, `svg` |

**Example prompt in Claude Desktop:**

> "Convert https://example.com to PDF"

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- A free **Aspose Cloud** account — sign up at [dashboard.aspose.cloud](https://dashboard.aspose.cloud/) and create an application to get your **Client ID** and **Client Secret**

## Installation

```bash
git clone https://github.com/aspose-html-cloud/Aspose.HTML-Cloud-MCP.git
cd Aspose.HTML-Cloud-MCP
dotnet build
```

## Configuration with MCP Clients

Credentials are passed via environment variables `ASPOSE_CLIENT_ID` and `ASPOSE_CLIENT_SECRET`.

### Claude Desktop

Edit your Claude Desktop config file:

- **macOS:** `~/Library/Application Support/Claude/claude_desktop_config.json`
- **Windows:** `%APPDATA%\Claude\claude_desktop_config.json`

```json
{
  "mcpServers": {
    "aspose-html-cloud": {
      "command": "dotnet",
      "args": ["run", "--project", "/absolute/path/to/Aspose.HTML-Cloud-MCP"],
      "env": {
        "ASPOSE_CLIENT_ID": "your-client-id",
        "ASPOSE_CLIENT_SECRET": "your-client-secret"
      }
    }
  }
}
```

### VS Code (Copilot)

Add to your `.vscode/settings.json` or user settings:

```json
{
  "mcp": {
    "servers": {
      "aspose-html-cloud": {
        "command": "dotnet",
        "args": ["run", "--project", "/absolute/path/to/Aspose.HTML-Cloud-MCP"],
        "env": {
          "ASPOSE_CLIENT_ID": "your-client-id",
          "ASPOSE_CLIENT_SECRET": "your-client-secret"
        }
      }
    }
  }
}
```

### Cursor

Add to your Cursor MCP configuration (`~/.cursor/mcp.json`):

```json
{
  "mcpServers": {
    "aspose-html-cloud": {
      "command": "dotnet",
      "args": ["run", "--project", "/absolute/path/to/Aspose.HTML-Cloud-MCP"],
      "env": {
        "ASPOSE_CLIENT_ID": "your-client-id",
        "ASPOSE_CLIENT_SECRET": "your-client-secret"
      }
    }
  }
}
```

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.

The Aspose.HTML Cloud API itself requires a separate subscription — a free tier is available at [aspose.cloud](https://purchase.aspose.cloud/pricing).

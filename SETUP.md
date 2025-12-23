# Setup Guide - Studieassistenten

## Getting Started

### 1. Get a Claude API Key

This application uses **Claude AI** by Anthropic for generating study materials.

**Steps to get your API key:**

1. Go to https://console.anthropic.com/
2. Sign up or log in with your account
3. Navigate to **API Keys** section
4. Click **"Create Key"**
5. Give it a name (e.g., "Studieassistenten")
6. Copy the key (you won't see it again!)

**Pricing:** Claude has a free tier and pay-as-you-go pricing. Claude Sonnet 3.5 costs:
- Input: $3 per million tokens (~750,000 words)
- Output: $15 per million tokens
- For typical student use, costs are very low (a few cents per session)

### 2. Configure the Application

1. Open `Server/appsettings.Development.json`
2. Replace `"your-api-key-here"` with your actual API key:

```json
{
  "Anthropic": {
    "ApiKey": "sk-ant-api03-..." // Paste your key here
  }
}
```

**Important:** Never commit your API key to Git! This file is already in `.gitignore`.

### 3. Install Prerequisites

- **.NET 8.0 SDK**: https://dotnet.microsoft.com/download/dotnet/8.0
- **Tesseract OCR** (already configured with Swedish language support)

### 4. Run the Application

```bash
# From the root directory
cd StudieAssistenten/Server
dotnet run
```

The application will start at:
- **HTTPS**: https://localhost:7247
- **HTTP**: http://localhost:5059

### 5. Using the Application

1. **Upload a document** (PDF, image, or text file)
2. **Extract text** using the OCR button
   - Works with scanned PDFs and images
   - Automatically detects Swedish text
3. **Generate study materials**:
   - **Flashcards**: 10-15 Q&A cards in Swedish
   - **Practice Test**: 5-10 questions with answers
   - **Summary**: Condensed bullet-point summary

## Features

- ✅ **Multi-format support**: PDF, images (JPG, PNG), text files
- ✅ **Swedish OCR**: Tesseract with Swedish language pack
- ✅ **Scanned PDF support**: Automatically converts pages to images
- ✅ **AI-powered generation**: Claude 3.5 Sonnet for high-quality Swedish content
- ✅ **PWA ready**: Works offline after first load
- ✅ **Blazor WebAssembly**: Fast, modern UI

## Troubleshooting

### "Please set your Anthropic API key"
- Check that `appsettings.Development.json` has your real API key
- Make sure it's not still set to `"your-api-key-here"`

### OCR not working
- Verify Tesseract is installed: `dotnet ef --version` should work
- Check that `Server/tessdata/swe.traineddata` exists

### Build errors
```bash
cd StudieAssistenten
dotnet restore
dotnet build
```

## Technology Stack

- **Frontend**: Blazor WebAssembly, Bootstrap 5
- **Backend**: ASP.NET Core 8.0, SQLite
- **OCR**: Tesseract 5.2.0 with Swedish support
- **AI**: Anthropic Claude 3.5 Sonnet
- **PDF**: PdfPig, Docnet.Core

## Why Claude instead of GPT?

Claude 3.5 Sonnet excels at:
- ✅ **Swedish language**: Better understanding and generation
- ✅ **Educational content**: Creates clearer, more structured study materials
- ✅ **Cost-effective**: Generally cheaper than GPT-4
- ✅ **Safety**: Better at following instructions and avoiding inappropriate content

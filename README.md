# Studieassistenten - Phase 1 Complete! 🎉

[![CI - Build and Test](https://github.com/david-s-svedberg/studieassistenten/actions/workflows/ci.yml/badge.svg)](https://github.com/david-s-svedberg/studieassistenten/actions/workflows/ci.yml)
[![E2E Tests](https://github.com/david-s-svedberg/studieassistenten/actions/workflows/e2e-tests.yml/badge.svg)](https://github.com/david-s-svedberg/studieassistenten/actions/workflows/e2e-tests.yml)
![Test Count](https://img.shields.io/badge/tests-107%20total-brightgreen)
![Integration](https://img.shields.io/badge/integration-44%20passing-success)
![Component](https://img.shields.io/badge/component-54%20passing-success)
![E2E](https://img.shields.io/badge/e2e-8%20tests-blue)

## What's Been Implemented

Phase 1 of the Studieassistenten project is now complete! Here's what has been built:

### ✅ Completed Features

1. **Project Structure**
   - Blazor WebAssembly PWA (Client)
   - ASP.NET Core 8.0 Web API (Server)
   - Shared models and DTOs library
   - SQLite database with Entity Framework Core

2. **Database & Models**
   - `StudyDocument` - Stores uploaded materials
   - `GeneratedContent` - Stores AI-generated content
   - `Flashcard` - Stores individual flashcards
   - Complete database context with relationships
   - Enums for processing types and document status

3. **Server API**
   - File upload endpoint (`POST /api/documents/upload`)
   - Get all documents (`GET /api/documents`)
   - Get single document (`GET /api/documents/{id}`)
   - Delete document (`DELETE /api/documents/{id}`)
   - File validation (type, size limits)
   - Local file storage system

4. **Client Application**
   - Modern, responsive UI with Bootstrap
   - Home page with feature overview
   - Upload page with drag-and-drop support
   - Documents list page with status indicators
   - Navigation menu
   - Document service for API communication

## How to Run

### Development Mode

1. **Navigate to the Server directory:**
   ```bash
   cd StudieAssistenten/Server
   ```

2. **Run the application:**
   ```bash
   dotnet run
   ```

3. **Access the application:**
   - Open your browser to: `https://localhost:7xxx` (check console output for exact port)
   - The Blazor WebAssembly app will load automatically

### What You Can Do Now

1. **Upload Materials** - Upload PDF, images, or text files
2. **View Documents** - See all uploaded materials with status
3. **Delete Documents** - Remove materials you no longer need

## Project Structure

```
StudieAssistenten/
├── Client/                          # Blazor WebAssembly PWA
│   ├── Pages/
│   │   ├── Index.razor             # Home page
│   │   ├── Upload.razor            # File upload page
│   │   └── Documents.razor         # Documents list
│   ├── Services/
│   │   └── DocumentService.cs      # API communication
│   └── wwwroot/                    # Static assets & PWA files
│
├── Server/                          # ASP.NET Core Web API
│   ├── Controllers/
│   │   └── DocumentsController.cs  # Document API endpoints
│   ├── Services/
│   │   └── FileUploadService.cs    # File handling logic
│   ├── Data/
│   │   └── ApplicationDbContext.cs # EF Core context
│   └── uploads/                    # Uploaded files (created automatically)
│
└── Shared/                          # Shared models & DTOs
    ├── Models/                      # Domain models
    ├── DTOs/                        # Data transfer objects
    └── Enums/                       # Enumerations
```

## Database

- **Type:** SQLite
- **Location:** `StudieAssistenten/Server/studieassistenten.db` (created automatically on first run)
- **Tables:**
  - StudyDocuments
  - GeneratedContents
  - Flashcards

## API Endpoints

### Documents

- `POST /api/documents/upload` - Upload a new document
  - Form data: `file` (required), `teacherInstructions` (optional)
  - Max file size: 50MB
  - Supported formats: PDF, JPG, PNG, TXT, DOCX

- `GET /api/documents` - Get all documents

- `GET /api/documents/{id}` - Get a specific document

- `DELETE /api/documents/{id}` - Delete a document

## Next Steps (Phase 2)

The next phase will implement:

1. **OCR Integration**
   - Azure AI Vision or Tesseract integration
   - Text extraction from images and scanned PDFs
   - Swedish language support
   - Text preview and editing

2. **Text Processing**
   - Detect if uploaded file needs OCR
   - Extract text from PDFs directly
   - Allow manual text editing

## Configuration

### File Upload Settings

Edit `Server/Services/FileUploadService.cs` to change:
- Upload directory location
- File size limits
- Allowed file types

### Database Connection

Edit `Server/appsettings.json` to change the database connection:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=studieassistenten.db"
  }
}
```

## Troubleshooting

### Port Already in Use
If you get a port conflict, edit `Server/Properties/launchSettings.json` to change the port numbers.

### Database Issues
If you encounter database errors, delete the `studieassistenten.db` file and restart the application. It will be recreated automatically.

### File Upload Errors
- Ensure the `uploads` directory has write permissions
- Check file size doesn't exceed 50MB
- Verify file type is supported

## PWA Features (Already Configured)

The application is set up as a Progressive Web App with:
- Service worker for offline capability
- Install prompt support
- App manifest for installability

The PWA features will be more apparent when deployed to a web server with HTTPS.

## Technologies Used

- **.NET 8.0** - Latest .NET framework
- **Blazor WebAssembly** - Client-side web UI framework
- **ASP.NET Core** - Web API backend
- **Entity Framework Core 8.0** - ORM for database
- **SQLite** - Lightweight database
- **Bootstrap 5** - UI framework

## Development Notes

- The solution is configured for .NET 8.0
- CORS is enabled for development
- Database is created automatically on first run
- File uploads are stored in the `uploads` folder
- All projects use C# 12 with nullable reference types enabled

## License

This project is licensed under the **Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License**.

[![License: CC BY-NC-SA 4.0](https://img.shields.io/badge/License-CC%20BY--NC--SA%204.0-lightgrey.svg)](https://creativecommons.org/licenses/by-nc-sa/4.0/)

### What this means:

- ✅ **You can** use, modify, and share this code
- ✅ **You must** give appropriate credit
- ❌ **You cannot** use this code for commercial purposes
- ✅ **You must** share derivatives under the same license

For the full license terms, see [LICENSE.md](LICENSE.md) or visit the [Creative Commons website](https://creativecommons.org/licenses/by-nc-sa/4.0/).

## Ready to Continue?

Phase 1 provides a solid foundation with:
- ✅ Complete project structure
- ✅ Database and models
- ✅ File upload functionality
- ✅ Basic UI navigation

You can now:
1. Test the upload functionality
2. Review the code and make adjustments
3. Proceed to Phase 2 (OCR integration)
4. Or customize the UI further

Let me know when you're ready to continue with Phase 2!

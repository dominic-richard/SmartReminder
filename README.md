# Webreminder

Webreminder is a reminder app with an ASP.NET Core web interface and an optional
Windows desktop shell. Reminders can trigger notifications and a desktop pixel
overlay. Completing a due reminder requires uploading a proof document or image.

## Requirements

- .NET 10 SDK for the web app.
- Windows and the .NET 11 SDK for the WPF desktop app.
- Microsoft Edge WebView2 Runtime to run the desktop shell.

## Run the web app

From the project root:

```powershell
dotnet run --project ReminderWeb\ReminderWeb.csproj --urls http://localhost:5079
```

Open <http://localhost:5079> in a browser. The SQLite database is created as
`ReminderWeb\reminders.db`, and EF Core applies database migrations when the app
starts.

## Run the desktop app

On Windows, run:

```powershell
dotnet run --project ReminderDesktop\ReminderDesktop.csproj
```

The desktop shell hosts the web app in WebView2 and displays the pixel overlay
when a reminder starts a pixel effect.

## Proof uploads

After a reminder is due, choose **Finished** and upload a PDF, Word document
(`.doc` or `.docx`), or image (`.jpg`, `.jpeg`, `.png`, `.gif`, `.webp`, or
`.bmp`) up to 10 MB. The app stores uploads under
`ReminderWeb\App_Data\Proofs` and keeps their metadata in SQLite. Completed
reminders provide a link to download the saved proof.

Edit and Delete are available before a reminder is due. At the due time both
actions are hidden; Edit remains unavailable, and Delete becomes available
again after proof is submitted and the reminder is completed.

## Build

```powershell
dotnet build ReminderWeb\ReminderWeb.csproj
dotnet build ReminderDesktop\ReminderDesktop.csproj
```

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE).

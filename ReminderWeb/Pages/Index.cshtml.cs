using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReminderWeb.Data;
using ReminderWeb.Models;

namespace ReminderWeb.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ReminderDbContext _context;

        public IndexModel(ReminderDbContext context)
        {
            _context = context;
        }

        public List<Reminder> Reminders { get; set; } = new();

      public async Task OnGetAsync()
{
    Reminders = await _context.Reminders
        .Include(r => r.Zones)
        .OrderBy(r => r.ReminderDateTime)
        .ToListAsync();
}
        public async Task<IActionResult> OnPostCreateReminderAsync(
            string title,
            DateTime reminderDateTime)
        {
            var reminder = new Reminder
            {
                Title = title,
                ReminderDateTime = reminderDateTime
            };

            _context.Reminders.Add(reminder);

            await _context.SaveChangesAsync();
            // Create 5 zones for this reminder
const int zoneCount = 5;
const int zoneDurationMinutes = 5;

for (int i = 0; i < zoneCount; i++)
{
    var zone = new ReminderZone
    {
        ReminderId = reminder.Id,
        ZoneNumber = i + 1,
        StartTime = reminderDateTime.AddMinutes(i * zoneDurationMinutes),
        EndTime = reminderDateTime.AddMinutes((i + 1) * zoneDurationMinutes),
        IsCompleted = false,
        IsActive = false
    };

    _context.ReminderZones.Add(zone);
}

await _context.SaveChangesAsync();

            return new JsonResult(new
            {
                success = true,
                id = reminder.Id
            });
        }

        public async Task<IActionResult> OnPostDeleteReminderAsync(int id)
        {
            var reminder = await _context.Reminders.FindAsync(id);

            if (reminder == null)
            {
                return new JsonResult(new
                {
                    success = false
                });
            }

            _context.Reminders.Remove(reminder);

            await _context.SaveChangesAsync();

            return new JsonResult(new
            {
                success = true
            });
        }
        public async Task<IActionResult> OnPostEditReminderAsync(
    int id,
    string title,
    DateTime reminderDateTime)
{
    var reminder = await _context.Reminders.FindAsync(id);

    if (reminder == null)
    {
        return new JsonResult(new
        {
            success = false
        });
    }

    reminder.Title = title;
    reminder.ReminderDateTime = reminderDateTime;
    reminder.IsCompleted = false;
    reminder.CompletionProof = null;
    reminder.EmergencyPromptedAt = null;
    reminder.EmergencyResponse = null;
    reminder.PixelEffectStartedAt = null;

    await _context.SaveChangesAsync();

    return new JsonResult(new
    {
        success = true
    });
}

        public async Task<IActionResult> OnPostCompleteReminderAsync(
            int id,
            string? proof)
        {
            var trimmedProof = proof?.Trim();
            if (string.IsNullOrWhiteSpace(trimmedProof))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Please enter proof of completion."
                });
            }

            if (trimmedProof.Length > 2000)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Proof must be 2000 characters or fewer."
                });
            }

            var reminder = await _context.Reminders.FindAsync(id);
            if (reminder == null)
            {
                return NotFound(new
                {
                    success = false,
                    error = "Reminder not found."
                });
            }

            reminder.CompletionProof = trimmedProof;
            reminder.IsCompleted = true;
            reminder.EmergencyPromptedAt = null;
            reminder.EmergencyResponse = null;
            reminder.PixelEffectStartedAt = null;

            await _context.SaveChangesAsync();

            return new JsonResult(new
            {
                success = true
            });
        }

        public async Task<IActionResult> OnPostStartPixelsAsync(int id)
        {
            var reminder = await _context.Reminders.FindAsync(id);
            if (reminder == null || reminder.IsCompleted)
            {
                return NotFound(new
                {
                    success = false,
                    error = "An active reminder was not found."
                });
            }

            reminder.PixelEffectStartedAt ??= DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            return new JsonResult(new
            {
                success = true,
                pixelEffectStartedAt = reminder.PixelEffectStartedAt
            });
        }

        public async Task<IActionResult> OnPostStartEmergencyAsync(int id)
        {
            var reminder = await _context.Reminders.FindAsync(id);
            if (reminder == null || reminder.IsCompleted)
            {
                return NotFound(new
                {
                    success = false,
                    error = "An active reminder was not found."
                });
            }

            reminder.EmergencyPromptedAt = DateTimeOffset.UtcNow;
            reminder.EmergencyResponse = null;
            await _context.SaveChangesAsync();

            return new JsonResult(new
            {
                success = true,
                promptedAt = reminder.EmergencyPromptedAt,
                expiresAt = reminder.EmergencyPromptedAt.Value.AddHours(1)
            });
        }

        public async Task<IActionResult> OnPostRespondEmergencyAsync(
            int id,
            string? response)
        {
            if (response is not ("done" or "notDone"))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Select Done or Not Done."
                });
            }

            var reminder = await _context.Reminders.FindAsync(id);
            if (reminder == null || reminder.IsCompleted ||
                reminder.EmergencyPromptedAt == null)
            {
                return NotFound(new
                {
                    success = false,
                    error = "An active emergency prompt was not found."
                });
            }

            if (reminder.EmergencyResponse != null)
            {
                return new JsonResult(new
                {
                    success = true,
                    pixelEffectStartedAt = reminder.PixelEffectStartedAt
                });
            }

            var now = DateTimeOffset.UtcNow;
            reminder.EmergencyResponse = response;
            if (reminder.EmergencyPromptedAt.Value.AddHours(1) <= now ||
                response == "done")
            {
                reminder.PixelEffectStartedAt ??= now;
            }

            await _context.SaveChangesAsync();

            return new JsonResult(new
            {
                success = true,
                pixelEffectStartedAt = reminder.PixelEffectStartedAt
            });
        }

        public async Task<IActionResult> OnPostEmergencyTimeoutAsync(int id)
        {
            var reminder = await _context.Reminders.FindAsync(id);
            if (reminder == null || reminder.IsCompleted)
            {
                return NotFound(new
                {
                    success = false,
                    error = "An active reminder was not found."
                });
            }

            var now = DateTimeOffset.UtcNow;
            if (reminder.EmergencyPromptedAt == null ||
                reminder.EmergencyResponse != null ||
                reminder.EmergencyPromptedAt.Value.AddHours(1) > now)
            {
                return new JsonResult(new
                {
                    success = true,
                    started = false
                });
            }

            reminder.PixelEffectStartedAt ??= now;
            await _context.SaveChangesAsync();

            return new JsonResult(new
            {
                success = true,
                started = true,
                pixelEffectStartedAt = reminder.PixelEffectStartedAt
            });
        }
    }
}
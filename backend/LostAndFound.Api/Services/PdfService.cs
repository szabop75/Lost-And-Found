using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LostAndFound.Domain.Entities;
using LostAndFound.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;

namespace LostAndFound.Api.Services;

public class PdfService
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly IHttpContextAccessor _http;

    public PdfService(ApplicationDbContext db, IConfiguration config, IHttpContextAccessor http)
    {
        _db = db;
        _config = config;
        _http = http;
        QuestPDF.Settings.License = LicenseType.Community;
        // Enable extra diagnostics for layout issues in development
        QuestPDF.Settings.EnableDebugging = true;
    }

    private byte[] GenerateDocument(string title, Action<IContainer, FoundItem> compose, FoundItem item)
    {
        // Load logo from Branding config (supports any image type supported by QuestPDF)
        var logoPath = _config["Branding:LogoPath"]; // optional
        byte[]? logoBytes = null;
        string? depLogoSvg = null;
        string? logoSvg = null;
        if (!string.IsNullOrWhiteSpace(logoPath) && File.Exists(logoPath))
        {
            try
            {
                if (Path.GetExtension(logoPath).Equals(".svg", StringComparison.OrdinalIgnoreCase))
                    logoSvg = File.ReadAllText(logoPath);
                else
                    logoBytes = File.ReadAllBytes(logoPath);
            }
            catch { logoBytes = null; logoSvg = null; }
        }

        // Prepare QR with deposit number (if available) and generated timestamp
        byte[]? qrPngBytes = null;
        try
        {
            var idLabel = item.Deposit?.DepositNumber != null
                ? $"Leadási szám: {item.Deposit.DepositNumber}"
                : $"Ügyiratszám: {item.Id}";
            var qrText = $"{idLabel}\nKészült: {DateTime.Now:yyyy.MM.dd. HH:mm}";
            using var gen = new QRCodeGenerator();
            using var data = gen.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q);
            var png = new PngByteQRCode(data);
            qrPngBytes = png.GetGraphic(6);
        }
        catch { qrPngBytes = null; }

        var doc = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);

                page.Header().Row(row =>
                {
                    // Left: Logo
                    row.ConstantItem(80).Height(80).Element(e =>
                    {
                        if (!string.IsNullOrWhiteSpace(logoSvg))
                        {
                            e.AlignMiddle().AlignCenter().Svg(logoSvg).FitArea();
                        }
                        else if (logoBytes != null)
                        {
                            e.AlignMiddle().AlignCenter().Image(logoBytes).FitArea();
                        }
                        else
                        {
                            e.Border(1).BorderColor(Colors.Grey.Medium).AlignMiddle().AlignCenter().Text("LOGO").FontColor(Colors.Grey.Darken2);
                        }
                    });

                    // Middle: centered texts
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().AlignCenter().Text(title).Bold();
                        var org = _config["Branding:OrganizationName"] ?? "";
                        if (!string.IsNullOrWhiteSpace(org))
                            col.Item().AlignCenter().Text(org);

                        col.Item().AlignCenter().Text(text =>
                        {
                            if (item.Deposit?.DepositNumber != null)
                            {
                                text.Span("Leadási szám: ").SemiBold();
                                text.Span(item.Deposit.DepositNumber);
                            }
                            else
                            {
                                text.Span("Ügyiratszám: ").SemiBold();
                                text.Span(item.Id.ToString());
                            }
                        });
                        col.Item().AlignCenter().Text(text =>
                        {
                            text.Span("Készült: ").SemiBold();
                            text.Span(DateTime.Now.ToString("yyyy.MM.dd. HH:mm"));
                        });
                    });

                    // Right: QR code
                    row.ConstantItem(80).Height(80).Element(e =>
                    {
                        if (qrPngBytes != null)
                            e.AlignMiddle().AlignCenter().Image(qrPngBytes).FitArea();
                    });
                });

                page.Content().Element(cnt => compose(cnt, item));

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Készült: ");
                    text.Span(DateTime.Now.ToString("yyyy.MM.dd. HH:mm"));
                });
            });
        });

        return doc.GeneratePdf();
    }

    public async Task<byte[]?> GenerateOwnerHandover(Guid id)
    {
        var item = await _db.FoundItems
            .Include(i => i.OwnerClaims)
            .Include(i => i.Deposit)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (item == null) return null;

        var lastClaim = item.OwnerClaims.OrderBy(x => x.ReleasedAt).LastOrDefault();

        return GenerateDocument("Átadás-átvételi jegyzőkönyv", (content, it) =>
        {
            content.Column(col =>
            {
                col.Item().Text("Tárgy adatai").Bold();
                col.Item().Text($"Kategória: {it.Category}{(it.Category == "Egyéb" && !string.IsNullOrWhiteSpace(it.OtherCategoryText) ? " - " + it.OtherCategoryText : string.Empty)}");
                col.Item().Text($"Megtalálás helye: {it.Deposit?.FoundLocation ?? "-"}");
                col.Item().Text($"Megtalálás ideje: {(it.Deposit?.FoundAt.HasValue == true ? it.Deposit!.FoundAt!.Value.ToString("yyyy.MM.dd. HH:mm") : "-")}");
                col.Item().Text($"Leírás: {it.Details}");

                col.Item().PaddingVertical(10).LineHorizontal(1);

                col.Item().Text("Tulajdonos adatai").Bold();
                col.Item().Text($"Név: {lastClaim?.OwnerName ?? "________________________"}");
                col.Item().Text($"Lakcím: {lastClaim?.OwnerAddress ?? "________________________"}");
                col.Item().Text($"Igazolványszám: {lastClaim?.OwnerIdNumber ?? "________________________"}");

                col.Item().PaddingTop(30).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().LineHorizontal(1);
                        c.Item().Text("Átadó").AlignCenter();
                    });
                    row.Spacing(20);
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().LineHorizontal(1);
                        c.Item().Text("Tulajdonos").AlignCenter();
                    });
                });
            });
        }, item);
    }

    public async Task<byte[]?> GenerateOfficeHandover(Guid id)
    {
        var item = await _db.FoundItems
            .Include(i => i.CustodyLogs)
            .Include(i => i.Deposit)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (item == null) return null;

        var officeLog = item.CustodyLogs
            .Where(l => l.ActionType == "TransferToOffice")
            .OrderBy(x => x.Timestamp).LastOrDefault();

        return GenerateDocument("Átadás-átvételi jegyzőkönyv (Okmányiroda)", (content, it) =>
        {
            content.Column(col =>
            {
                col.Item().Text("Tárgy adatai").Bold();
                col.Item().Text($"Kategória: {it.Category}{(it.Category == "Egyéb" && !string.IsNullOrWhiteSpace(it.OtherCategoryText) ? " - " + it.OtherCategoryText : string.Empty)}");
                col.Item().Text($"Megtalálás helye: {it.Deposit?.FoundLocation ?? "-"}");
                col.Item().Text($"Megtalálás ideje: {(it.Deposit?.FoundAt.HasValue == true ? it.Deposit!.FoundAt!.Value.ToString("yyyy.MM.dd. HH:mm") : "-")}");
                col.Item().Text($"Leírás: {it.Details}");

                col.Item().PaddingVertical(10).LineHorizontal(1);
                col.Item().Text("Okmányirodai átadás").Bold();
                col.Item().Text($"Átadó: {officeLog?.ActorUserId ?? "________________________"}");
                col.Item().Text($"Átadás időpontja: {(officeLog != null ? officeLog.Timestamp.ToString("yyyy.MM.dd. HH:mm") : "________________________")}");
                if (!string.IsNullOrWhiteSpace(officeLog?.Notes))
                    col.Item().Text($"Megjegyzés: {officeLog!.Notes}");

                col.Item().PaddingTop(30).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().LineHorizontal(1);
                        c.Item().Text("Átadó").AlignCenter();
                    });
                    row.Spacing(20);
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().LineHorizontal(1);
                        c.Item().Text("Okmányiroda átvevő").AlignCenter();
                    });
                });
            });
        }, item);
    }

    public async Task<byte[]?> GenerateDisposal(Guid id)
    {
        var item = await _db.FoundItems
            .Include(i => i.CustodyLogs)
            .Include(i => i.Deposit)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (item == null) return null;

        var disposalLog = item.CustodyLogs
            .Where(l => l.ActionType == "Dispose")
            .OrderBy(x => x.Timestamp).LastOrDefault();

        return GenerateDocument("Selejtezési jegyzőkönyv", (content, it) =>
        {
            content.Column(col =>
            {
                col.Item().Text("Tárgy adatai").Bold();
                col.Item().Text($"Kategória: {it.Category}{(it.Category == "Egyéb" && !string.IsNullOrWhiteSpace(it.OtherCategoryText) ? " - " + it.OtherCategoryText : string.Empty)}");
                col.Item().Text($"Megtalálás helye: {it.Deposit?.FoundLocation ?? "-"}");
                col.Item().Text($"Megtalálás ideje: {(it.Deposit?.FoundAt.HasValue == true ? it.Deposit!.FoundAt!.Value.ToString("yyyy.MM.dd. HH:mm") : "-")}");
                col.Item().Text($"Leírás: {it.Details}");

                col.Item().PaddingVertical(10).LineHorizontal(1);
                col.Item().Text("Selejtezés").Bold();
                col.Item().Text($"Selejtezést végző: {disposalLog?.ActorUserId ?? "________________________"}");
                col.Item().Text($"Időpont: {(disposalLog != null ? disposalLog.Timestamp.ToString("yyyy.MM.dd. HH:mm") : "________________________")}");
                if (!string.IsNullOrWhiteSpace(disposalLog?.Notes))
                    col.Item().Text($"Megjegyzés: {disposalLog!.Notes}");

                col.Item().PaddingTop(30).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().LineHorizontal(1);
                        c.Item().Text("Selejtezést végző").AlignCenter();
                    });
                    row.Spacing(20);
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().LineHorizontal(1);
                        c.Item().Text("Tanú / Jóváhagyó").AlignCenter();
                    });
                });
            });
        }, item);
    }

    public async Task<byte[]?> GenerateDeposit(Guid depositId)
    {
        var dep = await _db.Deposits
            .Include(d => d.Items)
            .Include(d => d.BusLine)
            .Include(d => d.Driver)
            .Include(d => d.StorageLocation)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == depositId);
        if (dep == null) return null;

        // Best-effort retrieve item cash summaries if exist
        var itemIds = dep.Items.Select(i => i.Id).ToList();
        var cashEntries = await _db.FoundItemCashes
            .Where(c => itemIds.Contains(c.FoundItemId))
            .Include(c => c.Entries)
            .ThenInclude(e => e.CurrencyDenomination)
            .ToListAsync();

        // Resolve receiver (logged-in) display name
        string? receiverDisplay = dep.CustodianUserId;
        try
        {
            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserName == dep.CustodianUserId || u.Id == dep.CustodianUserId || u.Email == dep.CustodianUserId);
            if (user != null)
            {
                // Prefer FullName, then Email, then UserName
                var full = (user as dynamic)?.FullName as string; // ApplicationUser has FullName
                receiverDisplay = full ?? user.Email ?? user.UserName ?? dep.CustodianUserId;
            }
        }
        catch { }
        // Fallback to current HTTP context user if repository value was not set
        if (string.IsNullOrWhiteSpace(receiverDisplay))
        {
            var userPrincipal = _http?.HttpContext?.User;
            if (userPrincipal != null)
            {
                // Try common claim types
                var name = userPrincipal.Identity?.Name;
                var fullName = userPrincipal.FindFirst("FullName")?.Value
                               ?? userPrincipal.FindFirst("name")?.Value; // common OIDC
                var preferredUser = userPrincipal.FindFirst("preferred_username")?.Value;
                var given = userPrincipal.FindFirst("given_name")?.Value;
                var family = userPrincipal.FindFirst("family_name")?.Value;
                var composed = string.Join(" ", new[] { given, family }.Where(s => !string.IsNullOrWhiteSpace(s)));
                var email = userPrincipal.FindFirst(ClaimTypes.Email)?.Value
                             ?? userPrincipal.FindFirst("email")?.Value;
                // NameIdentifier / sub gives us the UserId in most setups
                var userId = userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? userPrincipal.FindFirst("sub")?.Value;

                // Prefer FullName > Name > Email if available directly
                receiverDisplay = fullName
                                  ?? (!string.IsNullOrWhiteSpace(composed) ? composed : null)
                                  ?? preferredUser
                                  ?? name
                                  ?? email
                                  ?? receiverDisplay;

                // If still empty but we have a userId, try DB lookup for a nicer display
                if (string.IsNullOrWhiteSpace(receiverDisplay) && !string.IsNullOrWhiteSpace(userId))
                {
                    try
                    {
                        var u = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
                        if (u != null)
                        {
                            var nice = (u as dynamic)?.FullName as string;
                            receiverDisplay = nice ?? u.Email ?? u.UserName ?? userId;
                        }
                    }
                    catch { }
                }

                // If still empty, try lookup by username/email candidates (as Account/me would)
                if (string.IsNullOrWhiteSpace(receiverDisplay))
                {
                    var candidates = new[] { preferredUser, name, email }
                        .Where(v => !string.IsNullOrWhiteSpace(v))
                        .Distinct()
                        .ToList();
                    foreach (var cand in candidates)
                    {
                        try
                        {
                            var u = await _db.Users.AsNoTracking()
                                .FirstOrDefaultAsync(x => x.UserName == cand || x.Email == cand);
                            if (u != null)
                            {
                                var nice = (u as dynamic)?.FullName as string;
                                receiverDisplay = nice ?? u.Email ?? u.UserName ?? cand;
                                break;
                            }
                        }
                        catch { }
                    }
                }
            }
        }

        // Try resolve logo
        byte[]? logoBytes = null;
        string? depLogoSvg = null;
        var cfgLogo = _config["Branding:DepositLogoPath"];
        string? resolvedLogoPath = null;
        if (!string.IsNullOrWhiteSpace(cfgLogo))
            resolvedLogoPath = cfgLogo;
        else
        {
            // Fallback to repository-relative path: frontend/public/icons/tbusz-logo-vertikal.svg
            // Navigate from backend bin folder up to repo root heuristically
            try
            {
                var baseDir = AppContext.BaseDirectory; // e.g., backend/bin/Debug/net7.0
                var candidate = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "frontend", "public", "icons", "tbusz-logo-vertikal.svg"));
                if (File.Exists(candidate)) resolvedLogoPath = candidate;
            }
            catch { }
        }
        if (!string.IsNullOrWhiteSpace(resolvedLogoPath) && File.Exists(resolvedLogoPath))
        {
            try
            {
                if (Path.GetExtension(resolvedLogoPath).Equals(".svg", StringComparison.OrdinalIgnoreCase))
                    depLogoSvg = File.ReadAllText(resolvedLogoPath);
                else
                    logoBytes = File.ReadAllBytes(resolvedLogoPath);
            }
            catch { logoBytes = null; depLogoSvg = null; }
        }

        // Prepare QR code content for header (deposit number + generated timestamp)
        byte[]? qrPngBytes = null;
        try
        {
            var qrText = $"Deposit: {dep.DepositNumber}\nGenerated: {DateTime.Now:yyyy.MM.dd. HH:mm}";
            using var gen = new QRCodeGenerator();
            using var data = gen.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q);
            var png = new PngByteQRCode(data);
            qrPngBytes = png.GetGraphic(6); // scale factor
        }
        catch { qrPngBytes = null; }

        return Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);

                page.Header().Row(row =>
                {
                    // Left: Logo
                    row.ConstantItem(72).Height(72).Element(e =>
                    {
                        if (!string.IsNullOrWhiteSpace(depLogoSvg))
                        {
                            e.AlignMiddle().AlignCenter().Svg(depLogoSvg).FitArea();
                        }
                        else if (logoBytes != null)
                        {
                            e.AlignMiddle().AlignCenter().Image(logoBytes).FitArea();
                        }
                        else
                        {
                            e.Border(1).BorderColor(Colors.Grey.Medium).AlignMiddle().AlignCenter().Text("LOGO").FontColor(Colors.Grey.Darken2);
                        }
                    });
                    // Middle: Centered texts
                    row.RelativeItem().Column(h =>
                    {
                        h.Item().AlignCenter().Text("Talált tárgy leadási jegyzőkönyv").FontSize(20).Bold();
                        h.Item().AlignCenter().Text(text =>
                        {
                            text.Span("Leadási szám: ").SemiBold();
                            text.Span(dep.DepositNumber);
                        });
                        h.Item().AlignCenter().Text(text =>
                        {
                            text.Span("Készült: ").SemiBold();
                            text.Span(DateTime.Now.ToString("yyyy.MM.dd. HH:mm"));
                        });
                    });
                    // Right: QR code with deposit number and generated time
                    row.ConstantItem(72).Height(72).Element(e =>
                    {
                        if (qrPngBytes != null)
                        {
                            e.AlignMiddle().AlignCenter().Image(qrPngBytes).FitArea();
                        }
                    });
                });

                page.Content().PaddingTop(20).Column(col =>
                {
                    col.Item().Text("Leadó adatai").Bold();

                    // Labels (indented by 10px) and values in separate columns
                    col.Item().Row(r =>
                    {
                        // Labels column
                        r.ConstantItem(140).Column(labels =>
                        {
                            labels.Item().PaddingLeft(10).Text("Név:");
                            labels.Item().PaddingLeft(10).Text("Lakcím:");
                            labels.Item().PaddingLeft(10).Text("E-mail:");
                            labels.Item().PaddingLeft(10).Text("Telefon:");
                            labels.Item().PaddingLeft(10).Text("Igazolványszám:");
                        });

                        r.Spacing(6);

                        // Values column
                        r.RelativeItem().Column(values =>
                        {
                            values.Item().Text(t => t.Span(dep.FinderName ?? "-").WrapAnywhere());
                            values.Item().Text(t => t.Span(dep.FinderAddress ?? "-").WrapAnywhere());
                            values.Item().Text(t => t.Span(dep.FinderEmail ?? "-").WrapAnywhere());
                            values.Item().Text(t => t.Span(dep.FinderPhone ?? "-").WrapAnywhere());
                            values.Item().Text(t => t.Span(dep.FinderIdNumber ?? "-").WrapAnywhere());
                        });
                    });

                    col.Item().PaddingVertical(8).LineHorizontal(1);

                    col.Item().Text("Megtalálás").Bold();
                    col.Item().Row(r =>
                    {
                        r.ConstantItem(140).Column(labels =>
                        {
                            labels.Item().PaddingLeft(10).Text("Hely:");
                            labels.Item().PaddingLeft(10).Text("Idő:");
                        });
                        r.Spacing(6);
                        r.RelativeItem().Column(values =>
                        {
                            values.Item().Text(t => t.Span(dep.FoundLocation ?? "-").WrapAnywhere());
                            values.Item().Text(t => t.Span(dep.FoundAt.HasValue ? dep.FoundAt.Value.ToString("yyyy.MM.dd. HH:mm") : "-").WrapAnywhere());
                        });
                    });

                    col.Item().PaddingVertical(8).LineHorizontal(1);

                    col.Item().Text("Közlekedési adatok").Bold();
                    col.Item().Row(r =>
                    {
                        r.ConstantItem(140).Column(labels =>
                        {
                            labels.Item().PaddingLeft(10).Text("Jármű rendszáma:");
                            labels.Item().PaddingLeft(10).Text("Vonal/irány:");
                            labels.Item().PaddingLeft(10).Text("Járművezető neve:");
                        });
                        r.Spacing(6);
                        r.RelativeItem().Column(values =>
                        {
                            values.Item().Text(t => t.Span(dep.LicensePlate ?? "-").WrapAnywhere());
                            values.Item().Text(t => t.Span(dep.BusLine?.Name ?? "-").WrapAnywhere());
                            values.Item().Text(t => t.Span(dep.Driver?.Name ?? "-").WrapAnywhere());
                        });
                    });

                    col.Item().PaddingVertical(8).LineHorizontal(1);

                    col.Item().Text("Tárolási adatok").Bold();
                    col.Item().Row(r =>
                    {
                        r.ConstantItem(140).Column(labels =>
                        {
                            labels.Item().PaddingLeft(10).Text("Tárolási hely:");
                        });
                        r.Spacing(6);
                        r.RelativeItem().Column(values =>
                        {
                            values.Item().Text(t => t.Span(dep.StorageLocation?.Name ?? "-").WrapAnywhere());
                        });
                    });

                    col.Item().PaddingVertical(8).LineHorizontal(1);

                    col.Item().Text("Tételek").Bold();
                    col.Item().Table(tbl =>
                    {
                        tbl.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(1); // sorszám
                            cols.RelativeColumn(2); // kategória
                            cols.RelativeColumn(7); // leírás (készpénz bontás alatt)
                        });

                        tbl.Header(h =>
                        {
                            h.Cell().Text("#").SemiBold();
                            h.Cell().Text("Kategória").SemiBold();
                            h.Cell().Text("Leírás").SemiBold();
                        });

                        foreach (var it in dep.Items.OrderBy(i => i.DepositSubIndex ?? 0))
                        {
                            var cash = cashEntries.FirstOrDefault(c => c.FoundItemId == it.Id);
                            tbl.Cell().Text((it.DepositSubIndex ?? 0).ToString());
                            tbl.Cell().Text(t => t.Span(it.Category + (it.Category == "Egyéb" && !string.IsNullOrWhiteSpace(it.OtherCategoryText) ? " - " + it.OtherCategoryText : string.Empty)).WrapAnywhere());
                            tbl.Cell().Text(t => t.Span(it.Details ?? string.Empty).WrapAnywhere());

                            // If the item is cash and has denomination entries, render a breakdown row below
                            if (string.Equals(it.Category, "Készpénz", StringComparison.OrdinalIgnoreCase)
                                && cash != null && cash.Entries != null && cash.Entries.Count > 0)
                            {
                                var ordered = cash.Entries
                                    .Where(e => e.Count > 0 && e.CurrencyDenomination != null)
                                    .OrderByDescending(e => e.CurrencyDenomination!.ValueMinor)
                                    .ToList();

                                if (ordered.Count > 0)
                                {
                                    tbl.Cell().ColumnSpan(3).Element(container =>
                                        container.PaddingLeft(20).Column(col =>
                                        {
                                            col.Item().Text("Címletjegyzék").SemiBold();
                                            col.Item().Table(dt =>
                                            {
                                                // 4 denomination groups per row; each group = [label, count, gutter]
                                                // maximize label width to avoid wrapping; compact gutter
                                                dt.ColumnsDefinition(cdef =>
                                                {
                                                    // group 1
                                                    cdef.RelativeColumn(6); // label 1
                                                    cdef.RelativeColumn(2); // count 1
                                                    cdef.ConstantColumn(4); // gutter 1
                                                    // group 2
                                                    cdef.RelativeColumn(6); // label 2
                                                    cdef.RelativeColumn(2); // count 2
                                                    cdef.ConstantColumn(4); // gutter 2
                                                    // group 3
                                                    cdef.RelativeColumn(6); // label 3
                                                    cdef.RelativeColumn(2); // count 3
                                                    cdef.ConstantColumn(4); // gutter 3
                                                    // group 4
                                                    cdef.RelativeColumn(6); // label 4
                                                    cdef.RelativeColumn(2); // count 4
                                                    cdef.ConstantColumn(4); // gutter 4
                                                });

                                                for (int i = 0; i < ordered.Count; i += 4)
                                                {
                                                    var count = Math.Min(4, ordered.Count - i);
                                                    for (int j = 0; j < count; j++)
                                                    {
                                                        var ent = ordered[i + j];
                                                        var label = ent.CurrencyDenomination!.Label;
                                                        // label cell with right padding to separate from count
                                                        dt.Cell().PaddingRight(1).Text(t => t.Span(label.Replace(" ", "\u00A0")).FontSize(9));
                                                        // count cell (left-aligned) with slight left padding to keep near label
                                                        dt.Cell().PaddingLeft(1).Text(t => t.Span(ent.Count.ToString("N0") + " db").FontSize(9));
                                                        // gutter cell
                                                        dt.Cell().Text("");
                                                    }
                                                    // fill remaining cells in the row if less than 4
                                                    for (int j = count; j < 4; j++)
                                                    {
                                                        dt.Cell().Text(""); // empty label
                                                        dt.Cell().Text(""); // empty count
                                                        dt.Cell().Text(""); // empty gutter
                                                    }
                                                }
                                            });
                                        })
                                    );
                                }
                            }
                        }
                    });

                    // Place and signatures
                    col.Item().PaddingTop(16).Text($"Tatabánya, {DateTime.Now:yyyy.MM.dd.}");
                    // Increase space above signatures for better separation
                    col.Item().PaddingTop(40).Row(r =>
                    {
                        r.RelativeItem().Column(cc =>
                        {
                            cc.Item().LineHorizontal(1);
                            var giverLabel = string.IsNullOrWhiteSpace(dep.FinderName) ? "Átadó:" : $"Átadó: {dep.FinderName}";
                            cc.Item().AlignCenter().Text(giverLabel);
                        });
                        r.Spacing(30);
                        r.RelativeItem().Column(cc =>
                        {
                            cc.Item().LineHorizontal(1);
                            var receiverLabel = string.IsNullOrWhiteSpace(receiverDisplay) ? "Átvevő:" : $"Átvevő: {receiverDisplay}";
                            cc.Item().AlignCenter().Text(receiverLabel);
                        });
                    });
                });

                page.Footer().AlignCenter().Text($"Leadás száma: {dep.DepositNumber}");
            });
        })
        .WithMetadata(new DocumentMetadata
        {
            Title = $"{dep.DepositNumber}_{DateTime.Now:yyyyMMdd}"
        })
        .GeneratePdf();
    }
}

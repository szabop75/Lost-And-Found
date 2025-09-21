using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LostAndFound.Domain.Entities;
using LostAndFound.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LostAndFound.Api.Services;

public class PdfService
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;

    public PdfService(ApplicationDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private byte[] GenerateDocument(string title, Action<IContainer, FoundItem> compose, FoundItem item)
    {
        var logoPath = _config["Branding:LogoPath"]; // optional
        byte[]? logoBytes = null;
        if (!string.IsNullOrWhiteSpace(logoPath) && File.Exists(logoPath))
        {
            logoBytes = File.ReadAllBytes(logoPath);
        }

        var doc = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(title);
                        col.Item().Text(_config["Branding:OrganizationName"] ?? "VETRASYS Kft.");
                        col.Item().Text(text =>
                        {
                            text.Span("Ügyiratszám: ").SemiBold();
                            text.Span(item.Id.ToString());
                        });
                    });
                    row.ConstantItem(80).Height(80).Element(e =>
                    {
                        if (logoBytes != null)
                        {
                            e.Border(1).BorderColor(Colors.Grey.Medium).AlignMiddle().AlignCenter().Image(logoBytes);
                        }
                        else
                        {
                            e.Border(1).BorderColor(Colors.Grey.Medium).AlignMiddle().AlignCenter().Text("LOGO").FontColor(Colors.Grey.Darken2);
                        }
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
            .FirstOrDefaultAsync(i => i.Id == id);
        if (item == null) return null;

        var lastClaim = item.OwnerClaims.OrderBy(x => x.ReleasedAt).LastOrDefault();

        return GenerateDocument("Átadás-átvételi jegyzőkönyv", (content, it) =>
        {
            content.Column(col =>
            {
                col.Item().Text("Tárgy adatai").Bold();
                col.Item().Text($"Kategória: {it.Category}{(it.Category == "Egyéb" && !string.IsNullOrWhiteSpace(it.OtherCategoryText) ? " - " + it.OtherCategoryText : string.Empty)}");
                col.Item().Text($"Megtalálás helye: {it.FoundLocation ?? "-"}");
                col.Item().Text($"Megtalálás ideje: {(it.FoundAt.HasValue ? it.FoundAt.Value.ToString("yyyy.MM.dd. HH:mm") : "-")}");
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
                col.Item().Text($"Megtalálás helye: {it.FoundLocation ?? "-"}");
                col.Item().Text($"Megtalálás ideje: {(it.FoundAt.HasValue ? it.FoundAt.Value.ToString("yyyy.MM.dd. HH:mm") : "-")}");
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
                col.Item().Text($"Megtalálás helye: {it.FoundLocation ?? "-"}");
                col.Item().Text($"Megtalálás ideje: {(it.FoundAt.HasValue ? it.FoundAt.Value.ToString("yyyy.MM.dd. HH:mm") : "-")}");
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
}

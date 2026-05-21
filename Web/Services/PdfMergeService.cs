using iText.IO.Image;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Utils;
using IOPath = System.IO.Path;

namespace onePdfFile.Web.Services;

public class PdfMergeService
{
    private static readonly HashSet<string> ImageExts =
        [".png", ".jpg", ".jpeg", ".tif", ".tiff", ".bmp", ".gif", ".webp", ".svg"];

    public void Merge(IEnumerable<string> inputPaths, string outputPath)
    {
        using var writer = new PdfWriter(outputPath);
        using var destDoc = new PdfDocument(writer);

        foreach (var file in inputPaths)
        {
            string ext = IOPath.GetExtension(file).ToLowerInvariant();
            if (ext == ".pdf")
            {
                using var srcDoc = new PdfDocument(new PdfReader(file));
                new PdfMerger(destDoc).Merge(srcDoc, 1, srcDoc.GetNumberOfPages());
            }
            else if (ImageExts.Contains(ext))
            {
                AddImagePage(destDoc, file);
            }
        }
    }

    private static void AddImagePage(PdfDocument destDoc, string imagePath)
    {
        ImageData imgData;
        string ext = IOPath.GetExtension(imagePath).ToLowerInvariant();

        if (ext == ".webp")
        {
            using var bmp = System.Drawing.Image.FromFile(imagePath);
            using var ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            imgData = ImageDataFactory.Create(ms.ToArray());
        }
        else if (ext == ".svg")
        {
            var svgDoc = Svg.SvgDocument.Open(imagePath);
            using var bmp = svgDoc.Draw();
            using var ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            imgData = ImageDataFactory.Create(ms.ToArray());
        }
        else
        {
            imgData = ImageDataFactory.Create(imagePath);
        }

        var pageSize = PageSize.A4;
        var page = destDoc.AddNewPage(pageSize);
        var canvas = new PdfCanvas(page);

        float imgW = imgData.GetWidth();
        float imgH = imgData.GetHeight();
        float pageW = pageSize.GetWidth();
        float pageH = pageSize.GetHeight();
        float scale = Math.Min(pageW / imgW, pageH / imgH);

        canvas.AddImageWithTransformationMatrix(
            imgData,
            imgW * scale, 0, 0, imgH * scale,
            (pageW - imgW * scale) / 2f,
            (pageH - imgH * scale) / 2f,
            false);
        canvas.Release();
    }
}

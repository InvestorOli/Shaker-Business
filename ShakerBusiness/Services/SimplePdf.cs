using System.Globalization;
using System.Text;

namespace ShakerBusiness.Services;

public enum PdfFont
{
    Regular,
    Bold,
    Italic,
}

public readonly record struct PdfColor(double R, double G, double B)
{
    public static PdfColor FromHex(int rgb) => new(((rgb >> 16) & 0xFF) / 255.0, ((rgb >> 8) & 0xFF) / 255.0, (rgb & 0xFF) / 255.0);
}

public abstract record PdfShape;

public sealed record PdfRect(double X, double Y, double Width, double Height, PdfColor? Fill, PdfColor? Stroke, double LineWidth) : PdfShape;

public sealed record PdfLine(double X1, double Y1, double X2, double Y2, PdfColor Color, double LineWidth) : PdfShape;

public sealed record PdfPolygon(IReadOnlyList<(double X, double Y)> Points, PdfColor Fill, PdfColor Stroke, double LineWidth) : PdfShape;

public sealed record PdfLabel(double X, double Y, string Text, PdfFont Font, double Size, PdfColor Color) : PdfShape;

public sealed class SimplePdfPage(double width, double height)
{
    private static readonly int[] RegularWidths =
    [
        278, 278, 355, 556, 556, 889, 667, 191, 333, 333, 389, 584, 278, 333, 278, 278,
        556, 556, 556, 556, 556, 556, 556, 556, 556, 556, 278, 278, 584, 584, 584, 556,
        1015, 667, 667, 722, 722, 667, 611, 778, 722, 278, 500, 667, 556, 833, 722, 778,
        667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 278, 278, 278, 469, 556,
        333, 556, 556, 500, 556, 556, 278, 556, 556, 222, 222, 500, 222, 833, 556, 556,
        556, 556, 333, 500, 278, 556, 500, 722, 500, 500, 500, 334, 260, 334, 584,
    ];

    private static readonly int[] BoldWidths =
    [
        278, 333, 474, 556, 556, 889, 722, 238, 333, 333, 389, 584, 278, 333, 278, 278,
        556, 556, 556, 556, 556, 556, 556, 556, 556, 556, 333, 333, 584, 584, 584, 611,
        975, 722, 722, 722, 722, 667, 611, 778, 722, 278, 556, 722, 611, 833, 722, 778,
        667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 333, 278, 333, 584, 556,
        333, 556, 611, 556, 611, 556, 333, 611, 611, 278, 278, 556, 278, 889, 611, 611,
        611, 611, 389, 556, 333, 611, 556, 778, 556, 556, 500, 389, 280, 389, 584,
    ];

    private const int FirstPrintable = 32;
    private const int SharpSWidth = 611;
    private const int FallbackWidth = 556;
    private const int CircleSegments = 48;
    private const string SvgFontFamily = "Helvetica, Arial, sans-serif";

    private static readonly Dictionary<char, char> UmlautBase = new()
    {
        ['ä'] = 'a', ['ö'] = 'o', ['ü'] = 'u', ['Ä'] = 'A', ['Ö'] = 'O', ['Ü'] = 'U',
        ['é'] = 'e', ['è'] = 'e', ['á'] = 'a', ['à'] = 'a', ['í'] = 'i', ['ó'] = 'o', ['ú'] = 'u',
    };

    private readonly List<PdfShape> _shapes = [];

    public double Width => width;

    public double Height => height;

    public IReadOnlyList<PdfShape> Shapes => _shapes;

    public static double MeasureText(string text, PdfFont font, double size)
    {
        var table = font == PdfFont.Bold ? BoldWidths : RegularWidths;
        var total = 0;
        foreach (var raw in text)
        {
            total += CharWidth(raw, table);
        }

        return total * size / 1000.0;
    }

    public static IReadOnlyList<string> WrapText(string text, PdfFont font, double size, double maxWidth)
    {
        var lines = new List<string>();
        var line = new StringBuilder();
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = line.Length == 0 ? word : line.ToString() + " " + word;
            if (line.Length > 0 && MeasureText(candidate, font, size) > maxWidth)
            {
                lines.Add(line.ToString());
                line.Clear();
                candidate = word;
            }

            line.Clear();
            line.Append(candidate);
        }

        if (line.Length > 0)
        {
            lines.Add(line.ToString());
        }

        return lines;
    }

    public void FillRect(double x, double y, double w, double h, PdfColor color) =>
        _shapes.Add(new PdfRect(x, y, w, h, color, null, 0));

    public void StrokeRect(double x, double y, double w, double h, PdfColor color, double lineWidth) =>
        _shapes.Add(new PdfRect(x, y, w, h, null, color, lineWidth));

    public void Line(double x1, double y1, double x2, double y2, PdfColor color, double lineWidth) =>
        _shapes.Add(new PdfLine(x1, y1, x2, y2, color, lineWidth));

    public void Star(double centerX, double centerY, double outerRadius, PdfColor fill, PdfColor stroke, double lineWidth)
    {
        var innerRadius = outerRadius * 0.4;
        var points = new List<(double X, double Y)>();
        for (var point = 0; point < 10; point++)
        {
            var radius = point % 2 == 0 ? outerRadius : innerRadius;
            var angle = (Math.PI / 2) + (point * Math.PI / 5);
            points.Add((centerX + (radius * Math.Cos(angle)), centerY + (radius * Math.Sin(angle))));
        }

        _shapes.Add(new PdfPolygon(points, fill, stroke, lineWidth));
    }

    public void Circle(double centerX, double centerY, double radius, PdfColor fill, PdfColor stroke, double lineWidth)
    {
        var points = new List<(double X, double Y)>();
        for (var segment = 0; segment < CircleSegments; segment++)
        {
            var angle = 2 * Math.PI * segment / CircleSegments;
            points.Add((centerX + (radius * Math.Cos(angle)), centerY + (radius * Math.Sin(angle))));
        }

        _shapes.Add(new PdfPolygon(points, fill, stroke, lineWidth));
    }

    public void Text(double x, double y, string text, PdfFont font, double size, PdfColor color) =>
        _shapes.Add(new PdfLabel(x, y, text, font, size, color));

    public void CenteredText(double y, string text, PdfFont font, double size, PdfColor color) =>
        Text((width - MeasureText(text, font, size)) / 2, y, text, font, size, color);

    public void CenteredParagraph(double topY, IReadOnlyList<string> lines, PdfFont font, double size, double lineHeight, PdfColor color)
    {
        for (var index = 0; index < lines.Count; index++)
        {
            CenteredText(topY - (index * lineHeight), lines[index], font, size, color);
        }
    }

    public string ToPdfContent()
    {
        var content = new StringBuilder();
        foreach (var shape in _shapes)
        {
            switch (shape)
            {
                case PdfRect { Fill: { } fill } rect:
                    content.Append($"{Rgb(fill, fill: true)} {N(rect.X)} {N(rect.Y)} {N(rect.Width)} {N(rect.Height)} re f\n");
                    break;
                case PdfRect { Stroke: { } stroke } rect:
                    content.Append($"{Rgb(stroke, fill: false)} {N(rect.LineWidth)} w {N(rect.X)} {N(rect.Y)} {N(rect.Width)} {N(rect.Height)} re S\n");
                    break;
                case PdfLine line:
                    content.Append($"{Rgb(line.Color, fill: false)} {N(line.LineWidth)} w {N(line.X1)} {N(line.Y1)} m {N(line.X2)} {N(line.Y2)} l S\n");
                    break;
                case PdfPolygon polygon:
                    var path = string.Join(" ", polygon.Points.Select((p, index) => $"{N(p.X)} {N(p.Y)} {(index == 0 ? "m" : "l")}"));
                    content.Append($"{Rgb(polygon.Fill, fill: true)} {Rgb(polygon.Stroke, fill: false)} {N(polygon.LineWidth)} w {path} h B\n");
                    break;
                case PdfLabel label:
                    content.Append($"BT {Rgb(label.Color, fill: true)} /{FontName(label.Font)} {N(label.Size)} Tf {N(label.X)} {N(label.Y)} Td ({EscapePdf(label.Text)}) Tj ET\n");
                    break;
            }
        }

        return content.ToString();
    }

    public string ToSvg()
    {
        var svg = new StringBuilder();
        svg.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {N(width)} {N(height)}\" width=\"{N(width)}\" height=\"{N(height)}\">\n");
        foreach (var shape in _shapes)
        {
            switch (shape)
            {
                case PdfRect { Fill: { } fill } rect:
                    svg.Append($"<rect x=\"{N(rect.X)}\" y=\"{N(height - rect.Y - rect.Height)}\" width=\"{N(rect.Width)}\" height=\"{N(rect.Height)}\" fill=\"{Hex(fill)}\"/>\n");
                    break;
                case PdfRect { Stroke: { } stroke } rect:
                    svg.Append($"<rect x=\"{N(rect.X)}\" y=\"{N(height - rect.Y - rect.Height)}\" width=\"{N(rect.Width)}\" height=\"{N(rect.Height)}\" fill=\"none\" stroke=\"{Hex(stroke)}\" stroke-width=\"{N(rect.LineWidth)}\"/>\n");
                    break;
                case PdfLine line:
                    svg.Append($"<line x1=\"{N(line.X1)}\" y1=\"{N(height - line.Y1)}\" x2=\"{N(line.X2)}\" y2=\"{N(height - line.Y2)}\" stroke=\"{Hex(line.Color)}\" stroke-width=\"{N(line.LineWidth)}\"/>\n");
                    break;
                case PdfPolygon polygon:
                    var points = string.Join(" ", polygon.Points.Select(p => $"{N(p.X)},{N(height - p.Y)}"));
                    svg.Append($"<polygon points=\"{points}\" fill=\"{Hex(polygon.Fill)}\" stroke=\"{Hex(polygon.Stroke)}\" stroke-width=\"{N(polygon.LineWidth)}\" stroke-linejoin=\"miter\"/>\n");
                    break;
                case PdfLabel label:
                    svg.Append($"<text x=\"{N(label.X)}\" y=\"{N(height - label.Y)}\" font-family=\"{SvgFontFamily}\" font-size=\"{N(label.Size)}\" font-weight=\"{(label.Font == PdfFont.Bold ? "bold" : "normal")}\" font-style=\"{(label.Font == PdfFont.Italic ? "italic" : "normal")}\" fill=\"{Hex(label.Color)}\" xml:space=\"preserve\">{EscapeXml(label.Text)}</text>\n");
                    break;
            }
        }

        svg.Append("</svg>\n");
        return svg.ToString();
    }

    internal static string FontName(PdfFont font) => font switch
    {
        PdfFont.Bold => "F2",
        PdfFont.Italic => "F3",
        _ => "F1",
    };

    internal static string BaseFontName(PdfFont font) => font switch
    {
        PdfFont.Bold => "Helvetica-Bold",
        PdfFont.Italic => "Helvetica-Oblique",
        _ => "Helvetica",
    };

    private static int CharWidth(char raw, int[] table)
    {
        if (raw == 'ß')
        {
            return SharpSWidth;
        }

        var c = UmlautBase.GetValueOrDefault(raw, raw);
        var index = c - FirstPrintable;
        return index >= 0 && index < table.Length ? table[index] : FallbackWidth;
    }

    private static string EscapePdf(string text)
    {
        var builder = new StringBuilder();
        foreach (var c in text)
        {
            var code = c is >= ' ' and <= '~' or >= ' ' and <= 'ÿ' ? c : '?';
            if (code is '(' or ')' or '\\')
            {
                builder.Append('\\').Append(code);
            }
            else if (code > '~')
            {
                builder.Append('\\').Append(Convert.ToString((int)code, 8).PadLeft(3, '0'));
            }
            else
            {
                builder.Append(code);
            }
        }

        return builder.ToString();
    }

    private static string EscapeXml(string text)
    {
        var builder = new StringBuilder();
        foreach (var c in text)
        {
            var code = c is >= ' ' and <= '~' or >= ' ' and <= 'ÿ' ? c : '?';
            builder.Append(code switch
            {
                '&' => "&amp;",
                '<' => "&lt;",
                '>' => "&gt;",
                '"' => "&quot;",
                '\'' => "&apos;",
                _ => code.ToString(),
            });
        }

        return builder.ToString();
    }

    private static string N(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string Hex(PdfColor color) =>
        $"#{(int)Math.Round(color.R * 255):X2}{(int)Math.Round(color.G * 255):X2}{(int)Math.Round(color.B * 255):X2}";

    private static string Rgb(PdfColor color, bool fill) =>
        $"{N(color.R)} {N(color.G)} {N(color.B)} {(fill ? "rg" : "RG")}";
}

public static class SimplePdfWriter
{
    private static readonly Encoding Latin1 = Encoding.Latin1;

    public static byte[] Write(SimplePdfPage page, string title, string author)
    {
        var content = Latin1.GetBytes(page.ToPdfContent());
        var objects = new List<byte[]>
        {
            Latin1.GetBytes("<< /Type /Catalog /Pages 2 0 R >>"),
            Latin1.GetBytes("<< /Type /Pages /Kids [3 0 R] /Count 1 >>"),
            Latin1.GetBytes($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {Num(page.Width)} {Num(page.Height)}] /Contents 4 0 R /Resources << /Font << /F1 5 0 R /F2 6 0 R /F3 7 0 R >> >> >>"),
            StreamObject(content),
            FontObject(PdfFont.Regular),
            FontObject(PdfFont.Bold),
            FontObject(PdfFont.Italic),
            Latin1.GetBytes($"<< /Title ({EscapeInfo(title)}) /Author ({EscapeInfo(author)}) /Producer (Shaker Business) >>"),
        };

        using var stream = new MemoryStream();
        WriteAscii(stream, "%PDF-1.4\n");
        var offsets = new List<long>();
        for (var index = 0; index < objects.Count; index++)
        {
            offsets.Add(stream.Position);
            WriteAscii(stream, $"{index + 1} 0 obj\n");
            stream.Write(objects[index]);
            WriteAscii(stream, "\nendobj\n");
        }

        var xrefPosition = stream.Position;
        WriteAscii(stream, $"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            WriteAscii(stream, $"{offset:D10} 00000 n \n");
        }

        WriteAscii(stream, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R /Info {objects.Count} 0 R >>\nstartxref\n{xrefPosition}\n%%EOF\n");
        return stream.ToArray();
    }

    private static byte[] StreamObject(byte[] content)
    {
        using var stream = new MemoryStream();
        WriteAscii(stream, $"<< /Length {content.Length} >>\nstream\n");
        stream.Write(content);
        WriteAscii(stream, "\nendstream");
        return stream.ToArray();
    }

    private static byte[] FontObject(PdfFont font) =>
        Latin1.GetBytes($"<< /Type /Font /Subtype /Type1 /BaseFont /{SimplePdfPage.BaseFontName(font)} /Encoding /WinAnsiEncoding >>");

    private static string EscapeInfo(string text) =>
        new(text.Where(c => c is >= ' ' and <= '~' && c is not ('(' or ')' or '\\')).ToArray());

    private static string Num(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private static void WriteAscii(Stream stream, string text) => stream.Write(Encoding.ASCII.GetBytes(text));
}

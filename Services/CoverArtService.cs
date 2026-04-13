using System.Globalization;
using System.Net;
using System.Text;
using MusicStoreApp.Utilities;

namespace MusicStoreApp.Services;

public sealed class CoverArtService
{
    private readonly SongGenerator _songGenerator;

    public CoverArtService(SongGenerator songGenerator)
    {
        _songGenerator = songGenerator;
    }

    public string Generate(string locale, string seed, int index)
    {
        var record = _songGenerator.GenerateRecord(locale, seed, 0, index);
        var random = new StableRandom(StableRandom.Compose(StableRandom.ComposeString(locale, seed), (ulong)index, 0xC0FEFEUL));
        var background = BuildBackground(random);
        var accent = Hsl(random.Next(0, 360), random.Next(58, 82), random.Next(52, 70));
        var accentTwo = Hsl(random.Next(0, 360), random.Next(36, 70), random.Next(24, 44));
        var accentThree = Hsl(random.Next(0, 360), random.Next(42, 78), random.Next(48, 74));
        var highlight = Hsl(random.Next(0, 360), random.Next(70, 94), random.Next(68, 84));
        var textColor = "#fdf7ed";
        var variant = random.Next(0, 5);
        var rotation = random.Next(-18, 19);

        var builder = new StringBuilder();
        builder.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 420 420\" role=\"img\" aria-label=\"Album cover\">");
        builder.AppendLine("  <defs>");
        builder.AppendLine($"    <linearGradient id=\"g\" x1=\"0\" y1=\"0\" x2=\"1\" y2=\"1\">");
        builder.AppendLine($"      <stop offset=\"0%\" stop-color=\"{background.Item1}\" />");
        builder.AppendLine($"      <stop offset=\"48%\" stop-color=\"{accentTwo}\" />");
        builder.AppendLine($"      <stop offset=\"100%\" stop-color=\"{background.Item2}\" />");
        builder.AppendLine("    </linearGradient>");
        builder.AppendLine($"    <radialGradient id=\"glow\" cx=\"28%\" cy=\"20%\" r=\"88%\">");
        builder.AppendLine($"      <stop offset=\"0%\" stop-color=\"{highlight}\" stop-opacity=\"0.85\" />");
        builder.AppendLine($"      <stop offset=\"42%\" stop-color=\"{accent}\" stop-opacity=\"0.34\" />");
        builder.AppendLine("      <stop offset=\"100%\" stop-color=\"#ffffff\" stop-opacity=\"0\" />");
        builder.AppendLine("    </radialGradient>");
        builder.AppendLine("    <filter id=\"grain\" x=\"-20%\" y=\"-20%\" width=\"140%\" height=\"140%\">");
        builder.AppendLine("      <feTurbulence type=\"fractalNoise\" baseFrequency=\"0.95\" numOctaves=\"2\" stitchTiles=\"stitch\" />");
        builder.AppendLine("      <feColorMatrix type=\"saturate\" values=\"0\" />");
        builder.AppendLine("      <feComponentTransfer>");
        builder.AppendLine("        <feFuncA type=\"table\" tableValues=\"0 0.06\" />");
        builder.AppendLine("      </feComponentTransfer>");
        builder.AppendLine("    </filter>");
        builder.AppendLine("    <filter id=\"softBlur\" x=\"-20%\" y=\"-20%\" width=\"140%\" height=\"140%\">");
        builder.AppendLine("      <feGaussianBlur stdDeviation=\"18\" />");
        builder.AppendLine("    </filter>");
        builder.AppendLine("  </defs>");
        builder.AppendLine("  <rect width=\"420\" height=\"420\" fill=\"url(#g)\" rx=\"34\" />");
        builder.AppendLine("  <rect width=\"420\" height=\"420\" fill=\"url(#glow)\" rx=\"34\" opacity=\"0.82\" />");
        builder.AppendLine($"  <g opacity=\"0.28\" transform=\"rotate({rotation.ToString(CultureInfo.InvariantCulture)} 210 210)\">");
        builder.AppendLine($"    <ellipse cx=\"136\" cy=\"104\" rx=\"118\" ry=\"74\" fill=\"{accent}\" filter=\"url(#softBlur)\" />");
        builder.AppendLine($"    <ellipse cx=\"312\" cy=\"302\" rx=\"126\" ry=\"88\" fill=\"{accentThree}\" filter=\"url(#softBlur)\" />");
        builder.AppendLine("  </g>");
        builder.AppendLine($"  <path d=\"{BuildWavePath(random, 58, 18, 30)}\" stroke=\"#fff7ea\" stroke-width=\"1.5\" opacity=\"0.18\" fill=\"none\" />");
        builder.AppendLine($"  <path d=\"{BuildWavePath(random, 344, 24, 34)}\" stroke=\"#fff7ea\" stroke-width=\"1.25\" opacity=\"0.12\" fill=\"none\" />");

        switch (variant)
        {
            case 0:
                builder.AppendLine($"  <path d=\"{BuildBlobPath(random, 120, 138, 90, 72)}\" fill=\"{accent}\" opacity=\"0.86\" />");
                builder.AppendLine($"  <path d=\"{BuildBlobPath(random, 286, 262, 124, 102)}\" fill=\"{accentTwo}\" opacity=\"0.76\" />");
                builder.AppendLine($"  <path d=\"{BuildRibbonPath(random, 26, 296, 394, 122)}\" stroke=\"{highlight}\" stroke-width=\"13\" stroke-linecap=\"round\" opacity=\"0.3\" fill=\"none\" />");
                builder.AppendLine($"  <path d=\"{BuildRibbonPath(random, 42, 336, 364, 164)}\" stroke=\"#fff7ea\" stroke-width=\"5\" stroke-linecap=\"round\" opacity=\"0.38\" fill=\"none\" />");
                break;
            case 1:
                builder.AppendLine($"  <path d=\"{BuildWaveBandPath(random, 96, 78)}\" fill=\"{accent}\" opacity=\"0.86\" />");
                builder.AppendLine($"  <path d=\"{BuildWaveBandPath(random, 196, 96)}\" fill=\"{accentTwo}\" opacity=\"0.72\" />");
                builder.AppendLine($"  <path d=\"{BuildWaveBandPath(random, 286, 62)}\" fill=\"{accentThree}\" opacity=\"0.66\" />");
                builder.AppendLine($"  <path d=\"{BuildRibbonPath(random, 42, 104, 378, 324)}\" stroke=\"#fff7ea\" stroke-width=\"8\" stroke-linecap=\"round\" opacity=\"0.22\" fill=\"none\" />");
                break;
            case 2:
                builder.AppendLine("  <g opacity=\"0.94\">");
                builder.AppendLine($"    <path d=\"{BuildArcPath(210, 210, 142, 214, 222)}\" stroke=\"{accent}\" stroke-width=\"48\" stroke-linecap=\"round\" fill=\"none\" opacity=\"0.84\" />");
                builder.AppendLine($"    <path d=\"{BuildArcPath(210, 210, 96, 26, 164)}\" stroke=\"{highlight}\" stroke-width=\"22\" stroke-linecap=\"round\" fill=\"none\" opacity=\"0.9\" />");
                builder.AppendLine($"    <path d=\"{BuildArcPath(210, 210, 74, 228, 382)}\" stroke=\"{accentTwo}\" stroke-width=\"16\" stroke-linecap=\"round\" fill=\"none\" opacity=\"0.72\" />");
                builder.AppendLine("  </g>");
                builder.AppendLine($"  <circle cx=\"210\" cy=\"210\" r=\"18\" fill=\"{textColor}\" opacity=\"0.88\" />");
                break;
            case 3:
                builder.AppendLine($"  <path d=\"{BuildLandscapePath(random, 246, 58)}\" fill=\"{accentTwo}\" opacity=\"0.58\" />");
                builder.AppendLine($"  <path d=\"{BuildLandscapePath(random, 288, 82)}\" fill=\"{accent}\" opacity=\"0.72\" />");
                builder.AppendLine($"  <path d=\"{BuildLandscapePath(random, 332, 96)}\" fill=\"{accentThree}\" opacity=\"0.84\" />");
                builder.AppendLine($"  <circle cx=\"322\" cy=\"92\" r=\"38\" fill=\"{highlight}\" opacity=\"0.86\" />");
                builder.AppendLine($"  <path d=\"{BuildRibbonPath(random, 44, 82, 210, 256)}\" stroke=\"#fff7ea\" stroke-width=\"4\" stroke-linecap=\"round\" opacity=\"0.26\" fill=\"none\" />");
                break;
            default:
                for (var i = 0; i < 5; i++)
                {
                    var x = 70 + (i * 70) + random.Next(-10, 11);
                    var y = 82 + random.Next(-14, 15);
                    var width = 26 + random.Next(0, 16);
                    var height = 210 + random.Next(-24, 25);
                    var radius = width / 2;
                    var fill = i % 2 == 0 ? accent : accentThree;
                    builder.AppendLine($"  <rect x=\"{x}\" y=\"{y}\" width=\"{width}\" height=\"{height}\" rx=\"{radius}\" fill=\"{fill}\" opacity=\"0.7\" />");
                }

                builder.AppendLine($"  <path d=\"{BuildBlobPath(random, 256, 176, 68, 86)}\" fill=\"{highlight}\" opacity=\"0.82\" />");
                builder.AppendLine($"  <path d=\"{BuildRibbonPath(random, 56, 306, 366, 136)}\" stroke=\"#fff7ea\" stroke-width=\"10\" stroke-linecap=\"round\" opacity=\"0.24\" fill=\"none\" />");
                break;
        }

        builder.AppendLine("  <rect x=\"26\" y=\"250\" width=\"368\" height=\"124\" rx=\"28\" fill=\"#120f1fcc\" stroke=\"#fff7ea\" stroke-opacity=\"0.14\" />");
        builder.AppendLine($"  <text x=\"38\" y=\"300\" fill=\"{textColor}\" font-family=\"Georgia, serif\" font-size=\"42\" font-weight=\"700\">{Xml(record.Title, 18)}</text>");
        builder.AppendLine($"  <text x=\"38\" y=\"336\" fill=\"{textColor}\" font-family=\"Arial, sans-serif\" font-size=\"23\" opacity=\"0.92\">{Xml(record.Artist, 28)}</text>");
        builder.AppendLine($"  <text x=\"38\" y=\"365\" fill=\"{textColor}\" font-family=\"Arial, sans-serif\" font-size=\"16\" letter-spacing=\"2\">{WebUtility.HtmlEncode(record.Album.ToUpperInvariant())}</text>");
        builder.AppendLine("  <rect width=\"420\" height=\"420\" rx=\"34\" fill=\"#ffffff\" filter=\"url(#grain)\" opacity=\"0.8\" />");
        builder.AppendLine("  <rect x=\"18\" y=\"18\" width=\"384\" height=\"384\" rx=\"28\" fill=\"none\" stroke=\"#fff7ea\" opacity=\"0.16\" />");
        builder.AppendLine("</svg>");

        return builder.ToString();
    }

    private static Tuple<string, string> BuildBackground(StableRandom random)
    {
        return Tuple.Create(
            Hsl(random.Next(0, 360), random.Next(42, 76), random.Next(32, 54)),
            Hsl(random.Next(0, 360), random.Next(48, 80), random.Next(16, 34)));
    }

    private static string Hsl(int hue, int saturation, int lightness)
    {
        return $"hsl({hue.ToString(CultureInfo.InvariantCulture)} {saturation.ToString(CultureInfo.InvariantCulture)}% {lightness.ToString(CultureInfo.InvariantCulture)}%)";
    }

    private static string BuildWavePath(StableRandom random, int startY, int amplitude, int segmentWidth)
    {
        var points = GenerateWavePoints(random, startY, amplitude, segmentWidth);
        var builder = new StringBuilder();
        builder.Append($"M0 {points[0].ToString(CultureInfo.InvariantCulture)}");

        for (var i = 1; i < points.Count; i++)
        {
            var startX = (i - 1) * segmentWidth;
            var nextX = Math.Min(420, i * segmentWidth);
            var controlX = startX + ((nextX - startX) / 2);
            var controlY = startY + random.Next(-amplitude, amplitude + 1);
            builder.Append(
                $" Q{controlX.ToString(CultureInfo.InvariantCulture)} {controlY.ToString(CultureInfo.InvariantCulture)} {nextX.ToString(CultureInfo.InvariantCulture)} {points[i].ToString(CultureInfo.InvariantCulture)}");
        }

        return builder.ToString();
    }

    private static string BuildRibbonPath(StableRandom random, int startX, int startY, int endX, int endY)
    {
        var c1X = startX + random.Next(70, 140);
        var c1Y = startY + random.Next(-90, 91);
        var c2X = endX - random.Next(70, 140);
        var c2Y = endY + random.Next(-90, 91);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"M{startX} {startY} C{c1X} {c1Y}, {c2X} {c2Y}, {endX} {endY}");
    }

    private static string BuildBlobPath(StableRandom random, int centerX, int centerY, int radiusX, int radiusY)
    {
        var points = new (double X, double Y)[8];
        for (var i = 0; i < points.Length; i++)
        {
            var angle = (Math.PI * 2 * i) / points.Length;
            var rx = radiusX * (0.72 + (random.NextDouble() * 0.46));
            var ry = radiusY * (0.72 + (random.NextDouble() * 0.46));
            points[i] = (
                centerX + (Math.Cos(angle) * rx),
                centerY + (Math.Sin(angle) * ry));
        }

        var builder = new StringBuilder();
        builder.Append(
            $"M{points[0].X.ToString("0.##", CultureInfo.InvariantCulture)} {points[0].Y.ToString("0.##", CultureInfo.InvariantCulture)}");

        for (var i = 0; i < points.Length; i++)
        {
            var current = points[i];
            var next = points[(i + 1) % points.Length];
            var control1 = (
                X: current.X + ((next.X - current.X) * 0.35),
                Y: current.Y + ((next.Y - current.Y) * 0.12));
            var control2 = (
                X: current.X + ((next.X - current.X) * 0.65),
                Y: current.Y + ((next.Y - current.Y) * 0.88));

            builder.Append(
                $" C{control1.X.ToString("0.##", CultureInfo.InvariantCulture)} {control1.Y.ToString("0.##", CultureInfo.InvariantCulture)}, {control2.X.ToString("0.##", CultureInfo.InvariantCulture)} {control2.Y.ToString("0.##", CultureInfo.InvariantCulture)}, {next.X.ToString("0.##", CultureInfo.InvariantCulture)} {next.Y.ToString("0.##", CultureInfo.InvariantCulture)}");
        }

        builder.Append(" Z");
        return builder.ToString();
    }

    private static string BuildWaveBandPath(StableRandom random, int topY, int bandHeight)
    {
        const int segmentWidth = 34;
        var topPoints = GenerateWavePoints(random, topY, 18, segmentWidth);
        var bottomPoints = GenerateWavePoints(random, topY + bandHeight, 18, segmentWidth);
        var builder = new StringBuilder();

        builder.Append($"M0 {topPoints[0].ToString(CultureInfo.InvariantCulture)}");
        for (var i = 1; i < topPoints.Count; i++)
        {
            var startX = (i - 1) * segmentWidth;
            var nextX = Math.Min(420, i * segmentWidth);
            var controlX = startX + ((nextX - startX) / 2);
            var controlY = (topPoints[i - 1] + topPoints[i]) / 2;
            builder.Append(
                $" Q{controlX.ToString(CultureInfo.InvariantCulture)} {controlY.ToString(CultureInfo.InvariantCulture)} {nextX.ToString(CultureInfo.InvariantCulture)} {topPoints[i].ToString(CultureInfo.InvariantCulture)}");
        }

        builder.Append($" L420 {bottomPoints[^1].ToString(CultureInfo.InvariantCulture)}");

        for (var i = bottomPoints.Count - 2; i >= 0; i--)
        {
            var startX = Math.Min(420, (i + 1) * segmentWidth);
            var nextX = i * segmentWidth;
            var controlX = nextX + ((startX - nextX) / 2);
            var controlY = (bottomPoints[i + 1] + bottomPoints[i]) / 2;
            builder.Append(
                $" Q{controlX.ToString(CultureInfo.InvariantCulture)} {controlY.ToString(CultureInfo.InvariantCulture)} {nextX.ToString(CultureInfo.InvariantCulture)} {bottomPoints[i].ToString(CultureInfo.InvariantCulture)}");
        }

        builder.Append(" Z");
        return builder.ToString();
    }

    private static List<int> GenerateWavePoints(StableRandom random, int startY, int amplitude, int segmentWidth)
    {
        var points = new List<int> { startY };
        for (var x = segmentWidth; x <= 420; x += segmentWidth)
        {
            points.Add(startY + random.Next(-amplitude, amplitude + 1));
        }

        if ((points.Count - 1) * segmentWidth < 420)
        {
            points.Add(startY + random.Next(-amplitude, amplitude + 1));
        }

        return points;
    }

    private static string BuildArcPath(double centerX, double centerY, double radius, double startDegrees, double endDegrees)
    {
        var start = PolarToCartesian(centerX, centerY, radius, endDegrees);
        var end = PolarToCartesian(centerX, centerY, radius, startDegrees);
        var largeArc = endDegrees - startDegrees <= 180 ? "0" : "1";

        return string.Create(
            CultureInfo.InvariantCulture,
            $"M{start.X:0.##} {start.Y:0.##} A{radius:0.##} {radius:0.##} 0 {largeArc} 0 {end.X:0.##} {end.Y:0.##}");
    }

    private static (double X, double Y) PolarToCartesian(double centerX, double centerY, double radius, double angleDegrees)
    {
        var angle = (angleDegrees - 90) * (Math.PI / 180d);
        return (
            centerX + (radius * Math.Cos(angle)),
            centerY + (radius * Math.Sin(angle)));
    }

    private static string BuildLandscapePath(StableRandom random, int baseY, int amplitude)
    {
        var builder = new StringBuilder();
        builder.Append($"M0 420 L0 {baseY.ToString(CultureInfo.InvariantCulture)}");

        var currentX = 0;
        while (currentX < 420)
        {
            var nextX = Math.Min(420, currentX + random.Next(64, 118));
            var controlX = currentX + ((nextX - currentX) / 2);
            var peakY = baseY - random.Next(0, amplitude + 1);
            var nextY = baseY + random.Next(-18, 19);
            builder.Append(
                $" Q{controlX.ToString(CultureInfo.InvariantCulture)} {peakY.ToString(CultureInfo.InvariantCulture)} {nextX.ToString(CultureInfo.InvariantCulture)} {nextY.ToString(CultureInfo.InvariantCulture)}");
            currentX = nextX;
        }

        builder.Append(" L420 420 Z");
        return builder.ToString();
    }

    private static string Xml(string value, int maxLength)
    {
        var text = value.Length > maxLength ? $"{value[..(maxLength - 1)]}…" : value;
        return WebUtility.HtmlEncode(text);
    }
}

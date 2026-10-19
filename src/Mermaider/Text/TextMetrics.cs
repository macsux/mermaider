using System.Text.RegularExpressions;

namespace Mermaider.Text;

/// <summary>Metrics for a multi-line text block.</summary>
internal readonly record struct MultilineMetrics(double Width, double Height, int LineCount, double LineHeight);

/// <summary>
/// Font-agnostic text width estimation using character-class buckets.
/// All measurement operates on <see cref="ReadOnlySpan{T}"/> to avoid allocations.
/// </summary>
internal static partial class TextMetrics
{
	internal const double LineHeightRatio = 1.3;

	private const int TimeoutMs = 2000;

	[GeneratedRegex(@"</?(?:b|strong|i|em|u|s|del)\s*>", RegexOptions.IgnoreCase, TimeoutMs)]
	private static partial Regex FormattingTagPattern();

	// the host's measurer for the render (or layout) running on this thread; null: the built-in estimate
	[ThreadStatic]
	private static ITextMeasurer? CurrentMeasurer;

	/// <summary>Measures with <paramref name="measurer"/> on this thread until the scope is disposed (null keeps the current one).</summary>
	internal static MeasurerScope Use(ITextMeasurer? measurer)
	{
		var scope = new MeasurerScope(CurrentMeasurer);
		CurrentMeasurer = measurer ?? CurrentMeasurer;
		return scope;
	}

	internal readonly struct MeasurerScope(ITextMeasurer? previous) : IDisposable
	{
		public void Dispose() => CurrentMeasurer = previous;
	}

	internal static double MeasureTextWidth(ReadOnlySpan<char> text, double fontSize, int fontWeight)
	{
		if (CurrentMeasurer is { } measurer)
			return text.IsEmpty ? 0 : measurer.MeasureWidth(text, fontSize, fontWeight);

		var baseRatio = fontWeight >= 600 ? 0.60 : fontWeight >= 500 ? 0.57 : 0.54;

		var totalWidth = 0.0;
		foreach (var c in text)
			totalWidth += CharWidths.GetCharWidth(c);

		var minPadding = fontSize * 0.15;
		return (totalWidth * fontSize * baseRatio) + minPadding;
	}

	internal static double MeasurePlainWidth(ReadOnlySpan<char> line, double fontSize, int fontWeight)
	{
		if (!line.Contains('<'))
			return MeasureTextWidth(line, fontSize, fontWeight);

		var plain = FormattingTagPattern().Replace(line.ToString(), "");
		return MeasureTextWidth(plain, fontSize, fontWeight);
	}

	internal static MultilineMetrics MeasureMultiline(ReadOnlySpan<char> text, double fontSize, int fontWeight)
	{
		var lineHeight = fontSize * LineHeightRatio;
		var lineCount = 0;
		var maxWidth = 0.0;

		foreach (var line in text.EnumerateLines())
		{
			lineCount++;
			var w = MeasurePlainWidth(line, fontSize, fontWeight);
			if (w > maxWidth)
				maxWidth = w;
		}

		if (lineCount == 0)
			lineCount = 1;

		return new MultilineMetrics(maxWidth, lineCount * lineHeight, lineCount, lineHeight);
	}

	internal static double EstimateMonoTextWidth(ReadOnlySpan<char> text, double fontSize) =>
		text.Length * fontSize * 0.6;
}

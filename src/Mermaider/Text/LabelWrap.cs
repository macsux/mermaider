namespace Mermaider.Text;

/// <summary>
/// Wraps label text at word boundaries into balanced lines: as few lines as fit a maximum width, each as close to the same
/// width as the words allow, so a label never ends on a lone short word. Explicit line breaks are kept.
/// </summary>
internal static class LabelWrap
{
	private const double SlackFactor = 1.25;
	private const double WorthWrapping = 0.85;

	internal static string Wrap(string label, double maxWidth, double fontSize, int fontWeight)
	{
		if (label.Length == 0)
			return label;
		var lines = label.Split('\n');
		var changed = false;
		for (var i = 0; i < lines.Length; i++)
		{
			if (TextMetrics.MeasurePlainWidth(lines[i], fontSize, fontWeight) <= maxWidth)
				continue;
			var wrapped = WrapLine(lines[i], maxWidth, fontSize, fontWeight);
			changed |= !ReferenceEquals(wrapped, lines[i]);
			lines[i] = wrapped;
		}
		return changed ? string.Join('\n', lines) : label;
	}

	private static string WrapLine(string line, double maxWidth, double fontSize, int fontWeight)
	{
		var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		if (words.Length < 2)
			return line;
		// the greedy passes ask for a few runs of words each, over and over: measure each run once
		var widths = new Dictionary<(int From, int To), double>();
		double Width(int from, int to)
		{
			if (!widths.TryGetValue((from, to), out var w))
				widths[(from, to)] = w = TextMetrics.MeasurePlainWidth(string.Join(' ', words, from, to - from + 1), fontSize, fontWeight);
			return w;
		}

		var lines = Greedy(words.Length, maxWidth, Width);
		if (lines.Count < 2)
			return line;

		// the narrowest width that still needs no more lines: the greedy breaks at it are balanced
		var lo = 0.0;
		for (var i = 0; i < words.Length; i++)
			lo = Math.Max(lo, Width(i, i));
		var hi = maxWidth;
		for (var step = 0; step < 14 && hi - lo > 0.5; step++)
		{
			var mid = (lo + hi) / 2;
			if (Greedy(words.Length, mid, Width).Count <= lines.Count)
				hi = mid;
			else
				lo = mid;
		}

		// a line a little over the limit stays whole when wrapping would hardly narrow it (a long word and a short one)
		var whole = Width(0, words.Length - 1);
		if (whole <= maxWidth * SlackFactor && hi >= whole * WorthWrapping)
			return line;

		return string.Join('\n', Greedy(words.Length, hi, Width).Select(r => string.Join(' ', words, r.From, r.To - r.From + 1)));
	}

	private static List<(int From, int To)> Greedy(int count, double maxWidth, Func<int, int, double> width)
	{
		var lines = new List<(int From, int To)>();
		var from = 0;
		for (var i = 1; i < count; i++)
		{
			if (width(from, i) > maxWidth)
			{
				lines.Add((from, i - 1));
				from = i;
			}
		}
		lines.Add((from, count - 1));
		return lines;
	}
}

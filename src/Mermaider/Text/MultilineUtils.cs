using System.Buffers;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Mermaider.Text;

/// <summary>
/// Shared utilities for rendering multi-line text in SVG with inline formatting.
/// </summary>
internal static partial class MultilineUtils
{
	private const int TimeoutMs = 2000;

	private static readonly SearchValues<char> XmlSpecialCharacters = SearchValues.Create("&<>\"'");

	[GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase, TimeoutMs)]
	private static partial Regex BrTagPattern();

	[GeneratedRegex(@"\\n", RegexOptions.None, TimeoutMs)]
	private static partial Regex EscapedNewlinePattern();

	[GeneratedRegex(@"</?(?:sub|sup|small|mark)\s*>", RegexOptions.IgnoreCase, TimeoutMs)]
	private static partial Regex UnsupportedTagPattern();

	[GeneratedRegex(@"&(#?\w+);|(?<!&)#(\w+);", RegexOptions.None, TimeoutMs)]
	private static partial Regex CharacterReferencePattern();

	[GeneratedRegex(@"\*\*(.+?)\*\*", RegexOptions.None, TimeoutMs)]
	private static partial Regex MarkdownBoldPattern();

	[GeneratedRegex(@"(?<!\*)\*([^\s*](?:[^*]*[^\s*])?)\*(?!\*)", RegexOptions.None, TimeoutMs)]
	private static partial Regex MarkdownItalicPattern();

	[GeneratedRegex(@"~~(.+?)~~", RegexOptions.None, TimeoutMs)]
	private static partial Regex MarkdownStrikethroughPattern();

	[GeneratedRegex(@"<(/?)(b|strong|i|em|u|s|del)\s*>", RegexOptions.IgnoreCase, TimeoutMs)]
	private static partial Regex FormatTagPattern();

	[GeneratedRegex(@"</?(?:b|strong|i|em|u|s|del)\s*>", RegexOptions.IgnoreCase, TimeoutMs)]
	private static partial Regex HasFormatTagPattern();

	/// <summary>
	/// Normalize label text: strip quotes, convert &lt;br&gt; tags and \n to newlines,
	/// strip unsupported tags, convert markdown formatting to HTML tags, decode entities.
	/// </summary>
	internal static string NormalizeBrTags(string label)
	{
		var unquoted = label is ['"', .., '"'] ? label[1..^1] : label;
		var result = BrTagPattern().Replace(unquoted, "\n");
		result = EscapedNewlinePattern().Replace(result, "\n");
		result = UnsupportedTagPattern().Replace(result, "");
		result = MarkdownBoldPattern().Replace(result, "<b>$1</b>");
		result = MarkdownItalicPattern().Replace(result, "<i>$1</i>");
		result = MarkdownStrikethroughPattern().Replace(result, "<s>$1</s>");
		return DecodeEntities(result);
	}

	/// <summary>
	/// Decodes the character references Mermaid accepts in label text, HTML's (<c>&amp;quot;</c>, <c>&amp;#9829;</c>) and
	/// its own (<c>#quot;</c>, <c>#9829;</c>), into the characters they name. One naming <c>&amp;</c>, <c>&lt;</c> or
	/// <c>&gt;</c> becomes <c>&amp;amp;</c>, <c>&amp;lt;</c> or <c>&amp;gt;</c>, so an escaped <c>&amp;lt;b&amp;gt;</c> stays
	/// text beside the label's formatting tags: writers and measurers read those three back with <see cref="UnescapeMarkup"/>
	/// (<see cref="AppendEscapedXml"/> passes them through).
	/// </summary>
	internal static string DecodeEntities(string label) =>
		label.Contains(';') ? CharacterReferencePattern().Replace(label, DecodeReference) : label;

	private static string DecodeReference(Match m)
	{
		var name = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
		var html = (m.Groups[2].Success && char.IsAsciiDigit(name[0]) ? "&#" : "&") + name + ";";
		return WebUtility.HtmlDecode(html) switch
		{
			var same when same == html => m.Value,
			"&" => "&amp;",
			"<" => "&lt;",
			">" => "&gt;",
			var c => c
		};
	}

	/// <summary>The text a label's <c>&amp;amp;</c>, <c>&amp;lt;</c> and <c>&amp;gt;</c> stand for (see <see cref="DecodeEntities"/>).</summary>
	internal static string UnescapeMarkup(string text)
	{
		if (!text.Contains('&'))
			return text;

		var sb = new StringBuilder(text.Length);
		for (var i = 0; i < text.Length; i++)
		{
			var length = MarkupReferenceLength(text.AsSpan(i));
			if (length == 0)
			{
				_ = sb.Append(text[i]);
				continue;
			}

			_ = sb.Append(text[i + 1] switch { 'l' => '<', 'g' => '>', _ => '&' });
			i += length - 1;
		}
		return sb.ToString();
	}

	/// <summary>The length of the <c>&amp;amp;</c>, <c>&amp;lt;</c> or <c>&amp;gt;</c> <paramref name="text"/> starts with, else 0.</summary>
	private static int MarkupReferenceLength(ReadOnlySpan<char> text) =>
		text.StartsWith("&lt;") || text.StartsWith("&gt;") ? 4 : text.StartsWith("&amp;") ? 5 : 0;

	internal static void AppendEscapedXml(StringBuilder sb, ReadOnlySpan<char> value)
	{
		if (!value.ContainsAny(XmlSpecialCharacters))
		{
			_ = sb.Append(value);
			return;
		}

		for (var i = 0; i < value.Length; i++)
		{
			_ = value[i] switch
			{
				'&' when MarkupReferenceLength(value[i..]) > 0 => sb.Append('&'),
				'&' => sb.Append("&amp;"),
				'<' => sb.Append("&lt;"),
				'>' => sb.Append("&gt;"),
				'"' => sb.Append("&quot;"),
				'\'' => sb.Append("&#39;"),
				var c => sb.Append(c)
			};
		}
	}

	/// <summary>
	/// Returns <paramref name="value"/> with XML attribute-significant characters escaped.
	/// Used to neutralize attribute-breakout injection when a user-supplied string
	/// (e.g. a <c>style</c>/<c>classDef</c> color) is emitted into a double-quoted SVG
	/// attribute value. Returns the original instance unchanged when nothing needs escaping.
	/// </summary>
	internal static string EscapeAttr(string value)
	{
		if (!value.AsSpan().ContainsAny(XmlSpecialCharacters))
			return value;

		var sb = new StringBuilder(value.Length + 16);
		AppendEscapedAttr(sb, value.AsSpan());
		return sb.ToString();
	}

	internal static void AppendEscapedAttr(StringBuilder sb, ReadOnlySpan<char> value)
	{
		if (!value.ContainsAny(XmlSpecialCharacters))
		{
			_ = sb.Append(value);
			return;
		}

		for (var i = 0; i < value.Length; i++)
		{
			_ = value[i] switch
			{
				'&' when MarkupReferenceLength(value[i..]) > 0 => sb.Append('&'),
				'&' => sb.Append("&amp;"),
				'"' => sb.Append("&quot;"),
				'<' => sb.Append("&lt;"),
				'>' => sb.Append("&gt;"),
				var c => sb.Append(c)
			};
		}
	}

	internal static void AppendLineContent(StringBuilder sb, ReadOnlySpan<char> line)
	{
		if (!line.Contains('<'))
		{
			AppendEscapedXml(sb, line);
			return;
		}

		var lineStr = line.ToString();
		if (!HasFormatTagPattern().IsMatch(lineStr))
		{
			AppendEscapedXml(sb, line);
			return;
		}

		var bold = false;
		var italic = false;
		var underline = false;
		var strikethrough = false;
		var lastIndex = 0;

		foreach (var match in FormatTagPattern().EnumerateMatches(lineStr))
		{
			var m = FormatTagPattern().Match(lineStr, match.Index, match.Length);
			if (m.Index > lastIndex)
				AppendStyledSegment(sb, lineStr.AsSpan(lastIndex, m.Index - lastIndex), bold, italic, underline, strikethrough);

			lastIndex = m.Index + m.Length;
			var isClosing = m.Groups[1].Value == "/";
			var tag = m.Groups[2].Value.ToLowerInvariant();

			switch (tag)
			{
				case "b" or "strong":
					bold = !isClosing;
					break;
				case "i" or "em":
					italic = !isClosing;
					break;
				case "u":
					underline = !isClosing;
					break;
				case "s" or "del":
					strikethrough = !isClosing;
					break;
			}
		}

		if (lastIndex < lineStr.Length)
			AppendStyledSegment(sb, lineStr.AsSpan(lastIndex), bold, italic, underline, strikethrough);
	}

	private static void AppendStyledSegment(
		StringBuilder sb,
		ReadOnlySpan<char> text,
		bool bold, bool italic, bool underline, bool strikethrough)
	{
		if (!bold && !italic && !underline && !strikethrough)
		{
			AppendEscapedXml(sb, text);
			return;
		}

		_ = sb.Append("<tspan");
		if (bold)
			_ = sb.Append(" font-weight=\"bold\"");
		if (italic)
			_ = sb.Append(" font-style=\"italic\"");
		if (underline || strikethrough)
		{
			_ = sb.Append(" text-decoration=\"");
			if (underline)
				_ = sb.Append("underline");
			if (underline && strikethrough)
				_ = sb.Append(' ');
			if (strikethrough)
				_ = sb.Append("line-through");
			_ = sb.Append('"');
		}
		_ = sb.Append('>');
		AppendEscapedXml(sb, text);
		_ = sb.Append("</tspan>");
	}

	internal static void AppendMultilineText(
		StringBuilder sb,
		string text,
		double cx, double cy,
		double fontSize,
		string attrs,
		double baselineShift = 0.35)
	{
		var lines = text.Split('\n');

		if (lines.Length == 1)
		{
			var dy = fontSize * baselineShift;
			_ = sb.Append("<text x=\"").Append(Rendering.SvgFormatExtensions.SvgFormat(cx)).Append("\" y=\"").Append(Rendering.SvgFormatExtensions.SvgFormat(cy))
				.Append("\" ").Append(attrs)
				.Append(" dy=\"").Append(Rendering.SvgFormatExtensions.SvgFormat(dy)).Append("\">");
			AppendLineContent(sb, text.AsSpan());
			_ = sb.Append("</text>");
			return;
		}

		var lineHeight = fontSize * TextMetrics.LineHeightRatio;
		var firstDy = (-((lines.Length - 1) / 2.0) * lineHeight) + (fontSize * baselineShift);

		_ = sb.Append("<text x=\"").Append(Rendering.SvgFormatExtensions.SvgFormat(cx)).Append("\" y=\"").Append(Rendering.SvgFormatExtensions.SvgFormat(cy))
			.Append("\" ").Append(attrs).Append('>');

		for (var i = 0; i < lines.Length; i++)
		{
			var dy = i == 0 ? firstDy : lineHeight;
			_ = sb.Append("<tspan x=\"").Append(Rendering.SvgFormatExtensions.SvgFormat(cx)).Append("\" dy=\"").Append(Rendering.SvgFormatExtensions.SvgFormat(dy)).Append("\">");
			AppendLineContent(sb, lines[i].AsSpan());
			_ = sb.Append("</tspan>");
		}

		_ = sb.Append("</text>");
	}

	internal static void AppendMultilineTextWithBackground(
		StringBuilder sb,
		string text,
		double cx, double cy,
		double textWidth, double textHeight,
		double fontSize,
		double padding,
		string textAttrs,
		string bgAttrs)
	{
		var bgWidth = textWidth + (padding * 2);
		var bgHeight = textHeight + (padding * 2);

		_ = sb.Append("<rect x=\"").Append(cx - (bgWidth / 2))
			.Append("\" y=\"").Append(cy - (bgHeight / 2))
			.Append("\" width=\"").Append(bgWidth)
			.Append("\" height=\"").Append(bgHeight)
			.Append("\" ").Append(bgAttrs).Append(" />\n");

		AppendMultilineText(sb, text, cx, cy, fontSize, textAttrs);
	}
}

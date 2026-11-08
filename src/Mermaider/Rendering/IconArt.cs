using System.Text;
using Mermaider.Icons;

namespace Mermaider.Rendering;

/// <summary>Draws an <see cref="IconRegistry"/> icon: shared by architecture services / groups and flowchart icon-shape nodes.</summary>
internal static class IconArt
{
	/// <summary>Mermaid's default pictograms, drawn as line glyphs in the family ink (unless a custom icon overrides the name).</summary>
	private static DesignSystem.Glyph? BuiltInGlyph(string iconName)
	{
		var name = string.IsNullOrWhiteSpace(iconName) || !IconRegistry.TryGet(iconName, out _) ? IconRegistry.FallbackName : iconName.ToLowerInvariant();
		DesignSystem.Glyph? glyph = name switch
		{
			"cloud" => DesignSystem.Glyph.Cloud,
			"database" => DesignSystem.Glyph.Database,
			"disk" => DesignSystem.Glyph.Disk,
			"server" => DesignSystem.Glyph.Server,
			"internet" => DesignSystem.Glyph.Internet,
			"generic" => DesignSystem.Glyph.Generic,
			_ => null,
		};
		if (glyph is null)
			return null;
		// a user registration under a default name wins over the built-in glyph
		return IconRegistry.Resolve(name) == BuiltInIcons.Map[name] ? glyph : null;
	}

	/// <summary>
	/// Draws the icon centred at (<paramref name="cx"/>, <paramref name="cy"/>) in a <paramref name="size"/> slot: default
	/// pictograms as family-ink glyphs, vendor glyphs on their own gradient badge, anything else as the registered artwork.
	/// Artwork is embedded as a base64 data URI on an &lt;image&gt;, the one narrow case the SVG sanitizer allows an href
	/// through (see <see cref="SvgSanitizer"/>); the markup was sanitized when it entered the <see cref="IconRegistry"/>.
	/// </summary>
	internal static void Append(StringBuilder sb, DesignSystem ds, string iconName, double cx, double cy, double size, string ink)
	{
		if (BuiltInGlyph(iconName) is { } glyph)
		{
			DesignSystem.AppendGlyph(sb, glyph, cx, cy, ink, size / 22);
			return;
		}

		var inner = size;
		if (IconRegistry.TryGetBadgeGradient(iconName, out var gradient))
		{
			// vendor glyphs are white on transparent: they sit on a small badge in the vendor's own colours
			var fill = ds.DiagonalGradient("vi-" + SanitizeId(iconName.ToLowerInvariant()), gradient.Light, gradient.Dark);
			_ = sb.Append("<rect x=\"").Append(cx - (size / 2)).Append("\" y=\"").Append(cy - (size / 2))
				.Append("\" width=\"").Append(size).Append("\" height=\"").Append(size)
				.Append("\" rx=\"").Append(DesignSystem.Num(size / 4)).Append("\" ry=\"").Append(DesignSystem.Num(size / 4))
				.Append("\" fill=\"").Append(fill).Append("\" />");
			inner = size * 0.7;
		}

		var svg = IconRegistry.Resolve(iconName);
		var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(svg));
		_ = sb.Append("<image x=\"").Append(cx - (inner / 2)).Append("\" y=\"").Append(cy - (inner / 2))
			.Append("\" width=\"").Append(inner).Append("\" height=\"").Append(inner)
			.Append("\" href=\"data:image/svg+xml;base64,").Append(base64).Append("\" />");
	}

	/// <summary>Strips anything unsafe for an XML <c>id</c> attribute value.</summary>
	private static string SanitizeId(string id)
	{
		var buffer = new char[id.Length];
		for (var i = 0; i < id.Length; i++)
		{
			var c = id[i];
			buffer[i] = char.IsAsciiLetterOrDigit(c) || c is '-' or '_' ? c : '_';
		}
		return new string(buffer);
	}
}

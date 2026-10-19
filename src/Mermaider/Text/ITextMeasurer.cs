namespace Mermaider.Text;

/// <summary>
/// Measures text the way the host will draw it. Layout sizes boxes and wraps labels with it instead of Mermaider's
/// built-in character-class estimate, so a host that draws with its own fonts (a native renderer) gets boxes that fit.
/// </summary>
public interface ITextMeasurer
{
	/// <summary>
	/// The advance width, in px, of one line of plain text (formatting tags already removed), drawn at
	/// <paramref name="fontSize"/> px and CSS <paramref name="fontWeight"/> (400 regular, 500 medium, 600+ bold).
	/// </summary>
	double MeasureWidth(ReadOnlySpan<char> text, double fontSize, int fontWeight);
}

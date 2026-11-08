namespace Mermaider.Models;

/// <summary>
/// The icon of a flowchart icon-shape node (<c>A@{ icon: "aws:compute", form: "square", label: "EC2", pos: "t", h: 60 }</c>).
/// </summary>
/// <param name="Name">Icon name, resolved through <see cref="Icons.IconRegistry"/> (unknown names draw the fallback glyph).</param>
/// <param name="Form">Background drawn behind the icon; <see cref="IconForm.None"/> draws the icon alone.</param>
/// <param name="LabelPosition">Whether the label sits below (default) or above the icon.</param>
/// <param name="Height">Icon height in pixels: 48 by default, which is also the minimum.</param>
public sealed record NodeIcon(string Name, IconForm Form = IconForm.None, IconLabelPosition LabelPosition = IconLabelPosition.Bottom, double Height = NodeIcon.DefaultHeight)
{
	/// <summary>Mermaid's default (and minimum) icon height.</summary>
	public const double DefaultHeight = 48;
}

/// <summary>Background of a flowchart icon-shape node (Mermaid's <c>form</c>).</summary>
public enum IconForm
{
	/// <summary>No background: the icon alone.</summary>
	None,
	Square,
	Circle,
	Rounded
}

/// <summary>Where a flowchart icon-shape node's label sits (Mermaid's <c>pos</c>: <c>b</c> or <c>t</c>).</summary>
public enum IconLabelPosition
{
	Bottom,
	Top
}

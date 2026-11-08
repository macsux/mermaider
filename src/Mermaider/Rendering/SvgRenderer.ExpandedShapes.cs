using System.Text;
using Mermaider.Models;

namespace Mermaider.Rendering;

/// <summary>Outlines of the flowchart shapes only reachable through Mermaid's expanded syntax, <c>A@{ shape: name }</c>.</summary>
internal static partial class SvgRenderer
{
	/// <summary>Height of the wave along a document's bottom edge (and a paper tape's edges), peak to trough.</summary>
	private const double WaveDepth = 12;

	/// <summary>Size of a cut corner, notch or folded tag.</summary>
	private const double Notch = 12;

	/// <summary>Width of a curly brace, a horizontal cylinder's end cap or a stored-data shape's curved end.</summary>
	private const double CurveWidth = 10;

	/// <summary>
	/// Draws <paramref name="node"/> when its shape is one of the expanded shapes; false for every other shape.
	/// </summary>
	private static bool TryAppendExpandedShape(StringBuilder sb, PositionedNode node, string fill, string stroke, string sw)
	{
		var (x, y, w, h) = (node.X, node.Y, node.Width, node.Height);
		switch (node.Shape)
		{
			case NodeShape.NotchedRectangle:
				AppendPath(sb, $"M{N(x + Notch)},{N(y)} H{N(x + w)} V{N(y + h)} H{N(x)} V{N(y + Notch)} Z", fill, stroke, sw);
				return true;
			case NodeShape.NotchedPentagon:
				AppendPath(sb, $"M{N(x + Notch)},{N(y)} H{N(x + w - Notch)} L{N(x + w)},{N(y + Notch)} V{N(y + h)} H{N(x)} V{N(y + Notch)} Z", fill, stroke, sw);
				return true;
			case NodeShape.Hourglass:
				AppendPath(sb, $"M{N(x)},{N(y)} H{N(x + w)} L{N(x)},{N(y + h)} H{N(x + w)} Z", fill, stroke, sw);
				return true;
			case NodeShape.Bolt:
				AppendBolt(sb, x, y, w, h, fill, stroke, sw);
				return true;
			case NodeShape.BraceLeft:
				AppendBrace(sb, x, y, h, CurveWidth, stroke, sw);
				return true;
			case NodeShape.BraceRight:
				AppendBrace(sb, x + w, y, h, -CurveWidth, stroke, sw);
				return true;
			case NodeShape.Braces:
				AppendBrace(sb, x, y, h, CurveWidth, stroke, sw);
				_ = sb.Append('\n');
				AppendBrace(sb, x + w, y, h, -CurveWidth, stroke, sw);
				return true;
			case NodeShape.Delay:
				{
					var r = Math.Min(h / 2, w / 2);
					AppendPath(sb, $"M{N(x)},{N(y)} H{N(x + w - r)} A{N(r)},{N(h / 2)} 0 0 1 {N(x + w - r)},{N(y + h)} H{N(x)} Z", fill, stroke, sw);
					return true;
				}
			case NodeShape.HorizontalCylinder:
				{
					var (e, ry) = (CurveWidth, h / 2);
					AppendPath(sb, $"M{N(x + e)},{N(y)} H{N(x + w - e)} V{N(y + h)} H{N(x + e)} A{N(e)},{N(ry)} 0 0 1 {N(x + e)},{N(y)} Z", fill, stroke, sw);
					_ = sb.Append('\n');
					AppendPath(sb, $"M{N(x + w - e)},{N(y)} A{N(e)},{N(ry)} 0 0 1 {N(x + w - e)},{N(y + h)} A{N(e)},{N(ry)} 0 0 1 {N(x + w - e)},{N(y)} Z", fill, stroke, sw);
					return true;
				}
			case NodeShape.LinedCylinder:
				{
					AppendCylinder(sb, x, y, w, h, fill, stroke, sw);
					var lineY = y + CylinderRimY + 8;
					_ = sb.Append('\n');
					AppendPath(sb, $"M{N(x)},{N(lineY)} A{N(w / 2)},{N(CylinderRimY)} 0 0 0 {N(x + w)},{N(lineY)}", "none", stroke, sw);
					return true;
				}
			case NodeShape.CurvedTrapezoid:
				{
					var (point, r) = (Math.Min(16, w / 5), Math.Min(16, w / 5));
					AppendPath(sb, $"M{N(x)},{N(y + (h / 2))} L{N(x + point)},{N(y)} H{N(x + w - r)} A{N(r)},{N(h / 2)} 0 0 1 {N(x + w - r)},{N(y + h)} H{N(x + point)} Z", fill, stroke, sw);
					return true;
				}
			case NodeShape.DividedRectangle:
				AppendRect(sb, x, y, w, h, "0", fill, stroke, sw);
				AppendLine(sb, x, y + DividedBand, x + w, y + DividedBand, stroke, sw);
				return true;
			case NodeShape.Document:
				AppendPath(sb, DocumentPath(x, y, w, h), fill, stroke, sw);
				return true;
			case NodeShape.LinedDocument:
				AppendPath(sb, DocumentPath(x, y, w, h), fill, stroke, sw);
				AppendLine(sb, x + LinedInset, y, x + LinedInset, y + h - (WaveDepth / 2), stroke, sw);
				return true;
			case NodeShape.TaggedDocument:
				{
					AppendPath(sb, DocumentPath(x, y, w, h), fill, stroke, sw);
					var bottom = y + h - (WaveDepth / 2);
					_ = sb.Append('\n');
					AppendPath(sb, $"M{N(x + w)},{N(bottom - Notch)} V{N(bottom)} L{N(x + w - Notch)},{N(bottom)} Z", stroke, stroke, sw);
					return true;
				}
			case NodeShape.StackedDocument:
				{
					var o = NodeSizing.StackOffset;
					AppendPath(sb, DocumentPath(x + (2 * o), y, w - (2 * o), h - (2 * o)), fill, stroke, sw);
					_ = sb.Append('\n');
					AppendPath(sb, DocumentPath(x + o, y + o, w - (2 * o), h - (2 * o)), fill, stroke, sw);
					_ = sb.Append('\n');
					AppendPath(sb, DocumentPath(x, y + (2 * o), w - (2 * o), h - (2 * o)), fill, stroke, sw);
					return true;
				}
			case NodeShape.StackedRectangle:
				{
					var o = NodeSizing.StackOffset;
					AppendRect(sb, x + (2 * o), y, w - (2 * o), h - (2 * o), "0", fill, stroke, sw);
					_ = sb.Append('\n');
					AppendRect(sb, x + o, y + o, w - (2 * o), h - (2 * o), "0", fill, stroke, sw);
					_ = sb.Append('\n');
					AppendRect(sb, x, y + (2 * o), w - (2 * o), h - (2 * o), "0", fill, stroke, sw);
					return true;
				}
			case NodeShape.Triangle:
				AppendPath(sb, $"M{N(x + (w / 2))},{N(y)} L{N(x + w)},{N(y + h)} H{N(x)} Z", fill, stroke, sw);
				return true;
			case NodeShape.FlippedTriangle:
				AppendPath(sb, $"M{N(x)},{N(y)} H{N(x + w)} L{N(x + (w / 2))},{N(y + h)} Z", fill, stroke, sw);
				return true;
			case NodeShape.WindowPane:
				{
					var i = NodeSizing.WindowPaneInset;
					AppendRect(sb, x, y, w, h, "0", fill, stroke, sw);
					AppendLine(sb, x, y + i, x + w, y + i, stroke, sw);
					AppendLine(sb, x + i, y, x + i, y + h, stroke, sw);
					return true;
				}
			case NodeShape.FilledCircle:
				_ = sb.Append("<circle cx=\"").Append(N(x + (w / 2))).Append("\" cy=\"").Append(N(y + (h / 2)))
					.Append("\" r=\"").Append(N(Math.Min(w, h) / 2)).Append("\" fill=\"var(--_line-strong)\" stroke=\"none\" />");
				return true;
			case NodeShape.LinedRectangle:
				AppendRect(sb, x, y, w, h, "0", fill, stroke, sw);
				AppendLine(sb, x + LinedInset, y, x + LinedInset, y + h, stroke, sw);
				return true;
			case NodeShape.TaggedRectangle:
				AppendRect(sb, x, y, w, h, "0", fill, stroke, sw);
				_ = sb.Append('\n');
				AppendPath(sb, $"M{N(x + w)},{N(y + h - Notch)} V{N(y + h)} H{N(x + w - Notch)} Z", stroke, stroke, sw);
				return true;
			case NodeShape.SlopedRectangle:
				AppendPath(sb, $"M{N(x)},{N(y + SlopeRise)} L{N(x + w)},{N(y)} V{N(y + h)} H{N(x)} Z", fill, stroke, sw);
				return true;
			case NodeShape.Flag:
				{
					var a = WaveDepth / 2;
					AppendPath(sb,
						$"M{N(x)},{N(y + a)} Q{N(x + (w / 4))},{N(y - a)} {N(x + (w / 2))},{N(y + a)} T{N(x + w)},{N(y + a)} " +
						$"V{N(y + h - a)} Q{N(x + (w * 3 / 4))},{N(y + h + a)} {N(x + (w / 2))},{N(y + h - a)} T{N(x)},{N(y + h - a)} Z",
						fill, stroke, sw);
					return true;
				}
			case NodeShape.BowTieRectangle:
				{
					var (e, ry) = (CurveWidth, h / 2);
					// stored data: the left end bulges out, the right end curves in
					AppendPath(sb, $"M{N(x + e)},{N(y)} H{N(x + w)} A{N(e)},{N(ry)} 0 0 0 {N(x + w)},{N(y + h)} H{N(x + e)} A{N(e)},{N(ry)} 0 0 1 {N(x + e)},{N(y)} Z", fill, stroke, sw);
					return true;
				}
			case NodeShape.CrossedCircle:
				{
					AppendCircle(sb, x, y, w, h, fill, stroke, sw);
					var (cx, cy, d) = (x + (w / 2), y + (h / 2), Math.Min(w, h) / 2 / Math.Sqrt(2));
					AppendLine(sb, cx - d, cy - d, cx + d, cy + d, stroke, sw);
					AppendLine(sb, cx - d, cy + d, cx + d, cy - d, stroke, sw);
					return true;
				}
			case NodeShape.Text:
				// a text block is its label alone
				return true;
			default:
				return false;
		}
	}

	/// <summary>
	/// Where the label of an expanded shape is centred, relative to the node's centre: in the open part of shapes with a
	/// wavy edge, band, inner line or stacked copies behind them.
	/// </summary>
	private static (double Dx, double Dy) ExpandedShapeLabelOffset(PositionedNode node) => node.Shape switch
	{
		NodeShape.Document or NodeShape.TaggedDocument => (0, -WaveDepth / 2),
		NodeShape.LinedDocument => (LinedInset / 2, -WaveDepth / 2),
		NodeShape.StackedDocument => (-NodeSizing.StackOffset, NodeSizing.StackOffset - (WaveDepth / 2)),
		NodeShape.StackedRectangle => (-NodeSizing.StackOffset, NodeSizing.StackOffset),
		NodeShape.LinedRectangle => (LinedInset / 2, 0),
		NodeShape.WindowPane => (NodeSizing.WindowPaneInset / 2, NodeSizing.WindowPaneInset / 2),
		NodeShape.DividedRectangle => (0, DividedBand / 2),
		NodeShape.SlopedRectangle => (0, SlopeRise / 2),
		NodeShape.LinedCylinder => (0, 5),
		NodeShape.Triangle => (0, node.Height / 6),
		NodeShape.FlippedTriangle => (0, -node.Height / 6),
		_ => (0, 0),
	};

	private const double CylinderRimY = 7;
	private const double DividedBand = 12;
	private const double LinedInset = 8;
	private const double SlopeRise = 10;

	/// <summary>A rectangle whose bottom edge is a wave dipping <see cref="WaveDepth"/> below its sides' ends.</summary>
	private static string DocumentPath(double x, double y, double w, double h)
	{
		var a = WaveDepth / 2;
		var edge = y + h - a;
		return $"M{N(x)},{N(y)} H{N(x + w)} V{N(edge)} Q{N(x + (w * 3 / 4))},{N(edge - (2 * a))} {N(x + (w / 2))},{N(edge)} T{N(x)},{N(edge)} Z";
	}

	/// <summary>A lightning bolt filling the node box, as Mermaid draws its <c>bolt</c> shape.</summary>
	private static void AppendBolt(StringBuilder sb, double x, double y, double w, double h, string fill, string stroke, string sw)
	{
		var (half, gap) = (h / 2, w / 5);
		AppendPath(sb,
			$"M{N(x + w)},{N(y)} L{N(x)},{N(y + half + (gap / 2))} H{N(x + w - (2 * gap))} L{N(x)},{N(y + h)} " +
			$"L{N(x + w)},{N(y + half - (gap / 2))} H{N(x + (2 * gap))} Z",
			fill, stroke, sw);
	}

	/// <summary>
	/// A curly brace down the side at <paramref name="edgeX"/>, <paramref name="depth"/> wide: positive opens to the right
	/// (a left brace), negative to the left (a right brace).
	/// </summary>
	private static void AppendBrace(StringBuilder sb, double edgeX, double y, double h, double depth, string stroke, string sw)
	{
		var (tip, mid, open) = (edgeX, edgeX + (depth / 2), edgeX + depth);
		var r = Math.Min(Math.Abs(depth) / 2, h / 4);
		var ym = y + (h / 2);
		AppendPath(sb,
			$"M{N(open)},{N(y)} Q{N(mid)},{N(y)} {N(mid)},{N(y + r)} V{N(ym - r)} Q{N(mid)},{N(ym)} {N(tip)},{N(ym)} " +
			$"Q{N(mid)},{N(ym)} {N(mid)},{N(ym + r)} V{N(y + h - r)} Q{N(mid)},{N(y + h)} {N(open)},{N(y + h)}",
			"none", stroke, sw);
	}

	private static void AppendPath(StringBuilder sb, string d, string fill, string stroke, string sw) =>
		sb.Append("<path d=\"").Append(d)
			.Append("\" fill=\"").Append(fill)
			.Append("\" stroke=\"").Append(stroke)
			.Append("\" stroke-width=\"").Append(sw).Append("\" />");

	private static void AppendLine(StringBuilder sb, double x1, double y1, double x2, double y2, string stroke, string sw) =>
		sb.Append("\n<line x1=\"").Append(N(x1)).Append("\" y1=\"").Append(N(y1))
			.Append("\" x2=\"").Append(N(x2)).Append("\" y2=\"").Append(N(y2))
			.Append("\" stroke=\"").Append(stroke).Append("\" stroke-width=\"").Append(sw).Append("\" />");

	private static string N(double v) => DesignSystem.Num(v);
}

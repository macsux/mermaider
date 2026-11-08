using Mermaider.Models;
using Mermaider.Text;

namespace Mermaider.Rendering;

internal static class NodeSizing
{
	/// <summary>Widest line of a node label before it wraps (mermaid.js wraps a little narrower; wider reads better at this size).</summary>
	internal const double MaxContentWidth = 150;

	internal static string WrapLabel(string label) =>
		LabelWrap.Wrap(label, MaxContentWidth, RenderConstants.FontSizes.NodeLabel, RenderConstants.FontWeights.NodeLabel);

	/// <summary>Space between an icon's background and its edge (Mermaid's half padding).</summary>
	internal const double IconPadding = 8;

	/// <summary>Gap between an icon (or its background) and its label.</summary>
	internal const double IconLabelGap = 6;

	/// <summary>Size of a node: icon-shape nodes are sized for their icon and label, every other shape for its label.</summary>
	internal static (double Width, double Height) Estimate(MermaidNode node) =>
		node is { Shape: NodeShape.Icon, Icon: { } icon } ? EstimateIcon(node.Label, icon) : Estimate(node.Label, node.Shape);

	/// <summary>Side of the square an icon-shape node's icon (with its background, if any) takes up.</summary>
	internal static double IconBoxSize(NodeIcon icon) =>
		icon.Form == IconForm.None ? icon.Height : icon.Height + (2 * IconPadding);

	/// <summary>An icon-shape node: the icon box with its label stacked above or below it, as wide as the wider of the two.</summary>
	private static (double Width, double Height) EstimateIcon(string label, NodeIcon icon)
	{
		var box = IconBoxSize(icon);
		if (label.Length == 0)
			return (box, box);
		var metrics = TextMetrics.MeasureMultiline(WrapLabel(label).AsSpan(), RenderConstants.FontSizes.NodeLabel, RenderConstants.FontWeights.NodeLabel);
		return (Math.Max(box, metrics.Width + 8), box + IconLabelGap + metrics.Height);
	}

	internal static (double Width, double Height) Estimate(string label, NodeShape shape)
	{
		label = WrapLabel(label);
		var metrics = TextMetrics.MeasureMultiline(label.AsSpan(), RenderConstants.FontSizes.NodeLabel, RenderConstants.FontWeights.NodeLabel);

		var width = metrics.Width + (RenderConstants.NodePadding.Horizontal * 2);
		var height = metrics.Height + (RenderConstants.NodePadding.Vertical * 2);

		switch (shape)
		{
			case NodeShape.Diamond when label.Length == 0:
				// a label-less diamond is a state-diagram choice pseudo-state: a small marker, not a decision box
				return (32, 32);
			case NodeShape.Diamond:
				{
					var side = Math.Max(width, height) + RenderConstants.NodePadding.DiamondExtra;
					width = side;
					height = side;
					break;
				}
			case NodeShape.Circle or NodeShape.DoubleCircle:
				{
					var diameter = Math.Ceiling(Math.Sqrt((width * width) + (height * height))) + 8;
					width = shape == NodeShape.DoubleCircle ? diameter + 12 : diameter;
					height = width;
					break;
				}
			case NodeShape.Hexagon or NodeShape.Trapezoid or NodeShape.TrapezoidAlt or NodeShape.Parallelogram or NodeShape.ParallelogramAlt:
				width += RenderConstants.NodePadding.Horizontal;
				break;
			case NodeShape.Asymmetric:
				width += 12;
				break;
			case NodeShape.Cylinder:
				height += 14;
				break;
			case NodeShape.StateStart or NodeShape.StateEnd:
				return (24, 24);
			case NodeShape.ForkJoin:
				return (120, 8);
		}

		width = Math.Max(width, 64);
		height = Math.Max(height, 40);

		return (width, height);
	}
}

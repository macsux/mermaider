using System.Text.RegularExpressions;
using AwesomeAssertions;
using Mermaider.Models;

namespace Mermaider.Tests.Rendering;

public partial class FlowchartExpandedShapeRendererTests
{
	[GeneratedRegex("<text[^>]* y=\"([-\\d.]+)\"")]
	private static partial Regex TextYPattern();

	private static string NodeGroup(string svg, string id)
	{
		var match = Regex.Match(svg, $"<g class=\"node[^\"]*\" data-id=\"{id}\"[^>]*>(.*?)\n</g>", RegexOptions.Singleline);
		match.Success.Should().BeTrue($"node {id} should be rendered");
		return match.Value;
	}

	private static readonly NodeShape[] ExpandedShapes =
		Enum.GetValues<NodeShape>().SkipWhile(s => s != NodeShape.NotchedRectangle).ToArray();

	public static IEnumerable<Func<string>> ExpandedShapeNames() =>
	[
		() => "notch-rect", () => "hourglass", () => "bolt", () => "brace", () => "brace-r", () => "braces",
		() => "delay", () => "h-cyl", () => "lin-cyl", () => "curv-trap", () => "div-rect", () => "doc",
		() => "tri", () => "win-pane", () => "f-circ", () => "lin-doc", () => "lin-rect", () => "notch-pent",
		() => "flip-tri", () => "sl-rect", () => "docs", () => "st-rect", () => "flag", () => "bow-rect",
		() => "cross-circ", () => "tag-doc", () => "tag-rect",
	];

	[Test]
	public void Every_expanded_shape_except_text_has_a_test_name()
	{
		// guards ExpandedShapeNames against new shapes being added without a rendering check
		ExpandedShapeNames().Count().Should().Be(ExpandedShapes.Length - 1);
	}

	[Test]
	[MethodDataSource(nameof(ExpandedShapeNames))]
	public void Expanded_shape_draws_an_outline(string name)
	{
		var svg = MermaidRenderer.RenderSvg($"flowchart TD\n  A@{{ shape: {name}, label: \"Shape\" }} --> B");

		var node = NodeGroup(svg, "A");
		node.Should().MatchRegex("<(path|rect|polygon|circle|ellipse) ");
	}

	[Test]
	public void Diamond_from_shape_data_renders_as_a_diamond()
	{
		var svg = MermaidRenderer.RenderSvg("flowchart TD\n  A@{ shape: diamond, label: \"Decide\" }");

		var node = NodeGroup(svg, "A");
		node.Should().Contain("data-shape=\"diamond\"");
		node.Should().Contain("<polygon ");
		node.Should().Contain("Decide");
	}

	[Test]
	public void Text_shape_draws_its_label_without_an_outline()
	{
		var svg = MermaidRenderer.RenderSvg("flowchart TD\n  A@{ shape: text, label: \"Just words\" }");

		var node = NodeGroup(svg, "A");
		node.Should().Contain("data-shape=\"text\"");
		node.Should().NotMatchRegex("<(path|rect|polygon|circle|ellipse) ");
		node.Should().Contain("Just words");
	}

	[Test]
	[Arguments("bolt")]
	[Arguments("hourglass")]
	[Arguments("f-circ")]
	[Arguments("cross-circ")]
	public void Label_less_shapes_draw_no_text(string name)
	{
		var svg = MermaidRenderer.RenderSvg($"flowchart TD\n  A@{{ shape: {name}, label: \"Hidden\" }}");

		NodeGroup(svg, "A").Should().NotContain("<text");
	}

	[Test]
	public void Document_label_sits_above_the_wave()
	{
		var svg = MermaidRenderer.RenderSvg("flowchart TD\n  A@{ shape: doc, label: \"Doc\" }\n  B[Doc]");

		static double TextY(string group) =>
			double.Parse(TextYPattern().Match(group).Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
		static double Top(string group, string pattern) =>
			double.Parse(Regex.Match(group, pattern).Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);

		var doc = NodeGroup(svg, "A");
		var rect = NodeGroup(svg, "B");
		var docOffset = TextY(doc) - Top(doc, "<path d=\"M[-\\d.]+,([-\\d.]+)");
		var rectOffset = TextY(rect) - Top(rect, "<rect[^>]* y=\"([-\\d.]+)\"");
		// same label, but the document is taller by its wave and the label stays clear of it
		docOffset.Should().BeApproximately(rectOffset, 0.5);
	}

	[Test]
	[Arguments("sm-circ")]
	[Arguments("fr-circ")]
	public void Start_and_stop_circles_keep_flowchart_spacing(string shape)
	{
		static double Gap(string svg) => RectY(NodeGroup(svg, "C")) - RectY(NodeGroup(svg, "B"));
		static string Chain(string shape) =>
			MermaidRenderer.RenderSvg($"flowchart TD\n  S@{{ shape: {shape} }} --> A[Step one] --> B[Step two] --> C[Step three]");

		// the circles use the state-diagram shapes, but the flowchart isn't laid out as a state diagram
		Gap(Chain(shape)).Should().BeApproximately(Gap(Chain("rect")), 0.01);
	}

	[GeneratedRegex("<rect[^>]* y=\"([-\\d.]+)\"")]
	private static partial Regex RectYPattern();

	private static double RectY(string group) =>
		double.Parse(RectYPattern().Match(group).Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
}

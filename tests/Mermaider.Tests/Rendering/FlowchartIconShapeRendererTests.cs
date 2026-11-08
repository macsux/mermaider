using System.Text.RegularExpressions;
using AwesomeAssertions;
using Mermaider.Icons;

namespace Mermaider.Tests.Rendering;

public partial class FlowchartIconShapeRendererTests
{
	[GeneratedRegex("<rect ")]
	private static partial Regex RectTag();

	[GeneratedRegex("<text[^>]* y=\"([-\\d.]+)\"")]
	private static partial Regex TextY();

	[GeneratedRegex("<image[^>]* y=\"([-\\d.]+)\"")]
	private static partial Regex ImageY();

	[GeneratedRegex("<rect[^>]* height=\"([\\d.]+)\"")]
	private static partial Regex RectHeight();

	private static string NodeGroup(string svg, string id)
	{
		var match = Regex.Match(svg, $"<g class=\"node[^\"]*\" data-id=\"{id}\"[^>]*>(.*?)\n</g>", RegexOptions.Singleline);
		match.Success.Should().BeTrue($"node {id} should be rendered");
		return match.Value;
	}

	[Test]
	public void Custom_registered_icon_is_embedded_as_image()
	{
		const string name = "test:flowchart-icon-shape";
		try
		{
			IconRegistry.Register(name, """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><rect x="0" y="0" width="24" height="24" fill="#123456"/></svg>""");

			var svg = MermaidRenderer.RenderSvg($"flowchart TD\n  A@{{ icon: \"{name}\", label: \"Mine\" }}");

			var node = NodeGroup(svg, "A");
			node.Should().Contain("data-shape=\"icon\"");
			node.Should().Contain("<image ");
			node.Should().Contain("href=\"data:image/svg+xml;base64,");
			node.Should().Contain("Mine");
		}
		finally
		{
			IconRegistry.Unregister(name);
		}
	}

	[Test]
	public void No_form_draws_no_background()
	{
		var svg = MermaidRenderer.RenderSvg("flowchart TD\n  A@{ icon: \"aws:compute\" }");

		var node = NodeGroup(svg, "A");
		// the vendor badge is the only rect: no background shape behind it
		RectTag().Count(node).Should().Be(1);
		node.Should().NotContain("<circle ");
		node.Should().NotContain("<text");
	}

	[Test]
	public void Circle_form_draws_a_circle_behind_the_icon()
	{
		var svg = MermaidRenderer.RenderSvg("flowchart TD\n  A@{ icon: \"aws:compute\", form: \"circle\" }");

		NodeGroup(svg, "A").Should().Contain("<circle ");
	}

	[Test]
	public void Square_and_rounded_forms_differ_only_in_corner_radius()
	{
		var square = NodeGroup(MermaidRenderer.RenderSvg("flowchart TD\n  A@{ icon: \"aws:compute\", form: \"square\" }"), "A");
		var rounded = NodeGroup(MermaidRenderer.RenderSvg("flowchart TD\n  A@{ icon: \"aws:compute\", form: \"rounded\" }"), "A");

		RectTag().Count(square).Should().Be(2);
		square.Should().Contain("rx=\"0\"");
		RectTag().Count(rounded).Should().Be(2);
		rounded.Should().NotContain("rx=\"0\"");
	}

	[Test]
	public void Unknown_icon_draws_the_fallback_glyph()
	{
		var svg = MermaidRenderer.RenderSvg("flowchart TD\n  A@{ icon: \"fa:not-a-registered-icon\" }");

		NodeGroup(svg, "A").Should().Contain("<g transform=\"translate(");
	}

	[Test]
	public void Label_sits_below_by_default_and_above_with_pos_t()
	{
		static (double TextY, double IconY) Positions(string pos)
		{
			var node = NodeGroup(MermaidRenderer.RenderSvg($"flowchart TD\n  A@{{ icon: \"aws:compute\", label: \"Label\", pos: \"{pos}\" }}"), "A");
			var textY = double.Parse(TextY().Match(node).Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
			var iconY = double.Parse(ImageY().Match(node).Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
			return (textY, iconY);
		}

		var below = Positions("b");
		below.TextY.Should().BeGreaterThan(below.IconY + (48 * 0.7));
		var above = Positions("t");
		above.TextY.Should().BeLessThan(above.IconY);
	}

	[Test]
	public void Height_scales_the_node()
	{
		static double Height(string h)
		{
			var svg = MermaidRenderer.RenderSvg($"flowchart TD\n  A@{{ icon: \"cloud\", form: \"square\", h: {h} }}");
			var rect = RectHeight().Match(NodeGroup(svg, "A"));
			return double.Parse(rect.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
		}

		Height("96").Should().BeGreaterThan(Height("48") + 40);
	}

	[Test]
	public void Ascii_shows_label_or_icon_name()
	{
		var text = MermaidRenderer.RenderAscii("flowchart LR\n  A@{ icon: \"server\" } --> B@{ icon: \"cloud\", label: \"Edge\" }");

		text.Should().Contain("server");
		text.Should().Contain("Edge");
	}
}

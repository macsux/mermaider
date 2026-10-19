using AwesomeAssertions;
using Mermaider.Layout;
using Mermaider.Models;
using Mermaider.Parsing;
using Mermaider.Text;

namespace Mermaider.Tests.Rendering;

/// <summary>A host's <see cref="ITextMeasurer"/> sizes and wraps labels in place of the built-in estimate.</summary>
public class TextMeasurerTests
{
	private sealed class Wide : ITextMeasurer
	{
		internal int Calls;

		public double MeasureWidth(ReadOnlySpan<char> text, double fontSize, int fontWeight)
		{
			Calls++;
			return text.Length * fontSize;
		}
	}

	private static MermaidGraph Parse(string source) =>
		FlowchartParser.Parse(MermaidRenderer.PreprocessLines(DiagramPreprocessor.Process(source).CleanedText));

	private const string Source = """
		flowchart LR
		    A[Short] -->|a label long enough to wrap when measured wide enough| B[Another node]
		""";

	[Test]
	public void The_layout_measures_with_the_host_measurer()
	{
		var measurer = new Wide();
		var plain = DefaultLayoutProvider.Instance.LayoutFlowchart(Parse(Source));
		var measured = DefaultLayoutProvider.Instance.LayoutFlowchart(Parse(Source), new RenderOptions { TextMeasurer = measurer });

		measurer.Calls.Should().BeGreaterThan(0);
		measured.Nodes.Single(n => n.Id == "B").Width.Should().BeGreaterThan(plain.Nodes.Single(n => n.Id == "B").Width);
		measured.Edges.Single().Label.Should().Contain("\n", "a label wider than the edge-label limit wraps");
	}

	[Test]
	public void The_measurer_only_applies_to_its_own_render()
	{
		_ = DefaultLayoutProvider.Instance.LayoutFlowchart(Parse(Source), new RenderOptions { TextMeasurer = new Wide() });
		TextMetrics.MeasureTextWidth("abcd", 14, 500).Should().BeLessThan(4 * 14, "the estimate is back once the layout returns");
	}

	[Test]
	public void Svg_rendering_uses_the_measurer()
	{
		var measurer = new Wide();
		_ = MermaidRenderer.RenderSvg(Source, new RenderOptions { TextMeasurer = measurer });
		measurer.Calls.Should().BeGreaterThan(0);
	}
}

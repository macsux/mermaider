using AwesomeAssertions;
using Mermaider.Text;

namespace Mermaider.Tests.Text;

public class LabelWrapTests
{
	/// <summary>Every character 10px wide (a space too): widths are easy to reason about.</summary>
	private sealed class FixedWidth : ITextMeasurer
	{
		public double MeasureWidth(ReadOnlySpan<char> text, double fontSize, int fontWeight) => text.Length * 10;
	}

	private sealed class Counting : ITextMeasurer
	{
		internal int Calls;

		public double MeasureWidth(ReadOnlySpan<char> text, double fontSize, int fontWeight)
		{
			Calls++;
			return text.Length * 10;
		}
	}

	private static string Wrap(string label, double maxWidth)
	{
		using var scope = TextMetrics.Use(new FixedWidth());
		return LabelWrap.Wrap(label, maxWidth, 14, 500);
	}

	[Test]
	public void A_label_that_fits_is_returned_as_is()
	{
		const string label = "short label";
		Wrap(label, 200).Should().BeSameAs(label);
	}

	[Test]
	public void A_long_line_wraps_into_the_fewest_lines_that_fit()
	{
		Wrap("aaaa bbbb cccc dddd", 100).Split('\n').Should().HaveCount(2);
	}

	[Test]
	public void Wrapped_lines_are_balanced_instead_of_ending_on_an_orphan()
	{
		// greedy filling at 150px would give "aaaa bbbb cccc" / "dd"
		Wrap("aaaa bbbb cccc dd", 150).Should().Be("aaaa bbbb\ncccc dd");
	}

	[Test]
	public void Explicit_line_breaks_are_kept_and_each_line_wraps_on_its_own()
	{
		Wrap("title\naaaa bbbb cccc dddd", 100).Should().Be("title\naaaa bbbb\ncccc dddd");
	}

	[Test]
	public void A_line_a_little_over_the_limit_stays_whole_when_wrapping_would_hardly_narrow_it()
	{
		// 210px over a 200px limit; wrapping would leave a 190px box
		Wrap("a bcdefghijklmnopqrst", 200).Should().Be("a bcdefghijklmnopqrst");
	}

	[Test]
	public void A_very_long_line_measures_a_few_runs_per_word_not_every_run()
	{
		var words = string.Join(' ', Enumerable.Range(0, 2000).Select(i => "w" + i));
		var measurer = new Counting();
		using (TextMetrics.Use(measurer))
			_ = LabelWrap.Wrap(words, 150, 14, 500);
		measurer.Calls.Should().BeLessThan(2000 * 40);
	}

	[Test]
	public void A_single_word_wider_than_the_limit_stays_whole()
	{
		Wrap("supercalifragilistic", 100).Should().Be("supercalifragilistic");
	}

	[Test]
	public void Node_labels_wrap_at_the_node_width_and_edge_labels_wider()
	{
		var node = Mermaider.Rendering.NodeSizing.WrapLabel("Write one message into the stream's chat");
		node.Should().Contain("\n");
		Mermaider.Layout.LightweightLayoutEngine.EdgeLabelMaxWidth.Should().BeGreaterThan(Mermaider.Rendering.NodeSizing.MaxContentWidth);
	}
}

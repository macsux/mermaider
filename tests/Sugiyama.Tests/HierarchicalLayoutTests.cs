using AwesomeAssertions;
using Sugiyama;

namespace Sugiyama.Tests;

public class HierarchicalLayoutTests
{
	[Test]
	public void Lr_cycle_keeps_the_written_chain_in_order()
	{
		// A → B → C → D with C → B and D → A: turning only B → C backwards would be fewer back edges, but starts the chain at C
		var graph = new LayoutGraph(
			LayoutDirection.LR,
			[new LayoutNode("A", 80, 40), new LayoutNode("B", 80, 40), new LayoutNode("C", 80, 40), new LayoutNode("D", 80, 40)],
			[
				new LayoutEdge("A", "B"),
				new LayoutEdge("B", "C"),
				new LayoutEdge("C", "D"),
				new LayoutEdge("C", "B", LabelWidth: 110, LabelHeight: 22),
				new LayoutEdge("D", "A", LabelWidth: 130, LabelHeight: 22),
			],
			[]);

		var result = HierarchicalLayout.Compute(graph, LayoutOptions.Default);

		result.Nodes.OrderBy(n => n.X).Select(n => n.Id).Should().Equal("A", "B", "C", "D");
	}

	[Test]
	public void Td_loop_back_to_the_start_keeps_the_start_on_top()
	{
		// The decision node fans out, so a feedback-arc-set order would start there and drop the start to the bottom
		var graph = new LayoutGraph(
			LayoutDirection.TD,
			[
				new LayoutNode("Start", 80, 40),
				new LayoutNode("Ask", 80, 40),
				new LayoutNode("Yes", 80, 40),
				new LayoutNode("No", 80, 40),
				new LayoutNode("End", 80, 40),
			],
			[
				new LayoutEdge("Start", "Ask"),
				new LayoutEdge("Ask", "Yes"),
				new LayoutEdge("Ask", "No"),
				new LayoutEdge("Yes", "End"),
				new LayoutEdge("No", "End"),
				new LayoutEdge("End", "Start"),
			],
			[]);

		var result = HierarchicalLayout.Compute(graph, LayoutOptions.Default);
		double Y(string id) => result.Nodes.Single(n => n.Id == id).Y;

		Y("Start").Should().BeLessThan(Y("Ask"));
		Y("Ask").Should().BeLessThan(Y("Yes"));
		Y("Yes").Should().BeLessThan(Y("End"));
	}
}

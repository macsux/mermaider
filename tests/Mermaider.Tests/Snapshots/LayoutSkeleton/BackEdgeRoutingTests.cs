using AwesomeAssertions;
using Mermaider.Models;

namespace Mermaider.Tests.Snapshots.LayoutSkeleton;

/// <summary>
/// Back edges (against the flow) in every direction get a lane of their own instead of sharing the corridor, and the label
/// pill, of the forward edges between the same boxes.
/// </summary>
public class BackEdgeRoutingTests
{
	private static IEnumerable<PositionedGroup> Flatten(IEnumerable<PositionedGroup> gs) =>
		gs.SelectMany(g => new[] { g }.Concat(Flatten(g.Children)));

	[Test]
	public void A_back_edge_into_a_subgraph_loops_over_or_under_the_drawing()
	{
		var (_, layout) = FlowchartVisualLintTests.Layout("layout-lr-back-edge-into-subgraph");
		var back = layout.Edges.Single(e => e.Source == "RES" && e.Style == EdgeStyle.Dotted);
		var controllers = Flatten(layout.Groups).Single(g => g.Id == "Controllers");
		var res = layout.Nodes.Single(n => n.Id == "RES");

		// it leaves the resource through its top or bottom, runs beyond everything it passes, and comes down (or up) onto the
		// subgraph's border: never through the side the forward edges use, nor at the middle of it beside one of its nodes
		back.Points.Should().HaveCount(4);
		back.Points[0].X.Should().BeApproximately(back.Points[1].X, 0.01);
		back.Points[2].X.Should().BeApproximately(back.Points[3].X, 0.01);
		var lane = back.Points[1].Y;
		var over = lane < res.Y;
		(over ? lane < controllers.Y : lane > controllers.Y + controllers.Height).Should().BeTrue("the lane runs outside the subgraph it enters");
		back.Points[3].Y.Should().BeApproximately(over ? controllers.Y : controllers.Y + controllers.Height, 2);
		back.LabelPosition!.Value.Y.Should().BeApproximately(lane, 0.5, "the label sits on the lane, away from the forward edges' labels");
	}

	[Test]
	[Arguments("layout-lr-back-edge-into-subgraph")]
	[Arguments("layout-lr-parallel-labels")]
	[Arguments("layout-rl-back-edge")]
	[Arguments("layout-bt-back-edge")]
	[Arguments("layout-sibling-widths")]
	[Arguments("layout-state-long-labels")]
	public void Labels_never_sit_on_other_labels_lines_or_nodes(string slug)
	{
		var (graph, layout) = FlowchartVisualLintTests.Layout(slug);
		VisualLint.Run(layout, graph.Direction).Where(v => v.Check == VisualLint.LabelOverlap).Should().BeEmpty();
	}

	[Test]
	[Arguments("layout-rl-back-edge", "I2")]
	[Arguments("layout-bt-back-edge", "C")]
	public void A_blocked_back_edge_leaves_and_enters_across_the_flow(string slug, string from)
	{
		var (graph, layout) = FlowchartVisualLintTests.Layout(slug);
		var vertical = graph.Direction is Direction.TD or Direction.TB or Direction.BT;
		var back = layout.Edges.Single(e => e.Source == from);
		// across the flow: the first and last runs are horizontal in a vertical flow, vertical in a horizontal one
		static bool Horizontal(Point a, Point b) => Math.Abs(a.Y - b.Y) < 0.01;
		Horizontal(back.Points[0], back.Points[1]).Should().Be(vertical);
		Horizontal(back.Points[^2], back.Points[^1]).Should().Be(vertical);
	}
}

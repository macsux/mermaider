using AwesomeAssertions;
using Mermaider.Models;

namespace Mermaider.Tests.Parsing;

public class FlowchartExpandedShapeParserTests
{
	[Test]
	[Arguments("rect", NodeShape.Rectangle)]
	[Arguments("rounded", NodeShape.Rounded)]
	[Arguments("stadium", NodeShape.Stadium)]
	[Arguments("fr-rect", NodeShape.Subroutine)]
	[Arguments("cyl", NodeShape.Cylinder)]
	[Arguments("circle", NodeShape.Circle)]
	[Arguments("diam", NodeShape.Diamond)]
	[Arguments("hex", NodeShape.Hexagon)]
	[Arguments("lean-r", NodeShape.Parallelogram)]
	[Arguments("lean-l", NodeShape.ParallelogramAlt)]
	[Arguments("trap-b", NodeShape.Trapezoid)]
	[Arguments("trap-t", NodeShape.TrapezoidAlt)]
	[Arguments("dbl-circ", NodeShape.DoubleCircle)]
	[Arguments("text", NodeShape.Text)]
	[Arguments("notch-rect", NodeShape.NotchedRectangle)]
	[Arguments("lin-rect", NodeShape.LinedRectangle)]
	[Arguments("sm-circ", NodeShape.StateStart)]
	[Arguments("fr-circ", NodeShape.StateEnd)]
	[Arguments("fork", NodeShape.ForkJoin)]
	[Arguments("hourglass", NodeShape.Hourglass)]
	[Arguments("brace", NodeShape.BraceLeft)]
	[Arguments("brace-r", NodeShape.BraceRight)]
	[Arguments("braces", NodeShape.Braces)]
	[Arguments("bolt", NodeShape.Bolt)]
	[Arguments("doc", NodeShape.Document)]
	[Arguments("delay", NodeShape.Delay)]
	[Arguments("h-cyl", NodeShape.HorizontalCylinder)]
	[Arguments("lin-cyl", NodeShape.LinedCylinder)]
	[Arguments("curv-trap", NodeShape.CurvedTrapezoid)]
	[Arguments("div-rect", NodeShape.DividedRectangle)]
	[Arguments("tri", NodeShape.Triangle)]
	[Arguments("win-pane", NodeShape.WindowPane)]
	[Arguments("f-circ", NodeShape.FilledCircle)]
	[Arguments("notch-pent", NodeShape.NotchedPentagon)]
	[Arguments("flip-tri", NodeShape.FlippedTriangle)]
	[Arguments("sl-rect", NodeShape.SlopedRectangle)]
	[Arguments("docs", NodeShape.StackedDocument)]
	[Arguments("st-rect", NodeShape.StackedRectangle)]
	[Arguments("bow-rect", NodeShape.BowTieRectangle)]
	[Arguments("cross-circ", NodeShape.CrossedCircle)]
	[Arguments("tag-doc", NodeShape.TaggedDocument)]
	[Arguments("tag-rect", NodeShape.TaggedRectangle)]
	[Arguments("flag", NodeShape.Flag)]
	[Arguments("odd", NodeShape.Asymmetric)]
	[Arguments("lin-doc", NodeShape.LinedDocument)]
	public void Short_name_selects_its_shape(string name, NodeShape expected)
	{
		var graph = MermaidRenderer.Parse($"flowchart TD\n  A@{{ shape: {name} }}");

		graph.Nodes["A"].Shape.Should().Be(expected);
	}

	[Test]
	[Arguments("process", NodeShape.Rectangle)]
	[Arguments("event", NodeShape.Rounded)]
	[Arguments("pill", NodeShape.Stadium)]
	[Arguments("subroutine", NodeShape.Subroutine)]
	[Arguments("database", NodeShape.Cylinder)]
	[Arguments("question", NodeShape.Diamond)]
	[Arguments("in-out", NodeShape.Parallelogram)]
	[Arguments("inv-trapezoid", NodeShape.TrapezoidAlt)]
	[Arguments("card", NodeShape.NotchedRectangle)]
	[Arguments("shaded-process", NodeShape.LinedRectangle)]
	[Arguments("collate", NodeShape.Hourglass)]
	[Arguments("comment", NodeShape.BraceLeft)]
	[Arguments("lightning-bolt", NodeShape.Bolt)]
	[Arguments("document", NodeShape.Document)]
	[Arguments("das", NodeShape.HorizontalCylinder)]
	[Arguments("disk", NodeShape.LinedCylinder)]
	[Arguments("display", NodeShape.CurvedTrapezoid)]
	[Arguments("divided-process", NodeShape.DividedRectangle)]
	[Arguments("extract", NodeShape.Triangle)]
	[Arguments("internal-storage", NodeShape.WindowPane)]
	[Arguments("junction", NodeShape.FilledCircle)]
	[Arguments("loop-limit", NodeShape.NotchedPentagon)]
	[Arguments("manual-file", NodeShape.FlippedTriangle)]
	[Arguments("manual-input", NodeShape.SlopedRectangle)]
	[Arguments("st-doc", NodeShape.StackedDocument)]
	[Arguments("processes", NodeShape.StackedRectangle)]
	[Arguments("stored-data", NodeShape.BowTieRectangle)]
	[Arguments("summary", NodeShape.CrossedCircle)]
	[Arguments("tagged-document", NodeShape.TaggedDocument)]
	[Arguments("tag-proc", NodeShape.TaggedRectangle)]
	[Arguments("paper-tape", NodeShape.Flag)]
	[Arguments("lined-document", NodeShape.LinedDocument)]
	[Arguments("start", NodeShape.StateStart)]
	[Arguments("stop", NodeShape.StateEnd)]
	[Arguments("join", NodeShape.ForkJoin)]
	public void Alias_selects_its_shape(string name, NodeShape expected)
	{
		var graph = MermaidRenderer.Parse($"flowchart TD\n  A@{{ shape: {name} }}");

		graph.Nodes["A"].Shape.Should().Be(expected);
	}

	[Test]
	public void Shape_and_label_are_both_applied()
	{
		var graph = MermaidRenderer.Parse("flowchart TD\n  A@{ shape: diamond, label: \"Decide\" }");

		graph.Nodes["A"].Should().Be(new MermaidNode("A", "Decide", NodeShape.Diamond));
	}

	[Test]
	public void Without_a_label_the_node_keeps_its_id_as_label()
	{
		var graph = MermaidRenderer.Parse("flowchart TD\n  A@{ shape: doc }");

		graph.Nodes["A"].Label.Should().Be("A");
	}

	[Test]
	public void Shape_names_are_case_insensitive_and_may_be_quoted()
	{
		var graph = MermaidRenderer.Parse("flowchart TD\n  A@{ shape: \"Lin-Cyl\" } --> B@{ shape: 'HEX' }");

		graph.Nodes["A"].Shape.Should().Be(NodeShape.LinedCylinder);
		graph.Nodes["B"].Shape.Should().Be(NodeShape.Hexagon);
	}

	[Test]
	public void Unknown_shape_name_keeps_the_existing_shape()
	{
		var graph = MermaidRenderer.Parse("""
			flowchart TD
			  A{Decide} --> B
			  A@{ shape: no-such-shape, label: "Still a diamond" }
			""");

		graph.Nodes["A"].Shape.Should().Be(NodeShape.Diamond);
		graph.Nodes["A"].Label.Should().Be("Still a diamond");
	}

	[Test]
	public void Shape_data_on_a_node_referenced_earlier_replaces_its_shape()
	{
		var graph = MermaidRenderer.Parse("""
			flowchart TD
			  A --> B
			  B@{ shape: cyl, label: "Orders" }
			""");

		graph.Nodes["B"].Should().Be(new MermaidNode("B", "Orders", NodeShape.Cylinder));
	}

	[Test]
	[Arguments("f-circ")]
	[Arguments("hourglass")]
	[Arguments("bolt")]
	[Arguments("cross-circ")]
	[Arguments("sm-circ")]
	[Arguments("fr-circ")]
	[Arguments("fork")]
	public void Shapes_Mermaid_draws_without_a_label_drop_it(string name)
	{
		var graph = MermaidRenderer.Parse($"flowchart TD\n  A@{{ shape: {name}, label: \"Ignored\" }}");

		graph.Nodes["A"].Label.Should().BeEmpty();
	}

	[Test]
	public void Shaped_nodes_chain_with_edges_and_classes()
	{
		var graph = MermaidRenderer.Parse("""
			flowchart LR
			  A@{ shape: manual-input, label: "Enter" }:::input --> B@{ shape: decision, label: "Valid?" } -->|yes| C@{ shape: docs, label: "Reports" }
			""");

		graph.Nodes["A"].Shape.Should().Be(NodeShape.SlopedRectangle);
		graph.Nodes["B"].Shape.Should().Be(NodeShape.Diamond);
		graph.Nodes["C"].Shape.Should().Be(NodeShape.StackedDocument);
		graph.ClassAssignments["A"].Should().Be("input");
		graph.Edges.Select(e => (e.Source, e.Target, e.Label)).Should().Equal(("A", "B", null), ("B", "C", "yes"));
	}
}

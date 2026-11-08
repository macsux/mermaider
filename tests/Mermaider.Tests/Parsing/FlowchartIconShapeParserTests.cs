using AwesomeAssertions;
using Mermaider.Models;

namespace Mermaider.Tests.Parsing;

public class FlowchartIconShapeParserTests
{
	[Test]
	public void Parses_every_icon_option()
	{
		var graph = MermaidRenderer.Parse("""
			flowchart TD
			  A@{ icon: "fa:user", form: "square", label: "User Icon", pos: "t", h: 60 }
			""");

		var node = graph.Nodes["A"];
		node.Shape.Should().Be(NodeShape.Icon);
		node.Label.Should().Be("User Icon");
		node.Icon.Should().Be(new NodeIcon("fa:user", IconForm.Square, IconLabelPosition.Top, 60));
	}

	[Test]
	public void Defaults_to_no_form_no_label_label_below_and_height_48()
	{
		var graph = MermaidRenderer.Parse("flowchart TD\n  A@{ icon: \"database\" }");

		var node = graph.Nodes["A"];
		node.Shape.Should().Be(NodeShape.Icon);
		node.Label.Should().BeEmpty();
		node.Icon.Should().Be(new NodeIcon("database", IconForm.None, IconLabelPosition.Bottom, 48));
	}

	[Test]
	[Arguments("square", IconForm.Square)]
	[Arguments("circle", IconForm.Circle)]
	[Arguments("rounded", IconForm.Rounded)]
	[Arguments("Rounded", IconForm.Rounded)]
	[Arguments("blob", IconForm.None)]
	public void Parses_form(string form, IconForm expected)
	{
		var graph = MermaidRenderer.Parse($"flowchart TD\n  A@{{ icon: \"cloud\", form: \"{form}\" }}");

		graph.Nodes["A"].Icon!.Form.Should().Be(expected);
	}

	[Test]
	public void Height_below_48_is_raised_to_the_minimum()
	{
		var graph = MermaidRenderer.Parse("flowchart TD\n  A@{ icon: \"cloud\", h: 20 }");

		graph.Nodes["A"].Icon!.Height.Should().Be(48);
	}

	[Test]
	public void Icon_nodes_take_part_in_edges_on_either_side()
	{
		var graph = MermaidRenderer.Parse("""
			flowchart LR
			  A@{ icon: "aws:compute", label: "EC2" } --> B@{ icon: "database", label: "RDS" } --> C[Plain]
			""");

		graph.Nodes["A"].Shape.Should().Be(NodeShape.Icon);
		graph.Nodes["B"].Shape.Should().Be(NodeShape.Icon);
		graph.Nodes["C"].Shape.Should().Be(NodeShape.Rectangle);
		graph.Edges.Select(e => (e.Source, e.Target)).Should().Equal(("A", "B"), ("B", "C"));
	}

	[Test]
	public void Shape_data_on_a_node_referenced_earlier_replaces_its_default_shape()
	{
		var graph = MermaidRenderer.Parse("""
			flowchart TD
			  A --> B
			  B@{ icon: "server", label: "Server" }
			""");

		graph.Nodes["B"].Shape.Should().Be(NodeShape.Icon);
		graph.Nodes["B"].Label.Should().Be("Server");
	}

	[Test]
	public void Quoted_values_keep_commas_colons_and_braces()
	{
		var graph = MermaidRenderer.Parse("flowchart TD\n  A@{ icon: 'aws:compute', label: \"One, two: {three}\" }");

		graph.Nodes["A"].Icon!.Name.Should().Be("aws:compute");
		graph.Nodes["A"].Label.Should().Be("One, two: {three}");
	}

	[Test]
	public void Shape_data_may_span_lines()
	{
		var graph = MermaidRenderer.Parse("""
			flowchart TD
			  A@{
			    icon: "cloud",
			    form: "circle",
			    label: "Cloud"
			  }
			  A --> B
			""");

		graph.Nodes["A"].Icon.Should().Be(new NodeIcon("cloud", IconForm.Circle));
		graph.Nodes["A"].Label.Should().Be("Cloud");
		graph.Edges.Should().ContainSingle();
	}

	[Test]
	public void Class_shorthand_after_shape_data_still_applies()
	{
		var graph = MermaidRenderer.Parse("flowchart TD\n  A@{ icon: \"cloud\" }:::hot --> B");

		graph.ClassAssignments["A"].Should().Be("hot");
		graph.Edges.Should().ContainSingle();
	}

	[Test]
	public void Shape_data_without_an_icon_only_sets_the_label()
	{
		var graph = MermaidRenderer.Parse("flowchart TD\n  A@{ label: \"Named\" } --> B");

		graph.Nodes["A"].Shape.Should().Be(NodeShape.Rectangle);
		graph.Nodes["A"].Label.Should().Be("Named");
		graph.Nodes["A"].Icon.Should().BeNull();
	}
}

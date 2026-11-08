using Mermaider.Models;

namespace Mermaider.Rendering;

internal static class EnumNames
{
	internal static string ToLower(this EdgeStyle style) => style switch
	{
		EdgeStyle.Solid => "solid",
		EdgeStyle.Dotted => "dotted",
		EdgeStyle.Thick => "thick",
		EdgeStyle.Invisible => "invisible",
		_ => style.ToString().ToLowerInvariant(),
	};

	internal static string ToLower(this NodeShape shape) => shape switch
	{
		NodeShape.Rectangle => "rectangle",
		NodeShape.Rounded => "rounded",
		NodeShape.Stadium => "stadium",
		NodeShape.Diamond => "diamond",
		NodeShape.Circle => "circle",
		NodeShape.DoubleCircle => "doublecircle",
		NodeShape.Subroutine => "subroutine",
		NodeShape.Hexagon => "hexagon",
		NodeShape.Cylinder => "cylinder",
		NodeShape.Asymmetric => "asymmetric",
		NodeShape.Trapezoid => "trapezoid",
		NodeShape.TrapezoidAlt => "trapezoidalt",
		NodeShape.Parallelogram => "parallelogram",
		NodeShape.ParallelogramAlt => "parallelogramalt",
		NodeShape.StateStart => "statestart",
		NodeShape.StateEnd => "stateend",
		NodeShape.ForkJoin => "forkjoin",
		NodeShape.Icon => "icon",
		NodeShape.NotchedRectangle => "notchedrectangle",
		NodeShape.Hourglass => "hourglass",
		NodeShape.Bolt => "bolt",
		NodeShape.BraceLeft => "braceleft",
		NodeShape.BraceRight => "braceright",
		NodeShape.Braces => "braces",
		NodeShape.Delay => "delay",
		NodeShape.HorizontalCylinder => "horizontalcylinder",
		NodeShape.LinedCylinder => "linedcylinder",
		NodeShape.CurvedTrapezoid => "curvedtrapezoid",
		NodeShape.DividedRectangle => "dividedrectangle",
		NodeShape.Document => "document",
		NodeShape.Triangle => "triangle",
		NodeShape.WindowPane => "windowpane",
		NodeShape.FilledCircle => "filledcircle",
		NodeShape.LinedDocument => "lineddocument",
		NodeShape.LinedRectangle => "linedrectangle",
		NodeShape.NotchedPentagon => "notchedpentagon",
		NodeShape.FlippedTriangle => "flippedtriangle",
		NodeShape.SlopedRectangle => "slopedrectangle",
		NodeShape.StackedDocument => "stackeddocument",
		NodeShape.StackedRectangle => "stackedrectangle",
		NodeShape.Flag => "flag",
		NodeShape.BowTieRectangle => "bowtierectangle",
		NodeShape.CrossedCircle => "crossedcircle",
		NodeShape.TaggedDocument => "taggeddocument",
		NodeShape.TaggedRectangle => "taggedrectangle",
		NodeShape.Text => "text",
		_ => shape.ToString().ToLowerInvariant(),
	};

	internal static string ToLower(this ErCardinality c) => c switch
	{
		ErCardinality.One => "one",
		ErCardinality.Many => "many",
		ErCardinality.ZeroOne => "zeroone",
		ErCardinality.ZeroMany => "zeromany",
		_ => c.ToString().ToLowerInvariant(),
	};

	internal static string ToLower(this ClassRelationType t) => t switch
	{
		ClassRelationType.Inheritance => "inheritance",
		ClassRelationType.Composition => "composition",
		ClassRelationType.Aggregation => "aggregation",
		ClassRelationType.Association => "association",
		ClassRelationType.Dependency => "dependency",
		ClassRelationType.Realization => "realization",
		ClassRelationType.Lollipop => "lollipop",
		_ => t.ToString().ToLowerInvariant(),
	};

	internal static string ToLower(this SequenceBlockType t) => t switch
	{
		SequenceBlockType.Loop => "loop",
		SequenceBlockType.Alt => "alt",
		SequenceBlockType.Opt => "opt",
		SequenceBlockType.Par => "par",
		SequenceBlockType.Critical => "critical",
		SequenceBlockType.Break => "break",
		_ => t.ToString().ToLowerInvariant(),
	};

	internal static string ToLower(this SequenceNotePosition p) => p switch
	{
		SequenceNotePosition.Left => "left",
		SequenceNotePosition.Right => "right",
		SequenceNotePosition.Over => "over",
		_ => p.ToString().ToLowerInvariant(),
	};
}

namespace Mermaider.Models;

/// <summary>Visual shape of a node in a Mermaid diagram.</summary>
public enum NodeShape
{
	Rectangle,
	Rounded,
	Diamond,
	Stadium,
	Circle,
	Subroutine,
	DoubleCircle,
	Hexagon,
	Cylinder,
	Asymmetric,
	Trapezoid,
	TrapezoidAlt,

	/// <summary><c>[/text/]</c>, leaning right.</summary>
	Parallelogram,

	/// <summary><c>[\text\]</c>, leaning left.</summary>
	ParallelogramAlt,
	StateStart,
	StateEnd,
	ForkJoin,

	/// <summary>Flowchart icon shape, <c>A@{ icon: "name" }</c>; the icon itself is on the node's <see cref="NodeIcon"/>.</summary>
	Icon,

	// Shapes only reachable through Mermaid's expanded syntax, A@{ shape: name } (short name in the comment).
	// Appended after the older shapes so their numeric values stay the same for compiled consumers.

	/// <summary>Rectangle with its top-left corner cut off (<c>notch-rect</c>, card).</summary>
	NotchedRectangle,

	/// <summary>Two triangles meeting at their tips (<c>hourglass</c>, collate); draws no label.</summary>
	Hourglass,

	/// <summary>Lightning bolt (<c>bolt</c>, communication link); draws no label.</summary>
	Bolt,

	/// <summary>Text with a curly brace on its left (<c>brace</c>, comment).</summary>
	BraceLeft,

	/// <summary>Text with a curly brace on its right (<c>brace-r</c>).</summary>
	BraceRight,

	/// <summary>Text between curly braces (<c>braces</c>).</summary>
	Braces,

	/// <summary>Rectangle with a rounded right end (<c>delay</c>).</summary>
	Delay,

	/// <summary>Cylinder lying on its side (<c>h-cyl</c>, direct access storage).</summary>
	HorizontalCylinder,

	/// <summary>Cylinder with a second rim line (<c>lin-cyl</c>, disk storage).</summary>
	LinedCylinder,

	/// <summary>Pointed left end, rounded right end (<c>curv-trap</c>, display).</summary>
	CurvedTrapezoid,

	/// <summary>Rectangle with a band across its top (<c>div-rect</c>, divided process).</summary>
	DividedRectangle,

	/// <summary>Rectangle with a wavy bottom edge (<c>doc</c>, document).</summary>
	Document,

	/// <summary>Triangle pointing up (<c>tri</c>, extract).</summary>
	Triangle,

	/// <summary>Rectangle with lines along its top and left (<c>win-pane</c>, internal storage).</summary>
	WindowPane,

	/// <summary>Small filled circle (<c>f-circ</c>, junction); draws no label.</summary>
	FilledCircle,

	/// <summary>Document with a line down its left (<c>lin-doc</c>, lined document).</summary>
	LinedDocument,

	/// <summary>Rectangle with a line down its left (<c>lin-rect</c>, lined/shaded process).</summary>
	LinedRectangle,

	/// <summary>Rectangle with its top corners cut off (<c>notch-pent</c>, loop limit).</summary>
	NotchedPentagon,

	/// <summary>Triangle pointing down (<c>flip-tri</c>, manual file).</summary>
	FlippedTriangle,

	/// <summary>Rectangle whose top edge rises to the right (<c>sl-rect</c>, manual input).</summary>
	SlopedRectangle,

	/// <summary>Stacked documents (<c>docs</c>, multi-document).</summary>
	StackedDocument,

	/// <summary>Stacked rectangles (<c>st-rect</c>, multi-process).</summary>
	StackedRectangle,

	/// <summary>Rectangle with wavy top and bottom edges (<c>flag</c>, paper tape).</summary>
	Flag,

	/// <summary>Rectangle with concave left and convex right ends (<c>bow-rect</c>, stored data).</summary>
	BowTieRectangle,

	/// <summary>Circle with an X across it (<c>cross-circ</c>, summary); draws no label.</summary>
	CrossedCircle,

	/// <summary>Document with a folded bottom-right corner (<c>tag-doc</c>, tagged document).</summary>
	TaggedDocument,

	/// <summary>Rectangle with a folded bottom-right corner (<c>tag-rect</c>, tagged process).</summary>
	TaggedRectangle,

	/// <summary>Label only, no outline (<c>text</c>, text block).</summary>
	Text
}

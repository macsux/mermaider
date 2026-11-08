using System.Collections.Frozen;
using System.Globalization;
using System.Text.RegularExpressions;
using Mermaider.Models;
using Mermaider.Text;

namespace Mermaider.Parsing;

internal static partial class FlowchartParser
{
	private const int TimeoutMs = 2000;

	// ========================================================================
	// Generated regex patterns
	// ========================================================================

	[GeneratedRegex(@"^(?:graph|flowchart)\s+(TD|TB|LR|BT|RL)\s*$", RegexOptions.IgnoreCase, TimeoutMs)]
	private static partial Regex HeaderPattern();

	[GeneratedRegex(@"^classDef\s+(\w+)\s+(.+)$", RegexOptions.None, TimeoutMs)]
	private static partial Regex ClassDefPattern();

	[GeneratedRegex(@"^class\s+([\w,-]+)\s+(\w+)$", RegexOptions.None, TimeoutMs)]
	private static partial Regex ClassAssignPattern();

	[GeneratedRegex(@"^style\s+([\w,-]+)\s+(.+)$", RegexOptions.None, TimeoutMs)]
	private static partial Regex StylePattern();

	[GeneratedRegex(@"^linkStyle\s+(default|[\d,\s]+)\s+(.+)$", RegexOptions.None, TimeoutMs)]
	private static partial Regex LinkStylePattern();

	[GeneratedRegex(@"^direction\s+(TD|TB|LR|BT|RL)\s*$", RegexOptions.IgnoreCase, TimeoutMs)]
	private static partial Regex DirectionPattern();

	[GeneratedRegex(@"^subgraph\s+(.+)$", RegexOptions.None, TimeoutMs)]
	private static partial Regex SubgraphPattern();

	[GeneratedRegex(@"^([\w-]+)\s*\[(.+)\]$", RegexOptions.None, TimeoutMs)]
	private static partial Regex SubgraphBracketPattern();

	[GeneratedRegex(@"^(<)?(-{2,}>|-{3,}|-\.+->|-\.+-|={2,}>|={3,}|~{3,})(?:\|([^|]*)\|)?", RegexOptions.None, TimeoutMs)]
	private static partial Regex ArrowPattern();

	[GeneratedRegex(@"^(<)?(-{2}|={2}|-\.)\s+(.+?)\s+(-{2}>|={2}>|\.->)", RegexOptions.None, TimeoutMs)]
	private static partial Regex InlineTextArrowPattern();

	[GeneratedRegex(@"^:::([\w][\w-]*)", RegexOptions.None, TimeoutMs)]
	private static partial Regex ClassShorthandPattern();

	[GeneratedRegex(@"^([\w-]+)\(\(\((.+?)\)\)\)", RegexOptions.None, TimeoutMs)]
	private static partial Regex DoubleCirclePattern();

	[GeneratedRegex(@"^([\w-]+)\(\[(.+?)\]\)", RegexOptions.None, TimeoutMs)]
	private static partial Regex StadiumPattern();

	[GeneratedRegex(@"^([\w-]+)\(\((.+?)\)\)", RegexOptions.None, TimeoutMs)]
	private static partial Regex CirclePattern();

	[GeneratedRegex(@"^([\w-]+)\[\[(.+?)\]\]", RegexOptions.None, TimeoutMs)]
	private static partial Regex SubroutinePattern();

	[GeneratedRegex(@"^([\w-]+)\[\((.+?)\)\]", RegexOptions.None, TimeoutMs)]
	private static partial Regex CylinderPattern();

	[GeneratedRegex(@"^([\w-]+)\[/(.+?)\\\]", RegexOptions.None, TimeoutMs)]
	private static partial Regex TrapezoidPattern();

	[GeneratedRegex(@"^([\w-]+)\[/(.+?)/\]", RegexOptions.None, TimeoutMs)]
	private static partial Regex ParallelogramPattern();

	[GeneratedRegex(@"^([\w-]+)\[\\(.+?)\\\]", RegexOptions.None, TimeoutMs)]
	private static partial Regex ParallelogramAltPattern();

	[GeneratedRegex(@"^([\w-]+)\[\\(.+?)/\]", RegexOptions.None, TimeoutMs)]
	private static partial Regex TrapezoidAltPattern();

	[GeneratedRegex(@"^([\w-]+)>(.+?)\]", RegexOptions.None, TimeoutMs)]
	private static partial Regex AsymmetricPattern();

	[GeneratedRegex(@"^([\w-]+)\{\{(.+?)\}\}", RegexOptions.None, TimeoutMs)]
	private static partial Regex HexagonPattern();

	[GeneratedRegex(@"^([\w-]+)\[(.+?)\]", RegexOptions.Singleline, TimeoutMs)]
	private static partial Regex RectanglePattern();

	[GeneratedRegex(@"^([\w-]+)\((.+?)\)", RegexOptions.None, TimeoutMs)]
	private static partial Regex RoundedPattern();

	[GeneratedRegex(@"^([\w-]+)\{(.+?)\}", RegexOptions.None, TimeoutMs)]
	private static partial Regex DiamondPattern();

	[GeneratedRegex(@"^([\w]+(?:-[\w]+)*)", RegexOptions.None, TimeoutMs)]
	private static partial Regex BareNodePattern();

	private static readonly (Func<Regex> Pattern, NodeShape Shape)[] NodePatterns =
	[
		(DoubleCirclePattern, NodeShape.DoubleCircle),
		(StadiumPattern, NodeShape.Stadium),
		(CirclePattern, NodeShape.Circle),
		(SubroutinePattern, NodeShape.Subroutine),
		(CylinderPattern, NodeShape.Cylinder),
		(ParallelogramPattern, NodeShape.Parallelogram),
		(ParallelogramAltPattern, NodeShape.ParallelogramAlt),
		(TrapezoidPattern, NodeShape.Trapezoid),
		(TrapezoidAltPattern, NodeShape.TrapezoidAlt),
		(AsymmetricPattern, NodeShape.Asymmetric),
		(HexagonPattern, NodeShape.Hexagon),
		(RectanglePattern, NodeShape.Rectangle),
		(RoundedPattern, NodeShape.Rounded),
		(DiamondPattern, NodeShape.Diamond),
	];

	// ========================================================================
	// Parse entry point
	// ========================================================================

	internal static MermaidGraph Parse(string[] lines)
	{
		try
		{
			return ParseCore(JoinMultilineLabels(lines));
		}
		catch (RegexMatchTimeoutException ex)
		{
			throw new MermaidParseException(
				$"Parsing timed out after {ex.MatchTimeout.TotalSeconds}s — input may contain pathological patterns.",
				ex);
		}
	}

	private static string[] JoinMultilineLabels(string[] lines)
	{
		var result = new List<string>(lines.Length);
		for (var i = 0; i < lines.Length; i++)
		{
			var line = lines[i];
			if (OpensShapeData(line))
			{
				// expanded node syntax spread over lines: A@{\n  icon: "x",\n  label: "y"\n}
				while (++i < lines.Length)
				{
					line = line + " " + lines[i].Trim();
					if (!OpensShapeData(line))
						break;
				}
			}
			else if (line.Contains("[\"") && !line.Contains("\"]"))
			{
				while (++i < lines.Length)
				{
					line = line + "\n" + lines[i];
					if (lines[i].Contains("\"]"))
						break;
				}
			}
			result.Add(line);
		}
		return result.ToArray();
	}

	private static MermaidGraph ParseCore(string[] lines)
	{
		var headerMatch = HeaderPattern().Match(lines[0]);
		if (!headerMatch.Success)
			throw new MermaidParseException($"Invalid mermaid header: \"{lines[0]}\". Expected \"graph TD\", \"flowchart LR\", etc.");

		var direction = Enum.Parse<Direction>(headerMatch.Groups[1].Value.ToUpperInvariant());

		var nodes = new Dictionary<string, MermaidNode>();
		var edges = new List<MermaidEdge>();
		var subgraphs = new List<MermaidSubgraph>();
		var classDefs = new Dictionary<string, IReadOnlyDictionary<string, string>>();
		var classAssignments = new Dictionary<string, string>();
		var nodeStyles = new Dictionary<string, Dictionary<string, string>>();
		var edgeStyles = new Dictionary<int, Dictionary<string, string>>();
		IReadOnlyDictionary<string, string>? defaultEdgeStyle = null;
		var subgraphStack = new Stack<(string Id, string Label, List<string> NodeIds, List<MermaidSubgraph> Children, Direction? Dir)>();

		for (var i = 1; i < lines.Length; i++)
		{
			var line = lines[i];

			var classDefMatch = ClassDefPattern().Match(line);
			if (classDefMatch.Success)
			{
				var name = classDefMatch.Groups[1].Value;
				var propsStr = classDefMatch.Groups[2].Value;
				classDefs[name] = ParseStyleProps(propsStr);
				continue;
			}

			var classAssignMatch = ClassAssignPattern().Match(line);
			if (classAssignMatch.Success)
			{
				var nodeIds = classAssignMatch.Groups[1].Value.Split(',', StringSplitOptions.TrimEntries);
				var className = classAssignMatch.Groups[2].Value;
				foreach (var id in nodeIds)
					classAssignments[id] = className;
				continue;
			}

			var styleMatch = StylePattern().Match(line);
			if (styleMatch.Success)
			{
				var nodeIds = styleMatch.Groups[1].Value.Split(',', StringSplitOptions.TrimEntries);
				var props = ParseStyleProps(styleMatch.Groups[2].Value);
				foreach (var id in nodeIds)
				{
					if (!nodeStyles.TryGetValue(id, out var existing))
					{
						existing = [];
						nodeStyles[id] = existing;
					}
					foreach (var kvp in props)
						existing[kvp.Key] = kvp.Value;
				}
				continue;
			}

			var linkStyleMatch = LinkStylePattern().Match(line);
			if (linkStyleMatch.Success)
			{
				var indexPart = linkStyleMatch.Groups[1].Value.Trim();
				var props = ParseStyleProps(linkStyleMatch.Groups[2].Value);
				if (indexPart.Equals("default", StringComparison.OrdinalIgnoreCase))
				{
					defaultEdgeStyle = props;
				}
				else
				{
					var indices = indexPart.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
					foreach (var idxStr in indices)
					{
						if (int.TryParse(idxStr, out var idx))
						{
							if (!edgeStyles.TryGetValue(idx, out var existing))
							{
								existing = [];
								edgeStyles[idx] = existing;
							}
							foreach (var kvp in props)
								existing[kvp.Key] = kvp.Value;
						}
					}
				}
				continue;
			}

			var dirMatch = DirectionPattern().Match(line);
			if (dirMatch.Success && subgraphStack.Count > 0)
			{
				var current = subgraphStack.Pop();
				current.Dir = Enum.Parse<Direction>(dirMatch.Groups[1].Value.ToUpperInvariant());
				subgraphStack.Push(current);
				continue;
			}

			var subgraphMatch = SubgraphPattern().Match(line);
			if (subgraphMatch.Success)
			{
				var rest = subgraphMatch.Groups[1].Value.Trim();
				var bracketMatch = SubgraphBracketPattern().Match(rest);
				string id, label;
				if (bracketMatch.Success)
				{
					id = bracketMatch.Groups[1].Value;
					label = MultilineUtils.NormalizeBrTags(bracketMatch.Groups[2].Value);
				}
				else
				{
					label = MultilineUtils.NormalizeBrTags(rest);
					id = Regex.Replace(rest.Replace(' ', '_'), @"[^\w]", "", RegexOptions.None, TimeSpan.FromSeconds(2));
				}
				subgraphStack.Push((id, label, [], [], null));
				continue;
			}

			if (line == "end")
			{
				if (subgraphStack.Count > 0)
				{
					var completed = subgraphStack.Pop();
					var sg = new MermaidSubgraph
					{
						Id = completed.Id,
						Label = completed.Label,
						NodeIds = completed.NodeIds,
						Children = completed.Children,
						Direction = completed.Dir
					};
					if (subgraphStack.Count > 0)
					{
						var parent = subgraphStack.Pop();
						parent.Children.Add(sg);
						subgraphStack.Push(parent);
					}
					else
					{
						subgraphs.Add(sg);
					}
				}
				continue;
			}

			ParseEdgeLine(line, nodes, edges, classAssignments, subgraphStack);
		}

		var subgraphMap = new Dictionary<string, MermaidSubgraph>();
		CollectSubgraphMap(subgraphs, subgraphMap);

		var redirections = new Dictionary<int, (string? SourceSg, string? TargetSg)>();
		for (var e = 0; e < edges.Count; e++)
		{
			var edge = edges[e];
			var src = ResolveSubgraphRef(edge.Source, subgraphMap);
			var tgt = ResolveSubgraphRef(edge.Target, subgraphMap);
			if (src != edge.Source || tgt != edge.Target)
			{
				redirections[e] = (
					src != edge.Source ? edge.Source : null,
					tgt != edge.Target ? edge.Target : null);
				edges[e] = new MermaidEdge(src, tgt, edge.Label, edge.Style, edge.HasArrowStart, edge.HasArrowEnd, edge.MinLength);
			}
		}

		foreach (var (sgId, sgDef) in subgraphMap)
		{
			// An empty subgraph has nothing to redirect an edge to, so the implicit node stays as the endpoint
			if (sgDef.NodeIds.Count == 0 && sgDef.Children.Count == 0)
				continue;
			if (nodes.TryGetValue(sgId, out var n) && n.Label == sgId && n.Shape == NodeShape.Rectangle)
				_ = nodes.Remove(sgId);
		}

		if (classDefs.ContainsKey("default"))
		{
			foreach (var nodeId in nodes.Keys)
			{
				_ = classAssignments.TryAdd(nodeId, "default");
			}
		}

		return new MermaidGraph
		{
			IsFlowchart = true,
			Direction = direction,
			Nodes = nodes,
			NodeOrder = BuildSubgraphFirstNodeOrder(nodes, subgraphMap),
			Edges = edges,
			Subgraphs = subgraphs,
			ClassDefs = classDefs,
			ClassAssignments = classAssignments,
			NodeStyles = nodeStyles.ToDictionary(
				kvp => kvp.Key,
				kvp => (IReadOnlyDictionary<string, string>)kvp.Value),
			EdgeStyles = edgeStyles.ToDictionary(
				kvp => kvp.Key,
				kvp => (IReadOnlyDictionary<string, string>)kvp.Value),
			DefaultEdgeStyle = defaultEdgeStyle,
			SubgraphEdgeRedirections = redirections
		};
	}

	// Subgraph members come first (alphabetical by subgraph ID), matching dagre's
	// node-insertion order so the DFS cycle-breaker picks the same back-edges.
	private static List<string> BuildSubgraphFirstNodeOrder(
		Dictionary<string, MermaidNode> nodes,
		Dictionary<string, MermaidSubgraph> subgraphMap)
	{
		var order = new List<string>(nodes.Count);
		var seen = new HashSet<string>(StringComparer.Ordinal);

		foreach (var sg in subgraphMap.Values.OrderBy(sg => sg.Id, StringComparer.Ordinal))
		{
			foreach (var nodeId in sg.NodeIds)
			{
				if (nodes.ContainsKey(nodeId) && seen.Add(nodeId))
					order.Add(nodeId);
			}
		}

		foreach (var nodeId in nodes.Keys)
		{
			if (seen.Add(nodeId))
				order.Add(nodeId);
		}

		return order;
	}

	// ========================================================================
	// Edge line parsing
	// ========================================================================

	private static void ParseEdgeLine(
		string line,
		Dictionary<string, MermaidNode> nodes,
		List<MermaidEdge> edges,
		Dictionary<string, string> classAssignments,
		Stack<(string Id, string Label, List<string> NodeIds, List<MermaidSubgraph> Children, Direction? Dir)> subgraphStack)
	{
		var remaining = line.Trim();

		var (firstIds, rest) = ConsumeNodeGroup(remaining, nodes, classAssignments, subgraphStack);
		if (firstIds.Count == 0)
			return;

		remaining = rest.Trim();
		var prevGroupIds = firstIds;

		while (remaining.Length > 0)
		{
			bool hasArrowStart;
			string arrowOp;
			string? edgeLabel;
			int consumedLength;

			var inlineMatch = InlineTextArrowPattern().Match(remaining);
			if (inlineMatch.Success)
			{
				hasArrowStart = inlineMatch.Groups[1].Success;
				arrowOp = inlineMatch.Groups[4].Value;
				var rawLabel = inlineMatch.Groups[3].Value.Trim();
				edgeLabel = rawLabel.Length > 0 ? MultilineUtils.NormalizeBrTags(rawLabel) : null;
				consumedLength = inlineMatch.Length;
			}
			else
			{
				var arrowMatch = ArrowPattern().Match(remaining);
				if (!arrowMatch.Success)
					break;

				hasArrowStart = arrowMatch.Groups[1].Success;
				arrowOp = arrowMatch.Groups[2].Value;
				var rawLabel = arrowMatch.Groups[3].Success ? arrowMatch.Groups[3].Value.Trim() : null;
				edgeLabel = rawLabel is { Length: > 0 } ? MultilineUtils.NormalizeBrTags(rawLabel) : null;
				consumedLength = arrowMatch.Length;
			}

			remaining = remaining[consumedLength..].Trim();

			var style = ArrowStyleFromOp(arrowOp);
			var hasArrowEnd = arrowOp.EndsWith('>');
			var minLength = 1; // mermaid.js v12: extra dashes (----> vs -->) are visual-only, not layout rank

			var (nextIds, nextRest) = ConsumeNodeGroup(remaining, nodes, classAssignments, subgraphStack);
			if (nextIds.Count == 0)
				break;

			remaining = nextRest.Trim();

			foreach (var sourceId in prevGroupIds)
			{
				foreach (var targetId in nextIds)
				{
					edges.Add(new MermaidEdge(sourceId, targetId, edgeLabel, style, hasArrowStart, hasArrowEnd, minLength));
				}
			}

			prevGroupIds = nextIds;
		}
	}

	private static (List<string> Ids, string Remaining) ConsumeNodeGroup(
		string text,
		Dictionary<string, MermaidNode> nodes,
		Dictionary<string, string> classAssignments,
		Stack<(string Id, string Label, List<string> NodeIds, List<MermaidSubgraph> Children, Direction? Dir)> subgraphStack)
	{
		var (firstId, rest) = ConsumeNode(text, nodes, classAssignments, subgraphStack);
		if (firstId is null)
			return ([], text);

		var ids = new List<string> { firstId };
		var remaining = rest.Trim();

		while (remaining.StartsWith('&'))
		{
			remaining = remaining[1..].Trim();
			var (nextId, nextRest) = ConsumeNode(remaining, nodes, classAssignments, subgraphStack);
			if (nextId is null)
				break;
			ids.Add(nextId);
			remaining = nextRest.Trim();
		}

		return (ids, remaining);
	}

	private static (string? Id, string Remaining) ConsumeNode(
		string text,
		Dictionary<string, MermaidNode> nodes,
		Dictionary<string, string> classAssignments,
		Stack<(string Id, string Label, List<string> NodeIds, List<MermaidSubgraph> Children, Direction? Dir)> subgraphStack)
	{
		string? id = null;
		var remaining = text;

		foreach (var (patternFunc, shape) in NodePatterns)
		{
			var match = patternFunc().Match(text);
			if (match.Success)
			{
				id = match.Groups[1].Value;
				var rawLabel = match.Groups[2].Value;
				if (rawLabel.Length >= 2 && rawLabel[0] == '"' && rawLabel[^1] == '"')
					rawLabel = rawLabel[1..^1];
				var isMarkdown = rawLabel.StartsWith('`') && rawLabel.EndsWith('`');
				var label = isMarkdown
					? MultilineUtils.DecodeEntities(rawLabel[1..^1].Trim().Replace("\\n", "\n"))
					: MultilineUtils.NormalizeBrTags(rawLabel).Replace("\\n", "\n");
				RegisterNode(nodes, subgraphStack, new MermaidNode(id, label, shape, isMarkdown));
				remaining = text[match.Length..];
				break;
			}
		}

		if (id is null)
		{
			var bareMatch = BareNodePattern().Match(text);
			if (bareMatch.Success)
			{
				id = bareMatch.Groups[1].Value;
				if (nodes.ContainsKey(id))
					AddToCurrentSubgraph(subgraphStack, id);
				else
					RegisterNode(nodes, subgraphStack, new MermaidNode(id, id, NodeShape.Rectangle));
				remaining = text[bareMatch.Length..];
			}
		}

		if (id is null)
			return (null, text);

		if (remaining.StartsWith("@{", StringComparison.Ordinal) && TryReadShapeData(remaining, out var data, out var consumed))
		{
			ApplyShapeData(nodes, id, data);
			remaining = remaining[consumed..];
		}

		var classMatch = ClassShorthandPattern().Match(remaining);
		if (classMatch.Success)
		{
			classAssignments[id] = classMatch.Groups[1].Value;
			remaining = remaining[classMatch.Length..];
		}

		return (id, remaining);
	}

	// ========================================================================
	// Expanded node syntax: A@{ key: value, ... }
	// ========================================================================

	/// <summary>True when <paramref name="line"/> has an <c>@{</c> whose closing brace is not on it.</summary>
	private static bool OpensShapeData(string line)
	{
		var at = line.LastIndexOf("@{", StringComparison.Ordinal);
		return at >= 0 && !TryReadShapeData(line[at..], out _, out _);
	}

	/// <summary>
	/// Reads the <c>@{ ... }</c> block at the start of <paramref name="text"/> into its key/value pairs (keys lower-case,
	/// quotes stripped). Commas and braces inside quoted values don't end a pair or the block.
	/// </summary>
	private static bool TryReadShapeData(string text, out Dictionary<string, string> data, out int consumed)
	{
		data = new Dictionary<string, string>(StringComparer.Ordinal);
		consumed = 0;
		var pairStart = 2;
		var quote = '\0';
		for (var i = 2; i < text.Length; i++)
		{
			var c = text[i];
			if (quote != '\0')
			{
				if (c == quote)
					quote = '\0';
				continue;
			}
			switch (c)
			{
				case '"' or '\'':
					quote = c;
					break;
				case ',':
					AddShapeDataPair(data, text.AsSpan(pairStart, i - pairStart));
					pairStart = i + 1;
					break;
				case '}':
					AddShapeDataPair(data, text.AsSpan(pairStart, i - pairStart));
					consumed = i + 1;
					return true;
			}
		}
		return false;
	}

	private static void AddShapeDataPair(Dictionary<string, string> data, ReadOnlySpan<char> pair)
	{
		var colon = pair.IndexOf(':');
		if (colon <= 0)
			return;
		var key = pair[..colon].Trim().ToString().ToLowerInvariant();
		var value = pair[(colon + 1)..].Trim();
		if (value.Length >= 2 && value[0] is '"' or '\'' && value[^1] == value[0])
			value = value[1..^1];
		if (key.Length > 0)
			data[key] = value.ToString();
	}

	/// <summary>
	/// Mermaid's expanded shape names (short names and aliases, from Mermaid's shape table) and the shape each one draws.
	/// </summary>
	private static readonly FrozenDictionary<string, NodeShape> ShapeNames = new (NodeShape Shape, string[] Names)[]
	{
		(NodeShape.Rectangle, ["rect", "proc", "process", "rectangle"]),
		(NodeShape.Rounded, ["rounded", "event"]),
		(NodeShape.Stadium, ["stadium", "terminal", "pill"]),
		(NodeShape.Subroutine, ["fr-rect", "subprocess", "subproc", "framed-rectangle", "subroutine"]),
		(NodeShape.Cylinder, ["cyl", "db", "database", "cylinder"]),
		(NodeShape.Circle, ["circle", "circ"]),
		(NodeShape.Diamond, ["diam", "decision", "diamond", "question"]),
		(NodeShape.Hexagon, ["hex", "hexagon", "prepare"]),
		(NodeShape.Parallelogram, ["lean-r", "lean-right", "in-out"]),
		(NodeShape.ParallelogramAlt, ["lean-l", "lean-left", "out-in"]),
		(NodeShape.Trapezoid, ["trap-b", "priority", "trapezoid-bottom", "trapezoid"]),
		(NodeShape.TrapezoidAlt, ["trap-t", "manual", "trapezoid-top", "inv-trapezoid"]),
		(NodeShape.DoubleCircle, ["dbl-circ", "double-circle"]),
		(NodeShape.Text, ["text"]),
		(NodeShape.NotchedRectangle, ["notch-rect", "card", "notched-rectangle"]),
		(NodeShape.LinedRectangle, ["lin-rect", "lined-rectangle", "lined-process", "lin-proc", "shaded-process"]),
		(NodeShape.StateStart, ["sm-circ", "start", "small-circle"]),
		(NodeShape.StateEnd, ["fr-circ", "stop", "framed-circle"]),
		(NodeShape.ForkJoin, ["fork", "join"]),
		(NodeShape.Hourglass, ["hourglass", "collate"]),
		(NodeShape.BraceLeft, ["brace", "comment", "brace-l"]),
		(NodeShape.BraceRight, ["brace-r"]),
		(NodeShape.Braces, ["braces"]),
		(NodeShape.Bolt, ["bolt", "com-link", "lightning-bolt"]),
		(NodeShape.Document, ["doc", "document"]),
		(NodeShape.Delay, ["delay", "half-rounded-rectangle"]),
		(NodeShape.HorizontalCylinder, ["h-cyl", "das", "horizontal-cylinder"]),
		(NodeShape.LinedCylinder, ["lin-cyl", "disk", "lined-cylinder"]),
		(NodeShape.CurvedTrapezoid, ["curv-trap", "curved-trapezoid", "display"]),
		(NodeShape.DividedRectangle, ["div-rect", "div-proc", "divided-rectangle", "divided-process"]),
		(NodeShape.Triangle, ["tri", "extract", "triangle"]),
		(NodeShape.WindowPane, ["win-pane", "internal-storage", "window-pane"]),
		(NodeShape.FilledCircle, ["f-circ", "junction", "filled-circle"]),
		(NodeShape.NotchedPentagon, ["notch-pent", "loop-limit", "notched-pentagon"]),
		(NodeShape.FlippedTriangle, ["flip-tri", "manual-file", "flipped-triangle"]),
		(NodeShape.SlopedRectangle, ["sl-rect", "manual-input", "sloped-rectangle"]),
		(NodeShape.StackedDocument, ["docs", "documents", "st-doc", "stacked-document"]),
		(NodeShape.StackedRectangle, ["st-rect", "procs", "processes", "stacked-rectangle"]),
		(NodeShape.BowTieRectangle, ["bow-rect", "stored-data", "bow-tie-rectangle"]),
		(NodeShape.CrossedCircle, ["cross-circ", "summary", "crossed-circle"]),
		(NodeShape.TaggedDocument, ["tag-doc", "tagged-document"]),
		(NodeShape.TaggedRectangle, ["tag-rect", "tagged-rectangle", "tag-proc", "tagged-process"]),
		(NodeShape.Flag, ["flag", "paper-tape"]),
		(NodeShape.Asymmetric, ["odd"]),
		(NodeShape.LinedDocument, ["lin-doc", "lined-document"]),
	}.SelectMany(entry => entry.Names.Select(name => (name, entry.Shape)))
		.ToFrozenDictionary(pair => pair.name, pair => pair.Shape, StringComparer.OrdinalIgnoreCase);

	/// <summary>Shapes Mermaid draws without their label (it clears the label of these).</summary>
	private static bool DrawsNoLabel(NodeShape shape) =>
		shape is NodeShape.StateStart or NodeShape.StateEnd or NodeShape.ForkJoin or NodeShape.FilledCircle
			or NodeShape.Hourglass or NodeShape.Bolt or NodeShape.CrossedCircle;

	/// <summary>
	/// Applies an <c>@{ ... }</c> block to node <paramref name="id"/> (already registered by the caller). An <c>icon</c> key
	/// makes it an icon-shape node; otherwise a known <c>shape</c> name sets its shape and <c>label</c> its label. An
	/// unknown shape name keeps the shape the node already has.
	/// </summary>
	private static void ApplyShapeData(Dictionary<string, MermaidNode> nodes, string id, Dictionary<string, string> data)
	{
		var node = nodes[id];
		var label = data.TryGetValue("label", out var rawLabel)
			? MultilineUtils.NormalizeBrTags(rawLabel).Replace("\\n", "\n")
			: null;

		if (data.TryGetValue("icon", out var iconName) && iconName.Length > 0)
		{
			var form = data.GetValueOrDefault("form")?.ToLowerInvariant() switch
			{
				"square" => IconForm.Square,
				"circle" => IconForm.Circle,
				"rounded" => IconForm.Rounded,
				_ => IconForm.None,
			};
			var position = data.GetValueOrDefault("pos") is { } pos && pos.Equals("t", StringComparison.OrdinalIgnoreCase)
				? IconLabelPosition.Top
				: IconLabelPosition.Bottom;
			var height = data.TryGetValue("h", out var h) && double.TryParse(h, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
				? Math.Max(parsed, NodeIcon.DefaultHeight)
				: NodeIcon.DefaultHeight;
			// as Mermaid: an icon without a label shows no label
			nodes[id] = new MermaidNode(id, label ?? "", NodeShape.Icon, Icon: new NodeIcon(iconName, form, position, height));
		}
		else
		{
			if (data.TryGetValue("shape", out var shapeName) && ShapeNames.TryGetValue(shapeName.Trim(), out var shape))
				node = node with { Shape = shape };
			if (label is not null)
				node = node with { Label = label };
			if (DrawsNoLabel(node.Shape))
				node = node with { Label = "" };
			nodes[id] = node;
		}
	}

	// ========================================================================
	// Helpers
	// ========================================================================

	private static void RegisterNode(
		Dictionary<string, MermaidNode> nodes,
		Stack<(string Id, string Label, List<string> NodeIds, List<MermaidSubgraph> Children, Direction? Dir)> subgraphStack,
		MermaidNode node)
	{
		_ = nodes.TryAdd(node.Id, node);
		AddToCurrentSubgraph(subgraphStack, node.Id);
	}

	private static void AddToCurrentSubgraph(
		Stack<(string Id, string Label, List<string> NodeIds, List<MermaidSubgraph> Children, Direction? Dir)> subgraphStack,
		string nodeId)
	{
		if (subgraphStack.Count > 0)
		{
			var current = subgraphStack.Peek();
			if (!current.NodeIds.Contains(nodeId))
				current.NodeIds.Add(nodeId);
		}
	}

	private static EdgeStyle ArrowStyleFromOp(string op)
	{
		if (op.Contains('~'))
			return EdgeStyle.Invisible;
		if (op.Contains('.'))
			return EdgeStyle.Dotted;
		if (op.Contains('='))
			return EdgeStyle.Thick;
		return EdgeStyle.Solid;
	}


	private static void CollectSubgraphMap(IReadOnlyList<MermaidSubgraph> sgs, Dictionary<string, MermaidSubgraph> map)
	{
		foreach (var sg in sgs)
		{
			map[sg.Id] = sg;
			CollectSubgraphMap(sg.Children, map);
		}
	}

	private static string ResolveSubgraphRef(string id, Dictionary<string, MermaidSubgraph> sgMap)
	{
		while (sgMap.TryGetValue(id, out var sg))
		{
			string? firstNonSg = null;
			foreach (var child in sg.NodeIds)
			{
				if (!sgMap.ContainsKey(child))
				{ firstNonSg = child; break; }
			}
			if (firstNonSg != null)
				return firstNonSg;
			if (sg.NodeIds.Count > 0)
				id = sg.NodeIds[0];
			else if (sg.Children.Count > 0)
				id = sg.Children[0].Id;
			else
				break;
		}
		return id;
	}

	private static IReadOnlyDictionary<string, string> ParseStyleProps(string propsStr)
	{
		var props = new Dictionary<string, string>();
		foreach (var pair in propsStr.Split(','))
		{
			var colonIdx = pair.IndexOf(':');
			if (colonIdx <= 0)
				continue;
			var key = pair[..colonIdx].Trim();
			var val = pair[(colonIdx + 1)..].Trim();
			if (key.Length > 0 && val.Length > 0)
				props[key] = val;
		}
		return props;
	}

}

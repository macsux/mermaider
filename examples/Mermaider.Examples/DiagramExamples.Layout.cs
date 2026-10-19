namespace Mermaider.Examples;

public static partial class DiagramExamples
{
	private static DiagramExample[] CreateLayoutExamples() =>
	[
		// ── Layout cases (back edges, wrapped labels, labels sharing a corridor) ──

		new("layout-lr-back-edge-into-subgraph", "Back Edge into a Subgraph (LR)", DiagramCategory.Flowchart, """
			flowchart LR
			    subgraph Controllers
			        GHC["GitHub controller<br/>(built-in, batched polling)"]
			        FAC["FrigginAI controller<br/>(streams, diff comments, questions)"]
			        PLC["Agent-written controller<br/>(plugin, own ALC, own NuGet)"]
			    end

			    subgraph Store["Resource store"]
			        RES["Resource<br/>kind + spec + status + version"]
			    end

			    subgraph Obs["Observations"]
			        OB["Observation<br/>observer, resource, JSONPath,<br/>policy, delivered-result hash"]
			    end

			    EVAL{{"Status changed:<br/>re-evaluate each observation's JSONPath.<br/>Hash differs from delivered hash?"}}
			    PEND["Observation pending"]
			    WRITE["Write one message into the stream's chat<br/>(policy wake)"]
			    NOTE["Show it in Needs you<br/>(policy notify)"]
			    DONE["Delivered: delivered hash = result hash"]

			    GHC & FAC & PLC -->|write status| RES
			    RES --> EVAL
			    OB --> EVAL
			    EVAL -->|yes| PEND
			    PEND -->|stream idle| WRITE
			    PEND -->|policy notify| NOTE
			    WRITE --> DONE
			    NOTE --> DONE
			    RES -. "controllers reconcile only resources with observations" .-> Controllers
			""", "back edge into a subgraph"),

		new("layout-lr-parallel-labels", "Labelled Edges Both Ways (LR)", DiagramCategory.Flowchart, """
			flowchart LR
			    A[Client] -->|request token| B[Auth server]
			    A -->|refresh token| B
			    A -->|revoke| B
			    B -->|token issued to the client| A
			    B --> C[Audit log]
			""", "parallel labelled edges"),

		new("layout-rl-back-edge", "Back Edge (RL)", DiagramCategory.Flowchart, """
			flowchart RL
			    subgraph Ingest
			        I1[Reader] --> I2[Parser]
			    end
			    I2 -->|rows| S[(Store)]
			    S --> R[Report]
			    R -. "reprocess failed rows" .-> Ingest
			""", "back edge"),

		new("layout-bt-back-edge", "Back Edge (BT)", DiagramCategory.Flowchart, """
			flowchart BT
			    A[Start] --> B[Middle step]
			    B --> C[Finish]
			    C -->|retry from the start| A
			""", "back edge"),

		new("layout-sibling-widths", "Siblings of Different Widths", DiagramCategory.Flowchart, """
			flowchart LR
			    subgraph Sources
			        A[Short one]
			        B[The middle source has a far longer label than the others]
			        C[Short two]
			    end
			    A -->|sends| R[Receiver]
			    B -->|sends| R
			    C -->|sends| R
			""", "edges past a wider sibling"),

		new("layout-state-long-labels", "Long Transition Labels", DiagramCategory.State, """
			stateDiagram-v2
			    [*] --> UpToDate: registered, baseline empty
			    UpToDate --> Pending: status changed and result hash differs from delivered hash
			    Pending --> UpToDate: status changed back before delivery
			    Pending --> Held: (open) user active in the stream, unanswered question, or stream blocked
			    Held --> Pending: hold lifts
			    Pending --> Pending: stream mid-turn, more changes arrive (still one message later)
			    Pending --> UpToDate: message written into the chat, or shown in Needs you (delivered hash updated)
			    UpToDate --> Ended: observer releases it
			    Pending --> Ended: observer delivered, archived or abandoned (FrigginAI ends it)
			    UpToDate --> Ended: resource deleted
			    Ended --> [*]
			""", "wrapped labels"),
	];
}

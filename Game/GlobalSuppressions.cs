// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Performance", "HAA0101:Array allocation for params parameter", Justification = "Gotta use EmitSignal", Scope = "member", Target = "~M:ThousandYearsHome.Controls.Dialogue.DialogueEngine.ProcessTextPayload(ThousandYearsHome.Controls.Dialogue.DialogueTextPayload)")]
[assembly: SuppressMessage("Performance", "HAA0101:Array allocation for params parameter", Justification = "Gotta use EmitSignal", Scope = "member", Target = "~M:ThousandYearsHome.Controls.Dialogue.DialogueEngine.SetState(ThousandYearsHome.Controls.Dialogue.DialogueEngineState)")]
[assembly: SuppressMessage("Performance", "HAA0101:Array allocation for params parameter", Justification = "Gotta use EmitSignal", Scope = "member", Target = "~M:ThousandYearsHome.Controls.Dialogue.DialogueEngine.ProcessBreakPayload(ThousandYearsHome.Controls.Dialogue.DialogueBreakPayload)")]
[assembly: SuppressMessage("Performance", "HAA0601:Value type to reference type conversion causing boxing allocation", Justification = "Gotta use EmitSignal", Scope = "member", Target = "~M:ThousandYearsHome.Controls.Dialogue.DialogueEngine.SetState(ThousandYearsHome.Controls.Dialogue.DialogueEngineState)")]
[assembly: SuppressMessage("Performance", "HAA0401:Possible allocation of reference type enumerator", Justification = "No other API available", Scope = "member", Target = "~M:ThousandYearsHome.Controls.Dialogue.DialogueParser.SegmentText(System.String)~System.Collections.Generic.IEnumerable{ThousandYearsHome.Controls.Dialogue.DialogueSegment}")]
[assembly: SuppressMessage("Performance", "HAA0401:Possible allocation of reference type enumerator", Justification = "No other API available", Scope = "member", Target = "~M:ThousandYearsHome.Controls.Dialogue.DialogueEngine.AddToLabel(System.String,System.Single)~System.Boolean")]
[assembly: SuppressMessage("Performance", "HAA0601:Value type to reference type conversion causing boxing allocation", Justification = "Debugger-only code", Scope = "member", Target = "~P:ThousandYearsHome.Controls.Dialogue.DialogueSegment.DebuggerDisplay")]

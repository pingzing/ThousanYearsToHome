// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Performance", "HAA0101:Array allocation for params parameter", Justification = "Gotta use EmitSignal", Scope = "member", Target = "~M:ThousandYearsHome.Controls.Dialogue.DialogueBox.ProcessTextPayload(ThousandYearsHome.Controls.Dialogue.DialogueTextPayload)")]
[assembly: SuppressMessage("Performance", "HAA0101:Array allocation for params parameter", Justification = "Gotta use EmitSignal", Scope = "member", Target = "~M:ThousandYearsHome.Controls.Dialogue.DialogueBox.SetState(ThousandYearsHome.Controls.Dialogue.DialogueEngineState)")]
[assembly: SuppressMessage("Performance", "HAA0101:Array allocation for params parameter", Justification = "Gotta use EmitSignal", Scope = "member", Target = "~M:ThousandYearsHome.Controls.Dialogue.DialogueBox.ProcessBreakPayload(ThousandYearsHome.Controls.Dialogue.DialogueBreakPayload)")]
[assembly: SuppressMessage("Performance", "HAA0601:Value type to reference type conversion causing boxing allocation", Justification = "Gotta use EmitSignal", Scope = "member", Target = "~M:ThousandYearsHome.Controls.Dialogue.DialogueBox.SetState(ThousandYearsHome.Controls.Dialogue.DialogueEngineState)")]
[assembly: SuppressMessage("Performance", "HAA0401:Possible allocation of reference type enumerator", Justification = "No other API available", Scope = "member", Target = "~M:ThousandYearsHome.Controls.Dialogue.DialogueSegmenter.SegmentText(System.String)~System.Collections.Generic.IEnumerable{ThousandYearsHome.Controls.Dialogue.DialogueSegment}")]
[assembly: SuppressMessage("Performance", "HAA0401:Possible allocation of reference type enumerator", Justification = "No other API available", Scope = "member", Target = "~M:ThousandYearsHome.Controls.Dialogue.DialogueBox.AddToLabel(System.String,System.Single)~System.Boolean")]
[assembly: SuppressMessage("Performance", "HAA0601:Value type to reference type conversion causing boxing allocation", Justification = "Debugger-only code", Scope = "member", Target = "~P:ThousandYearsHome.Controls.Dialogue.DialogueSegment.DebuggerDisplay")]
[assembly: SuppressMessage("Performance", "HAA0101:Array allocation for params parameter", Justification = "<Pending>", Scope = "member", Target = "~M:ThousandYearsHome.Controls.Dialogue.DialogueBox.ProcessSilencePayload(ThousandYearsHome.Controls.Dialogue.DialogueSilencePayload)")]
[assembly: SuppressMessage("Performance", "HAA0101:Array allocation for params parameter", Justification = "<Pending>", Scope = "member", Target = "~M:ThousandYearsHome.Controls.Dialogue.DialogueBox.ProcessClearPayload(ThousandYearsHome.Controls.Dialogue.DialogueClearPayload)")]
[assembly: SuppressMessage("Performance", "HAA0401:Possible allocation of reference type enumerator", Justification = "Runs once per Player", Scope = "member", Target = "~M:ThousandYearsHome.Entities.PlayerEntity.PlayerStateMachine.Init(ThousandYearsHome.Entities.PlayerEntity.Player)")]
[assembly: SuppressMessage("Performance", "HAA0101:Array allocation for params parameter", Justification = "<Pending>", Scope = "member", Target = "~M:ThousandYearsHome.Entities.PlayerEntity.PlayerCamera.UpdateScroll")]
[assembly: SuppressMessage("Performance", "HAA0601:Value type to reference type conversion causing boxing allocation", Justification = "<Pending>", Scope = "member", Target = "~M:ThousandYearsHome.Entities.PlayerEntity.PlayerCamera.UpdateScroll")]
[assembly: SuppressMessage("Performance", "HAA0101:Array allocation for params parameter", Justification = "<Pending>", Scope = "member", Target = "~M:ThousandYearsHome.Entities.PlayerEntity.Player._PhysicsProcess(System.Single)")]
[assembly: SuppressMessage("Performance", "HAA0101:Array allocation for params parameter", Justification = "<Pending>", Scope = "member", Target = "~P:ThousandYearsHome.Entities.PlayerEntity.PlayerCamera.Current")]
[assembly: SuppressMessage("Performance", "HAA0101:Array allocation for params parameter", Justification = "<Pending>", Scope = "member", Target = "~M:ThousandYearsHome.Entities.PowerBall._PhysicsProcess(System.Single)")]

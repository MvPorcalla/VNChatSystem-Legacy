//=====================================
// MugiParser.cs
//=====================================

using System;
using System.Collections.Generic;
using UnityEngine;
using static ChatDialogueSystem.DebugHelper;

namespace ChatDialogueSystem
{
    public class MugiParser
    {
        private class ParserContext
        {
            public DialogueNode currentNode;
            public ChoiceData currentChoice;
            public bool inChoiceBlock;
            public bool processingChoiceContent;
            public int lineNumber;
            public string fileName;
        }

        public static Dictionary<string, DialogueNode> ParseMugiFile(TextAsset mugiFile)
        {
            var nodes = new Dictionary<string, DialogueNode>();
            
            if (mugiFile == null)
            {
                LogError(Category.MugiParser, "Null mugi file provided");
                return nodes;
            }

            Log(Category.MugiParser, $"Starting parse of file: {mugiFile.name}");

            string[] lines = mugiFile.text.Split('\n');
            var context = new ParserContext { fileName = mugiFile.name };

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                context.lineNumber = i + 1;
                
                try
                {
                    // Remove inline comments
                    line = StripInlineComments(line);
                    
                    // Skip empty lines and full-line comments
                    if (string.IsNullOrEmpty(line) || line.StartsWith("//"))
                        continue;

                    // Parse contact (ignore for now, handled by NPCChatData)
                    if (line.StartsWith("contact:"))
                        continue;

                    // Parse node title
                    if (TryParseNodeTitle(line, context, nodes))
                        continue;

                    // Skip node separator
                    if (line == "---" || line == "===")
                        continue;

                    if (context.currentNode == null)
                    {
                        LogWarning(Category.MugiParser, 
                            $"[{context.fileName}:{context.lineNumber}] Content found outside of node: {line}");
                        continue;
                    }

                    // Parse commands and content
                    if (TryParseJumpCommand(line, context)) continue;
                    if (TryParsePauseButton(line, context)) continue;
                    if (TryParseChoiceBlockStart(line, context)) continue;
                    if (TryParseChoiceBlockEnd(line, context)) continue;
                    if (TryParseChoiceOption(line, context)) continue;
                    if (TryParseMediaCommand(line, context)) continue;
                    if (TryParseDialogueLine(line, context)) continue;

                    // If we get here, the line wasn't recognized
                    LogWarning(Category.MugiParser, 
                        $"[{context.fileName}:{context.lineNumber}] Unrecognized syntax: {line}");
                }
                catch (Exception ex)
                {
                    LogError(Category.MugiParser, 
                        $"[{context.fileName}:{context.lineNumber}] Parse error: {line}\n{ex.Message}");
                }
            }

            // Finalize parser
            FinalizeParser(context, nodes);

            // Validate the dialogue graph
            ValidateDialogueGraph(nodes, mugiFile.name);

            Log(Category.MugiParser, $"Parsed {nodes.Count} nodes total from {mugiFile.name}");
            
            // Debug output for each node
            PrintNodeSummary(nodes);
            
            return nodes;
        }

        // ═══════════════════════════════════════════════════════════
        // ░ PARSING METHODS
        // ═══════════════════════════════════════════════════════════

        private static string StripInlineComments(string line)
        {
            int commentIndex = line.IndexOf("//");
            if (commentIndex >= 0)
            {
                return line.Substring(0, commentIndex).Trim();
            }
            return line;
        }

        private static bool TryParseNodeTitle(string line, ParserContext context, Dictionary<string, DialogueNode> nodes)
        {
            if (!line.StartsWith("title:"))
                return false;

            // Save previous node before creating new one
            if (context.currentNode != null)
            {
                FinalizeCurrentNode(context, nodes);
            }
            
            string nodeName = line.Substring(6).Trim();
            
            if (string.IsNullOrEmpty(nodeName))
            {
                LogWarning(Category.MugiParser, 
                    $"[{context.fileName}:{context.lineNumber}] Empty node name in title declaration");
                return true;
            }

            if (nodes.ContainsKey(nodeName))
            {
                LogWarning(Category.MugiParser, 
                    $"[{context.fileName}:{context.lineNumber}] Duplicate node name '{nodeName}' - previous node will be overwritten");
            }
            
            context.currentNode = new DialogueNode(nodeName);
            context.inChoiceBlock = false;
            context.processingChoiceContent = false;
            context.currentChoice = null;
            
            Log(Category.MugiParser, $"Starting node '{nodeName}' at line {context.lineNumber}");
            return true;
        }

        private static bool TryParseJumpCommand(string line, ParserContext context)
        {
            if (!line.StartsWith("<<jump") || !line.EndsWith(">>"))
                return false;

            string jumpTarget = line.Substring(6, line.Length - 8).Trim();
            
            if (string.IsNullOrEmpty(jumpTarget))
            {
                LogWarning(Category.MugiParser, 
                    $"[{context.fileName}:{context.lineNumber}] Empty jump target in <<jump>> command");
                return true;
            }
            
            Log(Category.MugiParser, 
                $"[Line {context.lineNumber}] Jump command: '{line}' -> target: '{jumpTarget}'");
            
            if (context.processingChoiceContent && context.currentChoice != null)
            {
                context.currentChoice.targetNode = jumpTarget;
                Log(Category.MugiParser, $"Set choice jump target to: {jumpTarget}");
            }
            else
            {
                context.currentNode.nextNode = jumpTarget;
                Log(Category.MugiParser, $"Set node auto-jump target to: {jumpTarget}");
            }
            
            return true;
        }

        private static bool TryParsePauseButton(string line, ParserContext context)
        {
            if (line != "-> ...")
                return false;

            if (context.processingChoiceContent)
            {
                LogWarning(Category.MugiParser, 
                    $"[{context.fileName}:{context.lineNumber}] Pause button (-> ...) found inside choice block - this may cause unexpected behavior");
            }
            else
            {
                int pauseAfterMessage = context.currentNode.messages.Count;
                context.currentNode.pausePoints.Add(pauseAfterMessage);
                Log(Category.MugiParser, 
                    $"[Line {context.lineNumber}] Added pause point after message {pauseAfterMessage} in node '{context.currentNode.nodeName}'");
            }
            
            return true;
        }

        private static bool TryParseChoiceBlockStart(string line, ParserContext context)
        {
            if (line != ">> choice")
                return false;

            if (context.inChoiceBlock)
            {
                LogWarning(Category.MugiParser, 
                    $"[{context.fileName}:{context.lineNumber}] Nested choice blocks are not supported - ignoring nested >> choice");
                return true;
            }

            context.inChoiceBlock = true;
            context.processingChoiceContent = false;
            Log(Category.MugiParser, $"[Line {context.lineNumber}] Starting choice block");
            return true;
        }

        private static bool TryParseChoiceBlockEnd(string line, ParserContext context)
        {
            if (line != ">> endchoice")
                return false;

            if (!context.inChoiceBlock)
            {
                LogWarning(Category.MugiParser, 
                    $"[{context.fileName}:{context.lineNumber}] Unexpected >> endchoice outside choice block");
                return true;
            }
            
            if (context.currentChoice != null)
            {
                ValidateAndAddChoice(context);
                context.currentChoice = null;
            }
            
            context.inChoiceBlock = false;
            context.processingChoiceContent = false;
            Log(Category.MugiParser, $"[Line {context.lineNumber}] Ended choice block");
            return true;
        }

        private static bool TryParseChoiceOption(string line, ParserContext context)
        {
            if (!context.inChoiceBlock || !line.StartsWith("-> \"") || !line.EndsWith("\""))
                return false;

            // Save previous choice
            if (context.currentChoice != null)
            {
                ValidateAndAddChoice(context);
            }
            
            string choiceText = line.Substring(3, line.Length - 4); // Remove -> " and "
            
            if (string.IsNullOrEmpty(choiceText))
            {
                LogWarning(Category.MugiParser, 
                    $"[{context.fileName}:{context.lineNumber}] Empty choice text");
                return true;
            }
            
            context.currentChoice = new ChoiceData(choiceText, "");
            context.processingChoiceContent = true;
            Log(Category.MugiParser, $"[Line {context.lineNumber}] Created new choice: '{choiceText}'");
            return true;
        }

        private static bool TryParseMediaCommand(string line, ParserContext context)
        {
            if (!line.StartsWith(">> media"))
                return false;

            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
            {
                LogWarning(Category.MugiParser,
                    $"[{context.fileName}:{context.lineNumber}] Invalid media command format: {line}");
                return true;
            }

            string speaker = parts[2];
            string imagePath = ExtractPathFromMediaCommand(line);

            if (string.IsNullOrEmpty(imagePath))
            {
                LogWarning(Category.MugiParser,
                    $"[{context.fileName}:{context.lineNumber}] No path found in media command: {line}");
                return true;
            }

            // ✅ NEW: Detect unlock flag
            bool shouldUnlock = line.Contains("unlock:true");

            var imageMessage = new MessageData(MessageData.MessageType.Image, speaker, "", imagePath);
            imageMessage.shouldUnlockCG = shouldUnlock; // ✅ Set the unlock flag

            // ✅ NEW: Log when unlockable CG detected
            if (shouldUnlock)
            {
                Log(Category.MugiParser,
                    $"[Line {context.lineNumber}] 📸 Unlockable CG detected: {imagePath}");
            }

            if (context.processingChoiceContent && context.currentChoice != null)
            {
                context.currentChoice.playerMessages.Add(imageMessage);
                Log(Category.MugiParser, $"[Line {context.lineNumber}] Added image to choice: {imagePath}");
            }
            else
            {
                context.currentNode.messages.Add(imageMessage);
                Log(Category.MugiParser,
                    $"[Line {context.lineNumber}] Added image to node: {imagePath}" +
                    (shouldUnlock ? " [UNLOCKABLE]" : ""));
            }

            return true;
        }

        private static bool TryParseDialogueLine(string line, ParserContext context)
        {
            if (!line.Contains(":"))
                return false;

            int colonIndex = line.IndexOf(':');
            string speaker = line.Substring(0, colonIndex).Trim();
            string content = line.Substring(colonIndex + 1).Trim();
            
            if (string.IsNullOrEmpty(speaker))
            {
                LogWarning(Category.MugiParser, 
                    $"[{context.fileName}:{context.lineNumber}] Empty speaker name: {line}");
                return true;
            }
            
            // Remove quotes if present
            if (content.StartsWith("\"") && content.EndsWith("\""))
            {
                content = content.Substring(1, content.Length - 2);
            }

            MessageData.MessageType msgType = MessageData.MessageType.Text;
            if (speaker.ToLower() == "system")
            {
                msgType = MessageData.MessageType.System;
            }

            var message = new MessageData(msgType, speaker, content);
            
            // Handle player choice messages (marked with #)
            if (context.processingChoiceContent && context.currentChoice != null && speaker.StartsWith("#"))
            {
                speaker = speaker.Substring(1).Trim(); // Remove #
                message.speaker = speaker;
                context.currentChoice.playerMessages.Add(message);
                Log(Category.MugiParser, $"[Line {context.lineNumber}] Added player message to choice: '{content}'");
            }
            else
            {
                context.currentNode.messages.Add(message);
                Log(Category.MugiParser, 
                    $"[Line {context.lineNumber}] Added message {context.currentNode.messages.Count - 1} to node: {speaker}: '{content}'");
            }
            
            return true;
        }

        // ═══════════════════════════════════════════════════════════
        // ░ HELPER METHODS
        // ═══════════════════════════════════════════════════════════

        private static string ExtractPathFromMediaCommand(string line)
        {
            int pathIndex = line.IndexOf("path:");
            if (pathIndex == -1)
            {
                return "";
            }
            
            // Everything after "path:" is the path (handles spaces in paths)
            string path = line.Substring(pathIndex + 5).Trim();
            return path;
        }

        private static void ValidateAndAddChoice(ParserContext context)
        {
            if (context.currentChoice == null) return;

            if (string.IsNullOrEmpty(context.currentChoice.targetNode))
            {
                LogWarning(Category.MugiParser, 
                    $"[{context.fileName}] Choice '{context.currentChoice.choiceText}' in node '{context.currentNode.nodeName}' " +
                    $"has no target node (missing <<jump>>)");
            }

            context.currentNode.choices.Add(context.currentChoice);
            Log(Category.MugiParser, 
                $"Added choice: '{context.currentChoice.choiceText}' -> '{context.currentChoice.targetNode}'");
        }

        private static void FinalizeCurrentNode(ParserContext context, Dictionary<string, DialogueNode> nodes)
        {
            // Add final choice if exists
            if (context.currentChoice != null)
            {
                ValidateAndAddChoice(context);
                context.currentChoice = null;
            }

            // Validate node before adding
            var node = context.currentNode;
            
            if (node.messages.Count == 0 && 
                (node.choices.Count > 0 || !string.IsNullOrEmpty(node.nextNode)))
            {
                LogWarning(Category.MugiParser, 
                    $"[{context.fileName}] Node '{node.nodeName}' has no messages but has choices/jumps - is this intentional?");
            }

            if (node.choices.Count > 0 && !string.IsNullOrEmpty(node.nextNode))
            {
                LogWarning(Category.MugiParser, 
                    $"[{context.fileName}] Node '{node.nodeName}' has both choices AND auto-jump - " +
                    $"the auto-jump will be ignored");
            }

            nodes[node.nodeName] = node;
            Log(Category.MugiParser, 
                $"Completed node '{node.nodeName}' - " +
                $"pausePoints: {node.pausePoints.Count}, " +
                $"nextNode: '{node.nextNode}', " +
                $"choices: {node.choices.Count}");
        }

        private static void FinalizeParser(ParserContext context, Dictionary<string, DialogueNode> nodes)
        {
            // Add final node if exists
            if (context.currentNode != null)
            {
                FinalizeCurrentNode(context, nodes);
            }

            // Warn if choice block was never closed
            if (context.inChoiceBlock)
            {
                LogWarning(Category.MugiParser, 
                    $"[{context.fileName}] Choice block was never closed with >> endchoice");
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ VALIDATION
        // ═══════════════════════════════════════════════════════════

        private static void ValidateDialogueGraph(Dictionary<string, DialogueNode> nodes, string fileName)
        {
            Log(Category.MugiParser, $"Validating dialogue graph for {fileName}...");

            foreach (var kvp in nodes)
            {
                var node = kvp.Value;
                
                // Validate auto-jump target
                if (!string.IsNullOrEmpty(node.nextNode) && !nodes.ContainsKey(node.nextNode))
                {
                    LogWarning(Category.MugiParser, 
                        $"[{fileName}] Node '{node.nodeName}' jumps to non-existent node '{node.nextNode}'");
                }
                
                // Validate choice targets
                foreach (var choice in node.choices)
                {
                    if (!string.IsNullOrEmpty(choice.targetNode) && !nodes.ContainsKey(choice.targetNode))
                    {
                        LogWarning(Category.MugiParser, 
                            $"[{fileName}] Choice '{choice.choiceText}' in node '{node.nodeName}' " +
                            $"targets non-existent node '{choice.targetNode}'");
                    }
                }
            }

            // Detect circular jumps (only for auto-jumps, not choices)
            DetectCircularJumps(nodes, fileName);

            Log(Category.MugiParser, $"Validation complete for {fileName}");
        }

        private static void DetectCircularJumps(Dictionary<string, DialogueNode> nodes, string fileName)
        {
            foreach (var node in nodes.Values)
            {
                if (string.IsNullOrEmpty(node.nextNode)) continue;
                if (node.choices.Count > 0) continue; // Skip nodes with choices
                
                var visited = new HashSet<string>();
                string current = node.nodeName;
                
                while (!string.IsNullOrEmpty(current) && nodes.ContainsKey(current))
                {
                    if (!visited.Add(current))
                    {
                        LogWarning(Category.MugiParser, 
                            $"[{fileName}] Circular auto-jump chain detected starting from node '{node.nodeName}'");
                        break;
                    }
                    
                    var currentNode = nodes[current];
                    if (currentNode.choices.Count > 0) break; // Exit if we hit choices
                    current = currentNode.nextNode;
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ DEBUG OUTPUT
        // ═══════════════════════════════════════════════════════════

        private static void PrintNodeSummary(Dictionary<string, DialogueNode> nodes)
        {
            foreach (var kvp in nodes)
            {
                var node = kvp.Value;
                string pausePointsStr = node.pausePoints.Count > 0 
                    ? $"[{string.Join(",", node.pausePoints)}]" 
                    : "none";
                Log(Category.MugiParser, 
                    $"Node '{node.nodeName}': {node.messages.Count} messages, " +
                    $"{node.choices.Count} choices, pausePoints: {pausePointsStr}, " +
                    $"nextNode: '{node.nextNode}'");
            }
        }
    }
}
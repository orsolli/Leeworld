﻿using UnityEngine;
using System;
using System.Collections.Generic;
using NodeEditorFramework;

namespace NodeEditorFramework
{
	public abstract class Node : ScriptableObject
	{
		[HideInInspector]
		public Rect rect = new Rect ();

		// Calculation graph
		[HideInInspector]
		public List<NodeInput> Inputs = new List<NodeInput>();
		[HideInInspector]
		public List<NodeOutput> Outputs = new List<NodeOutput>();
		[HideInInspector]
		public bool calculated = true;

		// State graph
		public List<Transition> transitions = new List<Transition> ();

		// Abstract member to get the ID of the node
		public abstract string GetID { get; }

		/// <summary>
		/// Function implemented by the children to create the node
		/// </summary>
		/// <param name="pos">Position.</param>
		public abstract Node Create (Vector2 pos);

		/// <summary>
		/// Function implemented by the children to draw the node
		/// </summary>
		public abstract void NodeGUI ();

		/// <summary>
		/// Function implemented by the children to calculate their outputs
		/// Should return Success/Fail
		/// </summary>
		public abstract bool Calculate ();
		
		/// <summary>
		/// Optional callback when the node is deleted
		/// </summary>
		public virtual void OnDelete () {}

		#region Member Functions

		/// <summary>
		/// Checks if there are no unassigned and no null-value inputs.
		/// </summary>
		public bool allInputsReady () 
		{
			for (int inCnt = 0; inCnt < Inputs.Count; inCnt++) 
			{
				if (Inputs[inCnt].connection == null || Inputs[inCnt].connection.IsValueNull)
					return false;
			}
			return true;
		}
		/// <summary>
		/// Checks if there are any unassigned inputs.
		/// </summary>
		public bool hasUnassignedInputs () 
		{
			for (int inCnt = 0; inCnt < Inputs.Count; inCnt++) 
			{
				if (Inputs [inCnt].connection == null)
					return true;
			}
			return false;
		}

		/// <summary>
		/// Returns whether every node this node depends on has been calculated
		/// </summary>
		public bool descendantsCalculated () 
		{
			for (int cnt = 0; cnt < Inputs.Count; cnt++) 
			{
				if (Inputs [cnt].connection != null && !Inputs [cnt].connection.body.calculated)
					return false;
			}
			return true;
		}

		/// <summary>
		/// Recursively checks whether this node is a child of the other node
		/// </summary>
		public bool isChildOf (Node otherNode)
		{
			if (otherNode == null)
				return false;
			for (int cnt = 0; cnt < Inputs.Count; cnt++) 
			{
				if (Inputs [cnt].connection != null) 
				{
					if (Inputs [cnt].connection.body == otherNode)
						return true;
					else if (Inputs [cnt].connection.body.isChildOf (otherNode)) // Recursively searching
						return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Call this method in your NodeGUI to setup an output knob aligning with the y position of the last GUILayout control drawn.
		/// </summary>
		/// <param name="outputIdx">The index of the output in the Node's Outputs list</param>
		protected void OutputKnob (int outputIdx)
		{
			if (Event.current.type == EventType.Repaint)
				Outputs[outputIdx].SetRect (GUILayoutUtility.GetLastRect ());
		}
		
		/// <summary>
		/// Call this method in your NodeGUI to setup an input knob aligning with the y position of the last GUILayout control drawn.
		/// </summary>
		/// <param name="inputIdx">The index of the input in the Node's Inputs list</param>
		protected void InputKnob (int inputIdx)
		{
			if (Event.current.type == EventType.Repaint)
				Inputs[inputIdx].SetRect (GUILayoutUtility.GetLastRect ());
		}

		/// <summary>
		/// Call this method to create an output on your node
		/// </summary>
		public void CreateOutput(string outputName, string outputType)
		{
			NodeOutput.Create(this, outputName, outputType);
		}

		/// <summary>
		/// Call this method to create an input on your node
		/// </summary>
		public void CreateInput(string inputName, string inputType)
		{
			NodeInput.Create(this, inputName, inputType);
		}

		/// <summary>
		/// Init this node. Has to be called when creating a child node
		/// </summary>
		public void InitBase () 
		{
			Calculate ();
			NodeEditor.curNodeCanvas.nodes.Add (this);
	#if UNITY_EDITOR
			if (name == "")
			{
				name = UnityEditor.ObjectNames.NicifyVariableName (GetID);
			}
			if (!String.IsNullOrEmpty(UnityEditor.AssetDatabase.GetAssetPath (NodeEditor.curNodeCanvas)))
			{
				UnityEditor.AssetDatabase.AddObjectToAsset (this, NodeEditor.curNodeCanvas);
				for (int inCnt = 0; inCnt < Inputs.Count; inCnt++) 
					UnityEditor.AssetDatabase.AddObjectToAsset (Inputs [inCnt], this);
				for (int outCnt = 0; outCnt < Outputs.Count; outCnt++) 
					UnityEditor.AssetDatabase.AddObjectToAsset (Outputs [outCnt], this);
				
				UnityEditor.AssetDatabase.ImportAsset (UnityEditor.AssetDatabase.GetAssetPath (NodeEditor.curNodeCanvas));
			}
	#endif
		}

		/// <summary>
		/// Returns the input knob that is at the position on this node or null
		/// </summary>
		public NodeInput GetInputAtPos (Vector2 pos) 
		{
			for (int inCnt = 0; inCnt < Inputs.Count; inCnt++) 
			{ // Search for an input at the position
				if (Inputs [inCnt].GetScreenKnob ().Contains (new Vector3 (pos.x, pos.y)))
					return Inputs [inCnt];
			}
			return null;
		}
		/// <summary>
		/// Returns the output knob that is at the position on this node or null
		/// </summary>
		public NodeOutput GetOutputAtPos (Vector2 pos) 
		{
			for (int outCnt = 0; outCnt < Outputs.Count; outCnt++) 
			{ // Search for an output at the position
				if (Outputs [outCnt].GetScreenKnob ().Contains (new Vector3 (pos.x, pos.y)))
					return Outputs [outCnt];
			}
			return null;
		}

		/// <summary>
		/// Draws the node knobs; splitted from curves because of the render order
		/// </summary>
		public void DrawKnobs () 
		{
			for (int outCnt = 0; outCnt < Outputs.Count; outCnt++) 
			{
				GUI.DrawTexture(Outputs[outCnt].GetGUIKnob(), ConnectionTypes.GetTypeData(Outputs[outCnt].type).OutputKnob);
			}
			for (int inCnt = 0; inCnt < Inputs.Count; inCnt++) 
			{
				GUI.DrawTexture(Inputs[inCnt].GetGUIKnob(), ConnectionTypes.GetTypeData(Inputs[inCnt].type).InputKnob);
			}
		}
		/// <summary>
		/// Draws the node curves; splitted from knobs because of the render order
		/// </summary>
		public void DrawConnections () 
		{
			for (int outCnt = 0; outCnt < Outputs.Count; outCnt++) 
			{
				NodeOutput output = Outputs [outCnt];
				for (int conCnt = 0; conCnt < output.connections.Count; conCnt++) 
				{
					NodeEditor.DrawConnection (output.GetGUIKnob ().center, 
					                          output.connections [conCnt].GetGUIKnob ().center,
											  ConnectionTypes.GetTypeData(output.type).col);
				}
			}
		}

		/// <summary>
		/// Draws the node transitions.
		/// </summary>
		public void DrawTransitions () 
		{
			for (int cnt = 0; cnt < transitions.Count; cnt++)
			{
				Vector2 StartPoint = transitions[cnt].startNode.rect.center + NodeEditor.curEditorState.zoomPanAdjust;
				Vector2 EndPoint = transitions[cnt].endNode.rect.center + NodeEditor.curEditorState.zoomPanAdjust;
				NodeEditor.DrawLine (StartPoint, EndPoint, Color.grey, null, 3);

				Rect selectRect = new Rect (0, 0, 20, 20);
				selectRect.center = Vector2.Lerp (StartPoint, EndPoint, 0.5f);

				if (GUI.Button (selectRect, "#"))
				{
					// TODO: Select
				}

			}
		}

		/// <summary>
		/// Deletes this Node from curNodeCanvas. Depends on that.
		/// </summary>
		public void Delete () 
		{
			NodeEditor.curNodeCanvas.nodes.Remove (this);
			for (int outCnt = 0; outCnt < Outputs.Count; outCnt++) 
			{
				NodeOutput output = Outputs [outCnt];
				for (int conCnt = 0; conCnt < output.connections.Count; conCnt++)
					output.connections [outCnt].connection = null;
				DestroyImmediate (output, true);
			}
			for (int inCnt = 0; inCnt < Inputs.Count; inCnt++) 
			{
				NodeInput input = Inputs [inCnt];
				if (input.connection != null)
					input.connection.connections.Remove (input);
				DestroyImmediate (input, true);
			}
			
			DestroyImmediate (this, true);

	#if UNITY_EDITOR
			if (!String.IsNullOrEmpty (UnityEditor.AssetDatabase.GetAssetPath (NodeEditor.curNodeCanvas))) 
			{
				UnityEditor.AssetDatabase.ImportAsset (UnityEditor.AssetDatabase.GetAssetPath (NodeEditor.curNodeCanvas));
			}
	#endif
			NodeEditorCallbacks.IssueOnDeleteNode (this);
		}
		
		#endregion
		
		#region Static Functions

		/// <summary>
		/// Check if an output and an input can be connected (same type, ...)
		/// </summary>
		public static bool CanApplyConnection (NodeOutput output, NodeInput input)
		{
			if (input == null || output == null)
				return false;

			if (input.body == output.body || input.connection == output)
				return false;

			if (input.type != output.type)
				return false;

			if (output.body.isChildOf (input.body)) 
			{
				NodeEditorWindow.editor.ShowNotification (new GUIContent ("Recursion detected!"));
				return false;
			}
			return true;
		}

		/// <summary>
		/// Applies a connection between output and input. 'CanApplyConnection' has to be checked before
		/// </summary>
		public static void ApplyConnection (NodeOutput output, NodeInput input)
		{
			if (input != null && output != null) 
			{
				if (input.connection != null) 
				{
					input.connection.connections.Remove (input);
				}
				input.connection = output;
				output.connections.Add (input);

				NodeEditor.RecalculateFrom (input.body);

				NodeEditorCallbacks.IssueOnAddConnection (input);
			}
		}

		/// <summary>
		/// Removes the connection from NodeInput.
		/// </summary>
		public static void RemoveConnection (NodeInput input)
		{
			NodeEditorCallbacks.IssueOnRemoveConnection (input);
			input.connection.connections.Remove (input);
			input.connection = null;
			NodeEditor.RecalculateFrom (input.body);
		}

		public static void CreateTransition (Node from, Node to) 
		{
			from.transitions.Add (Transition.Create (from, to));
		}

		#endregion
	}
}

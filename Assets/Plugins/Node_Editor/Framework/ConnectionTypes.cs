﻿using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using NodeEditorFramework;

namespace NodeEditorFramework
{
	public static class ConnectionTypes
	{
		public static Type NullType { get { return typeof(ConnectionTypes); } }
		
		// Static consistent information about types
		static Dictionary<string, TypeData> types = new Dictionary<string, TypeData> ();
		public static TypeData GetTypeData (string typeName)
		{
			if (types == null || types.Count == 0)
				FetchTypes();
			TypeData res;
			if (types.TryGetValue(typeName, out res) )
				return res;
			UnityEngine.Debug.LogError("No TypeData defined for: " + typeName);
			return types.First().Value;
		}
		
		public static Type GetType (string typeName)
		{
			if (types == null || types.Count == 0)
				FetchTypes();
			TypeData res;
			if (types.TryGetValue(typeName, out res))
				return res.Type ?? NullType;
			UnityEngine.Debug.LogError ("No TypeData defined for: " + typeName);
			return NullType;
		}
		
		/// <summary>
		/// Fetches every Type Declaration in the assembly
		/// </summary>
		public static void FetchTypes () 
		{ // Search the current and (if the NodeEditor is packed into a .dll) the calling one
			types = new Dictionary<string, TypeData> ();

			List<Assembly> scriptAssemblies = AppDomain.CurrentDomain.GetAssemblies ()
				.Where ((Assembly a) => a.FullName.StartsWith ("Assembly-"))
					.ToList (); // This filters out all script assemblies
			if (!scriptAssemblies.Contains (Assembly.GetExecutingAssembly ()))
				scriptAssemblies.Add (Assembly.GetExecutingAssembly ());
			foreach (Assembly assembly in scriptAssemblies) 
			{
				foreach (Type type in assembly.GetTypes ().Where (T => T.IsClass && !T.IsAbstract && T.GetInterfaces ().Contains (typeof (ITypeDeclaration)))) 
				{
					ITypeDeclaration typeDecl = assembly.CreateInstance (type.FullName) as ITypeDeclaration;
					if (typeDecl == null) 
					{
						UnityEngine.Debug.LogError ("Error with Type Declaration " + type.FullName);
						return;
					}
					Texture2D InputKnob = NodeEditor.LoadTexture(typeDecl.InputKnob_TexPath);
					Texture2D OutputKnob = NodeEditor.LoadTexture(typeDecl.OutputKnob_TexPath);
					types.Add(typeDecl.name, new TypeData(typeDecl.col, InputKnob, OutputKnob, typeDecl.Type));
				}
			}
		}
	}

	public struct TypeData 
	{
		public Color col;
		public Texture2D InputKnob;
		public Texture2D OutputKnob;
		public Type Type;
		
		public TypeData (Color color, Texture2D inKnob, Texture2D outKnob, Type type) 
		{
			col = color;
			InputKnob = NodeEditor.Tint (inKnob, color);
			OutputKnob = NodeEditor.Tint (outKnob, color);
			Type = type;
		}
	}

	public interface ITypeDeclaration
	{
		string name { get; }
		Color col { get; }
		string InputKnob_TexPath { get; }
		string OutputKnob_TexPath { get; }
		Type Type { get; }
	}

	// TODO: Node Editor: Built-In Connection Types
	public class FloatType : ITypeDeclaration 
	{
		public string name { get { return "Float"; } }
		public Color col { get { return Color.cyan; } }
		public string InputKnob_TexPath { get { return "Textures/In_Knob.png"; } }
		public string OutputKnob_TexPath { get { return "Textures/Out_Knob.png"; } }
		public Type Type { get { return typeof(float); } }
	}

	// TODO: Node Editor: Built-In Connection Types
	public class GameObjectType : ITypeDeclaration 
	{
		public string name { get { return "GameObject"; } }
		public Color col { get { return Color.yellow; } }
		public string InputKnob_TexPath { get { return "Textures/In_Knob.png"; } }
		public string OutputKnob_TexPath { get { return "Textures/Out_Knob.png"; } }
		public Type Type { get { return typeof(GameObject); } }
	}
}
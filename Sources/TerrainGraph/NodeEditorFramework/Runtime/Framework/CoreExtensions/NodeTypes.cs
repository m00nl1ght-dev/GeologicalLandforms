using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using NodeEditorFramework.Utilities;

namespace NodeEditorFramework 
{
	/// <summary>
	/// Handles fetching and storing of all Node declarations
	/// </summary>
	public static class NodeTypes
	{
		private static Dictionary<string, NodeTypeData> nodes;

		/// <summary>
		/// Fetches every Node Declaration in the script assemblies to provide the framework with custom node types
		/// </summary>
		public static void FetchNodeTypes() 
		{
			nodes = new Dictionary<string, NodeTypeData> ();
			foreach (Type type in ReflectionUtility.getSubTypes (typeof(Node)))	
			{
				if (type.IsAbstract) continue;
				object[] nodeAttributes = type.GetCustomAttributes(typeof(NodeAttribute), false);                    
				NodeAttribute attr = nodeAttributes.Length > 0 ? nodeAttributes[0] as NodeAttribute : null;
				if(attr == null || !attr.hide)
				{ // Only regard if it is not marked as hidden
				  // Fetch node information
					string ID, Title = "None";
					FieldInfo IDField = type.GetField("ID");
					if (IDField == null || attr == null)
					{ // Cannot read ID from const field or need to read Title because of missing attribute -> Create sample to read from properties
						Node sample = (Node)ScriptableObject.CreateInstance(type);
						ID = sample.GetID;
						Title = sample.Title;
						UnityEngine.Object.DestroyImmediate(sample);
					}
					else // Can read ID directly from const field
						ID = (string)IDField.GetValue(null);
					// Create Data from information
					NodeTypeData data = attr == null?  // Switch between explicit information by the attribute or node information
						new NodeTypeData(ID, Title, type, Type.EmptyTypes, 0) :
						new NodeTypeData(ID, attr.contextText, type, attr.limitToCanvasTypes, attr.orderValue);
					nodes.Add (ID, data);
				}
			}
		}

		/// <summary>
		/// Returns all recorded node definitions found by the system
		/// </summary>
		public static List<NodeTypeData> getNodeDefinitions () 
		{
			return nodes.Values.ToList ();
		}

		/// <summary>
		/// Returns the NodeData for the given node type ID
		/// </summary>
		public static NodeTypeData getNodeData (string typeID)
		{
			NodeTypeData data;
			nodes.TryGetValue (typeID, out data);
			return data;
		}

		/// <summary>
		/// Returns all node IDs that can automatically connect to the specified port.
		/// If port is null, all node IDs are returned.
		/// </summary>
		public static List<NodeTypeData> getCompatibleNodes (ConnectionPort port)
		{
			if (port == null)
				return NodeTypes.nodes.Values.ToList ();
			List<NodeTypeData> compatibleNodes = new();
			foreach (NodeTypeData nodeData in NodeTypes.nodes.Values)
			{ // Iterate over all nodes to check compability of any of their connection ports
				if (ConnectionPortManager.GetPortDeclarations (nodeData.typeID).Any (
					portDecl => portDecl.portInfo.IsCompatibleWith (port)))
					compatibleNodes.Add (nodeData);
			}
			return compatibleNodes;
		}
	}

	/// <summary>
	/// The NodeData contains the additional, editor specific data of a node type
	/// </summary>
	public struct NodeTypeData 
	{
		public readonly string typeID;
		public readonly string adress;
		public readonly Type type;
		public readonly Type[] limitToCanvasTypes;
		public readonly int orderValue;

		public NodeTypeData(string ID, string name, Type nodeType, Type[] limitedCanvasTypes, int orderVal)
		{
			typeID = ID;
			adress = name;
			type = nodeType;
			limitToCanvasTypes = limitedCanvasTypes;
			orderValue = orderVal;
		}
	}

	/// <summary>
	/// The NodeAttribute is used to specify editor specific data for a node type, later stored using a NodeData
	/// </summary>
	public class NodeAttribute : Attribute 
	{
		public bool hide { get; }
		public string contextText { get; }
		public int orderValue { get; }
		public Type[] limitToCanvasTypes { get; }

		public NodeAttribute (bool HideNode, string ReplacedContextText, int orderVal, params Type[] limitedCanvasTypes)
		{
			hide = HideNode;
			contextText = ReplacedContextText;
			orderValue = orderVal;
			limitToCanvasTypes = limitedCanvasTypes;
		}
	}
}
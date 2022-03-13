using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NodeEditorFramework.Utilities 
{
	/// <summary>
	/// Provides methods for loading resources both at runtime and in the editor; 
	/// Though, to load at runtime, they have to be in a resources folder
	/// </summary>
	public static class ResourceManager
	{
		private static AssetBundle _assetBundle;

		public static void InitAssetBundle(AssetBundle assetBundle)
		{
			_assetBundle = assetBundle;
		}
		
		public static T LoadResource<T>(string path) where T : Object
		{
			if (_assetBundle == null) throw new Exception("Asset bundle for terraingraph has not been set");
			var fullPath = "Assets/GL_NodeEditorFramework/Resources/" + path;
			return _assetBundle.LoadAsset<T>(fullPath);
		}

		private static readonly List<MemoryTexture> loadedTextures = new();

		/// <summary>
		/// Loads a texture in the resources folder in both the editor and at runtime and manages it in a memory for later use.
		/// If you don't wan't to optimise memory, just use LoadResource instead
		/// It's adviced to prepare the texPath using the function before to create a uniform 'path format', because textures are compared through their paths
		/// </summary>
		public static Texture2D LoadTexture (string texPath)
		{
			if (String.IsNullOrEmpty (texPath))
				return null;
			int existingInd = loadedTextures.FindIndex (memTex => memTex.path == texPath);
			if (existingInd != -1) 
			{ // If we have this texture in memory already, return it
				if (loadedTextures[existingInd].texture == null)
					loadedTextures.RemoveAt (existingInd);
				else
					return loadedTextures[existingInd].texture;
			}
			// Else, load up the texture and store it in memory
			Texture2D tex = LoadResource<Texture2D> (texPath);
			AddTextureToMemory (texPath, tex);
			return tex;
		}

		/// <summary>
		/// Loads up a texture tinted with col, and manages it in a memory for later use.
		/// It's adviced to prepare the texPath using the function before to create a uniform 'path format', because textures are compared through their paths
		/// </summary>
		public static Texture2D GetTintedTexture(string texPath, Color col)
		{
			string texMod = "Tint:" + col;
			Texture2D tintedTexture = GetTexture(texPath, texMod);
			if (tintedTexture == null)
			{ // We have to create a tinted version, perhaps even load the default texture if not yet in memory, and store it
				tintedTexture = LoadTexture(texPath);
				if (tintedTexture == null) throw new Exception("Missing texture: " + texPath);
				tintedTexture = RTEditorGUI.Tint(tintedTexture, col);
				AddTextureToMemory(texPath, tintedTexture, texMod); // Register texture for re-use
			}
			return tintedTexture;
		}

		/// <summary>
		/// Loads up a texture tinted with col, and manages it in a memory for later use.
		/// It's adviced to prepare the texPath using the function before to create a uniform 'path format', because textures are compared through their paths
		/// </summary>
		public static Texture2D GetTintedTexture(Texture2D tex, Color col)
		{
			MemoryTexture memTex = FindInMemory(tex);
			if (memTex != null && !string.IsNullOrEmpty (memTex.path))
				return GetTintedTexture(memTex.path, col);

			string texMod = "Tint:" + col;
			Texture2D tintedTexture = RTEditorGUI.Tint(tex, col);
			AddTextureToMemory(tex.name, tintedTexture, texMod); // Register texture for re-use
			return tintedTexture;
		}

		/// <summary>
		/// Records an additional texture for the manager memory with optional modifications
		/// It's adviced to prepare the texPath using the function before to create a uniform 'path format', because textures are compared through their paths
		/// </summary>
		public static void AddTextureToMemory (string texturePath, Texture2D texture, params string[] modifications)
		{
			if (texture == null) return;
			loadedTextures.Add (new MemoryTexture (texturePath, texture, modifications));
		}
		
		/// <summary>
		/// Returns whether the manager memory contains the texture
		/// It's adviced to prepare the texPath using the function before to create a uniform 'path format', because textures are compared through their paths
		/// </summary>
		public static MemoryTexture FindInMemory (Texture2D tex)
		{
			int existingInd = loadedTextures.FindIndex (memTex => memTex.texture == tex);
			return existingInd != -1? loadedTextures[existingInd] : null;
		}
		
		/// <summary>
		/// Whether the manager memory contains a texture with optional modifications
		/// It's adviced to prepare the texPath using the function before to create a uniform 'path format', because textures are compared through their paths
		/// </summary>
		public static bool HasInMemory (string texturePath, params string[] modifications)
		{
			int existingInd = loadedTextures.FindIndex (memTex => memTex.path == texturePath);
			return existingInd != -1 && EqualModifications (loadedTextures[existingInd].modifications, modifications);
		}
		
		/// <summary>
		/// Gets a texture already in manager memory with specified modifications (check with contains before!)
		/// It's adviced to prepare the texPath using the function before to create a uniform 'path format', because textures are compared through their paths
		/// </summary>
		public static MemoryTexture GetMemoryTexture (string texturePath, params string[] modifications)
		{
			List<MemoryTexture> textures = loadedTextures.FindAll (memTex => memTex.path == texturePath);
			if (textures.Count == 0)
				return null;
			foreach (MemoryTexture tex in textures)
				if (EqualModifications (tex.modifications, modifications))
					return tex;
			return null;
		}
		
		/// <summary>
		/// Gets a texture already in manager memory with specified modifications (check with 'HasInMemory' before!)
		/// It's adviced to prepare the texPath using the function before to create a uniform 'path format', because textures are compared through their paths
		/// </summary>
		public static Texture2D GetTexture (string texturePath, params string[] modifications)
		{
			MemoryTexture memTex = GetMemoryTexture (texturePath, modifications);
			return memTex?.texture;
		}

		/// <summary>
		/// Gets a texture already in manager memory with specified modifications (check with 'HasInMemory' before!)
		/// It's adviced to prepare the texPath using the function before to create a uniform 'path format', because textures are compared through their paths
		/// </summary>
		public static bool TryGetTexture (string texturePath, ref Texture2D tex, params string[] modifications)
		{
			MemoryTexture memTex = GetMemoryTexture (texturePath, modifications);
			if (memTex != null)
				tex = memTex.texture;
			return memTex != null;
		}
		
		private static bool EqualModifications (string[] modsA, string[] modsB) 
		{
			return modsA.Length == modsB.Length && Array.TrueForAll (modsA, mod => modsB.Count (oMod => mod == oMod) == modsA.Count (oMod => mod == oMod));
		}

		public static string[] AppendMod (string[] modifications, string newModification) 
		{
			string[] mods = new string[modifications.Length+1];
			modifications.CopyTo (mods, 0);
			mods[mods.Length-1] = newModification;
			return mods;
		}
		
		public class MemoryTexture 
		{
			public readonly string path;
			public readonly Texture2D texture;
			public readonly string[] modifications;
			
			public MemoryTexture (string texPath, Texture2D tex, params string[] mods) 
			{
				path = texPath;
				texture = tex;
				modifications = mods;
			}
		}
	}

}
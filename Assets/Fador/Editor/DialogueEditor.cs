/* 
Copyright (c) 2018, Marko 'Fador' Viitanen
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright
      notice, this list of conditions and the following disclaimer in the
      documentation and/or other materials provided with the distribution.
    * Neither the name of the Marko Viitanen nor the
      names of its contributors may be used to endorse or promote products
      derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL MARKO VIITANEN BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class DialogueEditor :  EditorWindow
{


	public DialogueData dialogueData;
	public Vector2 scrollPos = Vector2.zero;

	private int maxHorizontal = 1000;
	private int maxVertical = 1000;

	private int connectFrom = -1;
	
	Dictionary<string, int> nodeIDs = new Dictionary<string, int>();
	List<KeyValuePair<int, int>> attachedWindows = new List<KeyValuePair<int, int>>();

	private string dialogueDataProjectFilePath = "/StreamingAssets/";
	private string dialogueDataProjectFileName = "data.json";

	[MenuItem ("Window/Dialogue Editor")]
	static void Init()
	{
		EditorWindow.GetWindow (typeof(DialogueEditor)).Show ();
		
	}

	void OnGUI()
	{
		GUILayout.BeginHorizontal("box");
		dialogueDataProjectFileName = GUILayout.TextField(dialogueDataProjectFileName);
		if (GUILayout.Button ("Load data"))
		{
			LoadGameData();
		}
		
		if (GUILayout.Button ("New Node"))
		{
			if (dialogueData == null) dialogueData = new DialogueData();
			CreateNode();
		}

		if (dialogueData != null)
		{
			if (GUILayout.Button ("Save data"))
			{
				SaveGameData();
			}
			
			if (GUILayout.Button ("Regen"))
			{
				updateDataStructures();
			}
			
			scrollPos = GUI.BeginScrollView(new Rect(0, 0, position.width, position.height), scrollPos,
				new Rect(0, 0, maxHorizontal, maxVertical));

			for (int i = 0; i < attachedWindows.Count; i++)
			{
				Rect start = dialogueData.nodes[attachedWindows[i].Key].pos;
				Rect end = dialogueData.endPos;
				if(attachedWindows[i].Value != dialogueData.nodes.Length) end = dialogueData.nodes[attachedWindows[i].Value].pos;
				ConnectNodesWithCurve(start, end);
			}

			BeginWindows();
			for (int i = 0; i < dialogueData.nodes.Length; i++)
			{
				dialogueData.nodes[i].pos = GUILayout.Window(i, dialogueData.nodes[i].pos, DrawNodeWindow, dialogueData.nodes[i].node);
			}

			dialogueData.endPos = GUILayout.Window(dialogueData.nodes.Length, dialogueData.endPos,DrawEndWindow, "END");
			EndWindows();
			// Close the scroll view
			GUI.EndScrollView();
		}
		GUILayout.EndHorizontal();
	}
	
	void DrawNodeWindow(int id)
	{
		// Resize buttons
		GUILayout.BeginHorizontal("box");
			if (GUILayout.Button("<-"))
			{
				dialogueData.nodes[id].pos.width -= 10f;
			}
			if (GUILayout.Button("->"))
			{
				dialogueData.nodes[id].pos.width += 10f;
			}
			if (GUILayout.Button("^"))
			{
				dialogueData.nodes[id].pos.height -= 10f;
			}
			if (GUILayout.Button("v"))
			{
				dialogueData.nodes[id].pos.height += 10f;
			}
		GUILayout.EndHorizontal();
		
		// Node name label and textfield
		GUILayout.BeginHorizontal("box");
			GUILayout.Label("Name");
	
			string oldName = dialogueData.nodes[id].node;
			
			dialogueData.nodes[id].node = GUILayout.TextField(dialogueData.nodes[id].node);
			if (oldName != dialogueData.nodes[id].node)
			{
				//ToDo: rename nodes
				Debug.Log("Old:" + oldName + " New:" + dialogueData.nodes[id].node);
			}
		GUILayout.EndHorizontal();
		
		// All different dialogue textareas, usually only one used
		for (int i = 0; i < dialogueData.nodes[id].dialogue.Length; i++) {
			dialogueData.nodes[id].dialogue[i] = GUILayout.TextArea(dialogueData.nodes[id].dialogue[i]);
		}
		
		// All the answers and target states
		for (int i = 0; i < dialogueData.nodes[id].answers.Length; i++) {
			GUILayout.BeginHorizontal("box");
			dialogueData.nodes[id].answers[i].option = GUILayout.TextField(dialogueData.nodes[id].answers[i].option);
			dialogueData.nodes[id].answers[i].target = GUILayout.TextField(dialogueData.nodes[id].answers[i].target);
			if (GUILayout.Button("X"))
			{
				ArrayUtility.RemoveAt(ref dialogueData.nodes[id].answers, i);
				updateDataStructures();
			}
			GUILayout.EndHorizontal();
		}
		
		// Button for adding new answer/target
		if (GUILayout.Button("+"))
		{
			ArrayUtility.Add(ref dialogueData.nodes[id].answers, new DialogueAnswerData());
			updateDataStructures();
		}
		
		// Easy connector buttons
		GUILayout.BeginHorizontal("box");
		{
			GUIStyle selectedStyle = new GUIStyle(GUI.skin.button);
			// Highlight the currently selected "from" field
			if (connectFrom == id)
			{
				selectedStyle.normal.textColor = Color.red;
			}

			if (GUILayout.Button("From ->", selectedStyle))
			{
				connectFrom = id;
			}

			if (GUILayout.Button("-> To"))
			{
				if (connectFrom != -1)
				{
					DialogueAnswerData newAnswer = new DialogueAnswerData();
					newAnswer.target = dialogueData.nodes[id].node;
					ArrayUtility.Add(ref dialogueData.nodes[connectFrom].answers, newAnswer);
				}

				updateDataStructures();
			}
		}
		GUILayout.EndHorizontal();
		
		
		// Expand main window to fit all the node windows
		if (dialogueData.nodes[id].pos.x + dialogueData.nodes[id].pos.width > maxHorizontal)
		{
			maxHorizontal = (int)(dialogueData.nodes[id].pos.x + dialogueData.nodes[id].pos.width);
		}
		if (dialogueData.nodes[id].pos.y + dialogueData.nodes[id].pos.height > maxVertical)
		{
			maxVertical = (int)(dialogueData.nodes[id].pos.y + dialogueData.nodes[id].pos.height);
		}
		GUI.DragWindow();
		
	}
	void DrawEndWindow(int id)
	{
		GUILayout.TextField("END");
		if (GUILayout.Button("-> To"))
		{
			if (connectFrom != -1)
			{
				DialogueAnswerData newAnswer = new DialogueAnswerData();
				newAnswer.target = "END";
				ArrayUtility.Add(ref dialogueData.nodes[connectFrom].answers, newAnswer);
			}
			
			updateDataStructures();
		}
		GUI.DragWindow();
	}
	
	void ConnectNodesWithCurve(Rect start, Rect end) {
		Vector3 startPosition = new Vector3(start.x + start.width, start.y + start.height / 2, 0);
		Vector3 endPosition = new Vector3(end.x, end.y + end.height / 2, 0);
		Vector3 startTangent = startPosition + Vector3.right * 20;
		Vector3 endTangent = endPosition + Vector3.left * 20;
 
		Handles.DrawBezier(startPosition, endPosition, startTangent, endTangent, Color.black, null, 5);
	}

	private void CreateNode()
	{
		ArrayUtility.Add(ref dialogueData.nodes, new DialogueNodeData());
		updateDataStructures();
	}

	private void LoadGameData()
	{
		string filePath = Application.dataPath + dialogueDataProjectFilePath+dialogueDataProjectFileName;

		if (File.Exists (filePath)) {
			string dataAsJson = File.ReadAllText (filePath);
			dialogueData = JsonUtility.FromJson<DialogueData> (dataAsJson);
			updateDataStructures();
		} else 
		{
			dialogueData = new DialogueData();
		}
	}

	private void SaveGameData()
	{

		string dataAsJson = JsonUtility.ToJson (dialogueData, true);

		string filePath = Application.dataPath + dialogueDataProjectFilePath+dialogueDataProjectFileName;
		File.WriteAllText (filePath, dataAsJson);

	}

	private void updateDataStructures()
	{
		nodeIDs.Clear();
		attachedWindows.Clear();
		for (int i = 0; i < dialogueData.nodes.Length; i++)
		{
			nodeIDs[dialogueData.nodes[i].node] = i;
			if (dialogueData.nodes[i].pos.x + dialogueData.nodes[i].pos.width > maxHorizontal)
			{
				maxHorizontal = (int)(dialogueData.nodes[i].pos.x + dialogueData.nodes[i].pos.width);
			}
			if (dialogueData.nodes[i].pos.y + dialogueData.nodes[i].pos.height > maxVertical)
			{
				maxVertical = (int)(dialogueData.nodes[i].pos.y + dialogueData.nodes[i].pos.height);
			}
		}
		nodeIDs["END"] = dialogueData.nodes.Length;
		for (int i = 0; i < dialogueData.nodes.Length; i++)
		{
			for (int ii = 0; ii < dialogueData.nodes[i].answers.Length; ii++)
			{
				if(nodeIDs.ContainsKey(dialogueData.nodes[i].answers[ii].target))
					attachedWindows.Add(new KeyValuePair<int, int>(i, nodeIDs[dialogueData.nodes[i].answers[ii].target]));
			}
		}
	}
}
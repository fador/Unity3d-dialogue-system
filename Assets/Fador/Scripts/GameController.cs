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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class GameController : MonoBehaviour
{

	enum DialogueState
	{
		CLOSED,
		OPEN
	}

	private DialogueController dialogueController;
	private DialogueNodeData dialogueNode;
	private DialogueState dialogueState = DialogueState.CLOSED;
	private static GameController gameController;
	private GameObject dialogueCanvas;

	protected GameController () {}
	
	public static GameController instance
	{
		get
		{
			if (!gameController)
			{
				gameController = FindObjectOfType (typeof (GameController)) as GameController;
	
				if (!gameController)
				{
					Debug.LogError ("Only one instance possible");
				}
				else
				{
					gameController.Init(); 
				}
			}

			return gameController;
		}
	}

	public void startDialogue(string nodeName)
	{
		dialogueState = DialogueState.OPEN;
		dialogueNode = dialogueController.getDialogue(nodeName);
		drawDialogue();
	}

	public void answerDialogue(int answer)
	{
		if (dialogueState == DialogueState.OPEN)
		{
			string nextNode = dialogueNode.answers[answer].target;
			if (nextNode == "END")
			{
				endDialogue();
				return;
			}
			dialogueNode = dialogueController.getDialogue(nextNode);
			drawDialogue();
		}
	}

	void drawDialogue()
	{
		
	  Text nameText = dialogueCanvas.transform.Find("DialoguePanel/Name").gameObject.GetComponent<Text>();
		Text dialogueText = dialogueCanvas.transform.Find("DialoguePanel/Dialogue").gameObject.GetComponent<Text>();

		dialogueText.text = dialogueNode.dialogue[0];
		nameText.text = dialogueNode.node;

		GameObject[] answers;
		answers = new GameObject[3];
		answers[0] = dialogueCanvas.transform.Find("DialoguePanel/Answer1").gameObject;
		answers[1] = dialogueCanvas.transform.Find("DialoguePanel/Answer2").gameObject;
		answers[2] = dialogueCanvas.transform.Find("DialoguePanel/Answer3").gameObject;
		for (int i = 0; i < 3; i++)
		{
			if (dialogueNode.answers.Length > i)
			{
				answers[i].SetActive(true);
				answers[i].transform.Find("Text").gameObject.GetComponent<Text>().text = dialogueNode.answers[i].option;
			}
			else
			{
				answers[i].SetActive(false);
			}
		}
		
		dialogueCanvas.SetActive(true);
	}
	
	void endDialogue()
	{
		dialogueCanvas.SetActive(false);
		dialogueState = DialogueState.CLOSED;
	}
	
	void Init ()
	{
		
	}

	void Start()
	{
		dialogueController = GetComponent<DialogueController>();
		dialogueCanvas = GameObject.Find("DialogueCanvas");
		if (dialogueCanvas == null)
		{
			throw new Exception("DialogueCanvas not found from scene");
		}
		dialogueCanvas.SetActive(false);
		
		// For the demo, just start with Duck_01 dialogue
		GameController.instance.startDialogue("Duck_01");
		Debug.Log("Start dialogue");
	}
}

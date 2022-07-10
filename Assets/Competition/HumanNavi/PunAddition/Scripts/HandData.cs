using UnityEngine;
using System;
using System.Collections;

namespace SIGVerse.Competition.HumanNavigation
{
	public class HandData : MonoBehaviour
	{
		public enum HandType
		{
			LeftHand,
			RightHand,
		}
		
		[HeaderAttribute ("Hand Type")]
		public HandType handType;

		[HeaderAttribute ("Debug Info")]
		public bool  handTriggerState;
		public float handTriggerValue;

		public bool   isInteracting;
		public string currentlyInteractingName; 
		public string currentlyInteractingTag; 

		public bool  handTriggerDownForModerator;
		public bool  handTriggerUpForModerator;

		public bool  nearButtonDownForModerator;
		public bool  farButtonDownForModerator;

		private bool shouldClear = false;

		public void SetHandData
		(
			bool   handTriggerState, 
			float  handTriggerValue, 
			bool   isInteracting, 
			string currentlyInteractingName, 
			string currentlyInteractingTag,
			bool   handTriggerDown,
			bool   handTriggerUp, 
			bool   nearButtonPressed,
			bool   farButtonPressed
		){
			this.handTriggerState = handTriggerState;
			this.handTriggerValue = handTriggerValue;

			this.isInteracting = isInteracting;
			this.currentlyInteractingName = currentlyInteractingName; 
			this.currentlyInteractingTag  = currentlyInteractingTag;

			if (handTriggerDown)  { this.handTriggerDownForModerator = true; }
			if (handTriggerUp)    { this.handTriggerUpForModerator   = true; }
			if (nearButtonPressed){ this.nearButtonDownForModerator  = true; }
			if (farButtonPressed) { this.farButtonDownForModerator   = true; }
		}

		public void ClearDataForModerator()
		{
			this.shouldClear = true;
		}

		void LateUpdate()
		{
			if(this.shouldClear)
			{
				this.handTriggerDownForModerator = false;
				this.handTriggerUpForModerator   = false;
				this.nearButtonDownForModerator  = false;
				this.farButtonDownForModerator   = false;

				this.shouldClear = false;
			}
		}
	}
}


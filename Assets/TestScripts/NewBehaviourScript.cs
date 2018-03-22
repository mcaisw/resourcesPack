using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class NewBehaviourScript : MonoBehaviour {
    List<string> newOne;
	// Use this for initialization
	void Start () {
        newOne = stringArray().ToList();
        foreach (var item in newOne)
        {
            Debug.Log(item);
        }
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    string[] stringArray() {
        return new string[3] {"1","2","3"};
    }
}

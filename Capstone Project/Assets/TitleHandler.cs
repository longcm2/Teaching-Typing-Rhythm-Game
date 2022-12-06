using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class TitleHandler : MonoBehaviour
{
    // These three game objects hold the input fields on the title screen.
    public GameObject lifeCountField;
    public GameObject backlogMaxField;
    public GameObject songNameField;

    //  Start is called before the first frame update
    void Start()
    {   
        // Empty -- no start code.
    }

    //  Update is called once per frame
    void Update()
    {
        // Empty -- no update code.
    }

    /* LoadGame()
     * LoadGame will set the proper PlayerPrefs to carry over the user's choices and then
     * load the main game scene.
     *
     * @param - nothing
     * @return - void
     */
    public void LoadGame()
    {
        PlayerPrefs.SetInt("lives", int.Parse(lifeCountField.GetComponentInChildren<TMP_InputField>().text)); // Takes user input and places it into the life variable.
        PlayerPrefs.SetInt("backlogMax", int.Parse(backlogMaxField.GetComponentInChildren<TMP_InputField>().text)); // Takes user input and places it into the backlogLim variable.
        PlayerPrefs.SetString("songName", songNameField.GetComponentInChildren<TMP_InputField>().text);
        SceneManager.LoadScene("Main");
    }
}
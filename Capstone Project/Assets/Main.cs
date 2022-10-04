using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using TMPro;

public class Main : MonoBehaviour
{
    public AudioSource enigma;
    public GameObject buttonPrefab;
    public GameObject panelToAttachButtonsTo;
    public Queue<GameObject> Notes = new Queue<GameObject>();
    public Queue<GameObject> Lyrics = new Queue<GameObject>();

    //  Start is called before the first frame update
    void Start()
    {
        // Loads and plays the audio file.
        enigma = GetComponent<AudioSource>();
        enigma.Play();

        loadNotes();
    }

    //  Update is called once per frame
    void Update()
    {
        // If space is pressed, hide the first note and make the next one active.
        if (Input.GetKeyDown(KeyCode.Space)) {
            Notes.Dequeue().SetActive(false);
            Lyrics.Dequeue().SetActive(false);
            Notes.Peek().SetActive(true);
            Lyrics.Peek().SetActive(true);
        }
    }

    ///  This function is called in Start(). It loads every note and handles their deletion.
    void loadNotes()
    {
        double prevoffset = 15.5; // The initial offset -- hard coded for now, will be song-specific in the future.

        // Reads and parses the given .lt file.
        using (StreamReader sr = File.OpenText("Assets/enigma.lt"))
        {
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                // Calculates the offset from the start.
                string offset = Regex.Replace(line, "\\{\\\"onset\\\"\\: ", "");
                offset = Regex.Replace(offset, "\\, \\\"duration(.)*", "");
                double diff = (double.Parse(offset) / 1000000000) - prevoffset;
                
                // Calculates the duration of the note; the grace period is calculated by adding before and after this number.
                string lengthString = Regex.Replace(line, "(.)*duration\\\"\\: ", "");
                double length = double.Parse(Regex.Replace(offset, "\\, \\\"lyrics", "")) / 1000000000;

                // Parses the lyric from the string.
                string lyric = Regex.Replace(line, "(.)*lyrics\\\"\\: ", "");
                lyric = Regex.Replace(lyric, "\\\"\\, \\\"phonemes(.)*", "");

                // Parses the pitch from the string.
                // TODO

                // Used to determine when a lyric/note should be hidden.
                int i = 0;

                // This is a subroutine, somewhat like a lambda function, that handles the note creation and queueing.
                IEnumerator CreateNote(string lyric_, int i)
                {
                    yield return new WaitForSeconds((int) diff); //  Tells this coroutine to hang on for a bit

                    // Instantiate note and lyric
                    GameObject note = Instantiate(Resources.Load("note", typeof(GameObject))) as GameObject;
                    GameObject lyric = Instantiate(Resources.Load("lyric", typeof(GameObject))) as GameObject;

                    lyric.GetComponent<TextMeshProUGUI>().SetText(lyric_);

                    //temp.text = lyric_; //  Attempts to change the text in the lyric object
                    
                    // Adds the gameobjectsto their respective queues
                    Notes.Enqueue(note);
                    Lyrics.Enqueue(lyric);

                    // Hides the note so that the Delete function can awaken it later.
                    note.SetActive(false);
                    lyric.SetActive(false);
                    // If the first note, hide this.
                    if (i == 0) {
                        note.SetActive(true);
                        lyric.SetActive(true);
                    }

                    // Assigns the objects to a canvas so they can be displayed
                    note.transform.SetParent(panelToAttachButtonsTo.transform);
                    lyric.transform.SetParent(panelToAttachButtonsTo.transform);
                }

                StartCoroutine(CreateNote(lyric, i)); //  Starts the subroutine to display and control the note

                i++; // Increment i so that only the first note is unhidden ever.
            }
        } 
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;
using TMPro;

public class Main : MonoBehaviour
{
    public AudioSource enigma;
    public GameObject buttonPrefab;
    public GameObject panelToAttachButtonsTo;
    public Queue<GameObject> Notes = new Queue<GameObject>();
    public Queue<GameObject> Lyrics = new Queue<GameObject>();
    public bool active;
    public int health = 2;

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
        if (active && Input.GetKeyDown(KeyCode.Space)) {
            Notes.Dequeue().SetActive(false);
            Lyrics.Dequeue().SetActive(false);
            if (Notes.Count != 0) {
                Notes.Peek().SetActive(true);
                Lyrics.Peek().SetActive(true);
            }
            else {
                active = false;
            }
        }

        if (!active && Notes.Count != 0) {
            Notes.Peek().SetActive(true);
            Lyrics.Peek().SetActive(true);
            active = true;
        }

        if (health == 0) {
            // SceneManager.LoadScene("GameOver");
        }
    }

    ///  This function is called in Start(). It loads every note and handles their deletion.
    void loadNotes()
    {
        double prevoffset = 0; // The initial offset -- hard coded for now, will be song-specific in the future.
        double scaleFactor = 0.47; // The scale factor -- kinda like a tempo.

        // Reads and parses the given .lt file.
        using (StreamReader sr = File.OpenText("Assets/enigma.lt"))
        {
            string line;

            // Used to determine when a lyric/note should be hidden.
            int noteCount = 0;

            while ((line = sr.ReadLine()) != null)
            {
                if (!line.Contains("+")) {
                    // Calculates the offset from the start.
                    string offset = Regex.Replace(line, "\\{\\\"onset\\\"\\: ", "");
                    offset = Regex.Replace(offset, "\\, \\\"duration(.)*", "");
                    double diff = (double.Parse(offset) / 1000000000) - prevoffset;
                    diff = diff * scaleFactor;
                    
                    // Calculates the duration of the note; the grace period is calculated by adding before and after this number.
                    string lengthString = Regex.Replace(line, "(.)*duration\\\"\\: ", "");
                    double length = double.Parse(Regex.Replace(offset, "\\, \\\"lyrics", "")) / 1000000000;
                    length = length * scaleFactor;

                    // Parses the lyric from the string.
                    string lyric = Regex.Replace(line, "(.)*lyrics\\\"\\: \\\"", "");
                    lyric = Regex.Replace(lyric, "\\\"\\, \\\"phonemes(.)*", "");

                    // Parses the pitch from the string.
                    // TODO

                    // This is a subroutine, somewhat like a lambda function, that handles the note creation and queueing.
                    IEnumerator CreateNote(string lyric_, int noteCount)
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
                        // If the first note, hide this. Set the active boolean to true.
                        if (noteCount == 0) {
                            note.SetActive(true);
                            lyric.SetActive(true);
                            active = true;
                        }

                        // Assigns the objects to a canvas so they can be displayed
                        note.transform.SetParent(panelToAttachButtonsTo.transform);
                        lyric.transform.SetParent(panelToAttachButtonsTo.transform);

                        yield return new WaitForSeconds(3); //  Tells this coroutine to hang on for a bit


                        // Sets the health.
                        if (note.activeSelf) {
                            health--;
                        }
                    }

                    StartCoroutine(CreateNote(lyric, noteCount)); //  Starts the subroutine to display and control the note

                    noteCount++; // Increment i so that only the first note is unhidden ever.
                }
            }
        } 
    }
}
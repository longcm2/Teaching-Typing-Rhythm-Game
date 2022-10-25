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
    public AudioSource enigma; // Holds the music.
    public GameObject canvas; // Holds the canvas to attach created prefabs to.
    public Queue<GameObject> Lyrics = new Queue<GameObject>(); // Holds the lyric to be displayed.
    public GameObject lifeCount; // Holds the display text for the number of lifes.
    public GameObject input; // Holds the input box.
    public GameObject gameOver; // Holds the game over picture.
    public GameObject winScreen; //Holds the game win picture.
    public GameObject backlog; // Holds the display text for the number of notes in the queue.
    public GameObject noteBack; // The square behind the lyric.
    public bool active; // Used to determine which branch to take in Update().
    public bool gameWin = false; // A somewhat finnicky variable used to tell if the game has been won by use of multiple race conditions -- should likely be replaced by a song specific check.
    public int life; // Holds the number of lives

    //  Start is called before the first frame update
    void Start()
    {   
        life = 5; // Initializes the number of lives to 5.

        // Plays the audio file.
        enigma.Play();

        loadNotes(); // Calls the function that reads an .lt file.
    }

    //  Update is called once per frame
    void Update()
    {
        input.GetComponent<TMP_InputField>().ActivateInputField(); // Makes the text field active.

        /* This if statement block first ensures that there is an active note. It then will use a comparison to determine if the text
         * in the input field is the same as the text in the currently displayed lyric. If it is, then it will destroy the lyric and
         * potentially display the new one if there is another note in the queue.
         */
        if (active && Lyrics.Count != 0) {
            if (checkUserInput()) {
                input.GetComponentInChildren<TMP_InputField>().text = ""; // Clears the input field.
                noteBack.SetActive(false);
                Destroy(Lyrics.Dequeue());
                if (Lyrics.Count != 0) {
                    showNote();
                }
                else {
                    active = false;
                }
            }
        }

        // This block of code is executed if there isn't an active lyric yet there are some in the queue.
        if (!active && Lyrics.Count != 0) {
            showNote();
        }

        // If there are no notes in the queue, we await for more and hide the note backing.
        if (Lyrics.Count == 0) {
            noteBack.SetActive(false);
        }

        // This block of code changes the color of the backlog number depending on the severity and closeness to 25, the highest number it can be before a life is lost.
        if (Lyrics.Count < 10) {
            backlog.GetComponent<TextMeshProUGUI>().color = new Color (255, 255, 255); // white
        }
        if (Lyrics.Count >= 10) {
            backlog.GetComponent<TextMeshProUGUI>().color = new Color (255, 255, 0); // yellow
        }
        if (Lyrics.Count >= 15) {
            backlog.GetComponent<TextMeshProUGUI>().color = new Color (255, 127, 0); // orange
        }
        if (Lyrics.Count >= 20) {
            backlog.GetComponent<TextMeshProUGUI>().color = new Color (255, 0, 0); // red
        }
        
        // If there are more than 25 lyrics in the backlog, a life is lost. 25 notes are removed from the queue,
            // and if there is a new note in the queue after those removals, it is activated.
        if (Lyrics.Count > 25) {
            updateLife();
            for (int i = 0; i < 25; i++) {
                Destroy(Lyrics.Dequeue());
            }

            if (Lyrics.Count != 0) {
                showNote();
            }
            else {
                noteBack.SetActive(false);
                active = false;
            }
        }

        backlog.GetComponent<TextMeshProUGUI>().SetText(Lyrics.Count.ToString()); // This line of code updates the actual backlog number.

        // This block of code handles the fail case, where all the lives have been exhausted. The music is stopped and the fail screen is activated.
        if (life == 0) {
            enigma.Stop();
            gameOver.SetActive(true);
            gameWin = false;
            quitGame();
        }

        // This code handles the win condition. The music isn't stopped here, since I'd prefer to let it play out to the end.
        if (gameWin && Lyrics.Count == 0) {
            winScreen.SetActive(true);
            quitGame();
        }
    }

    /* loadNotes
     * 
     * This function is called in Start(). It parses every note and handles their loading, along with
     * their timing. It also handles activation in the case of the first note.
     * 
     * gameWin is modified often in this function -- this is intentional. Any time a potential continuation
     * to the game is presented, gameWin is set to false. Ten seconds after every note finishes, gameWin is
     * set to true. This will likely need to be replaced with a song-specific wait check that checks the
     * status of the GameOver object after a certain number of seconds, as it will fail for certain songs
     * with very long gaps between verses.
     * 
     * @param - none
     * @return - none
     */
    void loadNotes()
    {
        double startDelay = -0.1; // The initial offset -- hard coded for now, will be song-specific in the future. By advancing the notes forward a small bit, both
                                            // mental and physical lag are considered.
        double scaleFactor = 0.5; // The scale factor -- this is used to account for the tempo which is independent from the ns listed in the .lt file.

        // This block of code reads and parses the .lt file.
        using (StreamReader sr = File.OpenText("Assets/Resources/enigma.lt"))
        {
            string line;

            while ((line = sr.ReadLine()) != null)
            {
                if (!line.Contains("+") && !line.Contains("-")) {
                    // Calculates the offset from the start.
                    string offset = Regex.Replace(line, "\\{\\\"onset\\\"\\: ", ""); // Removes everything before the offset.
                    offset = Regex.Replace(offset, "\\, \\\"duration(.)*", ""); // Removes everything after the offset.
                    double diff = (double.Parse(offset) / 1000000000); // Turns the string representation of offset into a double and converts it from nanoseconds into seconds. 
                    diff = (diff * scaleFactor) + startDelay; // Scales and delays the diff.
                    

                    // Calculates the duration of the note -- while I do not have any plans for this data currently, the code for it is useful.
                    // string lengthString = Regex.Replace(line, "(.)*duration\\\"\\: ", "");
                    // double length = double.Parse(Regex.Replace(offset, "\\, \\\"lyrics", "")) / 1000000000;
                    // length = length * scaleFactor;

                    // Parses the lyric from the line.
                    string lyric = Regex.Replace(line, "(.)*lyrics\\\"\\: \\\"", ""); // Removes everything before the lyric.
                    lyric = Regex.Replace(lyric, "\\\"\\, \\\"phonemes(.)*", ""); // Removes everything after the lyric.

                    // This is a subroutine, somewhat like a lambda function, that handles the note creation and queueing.
                    IEnumerator createNote(string lyric_, double diff_)
                    {
                        gameWin = false;
                        yield return new WaitForSeconds((float) diff_); //  Tells this coroutine to hang on for a bit. gameWin is set to false before and after it
                                                                            // to minimize chances of a false positive.
                        gameWin = false;

                        GameObject lyric = Instantiate(Resources.Load("lyric", typeof(GameObject))) as GameObject; // Creates a GameObject based on the "lyric" prefab.

                        lyric.GetComponent<TextMeshProUGUI>().SetText(lyric_); // Sets the text of that lyric object.
                        
                        Lyrics.Enqueue(lyric);

                        // Hides the note so that the Update function can awaken it later.
                        lyric.SetActive(false);

                        // Assigns the lyric to the canvas so it can be displayed.
                        lyric.transform.SetParent(canvas.transform);

                        yield return new WaitForSeconds(10); // Tells the coroutine to wait 10 more seconds before setting gameWin to true.
                            gameWin = true;
                    }

                    StartCoroutine(createNote(lyric, diff)); // Starts the above subroutine to display and control the note.
                }
            }
        }
    }

    /* checkUserInput 
     *
     * This function is called by update to determine if a note needs to progress or not. It was moved here to keep things clean.
     * 
     * @param - none
     * @return - bool indicating if the user's input matches the current lyric's string
     */
    bool checkUserInput() {
        return Lyrics.Peek().GetComponent<TextMeshProUGUI>().text.Trim().Equals // The lyric's text field is obtained and then trimmed before applying the Equals() function to another string.
                    (input.GetComponentInChildren<TextMeshProUGUI>().text.Trim(). // This string is pulled from the input field, also being trimmed.
                            Remove(input.GetComponentInChildren<TextMeshProUGUI>().text.Trim().Length - 1, 1)); // I have added a manual trim of 1 character off the end because
                                                                                                                    // the input field, for some reason, consistently had an extra
                                                                                                                    // empty spot at the end of its string which ruined the comparison.
    }

    /* updateLife
     * 
     * This function is called to decrement life and reflect that in the displayed life counter.
     * 
     * @param - none
     * @return - none
     */
    void updateLife() {
        life--;
        lifeCount.GetComponent<TextMeshProUGUI>().SetText(life.ToString());
    }

    /* showNote
     * 
     * This function is called to show a note if the queue isn't empty. It was moved here to 
     * avoid code repetition.
     * 
     * @param - none
     * @return - none
     */
    void showNote() {
        noteBack.SetActive(true);
        Lyrics.Peek().SetActive(true);
        active = true;
    }

    /* quitGame
     * 
     * This couroutine is called to leave the game in the event of a win or loss. It waits 10 seconds then quits.
     * 
     * @param - none
     * @return - none
     */
    IEnumerator quitGame() {
        yield return new WaitForSeconds(10);
        Application.Quit();
    }
}
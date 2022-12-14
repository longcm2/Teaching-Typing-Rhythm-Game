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
using System.Text.RegularExpressions;
using UnityEngine.Networking;  // UnityWebRequest 
using TMPro;

public class GameHandler : MonoBehaviour
{
    public AudioSource gameAudio; // Holds the music.
    public GameObject mainGame; // Holds the canvas to attach created prefabs to.
    public Queue<GameObject> Lyrics = new Queue<GameObject>(); // Holds the lyric to be displayed.
    public GameObject wynnDefault; // Wynn's default animation
    public GameObject wynnSing; // Wynn's sing animation
    public GameObject marieDefault; // Marie's default image
    public GameObject marieSing; // Marie's sing animation
    public GameObject lifeField; // Holds the display text for the number of lifes.
    public GameObject input; // Holds the input box.
    public GameObject gameOver; // Holds the game over picture.
    public GameObject winScreen; //Holds the game win picture.
    public GameObject backlogField; // Holds the display text for the number of notes in the queue.
    public GameObject powerupField; // Holds the display text for the power count
    public GameObject noteBack; // The square behind the lyric.
    public GameObject songTitle;
    public GameObject songArtist;

    // Internal variables
    public bool active; // Used to determine which branch to take in Update() in terms of queue management.
    public bool gameWin = false; // Is set to true after the last note has been played
    public int life = 5; // Chose by user
    public int backlogMax = 25; // Chose by user
    public double endTime = 999; // Is eventually set to the launch time of the last note.
    public int powerupCount = 0; // 0-100 based on user input throughout the game
    public int wynnSingAnim = 0; // 0 if wynn should be static, above otherwise
    public int marieSingAnim = 0; // 0 if the marie should be static, above otherwise

    //  Start is called before the first frame update
    void Start()
    {   
        life = PlayerPrefs.GetInt("lives"); // Takes user input and places it into the life variable.
        backlogMax = PlayerPrefs.GetInt("backlogMax"); // Takes user input and places it into the backlogLim variable.
        LoadNotes(PlayerPrefs.GetString("songName")); // Calls the main loading function, effectively "starting" the game.
    }

    //  Update is called once per frame
    void Update()
    {
        input.GetComponent<TMP_InputField>().ActivateInputField(); // Makes the text field active every single frame.

        ProcessAnimations(); // Processes animation code.

        //This block checks if the powerup is available, setting the powerup counter color
        // to a vibrant teal if it does. If the user presses the ` key, it clears the entire
        // queue and resets the count to zero.
        if (powerupCount >= 98) {
            powerupField.GetComponent<TextMeshProUGUI>().color = new Color (0, 255, 255);
            if (Input.GetKeyDown(KeyCode.BackQuote)) {
                powerupCount = 0;
                input.GetComponentInChildren<TMP_InputField>().text = ""; // Clears the input field.
                for (int i = 0; i < Lyrics.Count; i++) {
                    Destroy(Lyrics.Dequeue());
                }

                if (Lyrics.Count != 0) {
                    showNote();
                }
                else {
                    noteBack.SetActive(false);
                    active = false;
                }

                powerupCount = 0;
            }
        }
        // If the powerupCount is 75 or greater, set the color to blue-green.
        else if (powerupCount > 74) {
            powerupField.GetComponent<TextMeshProUGUI>().color = new Color (0, 127, 127);
        }
        // Otherwise, set it to white.
        else {
            powerupField.GetComponent<TextMeshProUGUI>().color = new Color (255, 255, 255);
        }

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
        
        // If there are more than 25 lyrics in the backlog, a life is lost. 25 notes are removed from the queue,
            // and if there is a new note in the queue after those removals, it is activated.
        if (Lyrics.Count > backlogMax) {
            life--;
            input.GetComponentInChildren<TMP_InputField>().text = ""; // Clears the input field.
            for (int i = 0; i < backlogMax; i++) {
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

        ProcessBacklogTextColors();

        // This block of code handles the fail case, where all the lives have been exhausted. The music is stopped and the fail screen is activated.
        if (life == 0) {
            gameAudio.Stop();
            gameOver.SetActive(true);
            gameWin = false;
            quitGame();
        }

        // This code handles the win condition. The music isn't stopped here, since I'd prefer to let it play out to the end.
        if (gameWin && Lyrics.Count == 0) {
            winScreen.SetActive(true);
            quitGame();
        }

        UpdateGUIVariables();
    }

    /* UpdateGUIVariables
     * 
     * Updates every text object in the UI made to display a certain variable.
     * 
     * @param - none
     * @return - none
     */
    void UpdateGUIVariables() {
        backlogField.GetComponent<TextMeshProUGUI>().SetText(Lyrics.Count.ToString()); // This line of code updates the actual backlog number.
        powerupField.GetComponent<TextMeshProUGUI>().SetText(powerupCount.ToString());
        lifeField.GetComponent<TextMeshProUGUI>().SetText(life.ToString());
    }

    /* ProcessAnimations
     * 
     * Handles all user and enemy animations. Moved here from Update().
     * 
     * @param - none
     * @return - none
     */
    void ProcessAnimations() {
        // If the user typed, change the wynn's animation.
        if (Input.anyKey) {
            if (Time.frameCount % 10 == 0 && powerupCount != 100) powerupCount++;
            wynnSingAnim++;
        }
        else {
            if (Time.frameCount % 10 == 0 && powerupCount > 0) powerupCount--;
        }

        // If there isn't any reason for the singing animation to be playing, stop it. Otherwise, play it.
        if (wynnSingAnim == 0) {
            wynnDefault.SetActive(true);
            wynnSing.SetActive(false);
        }
        else {
            wynnDefault.SetActive(false);
            wynnSing.SetActive(true);
        }
        if (marieSingAnim == 0) {
            marieDefault.SetActive(true);
            marieSing.SetActive(false);
        }
        else {
            marieDefault.SetActive(false);
            marieSing.SetActive(true);
        }

        // Disable the animations every half second -- gives them movement and life.
        if (Time.frameCount % 30 == 0) {
            wynnSingAnim = 0;
            marieSingAnim = 0;
        }
    }

    /* ProcessBacklogTextColors
     * 
     * Changes the color of the backlog counter based on the current
     * backlog percentage capacity. Moved here from Update() to clean it up.
     * 
     * @param - none
     * @return - none
     */
    void ProcessBacklogTextColors() {
        // This block of code changes the color of the backlog number depending on the severity and closeness to 25, the highest number it can be before a life is lost.
        if (Lyrics.Count < (0.5 * backlogMax)) {
            backlogField.GetComponent<TextMeshProUGUI>().color = new Color (255, 255, 255); // white
        }
        if (Lyrics.Count >= 0.5 * backlogMax) {
            backlogField.GetComponent<TextMeshProUGUI>().color = new Color (255, 255, 0); // yellow
        }
        if (Lyrics.Count >= 0.75 * backlogMax) {
            backlogField.GetComponent<TextMeshProUGUI>().color = new Color (255, 127, 0); // orange
        }
        if (Lyrics.Count >= 0.90 * backlogMax) {
            backlogField.GetComponent<TextMeshProUGUI>().color = new Color (255, 0, 0); // red
        }
    }

    /* LoadClip
     * 
     * This function is used to load the audio file from the user computer. The physical path is made
     * in the Update() function under the titleScreen section.
     * 
     * Unity has no easy way of loading audio from a file; therefore, the web service is used to
     * request a local file, similarly to opening a PDF or text file in a modern web browser.
     * 
     * @param - path: the path of the audio file to load.
     * @return - Task<AudioClip>: a task that will return an AudioClip after it's finished running.
     */
    async Task<AudioClip> LoadClip(string path) {
        AudioClip clip = null;
        using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.WAV)) {
            uwr.SendWebRequest();

            try {
                while (!uwr.isDone) await Task.Delay(5); // Try to load the file every few seconds.

                if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError) {
                    Debug.Log($"{uwr.error}"); // Log the error message.
                }
                else {
                    clip = DownloadHandlerAudioClip.GetContent(uwr); // Set clip equal to the loaded content.
                }
            }
            catch (Exception err) {
                Debug.Log($"{err.Message}, {err.StackTrace}"); // Log the error message.
            }
        }

        return clip;
    }

    /* LoadNotes
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
    async void LoadNotes(string songName)
    {
        string path = Application.dataPath + "/../songs/" + songName + ".wav"; // Build the path -- should be platform inclusive.

        // Loads the audio -- asynchronous since it can take a moment.
        AudioClip loadedAudio = await LoadClip(path);

        gameAudio.clip = loadedAudio;

        double startDelay = 0.5; // The initial offset -- hard coded for now, will be song-specific in the future. By advancing the notes forward a small bit, both
                                            // mental and physical lag are considered.
        double scaleFactor = 0.5; // The scale factor -- this is used to account for the tempo which is independent from the ns listed in the .lt file.

        // This block of code reads and parses the .lt file.
        using (StreamReader sr = File.OpenText("songs/" + songName + ".lt"))
        {
            // Pulls the song title and artist from the first two lines.
            songTitle.GetComponent<TextMeshProUGUI>().SetText(sr.ReadLine());
            songArtist.GetComponent<TextMeshProUGUI>().SetText(sr.ReadLine());

            // Grabs the numbers for the start delay and scale factor from the next two lines.
            startDelay = double.Parse(sr.ReadLine());
            scaleFactor = 0.947368 - (0.00263158 * double.Parse(sr.ReadLine())); // A formula to convert from tempo to scale.

            // Pulls each lyric from the rest of the file.
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
                        yield return new WaitForSeconds((float) diff_); //  Tells this coroutine to hang on for a bit.

                        GameObject lyric = Instantiate(Resources.Load("lyric", typeof(GameObject))) as GameObject; // Creates a GameObject based on the "lyric" prefab.

                        lyric.GetComponent<TextMeshProUGUI>().SetText(lyric_); // Sets the text of that lyric object.
                        
                        Lyrics.Enqueue(lyric);

                        marieSingAnim++; // Enable the marie animation.

                        // Hides the note so that the Update function can awaken it later.
                        lyric.SetActive(false);

                        // Assigns the lyric to the canvas so it can be displayed.
                        lyric.transform.SetParent(mainGame.transform);
                    }

                    endTime = diff; // Sets the endtime to the most recent note.

                    StartCoroutine(createNote(lyric, diff)); // Starts the above subroutine to display and control the note.
                }
            }
        }

        gameAudio.Play();

        // Sets gameWin to true after waiting for the last note to enter the queue.
        IEnumerator endTimer(double diff_)
        {
            yield return new WaitForSeconds((float) diff_);
            gameWin = true;
        }
        
        StartCoroutine(endTimer(endTime));
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
     * This function is called to leave the game in the event of a win or loss. It waits 10 seconds then quits.
     * 
     * @param - none
     * @return - none
     */
    void quitGame() {
        StartCoroutine(waitForSecondsThenQuit(10));

        // A coroutine to wait to let the player wallow in their loss (or win).
        IEnumerator waitForSecondsThenQuit(int i) {
            yield return new WaitForSeconds(i);
            Application.Quit(); // Only works in compiled builds.
        }
    }
}
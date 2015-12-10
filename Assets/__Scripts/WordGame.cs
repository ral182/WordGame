using UnityEngine;
using System.Collections;
using System.Collections.Generic; // We'll be using List<> & Dictionary<>
using System.Linq; // We'll be using LINQ
public enum GameMode {
	preGame, // Before the game starts
	loading, // The word list is loading and being parsed
	makeLevel, // The individual WordLevel is being created
	levelPrep, // The level visuals are Instantiated
	inLevel // The level is in progress
}
public class WordGame : MonoBehaviour {
	static public WordGame S; // Singleton

	public GameObject prefabLetter;
	public Rect wordArea = new Rect(-24,19,48,28);
	public float letterSize = 1.5f;
	public bool showAllWyrds = true;
	public float bigLetterSize = 4f;
	public Color bigColorDim = new Color(0.8f, 0.8f, 0.8f);
	public Color bigColorSelected = Color.white;
	public Vector3 bigLetterCenter = new Vector3(0, -16, 0);
	public List<float> scoreFontSizes = new List<float> { 24, 36, 36, 1 };
	public Vector3 scoreMidPoint = new Vector3(1,1,0);
	public float scoreComboDelay = 0.5f;
	public Color[] wyrdPalette;

	public bool ________________;
	public GameMode mode = GameMode.preGame;
	public WordLevel currLevel;
	public List<Wyrd> wyrds;
	public List<Letter> bigLetters;
	public List<Letter> bigLettersActive;
	public string testWord;
	private string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

	void Awake() {
		S = this; // Assign the singleton
	}
	void Start () {
		mode = GameMode.loading;
		// Tells WordList.S to start parsing all the words
		WordList.S.Init();
	}
	// Called by the SendMessage() command from WordList
	public void WordListParseComplete() {
		mode = GameMode.makeLevel;
		// Make a level and assign it to currLevel, the current WordLevel
		currLevel = MakeWordLevel();
	}
	// With the default value of -1, this method will generate a level from
	// a random word.
	public WordLevel MakeWordLevel(int levelNum = -1) {
		WordLevel level = new WordLevel();
		if (levelNum == -1) {
			// Pick a random level
			level.longWordIndex = Random.Range(0,WordList.S.longWordCount);
		} else {
			// This can be added later
		}
		level.levelNum = levelNum;
		level.word = WordList.S.GetLongWord(level.longWordIndex);
		level.charDict = WordLevel.MakeCharDict(level.word);
		// Call a coroutine to check all the words in the WordList and see
		// whether each word can be spelled by the chars in level.charDict
		StartCoroutine( FindSubWordsCoroutine(level) );
		// This returns the level before the coroutine finishes, so
		// SubWordSearchComplete() is called when the coroutine is done
		return( level );
	}
	// A coroutine that finds words that can be spelled in this level
	public IEnumerator FindSubWordsCoroutine(WordLevel level) {
		level.subWords = new List<string>();
		string str;
		List<string> words = WordList.S.GetWords();
		// ^ This is very fast because List<string> is passed by reference
		// Iterate through all the words in the WordList
		for (int i=0; i<WordList.S.wordCount; i++) {
			str = words[i];
			// Check whether each one can be spelled using level.charDict
			if (WordLevel.CheckWordInLevel(str, level)) {
				level.subWords.Add(str);
			}
			// Yield if we've parsed a lot of words this frame
			if (i%WordList.S.numToParseBeforeYield == 0) {
				// yield until the next frame
				yield return null;
			}
		}
		// List<string>.Sort() sorts alphabetically by default
		level.subWords.Sort ();
		// Now sort by length to have words grouped by number of letters
		level.subWords = SortWordsByLength(level.subWords).ToList();
		// The coroutine is complete, so call SubWordSearchComplete()
		SubWordSearchComplete();
	}
	public static IEnumerable<string> SortWordsByLength(IEnumerable<string> e)
	{
		// Use LINQ to sort the array received and return a copy
		// The LINQ syntax is different from regular C# and is beyond
		// the scope of this book // 1
		var sorted = from s in e
			orderby s.Length ascending
				select s;
		return sorted;
	}
	public void SubWordSearchComplete() {
		mode = GameMode.levelPrep;
		Layout(); // Call the Layout() function after SubWordSearch
	}
	void Layout() {
		// Place the letters for each subword of currLevel on screen
		wyrds = new List<Wyrd>();
		// Declare a lot of variables that will be used in this method
		GameObject go;
		Letter lett;
		string word;
		Vector3 pos;
		float left = 0;
		float columnWidth = 3;
		char c;
		Color col;
		Wyrd wyrd;
		// Determine how many rows of Letters will fit on screen
		int numRows = Mathf.RoundToInt(wordArea.height/letterSize);
		// Make a Wyrd of each level.subWord
		for (int i=0; i<currLevel.subWords.Count; i++) {
			wyrd = new Wyrd();
			word = currLevel.subWords[i];
			// if the word is longer than columnWidth, expand it
			columnWidth = Mathf.Max( columnWidth, word.Length );
			// Instantiate a PrefabLetter for each letter of the word
			for (int j=0; j<word.Length; j++) {
				c = word[j]; // Grab the jth char of the word
				go = Instantiate(prefabLetter) as GameObject;
				lett = go.GetComponent<Letter>();
				lett.c = c; // Set the c of the Letter
				// Position the Letter
				pos = new Vector3(wordArea.x+left+j*letterSize, wordArea.y, 0);
				// The % here makes multiple columns line up
				pos.y -= (i%numRows)*letterSize;
				lett.pos = pos;
				go.transform.localScale = Vector3.one*letterSize;
				wyrd.Add(lett);
			}
			if (showAllWyrds) wyrd.visible = true; // This line is for testing

			// Color the wyrd based on length
			wyrd.color = wyrdPalette[word.Length-WordList.S.wordLengthMin];

			wyrds.Add(wyrd);
			// If we've gotten to the numRows(th) row, start a new column
			if (i%numRows == numRows-1) {
				left += (columnWidth+0.5f)*letterSize;
			}
		}

		// Place the big letters
		// Initialize the List<>s for big Letters
		bigLetters = new List<Letter>();
		bigLettersActive = new List<Letter>();
		// Create a big Letter for each letter in the target word
		for (int i=0; i<currLevel.word.Length; i++) {
			// This is similar to the process for a normal Letter
			c = currLevel.word[i];
			go = Instantiate(prefabLetter) as GameObject;
			lett = go.GetComponent<Letter>();
			lett.c = c;
			go.transform.localScale = Vector3.one*bigLetterSize;
			// Set the initial position of the big Letters below screen
			pos = new Vector3( 0, -100, 0 );
			lett.pos = pos;

			// Increment lett.timeStart to have big Letters come in last
			lett.timeStart = Time.time + currLevel.subWords.Count*0.05f;
			lett.easingCurve = Easing.Sin+"-0.18"; // Bouncy easing

			col = bigColorDim;
			lett.color = col;
			lett.visible = true; // This is always true for big letters
			lett.big = true;
			bigLetters.Add(lett);
		}
		// Shuffle the big letters
		bigLetters = ShuffleLetters(bigLetters);
		// Arrange them on screen
		ArrangeBigLetters();
		// Set the mode to be in-game
		mode = GameMode.inLevel;
	}
	// This shuffles a List<Letter> randomly and returns the result
	List<Letter> ShuffleLetters(List<Letter> letts) {
		List<Letter> newL = new List<Letter>();
		int ndx;
		while(letts.Count > 0) {
			ndx = Random.Range(0,letts.Count);
			newL.Add(letts[ndx]);
			letts.RemoveAt(ndx);
		}
		return(newL);
	}
	// This arranges the big Letters on screen
	void ArrangeBigLetters() {
		// The halfWidth allows the big Letters to be centered
		float halfWidth = ( (float) bigLetters.Count )/2f-0.5f;
		Vector3 pos;
		for (int i=0; i<bigLetters.Count; i++) {
			pos = bigLetterCenter;
			pos.x += (i-halfWidth)*bigLetterSize;
			bigLetters[i].pos = pos;
		}
		// bigLettersActive
		halfWidth = ( (float) bigLettersActive.Count )/2f-0.5f;
		for (int i=0; i<bigLettersActive.Count; i++) {
			pos = bigLetterCenter;
			pos.x += (i-halfWidth)*bigLetterSize;
			pos.y += bigLetterSize*1.25f;
			bigLettersActive[i].pos = pos;
		}
	}
	void Update() {
		// Declare a couple of useful local variables
		Letter lett;
		char c;
		switch (mode) {
		case GameMode.inLevel:
			// Iterate through each char input by the player this frame
			foreach (char cIt in Input.inputString) {
				// Shift cIt to UPPERCASE
				c = System.Char.ToUpperInvariant(cIt);
				// Check to see if it's an uppercase letter
				if (upperCase.Contains(c)) { // Any uppercase letter
					// Find an available Letter in bigLetters with this char
					lett = FindNextLetterByChar(c);
					// If a Letter was returned
					if (lett != null) {
						// ... then add this char to the testWord and move the
						// returned big Letter to bigLettersActive
						testWord += c.ToString();
						// Move it from the inactive to the active List<>
						bigLettersActive.Add(lett);
						bigLetters.Remove(lett);
						lett.color = bigColorSelected; // Make it the active color
						ArrangeBigLetters(); // Rearrange the big Letters
					}
				}
				if (c == '\b') { // Backspace
					// Remove the last Letter in bigLettersActive
					if (bigLettersActive.Count == 0) return;
					if (testWord.Length > 1) {
						// Clear the last char of testWord
						testWord = testWord.Substring(0,testWord.Length-1);
					} else {
						testWord = "";
					}
					lett = bigLettersActive[bigLettersActive.Count-1];
					// Move it from the active to the inactive List<>
					bigLettersActive.Remove(lett);
					bigLetters.Add (lett);
					lett.color = bigColorDim; // Make it the inactive color
					ArrangeBigLetters(); // Rearrange the big Letters
				}
				if (c == '\n' || c == '\r') { // Return/Enter
					// Test the testWord against the words in WordLevel
					StartCoroutine( CheckWord() );
				}
				if (c == ' ') { // Space
					// Shuffle the bigLetters
					bigLetters = ShuffleLetters(bigLetters);
					ArrangeBigLetters();
				}
			}
			break;
		}
	}
	// This finds an available Letter with the char c in bigLetters.
	// If there isn't one available, it returns null.
	Letter FindNextLetterByChar(char c) {
		// Search through each Letter in bigLetters
		foreach (Letter l in bigLetters) {
			// If one has the same char as c
			if (l.c == c) {
				// ...then return it
				return(l);
			}
		}
		// Otherwise, return null
		return( null );
	}
	public IEnumerator CheckWord() {
		// Test testWord against the level.subWords
		string subWord;
		bool foundTestWord = false;
		// Create a List<int> to hold the indices of other subWords that are
		// contained within testWord
		List<int> containedWords = new List<int>();
		// Iterate through each word in currLevel.subWords
		for (int i=0; i<currLevel.subWords.Count; i++) {
			// If the ith Wyrd on screen has already been found
			if (wyrds[i].found) {
				// ...then continue & skip the rest of this iteration
				continue;
				// This works because the Wyrds on screen and the words in the
				// subWords List<> are in the same order
			}
			subWord = currLevel.subWords[i];
			// if this subWord is the testWord
			if (string.Equals(testWord, subWord)) {
				// ...then highlight the subWord
				HighlightWyrd(i);
				Score( wyrds[i], 1 ); // Score the testWord
				foundTestWord = true;
			} else if (testWord.Contains(subWord)) {
				// ^else if testWord contains this subWord (e.g., SAND contains AND)
				// ...then add it to the list of containedWords
				containedWords.Add(i);
			}
		}
		// If the test word was found in subWords
		if (foundTestWord) {
			// ...then highlight the other words contained in testWord
			int numContained = containedWords.Count;
			int ndx;
			// Highlight the words in reverse order
			for (int i=0; i<containedWords.Count; i++) {

				// yield for a bit before highlighting each word
				yield return( new WaitForSeconds(scoreComboDelay) );

				ndx = numContained-i-1;
				HighlightWyrd( containedWords[ndx] );
				Score( wyrds[ containedWords[ndx] ], i+2 ); // Score other words
				// The second parameter (i+2) is the # of this word in the combo
			}
		}
		// Clear the active big Letters regardless of whether testWord was valid
		ClearBigLettersActive();
	}
	// Highlight a Wyrd
	void HighlightWyrd(int ndx) {
		// Activate the subWord
		wyrds[ndx].found = true; // Let it know it's been found
		// Lighten its color
		wyrds[ndx].color = (wyrds[ndx].color+Color.white)/2f;
		wyrds[ndx].visible = true; // Make its 3D Text visible
	}
	// Remove all the Letters from bigLettersActive
	void ClearBigLettersActive() {
		testWord = ""; // Clear the testWord
		foreach (Letter l in bigLettersActive) {
			bigLetters.Add(l); // Add each Letter to bigLetters
			l.color = bigColorDim; // Set it to the inactive color
		}
		bigLettersActive.Clear(); // Clear the List<>
		ArrangeBigLetters(); // Rearrange the Letters on screen
	}
	// Add to the score for this word
	// int combo is the number of this word in a combo
	void Score(Wyrd wyrd, int combo) {
		// Get the position of the first Letter in the wyrd
		Vector3 pt = wyrd.letters[0].transform.position;
		// Create a List<> of Bezier points for the FloatingScore
		List<Vector3> pts = new List<Vector3>();
		// Convert the pt to a ViewportPoint. ViewportPoints range from 0 to 1
		// across the screen and are used for GUI coordinates
		pt = Camera.main.WorldToViewportPoint(pt);
		pt.z = 0;
		// Make pt the first Bezier point
		pts.Add(pt);
		// Add a second Bezier point
		pts.Add( scoreMidPoint );
		// Make the Scoreboard the last Bezier point
		pts.Add(Scoreboard.S.transform.position);
		// Set the value of the Floating Score
		int value = wyrd.letters.Count * combo;
		FloatingScore fs = Scoreboard.S.CreateFloatingScore(value, pts);
		fs.timeDuration = 2f;
		fs.fontSizes = scoreFontSizes;
		// Double the InOut Easing effect
		fs.easingCurve = Easing.InOut+Easing.InOut;
		// Make the text of the FloatingScore something like "3 x 2"
		string txt = wyrd.letters.Count.ToString();
		if (combo > 1) {
			txt += " x "+combo;
		}
		fs.guiText.text = txt;
	}
}
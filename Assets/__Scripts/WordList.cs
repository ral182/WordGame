using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class WordList : MonoBehaviour {
	public static WordList S;
	public TextAsset wordListText;
	public int numToParseBeforeYield = 10000;
	public int wordLengthMin = 3;
	public int wordLengthMax = 7;
	public bool ________________;
	public int currLine = 0;
	public int totalLines;
	public int longWordCount;
	public int wordCount;
	// Some variables below are private to keep them from appearing in the
	// Inspector. Because these variables will be so long, it can drastically
	// slow playback if the Inspector is trying to display them. Private
	// variables are restricted so that only this instance of the WordList
	// class can see them.
	private string[] lines; // 1
	private List<string> longWords;
	private List<string> words;
	void Awake() {
		S = this; // Set up the singleton
	}

	public void Init() { // This line replaces "void Start()"
		// Split the text of wordListText on line feeds, which creates a large,
		// populated string[] with all the words from the list
		lines = wordListText.text.Split('\n');
		totalLines = lines.Length;
		// This starts the coroutine ParseLines(). Coroutines can be paused in
		// the middle to allow other code to execute.
		StartCoroutine( ParseLines() ); // 2
	}
	// All coroutines have IEnumerator as their return type.
	public IEnumerator ParseLines() { // 3
		string word;
		// Init the Lists to hold the longest words and all valid words
		longWords = new List<string>();
		words = new List<string>();
		for (currLine = 0; currLine < totalLines; currLine++) {
			word = lines[currLine];
			// If the word is as long as wordLengthMax
			if (word.Length == wordLengthMax) {
				// ...then store it in longWords
				longWords.Add(word);
			}
			// If it's between wordLengthMin and wordLengthMax in length
			if ( word.Length>=wordLengthMin && word.Length<=wordLengthMax ) {
				// ...then add it to the list of all valid words
				words.Add(word);
			}
			// Determine whether the coroutine should yield
			// This uses a modulus (%) function to yield every 10,000th record
			// (or whatever you have numToParseBeforeYield set to)
			if (currLine % numToParseBeforeYield == 0) {
				// Count the words in each list to show that the parsing is
				// progressing
				longWordCount = longWords.Count;
				wordCount = words.Count;
				// This yields execution until the next frame
				yield return null; // 4
				// The yield will cause the execution of this method to wait
				// here while other code executes and then continue from this
				// point.
			}
		}
		// Send a message to this gameObject to let it know the parse is done
		gameObject.SendMessage("WordListParseComplete");
	}
	// These methods allow other classes to access the private List<string>s
	public List<string> GetWords() {
		return(words);
	}
	public string GetWord(int ndx) {
		return( words[ndx] );
	}
	public List<string> GetLongWords() {
		return( longWords );
	}
	public string GetLongWord(int ndx) {
		return( longWords[ndx] );
	}
}
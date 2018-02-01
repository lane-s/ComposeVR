using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using VRTK;

public class BrowserColumnEventArgs : EventArgs {
    public BrowserColumnEventArgs(BrowserColumn b, int selectionIndex, int pageChange) {
        col = b;
        selIndex = selectionIndex;
        pgChange = pageChange;
    }

    private BrowserColumn col;
    private int selIndex;
    private int pgChange;

    public BrowserColumn browserColumn{
        get { return col; }
    }

    public int selectionIndex {
        get { return selIndex;  }
    }

    public int pageChange {
        get { return pgChange; }
    }
}

public class BrowserColumn : CommandReceiver {

    public Transform resultButtonPrefab;
    public float resultSpacing = 0;
    public float resultStartOffset = 80;

    public event EventHandler<BrowserColumnEventArgs> ItemSelected;
    public event EventHandler<BrowserColumnEventArgs> PageChange;

    private List<Button> resultButtons;

    private UnityAction buttonPressed;

    private Button upArrow;
    private Button downArrow;

    private int currentPage = 0;
    private int numPages = 1;
    

    //TODO Call event PageChange when PageScrollBar changes page
    //TODO Call event PageChange when up/down button is pushed
    //TODO Display list of results when UpdateColumn is called
    //TODO Highlight result that is pointed at
    //TODO When result is clicked, call ItemSelected event with scrollPosition + resultIndex

    private void Awake() {
        resultButtons = new List<Button>();
        Register("browser/" + gameObject.name);

        upArrow = transform.Find("UpArrow").GetComponent<Button>();
        upArrow.onClick.AddListener(UpArrowClicked);

        downArrow = transform.Find("DownArrow").GetComponent<Button>();
        downArrow.onClick.AddListener(DownArrowClicked);
    }

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void BrowserColumnChanged(string[] args) {
        string[] results = { };

        if (args.Length > 2) {
            results = new string[args.Length - 2];
            for(int i = 2; i < args.Length; i++) {
                results[i - 2] = args[i];
            }
        }

        OnBrowserColumnChanged(int.Parse(args[0]), int.Parse(args[1]), results);
    }

    private void OnBrowserColumnChanged(int resultsPerPage, int totalResults, string[] results) {

        numPages = (totalResults + resultsPerPage - 1) / resultsPerPage;
        float buttonHeight = resultButtonPrefab.GetComponent<RectTransform>().localScale.y;

        //Resize canvas
        //Vector3 newScale = GetComponent<RectTransform>().localScale;
        //newScale.y = (buttonHeight + resultSpacing) * results.Length - resultSpacing;
        //GetComponent<RectTransform>().localScale = newScale;

        //Remove extra buttons
        while(resultButtons.Count > resultsPerPage) {
            Destroy(resultButtons[resultButtons.Count - 1].gameObject);
            resultButtons.RemoveAt(resultButtons.Count - 1);
        }

        //Add buttons as needed
        Vector3 buttonPosition = new Vector3(0, (buttonHeight + resultSpacing)*resultButtons.Count - resultStartOffset, 0);

        while(resultButtons.Count < resultsPerPage) {
            Transform newButton = Instantiate(resultButtonPrefab) as Transform;
            newButton.SetParent(transform);
            newButton.localPosition = Vector3.zero;
            newButton.localRotation = Quaternion.Euler(0, 0, 0);

            Button nb = newButton.GetComponent<Button>();
            int buttonIndex = resultButtons.Count;

            //Set up pressed callback with index
            nb.onClick.AddListener(
                () => {OnItemSelect(buttonIndex);}
            );

            //Position button
            RectTransform t = nb.GetComponent<RectTransform>();
            t.position = Vector3.zero;
            t.rotation = Quaternion.Euler(0, 0, 0);

            t.localScale = Vector3.one;
            t.localEulerAngles = Vector3.zero;
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.Euler(0, 0, 0);
            t.anchoredPosition = buttonPosition;

            buttonPosition += Vector3.down * t.rect.height + Vector3.down * resultSpacing;

            resultButtons.Add(nb);
        }


        //Label each button with result name
        for(int i = 0; i < results.Length; i++) {
            resultButtons[i].gameObject.SetActive(true);
            Text buttonText = resultButtons[i].GetComponentInChildren<Text>();
            buttonText.text = results[i];
        }

        //Deactivate buttons not needed to display the results
        for(int i = results.Length; i < resultButtons.Count; i++) {
            resultButtons[i].gameObject.SetActive(false);
        }

        UpdateArrowVisiblity();
    }

    private void OnItemSelect(int itemIndex) {
        Debug.Log("Item " + itemIndex + " selected!");
        if(ItemSelected != null) {
            BrowserColumnEventArgs e = new BrowserColumnEventArgs(this, itemIndex, 0);
            ItemSelected(this, e);
        }
    }

    private void UpArrowClicked() {
        OnPageChange(-1);
    }

    private void DownArrowClicked() {
        OnPageChange(1);
    }

    private void UpdateArrowVisiblity() {
        if (currentPage == 0) {
            upArrow.gameObject.SetActive(false);
        }
        else {
            upArrow.gameObject.SetActive(true);
        }

        if (currentPage >= numPages - 1) {
            currentPage = numPages - 1;
            downArrow.gameObject.SetActive(false);
        }
        else {
            downArrow.gameObject.SetActive(true);
        }
    }

    public void setPage(int pg) {
        currentPage = pg;
        UpdateArrowVisiblity();
    }

    private void OnPageChange(int pageChange) {
        setPage(currentPage + pageChange);

        if(PageChange != null) {
            BrowserColumnEventArgs e = new BrowserColumnEventArgs(this, -1, pageChange);
            PageChange(this, e);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class DeviceBrowser : CommandReceiver {

    private BrowserColumn resultsColumn;
    private List<BrowserColumn> filterColumns;

    void Awake() {
        Register("browser");
    }

    // Use this for initialization
    void Start () {
        filterColumns = new List<BrowserColumn>();

        foreach (BrowserColumn c in GetComponentsInChildren<BrowserColumn>()) {
            c.ItemSelected += OnItemSelected;
            c.PageChange += OnPageChanged;

            if (c.name.Equals("Results")) {
                resultsColumn = c;
            }
            else {
                filterColumns.Add(c);
            }

            c.gameObject.SetActive(false);
        }
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    /// <summary>
    /// Opens the browser on supplied module
    /// </summary>
    /// <param name="moduleID"> The module that the browser needs to browse for</param>
    /// <param name="browserAnchor"> The transform where the browser should display</param>
    public void openBrowser(string moduleID, string contentType) {
        DAWCommand.closeBrowser(getClient());
        DAWCommand.openBrowser(getClient(), moduleID, contentType);

        resultsColumn.gameObject.SetActive(true);

        foreach(BrowserColumn c in filterColumns) {
            if (c.name.Equals("Tags") && contentType != "Presets"){
                continue;
            }

            c.gameObject.SetActive(true);
        }

        //Show canvas and set size based on total number of columns
    }

    public void closeBrowser() {
        resultsColumn.gameObject.SetActive(false);
        resultsColumn.resetColumn();

        foreach (BrowserColumn c in filterColumns) {
            c.gameObject.SetActive(false);
            c.resetColumn();
        }
    }


    public void OnPageChanged(object sender, BrowserColumnEventArgs e) {
        if (e.browserColumn.name.Equals("Results") && e.pageChange != 0) {
            DAWCommand.changeResultsPage(getClient(), e.pageChange);
        }
        else {
            Debug.Log(e.browserColumn.name);
            DAWCommand.changeFilterPage(getClient(), e.browserColumn.name, e.pageChange);
        }
    }

    public void OnItemSelected(object sender, BrowserColumnEventArgs e) {
        if (e.browserColumn.name.Equals("Results")) {
            DAWCommand.loadDevice(getClient(), e.selectionIndex, "");
            closeBrowser();
        }
        else {
            DAWCommand.selectFilterEntry(getClient(), e.browserColumn.name, e.selectionIndex);
        }
    }

}

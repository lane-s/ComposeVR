using System.Collections;
using System.Collections.Generic;

/// <summary>
/// A collection of commands which can be sent to the connected DAW
/// </summary>
public static class DAWCommand {

    /// <summary>
    /// Creates a new sound module (corresponding to a new track) on the DAW
    /// </summary>
    /// <param name="client"></param>
    /// <param name="senderID"> The id to give the sound module</param>
	public static void createSoundModule(TCPClient client, string senderID) {
        string command = CommandRouter.createCommand("app", "createSoundModule", senderID);
        client.send(command);
    }

    /// <summary>
    /// Opens a browser on a module
    /// </summary>
    /// <param name="client"></param>
    /// <param name="moduleID">The module to browse on</param>
    public static void openBrowser(TCPClient client, string moduleID, string contentType) {
        string command = CommandRouter.createCommand(moduleID, "openBrowser", contentType);
        client.send(command);
    }

    public static void closeBrowser(TCPClient client) {
        string command = CommandRouter.createCommand("browser", "closeBrowser", "");
        client.send(command);
    }

    public static void changeResultsPage(TCPClient client, int pageChange) {
        string command = CommandRouter.createCommand("browser", "changeResultsPage", pageChange.ToString());
        client.send(command);
    }

    public static void loadDevice(TCPClient client, int selectionIndex, string deviceName) {
        string command = CommandRouter.createCommand("browser", "loadDevice", selectionIndex.ToString(), deviceName);
        client.send(command);
    }

    public static void changeFilterPage(TCPClient client, string columnName, int pageChange) {
        string command = CommandRouter.createCommand("browser", "changeFilterPage", columnName, pageChange.ToString());
        client.send(command);
    }

    public static void selectFilterEntry(TCPClient client, string columnName, int selectionIndex) {
        string command = CommandRouter.createCommand("browser", "selectFilterEntry", columnName, selectionIndex.ToString());
        client.send(command);
    }

}

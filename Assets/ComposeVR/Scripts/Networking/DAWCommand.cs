using System.Collections;
using System.Collections.Generic;

public static class DAWCommand {

	public static void createSoundModule(TCPClient client, string senderID) {
        string command = CommandRouter.createCommand("app", "createSoundModule", senderID);
        client.send(command);
    }

    
    public static void requestBrowser(TCPClient client, string receiverID, int selectionIndex, int pageChange, bool cancel, string deviceName) {
        string command = CommandRouter.createCommand(receiverID, "requestBrowser", selectionIndex.ToString(), pageChange.ToString(), cancel.ToString(), deviceName);
        client.send(command);
    }


}

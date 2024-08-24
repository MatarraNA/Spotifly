using CommandTerminal;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CustomCommands 
{
    [RegisterCommand("connect", Help = "Connects to the given steam id", MaxArgCount = 1, MinArgCount = 1)]
    static void CommandVolume(CommandArg[] args)
    {
        if (args.Length != 1)
        {
            Terminal.Log(TerminalLogType.Error, "Error: Volume must include argument value between 0 and 1");
            return;
        }

        string steamid = args[0].String;

        // attempt connect
        NetworkController.Instance.StartClient(steamid);

        // after connecting, transition to gameplay UI
        MainUI.instance.StartCoroutine(MainUI.instance.ScreenTransitionCoro(MainUI.instance.MainScreen, MainUI.instance.GameplayScreen, 0.66f));
    }
}

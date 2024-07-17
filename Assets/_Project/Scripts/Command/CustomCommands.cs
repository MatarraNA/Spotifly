using CommandTerminal;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CustomCommands 
{
    [RegisterCommand("volume", Help = "Sets the volume between 0 and 1", MaxArgCount = 1, MinArgCount = 1)]
    static void CommandVolume(CommandArg[] args)
    {
        if (args.Length != 1)
        {
            Terminal.Log(TerminalLogType.Error, "Error: Volume must include argument value between 0 and 1");
            return;
        }

        float val = args[0].Float;
        PlayerPrefs.SetFloat("master_volume", Mathf.Clamp(val, 0f, 1f));
    }
}

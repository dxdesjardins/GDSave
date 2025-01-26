using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

public static class ExtensionsString
{
    public static string GetDaySuffix(int day) {
        if (day % 100 >= 11 && day % 100 <= 13)
            return day + "th";
        return (day % 10) switch {
            1 => day + "st",
            2 => day + "nd",
            3 => day + "rd",
            _ => day + "th",
        };
    }
}

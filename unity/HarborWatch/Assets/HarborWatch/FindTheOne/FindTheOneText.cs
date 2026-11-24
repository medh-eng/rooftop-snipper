using HarborWatch.Core;

namespace HarborWatch
{
    /// <summary>
    /// Dossier wording for the Unity build. Index meanings are shared with the
    /// browser build (game/game.js LABEL); the outfit words describe the 3D
    /// rig's jacket colours instead of the browser's photo sprites.
    /// </summary>
    public static class FindTheOneText
    {
        static readonly string[][] Labels =
        {
            new[] { "Mustard jacket", "Green jacket", "Pale beige jacket", "Charcoal jacket" },
            new[] { "No hat", "Red cap", "Blue cap", "Yellow cap", "White beanie", "Black beanie" },
            new[] { "Nothing", "Backpack", "Briefcase" },
            new[] { "Strolls slowly", "Normal pace", "In a hurry (jogging)" },
            new[] { "No habit", "Keeps checking phone", "Glances over shoulder", "Stops and loiters" },
            new[] { "Reacts to noise", "Doesn't flinch at noise" },
            new[] { "Not in the cell", "Answers the cell ping" }
        };

        public static string Clue(Clue c) { return Labels[(int)c.Attr][c.Value]; }

        public static string Icon(Attr a)
        {
            switch (a)
            {
                case Attr.Outfit: return "COAT";
                case Attr.Headwear: return "HAT";
                case Attr.Carry: return "BAG";
                case Attr.Pace: return "PACE";
                case Attr.Habit: return "HABIT";
                case Attr.Calm: return "NOISE";
                default: return "PING";
            }
        }

        public const string HowTo =
            "1  READ THE DOSSIER\nIntel arrives line by line. Tap a locked line to buy it now for 8 seconds.\n\n" +
            "2  WATCH THE CROWD\nDrag to look. LENS zooms in, but hold it too long and your glint warns the courier.\n\n" +
            "3  PROVOKE\nHORN: everyone stops and looks, except a trained courier and anyone in headphones.\n" +
            "PING: every Red Signal member touches an earpiece.\n\n" +
            "4  TAG\nTap a person to select. Hold TAG until the ring locks, then release. Passers-by block the shot.\n\n" +
            "5  BE BRAVE\nTagging before all intel arrives multiplies your score. A wrong tag spooks the courier: one more shot, 15 seconds.\n\n" +
            "6  ECHO (case 4 on)\nReplay the last 8 seconds of the crowd, once per case, for 6 seconds of your clock. Catch who did not flinch or who touched an ear. Drag and zoom while it plays.";
    }
}

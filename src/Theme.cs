namespace PomodoroGarden
{
    // Colours of the window chrome (everything around the scene). The scene, the
    // plants and the mode colours (red / green / blue) are the same in both themes.
    sealed class Theme
    {
        public int Bg, Bar, Frame, Text, Muted, Hint, Faint, Active;
        public int Button, ButtonHover, ButtonShine, ButtonDark;
        public bool DimScene;

        public static readonly Theme Classic = new Theme
        {
            Bg = Pal.Ink, Bar = Pal.Charcoal, Frame = Pal.Charcoal,
            Text = Pal.White, Muted = Pal.Slate, Hint = Pal.Silver, Faint = Pal.Charcoal, Active = Pal.Navy,
            Button = Pal.Button, ButtonHover = Pal.ButtonHover, ButtonShine = Pal.Slate, ButtonDark = Pal.Shadow,
            DimScene = false,
        };

        // Near-black chrome, softer text, and a dimmed view through the window.
        public static readonly Theme Dark = new Theme
        {
            Bg = unchecked((int)0xFF0B0C12), Bar = unchecked((int)0xFF14161F), Frame = unchecked((int)0xFF1C1F2B),
            Text = unchecked((int)0xFFD6DAE3), Muted = unchecked((int)0xFF4E5670), Hint = unchecked((int)0xFF8A94AB),
            Faint = unchecked((int)0xFF1C1F2B), Active = unchecked((int)0xFF1B2447),
            Button = unchecked((int)0xFF1E2130), ButtonHover = unchecked((int)0xFF2A2E42),
            ButtonShine = unchecked((int)0xFF363B52), ButtonDark = unchecked((int)0xFF050609),
            DimScene = true,
        };
    }
}

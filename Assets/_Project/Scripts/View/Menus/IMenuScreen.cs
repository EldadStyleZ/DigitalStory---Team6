namespace DigitalStory.View
{
    /// <summary>One screen of the menus, written onto the shared card.</summary>
    public interface IMenuScreen
    {
        /// <summary>Adds this screen's lines to an empty card.</summary>
        void Fill(MenuCard card);
    }
}

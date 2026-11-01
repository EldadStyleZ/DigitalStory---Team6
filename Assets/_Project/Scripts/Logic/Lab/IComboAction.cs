namespace DigitalStory.Logic
{
    /// <summary>What a finished combo does. Add a kind of combo by adding one of these.</summary>
    public interface IComboAction
    {
        /// <summary>False when there is no room for what it would do.</summary>
        bool HasRoom { get; }

        void Perform();
    }
}
